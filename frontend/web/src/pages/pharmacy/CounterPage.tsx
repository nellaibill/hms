import { ApiError, type CounterItemResponse, type CreateSaleRequest, type HeldBillResponse, type IndentResponse, type PrescriptionQueueItemResponse, type SalePaymentMethod, type SaleResponse } from '@hms/shared';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { AlertTriangle, BedDouble, ClipboardList, FilePlus2, Loader2, PauseCircle, Printer, Stethoscope, Store, Trash2, UserRound, X } from 'lucide-react';
import { useEffect, useMemo, useRef, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { useToast } from '@/components/ui/toast-context';
import { useAuth } from '@/features/auth/AuthContext';
import { expiryLabel, money, quantity, todayIso, toNumber } from '@/features/pharmacy/common/format';
import { REGISTER_SCHEDULES, SCHEDULE_BADGE } from '@/features/pharmacy/common/labels';
import { LookupInput, type LookupInputHandle } from '@/features/pharmacy/common/LookupInput';
import { useHotkeys } from '@/features/pharmacy/common/useHotkeys';
import { BillPrint } from '@/features/pharmacy/counter/BillPrint';
import { type CartLine, type CounterPatient, type HeldCart, packBreakdown, previewBill } from '@/features/pharmacy/counter/cart';
import { BillSavedDialog, HeldBillsDialog, SubstitutesDialog } from '@/features/pharmacy/counter/CounterDialogs';
import { PatientLookup } from '@/features/pharmacy/counter/PatientLookup';
import { loadCounterPatient } from '@/features/pharmacy/counter/patients';
import { KeyHint, PharmacyPage } from '@/features/pharmacy/shell/PharmacyPage';
import { StoreSelect, useStoresQuery } from '@/features/pharmacy/stores';
import { cn } from '@/lib/utils';
import { admissionsApi, pharmacyApi } from '@/services/apiClient';

const TENDERS: { method: SalePaymentMethod; label: string }[] = [
  { method: 'Cash', label: 'Cash' },
  { method: 'Upi', label: 'UPI' },
  { method: 'Card', label: 'Card' },
  { method: 'BankTransfer', label: 'Bank' },
];

const EMPTY_TENDER: Record<SalePaymentMethod, string> = { Cash: '', Upi: '', Card: '', BankTransfer: '' };

const cell = 'h-8 w-full rounded border border-input bg-background px-2 text-sm outline-none focus:border-primary focus:ring-2 focus:ring-primary/20';

/**
 * The billing counter. One screen makes a pharmacy bill from start to finish: find the
 * patient (or bill a walk-in), scan or type items, take the money, print. It is laid out to
 * be worked from the keyboard — the item box is where the cursor rests, a chosen item drops
 * into the bill with its quantity selected, and the function keys do the rest.
 */
export default function CounterPage() {
  const queryClient = useQueryClient();
  const { toast } = useToast();
  const { hasPermission } = useAuth();

  const [storeId, setStoreId] = useState('');
  const [patient, setPatient] = useState<CounterPatient | null>(null);
  const [customerName, setCustomerName] = useState('');
  const [customerPhone, setCustomerPhone] = useState('');
  const [doctorName, setDoctorName] = useState('');
  const [lines, setLines] = useState<CartLine[]>([]);
  const [billDiscount, setBillDiscount] = useState('');
  const [primary, setPrimary] = useState<SalePaymentMethod>('Cash');
  const [split, setSplit] = useState(false);
  const [tender, setTender] = useState(EMPTY_TENDER);
  const [reference, setReference] = useState('');
  const [onCredit, setOnCredit] = useState(false);
  const [toAdmission, setToAdmission] = useState(false);
  const [heldBillId, setHeldBillId] = useState<string | undefined>();
  const [error, setError] = useState<string | null>(null);

  // A bill started from the prescription queue or the indent queue arrives with these.
  const [params, setParams] = useSearchParams();
  const [prescription, setPrescription] = useState<PrescriptionQueueItemResponse | null>(null);
  const [rxMatches, setRxMatches] = useState<(CounterItemResponse | null)[]>([]);
  const [indent, setIndent] = useState<IndentResponse | null>(null);

  const [savedBill, setSavedBill] = useState<SaleResponse | null>(null);
  const [printBill, setPrintBill] = useState<SaleResponse | null>(null);
  const [showHeld, setShowHeld] = useState(false);
  const [substituteFor, setSubstituteFor] = useState<CartLine | null>(null);

  const itemRef = useRef<LookupInputHandle>(null);
  const patientRef = useRef<LookupInputHandle>(null);
  const receivedRef = useRef<HTMLInputElement>(null);
  const quantityRefs = useRef(new Map<string, HTMLInputElement>());

  const { data: stores } = useStoresQuery();
  const store = stores?.find((candidate) => (storeId ? candidate.id === storeId : candidate.isDefault));
  const { data: settings } = useQuery({ queryKey: ['pharmacy', 'settings'], queryFn: () => pharmacyApi.getSettings(), staleTime: 300_000 });
  const { data: heldBills } = useQuery({
    queryKey: ['pharmacy', 'counter', 'held', storeId],
    queryFn: () => pharmacyApi.getHeldBills(storeId || undefined),
  });

  // A patient who is admitted right now can have the bill put on their admission.
  const { data: admission } = useQuery({
    queryKey: ['pharmacy', 'counter', 'admission', patient?.id],
    queryFn: async () => (await admissionsApi.getAdmissions({ patientId: patient!.id, status: 'Admitted', pageSize: 1 })).items[0] ?? null,
    enabled: Boolean(patient),
  });

  const discountPercent = Math.min(Math.max(toNumber(billDiscount), 0), 100);
  const bill = useMemo(() => previewBill(lines, discountPercent), [lines, discountPercent]);

  // What was tendered, as the server will read it. With a single tender, a blank amount
  // means "exactly the bill" — the usual case, and one keystroke fewer.
  const payments = useMemo(() => {
    if (split) {
      return TENDERS.map(({ method }) => ({ method, amount: toNumber(tender[method]) })).filter((payment) => payment.amount > 0);
    }
    const typed = tender[primary].trim();
    const deferred = onCredit || toAdmission;
    const amount = typed === '' ? (deferred ? 0 : bill.net) : toNumber(typed);
    return amount > 0 ? [{ method: primary, amount }] : [];
  }, [split, tender, primary, bill.net, onCredit, toAdmission]);

  const nonCash = payments.filter((payment) => payment.method !== 'Cash').reduce((sum, payment) => sum + payment.amount, 0);
  const cash = payments.filter((payment) => payment.method === 'Cash').reduce((sum, payment) => sum + payment.amount, 0);
  const change = Math.max(cash - Math.max(bill.net - nonCash, 0), 0);
  const due = Math.max(bill.net - nonCash - cash, 0);

  const registerLine = lines.find((line) => REGISTER_SCHEDULES.has(line.item.schedule));
  const billName = patient?.name ?? customerName.trim();
  const needsDetails = Boolean(registerLine) && (!billName || !doctorName.trim());

  useEffect(() => {
    // An admitted patient's bill is credited to the admission unless the cashier says otherwise.
    setToAdmission(Boolean(admission));
    if (admission && !doctorName) setDoctorName(admission.consultantName);
    // Only when the admission lookup settles, not on every keystroke in the doctor box.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [admission]);

  const rxId = params.get('rx');
  const rxDate = params.get('date') ?? todayIso();
  const indentId = params.get('indent');

  // From the prescription queue: the patient and doctor are set, and each prescribed drug is
  // looked up in the catalog so the likeliest item is one click from the bill. The doctor
  // wrote free text, so the match is offered, never added on its own.
  useEffect(() => {
    if (!rxId) return;
    let cancelled = false;
    void (async () => {
      try {
        const queue = await pharmacyApi.getPrescriptionQueue(rxDate, rxDate);
        const found = queue.find((item) => item.consultationId === rxId);
        if (!found || cancelled) {
          if (!cancelled) setError('That prescription could not be found.');
          return;
        }
        const [loadedPatient, matches] = await Promise.all([
          loadCounterPatient(found.patientId),
          Promise.all(found.drugs.map((drug) => pharmacyApi.searchCounterItems(drug.drugName, storeId || undefined, 5).then((items) => items[0] ?? null).catch(() => null))),
        ]);
        if (cancelled) return;
        setPatient(loadedPatient);
        setDoctorName(found.doctorName);
        setPrescription(found);
        setRxMatches(matches);
      } catch {
        if (!cancelled) setError('That prescription could not be loaded.');
      }
    })();
    return () => {
      cancelled = true;
    };
    // Loaded once per prescription; changing store afterwards does not re-match.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [rxId, rxDate]);

  // From the indent queue: the patient, and every item still to be issued at its pending
  // quantity. These are exact catalog items, so they go straight onto the bill.
  useEffect(() => {
    if (!indentId) return;
    let cancelled = false;
    void (async () => {
      try {
        const found = await pharmacyApi.getIndentById(indentId);
        const pending = found.lines.filter((line) => line.pendingQuantity > 0);
        const [loadedPatient, items] = await Promise.all([loadCounterPatient(found.patientId), pharmacyApi.lookupCounterItems(pending.map((line) => line.productId), found.storeId)]);
        if (cancelled) return;
        const byId = new Map(items.map((item) => [item.productId, item]));
        setPatient(loadedPatient);
        setDoctorName(found.doctorName ?? '');
        setIndent(found);
        setLines(
          pending.flatMap((line) => {
            const item = byId.get(line.productId);
            return item ? [{ key: crypto.randomUUID(), item, batchId: '', quantity: String(line.pendingQuantity), discount: '' }] : [];
          }),
        );
      } catch {
        if (!cancelled) setError('That indent could not be loaded.');
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [indentId]);

  function addPrescribed(index: number) {
    const item = rxMatches[index];
    const drug = prescription?.drugs[index];
    if (!item || !drug) return;
    if (lines.some((line) => line.item.productId === item.productId)) return;
    const units = drug.suggestedQuantity ?? (item.unitsPerPack > 1 ? item.unitsPerPack : 1);
    setLines((current) => [...current, { key: crypto.randomUUID(), item, batchId: '', quantity: String(units), discount: '' }]);
  }

  function resetBill() {
    setPrescription(null);
    setRxMatches([]);
    setIndent(null);
    if (params.has('rx') || params.has('indent')) setParams({}, { replace: true });
    setPatient(null);
    setCustomerName('');
    setCustomerPhone('');
    setDoctorName('');
    setLines([]);
    setBillDiscount('');
    setPrimary('Cash');
    setSplit(false);
    setTender(EMPTY_TENDER);
    setReference('');
    setOnCredit(false);
    setToAdmission(false);
    setHeldBillId(undefined);
    setError(null);
    setTimeout(() => itemRef.current?.focus(), 0);
  }

  function focusQuantity(key: string) {
    setTimeout(() => {
      const input = quantityRefs.current.get(key);
      input?.focus();
      input?.select();
    }, 0);
  }

  function addItem(item: CounterItemResponse) {
    setError(null);
    const existing = lines.find((line) => line.item.productId === item.productId && !line.batchId);
    if (existing) {
      focusQuantity(existing.key);
      return;
    }

    const line: CartLine = {
      key: crypto.randomUUID(),
      item,
      batchId: '',
      // A strip is the usual sale; the quantity is left selected, so typing replaces it.
      quantity: String(item.unitsPerPack > 1 ? item.unitsPerPack : 1),
      discount: '',
    };
    setLines((current) => [...current, line]);
    focusQuantity(line.key);
  }

  function updateLine(key: string, patch: Partial<CartLine>) {
    setLines((current) => current.map((line) => (line.key === key ? { ...line, ...patch } : line)));
  }

  function removeLine(key: string) {
    setLines((current) => current.filter((line) => line.key !== key));
    itemRef.current?.focus();
  }

  function handleQuantityKey(event: React.KeyboardEvent<HTMLInputElement>, index: number) {
    if (event.key === 'Enter') {
      event.preventDefault();
      itemRef.current?.focus();
    } else if (event.key === 'ArrowDown' && lines[index + 1]) {
      event.preventDefault();
      focusQuantity(lines[index + 1].key);
    } else if (event.key === 'ArrowUp' && lines[index - 1]) {
      event.preventDefault();
      focusQuantity(lines[index - 1].key);
    }
  }

  const saleMutation = useMutation({
    mutationFn: (request: CreateSaleRequest) => pharmacyApi.createSale(request),
    onSuccess: (sale) => {
      void queryClient.invalidateQueries({ queryKey: ['pharmacy'] });
      setSavedBill(sale);
      setPrintBill(sale);
      resetBill();
    },
    onError: (failure) => setError(failure instanceof ApiError ? failure.message : 'Could not save the bill.'),
  });

  function collect() {
    if (saleMutation.isPending || savedBill) return;

    if (lines.length === 0) {
      setError('Add an item to the bill first.');
      itemRef.current?.focus();
      return;
    }
    if (bill.hasShortLine) {
      setError('One or more items are short of stock. Reduce the quantity, choose a substitute, or remove the line.');
      return;
    }
    if (needsDetails) {
      setError(`${registerLine!.item.productName} is a scheduled drug: enter the patient's name and the doctor.`);
      return;
    }
    if (due > 0 && !onCredit && !toAdmission) {
      setError(`₹${money(due)} short. Enter the full amount, or give credit to a registered patient.`);
      receivedRef.current?.focus();
      return;
    }
    setError(null);

    saleMutation.mutate({
      storeId: storeId || undefined,
      patientId: patient?.id,
      customerName: patient ? undefined : customerName.trim() || undefined,
      customerPhone: customerPhone.trim() || undefined,
      doctorName: doctorName.trim() || undefined,
      admissionId: toAdmission ? admission?.id : undefined,
      prescriptionId: prescription?.consultationId,
      indentId: indent?.id,
      discountPercent,
      lines: lines.map((line, index) => ({
        productId: line.item.productId,
        productBatchId: line.batchId || undefined,
        quantity: bill.lines[index].quantity,
        discountPercent: line.discount.trim() === '' ? undefined : Math.min(Math.max(toNumber(line.discount), 0), 100),
      })),
      payments: payments.map((payment) => ({ ...payment, referenceNo: payment.method === 'Cash' ? undefined : reference.trim() || undefined })),
      allowCredit: onCredit,
      chargeToAdmission: toAdmission && Boolean(admission),
      heldBillId,
    });
  }

  const holdMutation = useMutation({
    mutationFn: () => {
      const cart: HeldCart = {
        version: 1,
        patient,
        customerName,
        customerPhone,
        doctorName,
        admissionId: admission?.id ?? '',
        discount: billDiscount,
        lines: lines.map((line) => ({ productId: line.item.productId, batchId: line.batchId, quantity: line.quantity, discount: line.discount })),
      };
      return pharmacyApi.holdBill({ storeId: storeId || undefined, label: billName || 'Walk-in customer', itemCount: lines.length, amount: bill.net, payload: JSON.stringify(cart) });
    },
    onSuccess: async (held) => {
      // A recalled bill held again replaces the copy it came from.
      if (heldBillId) await pharmacyApi.discardHeldBill(heldBillId).catch(() => undefined);
      void queryClient.invalidateQueries({ queryKey: ['pharmacy', 'counter', 'held'] });
      toast({ title: 'Bill held', description: `${held.label} · ₹${money(held.amount)}`, variant: 'success' });
      resetBill();
    },
    onError: (failure) => setError(failure instanceof ApiError ? failure.message : 'Could not hold the bill.'),
  });

  function hold() {
    if (lines.length === 0) {
      setShowHeld(true);
      return;
    }
    holdMutation.mutate();
  }

  async function recall(held: HeldBillResponse) {
    setShowHeld(false);
    try {
      const cart = JSON.parse(held.payload) as HeldCart;
      // Stock and prices may have moved while the bill was parked: read every item afresh.
      const items = await pharmacyApi.lookupCounterItems(cart.lines.map((line) => line.productId), held.storeId);
      const byId = new Map(items.map((item) => [item.productId, item]));

      resetBill();
      setStoreId(stores && stores.length > 1 ? held.storeId : '');
      setPatient(cart.patient);
      setCustomerName(cart.customerName);
      setCustomerPhone(cart.customerPhone);
      setDoctorName(cart.doctorName);
      setBillDiscount(cart.discount);
      setHeldBillId(held.id);
      setLines(
        cart.lines.flatMap((line) => {
          const item = byId.get(line.productId);
          if (!item) return [];
          const batchStillThere = item.batches.some((batch) => batch.productBatchId === line.batchId);
          return [{ key: crypto.randomUUID(), item, batchId: batchStillThere ? line.batchId : '', quantity: line.quantity, discount: line.discount }];
        }),
      );
    } catch {
      setError('That held bill could not be opened.');
    }
  }

  async function discard(held: HeldBillResponse) {
    await pharmacyApi.discardHeldBill(held.id).catch(() => undefined);
    void queryClient.invalidateQueries({ queryKey: ['pharmacy', 'counter', 'held'] });
  }

  function print(sale: SaleResponse) {
    setPrintBill(sale);
    // Let the bill render into the print target before the print dialog reads the page.
    setTimeout(() => window.print(), 100);
  }

  function chooseTender(method: SalePaymentMethod) {
    setPrimary(method);
    if (!split) setTender(EMPTY_TENDER);
    setTimeout(() => receivedRef.current?.focus(), 0);
  }

  const canBill = hasPermission('pharmacy.create');
  useHotkeys(
    {
      F2: () => itemRef.current?.focus(),
      F4: hold,
      F6: () => setShowHeld(true),
      F7: () => patientRef.current?.focus(),
      F8: () => receivedRef.current?.focus(),
      F9: () => printBill && print(printBill),
      F10: collect,
      'ctrl+Enter': collect,
    },
    !savedBill,
  );

  const heldCount = heldBills?.length ?? 0;

  return (
    <PharmacyPage
      title="Billing counter"
      subtitle={
        <span className="flex items-center gap-1.5">
          <Store className="h-3.5 w-3.5" />
          {store?.name ?? 'Pharmacy'} · {new Date().toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' })}
        </span>
      }
      flush
      actions={
        <>
          <Button size="sm" variant="outline" onClick={() => setShowHeld(true)}>
            <PauseCircle className="h-4 w-4" />
            Held bills{heldCount > 0 ? ` (${heldCount})` : ''}
          </Button>
          {printBill && (
            <Button size="sm" variant="outline" onClick={() => print(printBill)}>
              <Printer className="h-4 w-4" />
              Reprint {printBill.billNo}
            </Button>
          )}
          <Button size="sm" variant="outline" onClick={resetBill} disabled={lines.length === 0 && !patient && !customerName}>
            <FilePlus2 className="h-4 w-4" />
            New bill
          </Button>
        </>
      }
    >
      <div className="grid min-h-0 flex-1 grid-cols-1 gap-3 p-3 lg:h-[calc(100dvh-14.75rem)] lg:min-h-[440px] lg:grid-cols-[minmax(0,1fr)_320px]">
        {/* ---- Left: customer, item entry, lines ---- */}
        <div className="flex min-h-0 flex-col gap-3">
          <section className="rounded-lg border border-border bg-background p-3">
            {patient ? (
              <div className="flex flex-wrap items-center gap-3">
                <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-primary/10 text-sm font-semibold text-primary">
                  {patient.name
                    .split(' ')
                    .map((part) => part[0])
                    .slice(0, 2)
                    .join('')
                    .toUpperCase()}
                </span>
                <div className="min-w-0 flex-1">
                  <p className="truncate font-medium text-foreground">{patient.name}</p>
                  <p className="truncate text-xs text-muted-foreground">
                    {patient.uhid}
                    {patient.phone ? ` · ${patient.phone}` : ''}
                    {admission ? ` · Admitted ${admission.admissionNumber}${admission.wardName ? `, ${admission.wardName} ${admission.bedNumber}` : ''}` : ''}
                  </p>
                </div>
                <label className="flex w-56 flex-col gap-0.5">
                  <span className="text-[11px] font-medium uppercase tracking-wide text-muted-foreground">Doctor</span>
                  <input value={doctorName} onChange={(event) => setDoctorName(event.target.value)} className={cn(cell, needsDetails && !doctorName.trim() && 'border-destructive')} />
                </label>
                <Button size="sm" variant="outline" onClick={() => { setPatient(null); setToAdmission(false); setOnCredit(false); }}>
                  <X className="h-3.5 w-3.5" />
                  Walk-in
                </Button>
              </div>
            ) : (
              <div className="grid gap-2 md:grid-cols-[minmax(0,1.3fr)_minmax(0,1fr)_150px_minmax(0,1fr)]">
                <label className="flex flex-col gap-0.5">
                  <span className="text-[11px] font-medium uppercase tracking-wide text-muted-foreground">Registered patient</span>
                  <PatientLookup ref={patientRef} onSelect={(next) => { setPatient(next); setCustomerName(''); setCustomerPhone(''); itemRef.current?.focus(); }} />
                </label>
                <label className="flex flex-col gap-0.5">
                  <span className="text-[11px] font-medium uppercase tracking-wide text-muted-foreground">or walk-in name</span>
                  <input value={customerName} maxLength={150} placeholder="Cash customer" onChange={(event) => setCustomerName(event.target.value)} className={cn(cell, 'h-9', needsDetails && !billName && 'border-destructive')} />
                </label>
                <label className="flex flex-col gap-0.5">
                  <span className="text-[11px] font-medium uppercase tracking-wide text-muted-foreground">Phone</span>
                  <input value={customerPhone} maxLength={20} inputMode="tel" onChange={(event) => setCustomerPhone(event.target.value)} className={cn(cell, 'h-9')} />
                </label>
                <label className="flex flex-col gap-0.5">
                  <span className="text-[11px] font-medium uppercase tracking-wide text-muted-foreground">Doctor</span>
                  <input value={doctorName} maxLength={150} onChange={(event) => setDoctorName(event.target.value)} className={cn(cell, 'h-9', needsDetails && !doctorName.trim() && 'border-destructive')} />
                </label>
              </div>
            )}

            {(patient?.allergies.length || needsDetails || admission) && (
              <div className="mt-2 flex flex-wrap gap-1.5 text-xs">
                {admission && (
                  <span className="flex items-center gap-1 rounded bg-primary/10 px-2 py-0.5 text-primary">
                    <BedDouble className="h-3.5 w-3.5" />
                    Inpatient · {admission.consultantName}
                  </span>
                )}
                {patient && patient.allergies.length > 0 && (
                  <span className="flex items-center gap-1 rounded bg-destructive/10 px-2 py-0.5 text-destructive">
                    <AlertTriangle className="h-3.5 w-3.5" />
                    Allergy on record: {patient.allergies.join(', ')}
                  </span>
                )}
                {needsDetails && (
                  <span className="flex items-center gap-1 rounded bg-warning/15 px-2 py-0.5 text-warning">
                    <AlertTriangle className="h-3.5 w-3.5" />
                    {registerLine!.item.productName} goes in the {SCHEDULE_BADGE[registerLine!.item.schedule]?.label} register: patient name and doctor are required
                  </span>
                )}
              </div>
            )}
          </section>

          {indent && (
            <p className="flex items-center gap-2 rounded-lg border border-primary/30 bg-primary/5 px-3 py-2 text-sm text-foreground">
              <ClipboardList className="h-4 w-4 shrink-0 text-primary" />
              Issuing indent <span className="font-mono text-xs">{indent.indentNo}</span>
              <span className="text-muted-foreground">{[indent.location, indent.isUrgent ? 'urgent' : null].filter(Boolean).join(' · ')}</span>
            </p>
          )}

          {prescription && (
            <section className="rounded-lg border border-primary/30 bg-primary/5 p-3">
              <p className="flex items-center gap-2 text-sm font-medium text-foreground">
                <Stethoscope className="h-4 w-4 text-primary" />
                Prescription from {prescription.doctorName}
                <span className="font-normal text-muted-foreground">Check each match against what the doctor wrote before adding it.</span>
              </p>
              <ul className="mt-2 grid gap-1.5 text-sm md:grid-cols-2">
                {prescription.drugs.map((drug, index) => {
                  const match = rxMatches[index];
                  const added = match ? lines.some((line) => line.item.productId === match.productId) : false;
                  return (
                    <li key={index} className="flex items-center justify-between gap-2 rounded-md border border-border bg-background px-2.5 py-1.5">
                      <span className="min-w-0">
                        <span className="block truncate font-medium text-foreground">{drug.drugName}</span>
                        <span className="block truncate text-xs text-muted-foreground">
                          {[drug.dose, drug.frequency, drug.durationDays ? `${drug.durationDays} days` : null, drug.suggestedQuantity != null ? `${quantity(drug.suggestedQuantity)} units` : null].filter(Boolean).join(' · ') || 'No dose details'}
                        </span>
                        <span className={cn('block truncate text-xs', match ? (match.availableQuantity > 0 ? 'text-success' : 'text-destructive') : 'text-warning')}>
                          {match ? `→ ${match.productName} · ${match.availableQuantity > 0 ? `${quantity(match.availableQuantity)} in stock` : 'out of stock'}` : 'No match in the catalog — search for it below'}
                        </span>
                      </span>
                      {match && (
                        <Button size="sm" variant={added ? 'outline' : 'secondary'} className="h-7 shrink-0" disabled={added} onClick={() => addPrescribed(index)}>
                          {added ? 'Added' : 'Add'}
                        </Button>
                      )}
                    </li>
                  );
                })}
              </ul>
            </section>
          )}

          <div className="flex items-end gap-2">
            <LookupInput<CounterItemResponse>
              ref={itemRef}
              className="flex-1"
              queryKey={['pharmacy', 'counter', 'items', storeId]}
              search={(term) => pharmacyApi.searchCounterItems(term, storeId || undefined)}
              getKey={(item) => item.productId}
              onSelect={addItem}
              ariaLabel="Add item"
              placeholder="Scan barcode, or type brand, generic or code…"
              emptyText="No item found."
              autoFocus
              inputClassName="h-11 text-base"
              renderItem={(item) => (
                <div className="flex items-center justify-between gap-3">
                  <span className="min-w-0">
                    <span className="font-medium">{item.productName}</span>
                    {SCHEDULE_BADGE[item.schedule] && (
                      <span className={cn('ml-1.5 rounded px-1 py-px text-[10px] font-semibold', SCHEDULE_BADGE[item.schedule]!.className)}>{SCHEDULE_BADGE[item.schedule]!.label}</span>
                    )}
                    <span className="block truncate text-xs text-muted-foreground">
                      {item.genericName ?? '—'}
                      {item.rackLocation ? ` · rack ${item.rackLocation}` : ''}
                      {item.batches[0] ? ` · exp ${expiryLabel(item.batches[0].expiryDate)}` : ''}
                    </span>
                  </span>
                  <span className="shrink-0 text-right">
                    <span className="block tabular-nums">₹{money(item.unitPrice)}</span>
                    <span className={cn('block text-xs tabular-nums', item.availableQuantity > 0 ? 'text-success' : 'text-destructive')}>
                      {item.availableQuantity > 0 ? `${quantity(item.availableQuantity)} in stock` : 'Out of stock'}
                    </span>
                  </span>
                </div>
              )}
            />
            <StoreSelect id="counter-store" value={storeId} onValueChange={(next) => { setStoreId(next); setLines([]); }} label="Selling from" />
          </div>

          <section className="flex min-h-[200px] flex-1 flex-col overflow-hidden rounded-lg border border-border bg-background">
            <div className="flex-1 overflow-auto">
              {/* Fixed layout: every column but Item has a set width, so Item takes all the
                  room that is left instead of being squeezed by the inputs beside it. */}
              <table className="w-full table-fixed text-sm">
                <thead className="sticky top-0 z-10 bg-sidebar-active text-left text-[11px] font-medium uppercase tracking-wide text-sidebar-active-foreground">
                  <tr>
                    <th className="w-8 px-2 py-2">#</th>
                    <th className="px-2 py-2">Item</th>
                    <th className="w-44 px-2 py-2">Batch · expiry</th>
                    <th className="w-24 px-2 py-2 text-right">Qty</th>
                    <th className="w-[72px] px-2 py-2 text-right">MRP</th>
                    <th className="w-16 px-2 py-2 text-right">Disc %</th>
                    <th className="w-24 px-2 py-2 text-right">Amount</th>
                    <th className="w-8 px-2 py-2" />
                  </tr>
                </thead>
                <tbody className="divide-y divide-border">
                  {lines.length === 0 && (
                    <tr>
                      <td colSpan={8} className="px-3 py-14 text-center text-sm text-muted-foreground">
                        Scan or search an item to start the bill.
                      </td>
                    </tr>
                  )}
                  {lines.map((line, index) => {
                    const preview = bill.lines[index];
                    const short = preview.shortBy > 0;
                    const outOfStock = line.item.availableQuantity <= 0;
                    const badge = SCHEDULE_BADGE[line.item.schedule];
                    const breakdown = packBreakdown(preview.quantity, line.item.unitsPerPack);
                    return (
                      <tr key={line.key} className={cn(short ? 'bg-warning/10' : 'hover:bg-muted/30')}>
                        <td className="px-2 py-1.5 align-top text-muted-foreground">{index + 1}</td>
                        <td className="px-2 py-1.5 align-top">
                          <div className="flex items-center gap-1.5 font-medium text-foreground">
                            <span className="truncate" title={line.item.productName}>
                              {line.item.productName}
                            </span>
                            {badge && <span className={cn('shrink-0 rounded px-1 py-px text-[10px] font-semibold', badge.className)}>{badge.label}</span>}
                          </div>
                          <div className="truncate text-xs text-muted-foreground" title={line.item.genericName ?? undefined}>
                            {line.item.genericName ?? ''}
                            {line.item.rackLocation ? `${line.item.genericName ? ' · ' : ''}rack ${line.item.rackLocation}` : ''}
                            {line.item.unitsPerPack > 1 ? ` · strip of ${quantity(line.item.unitsPerPack)}` : ''}
                          </div>
                          {short && (
                            <div className="mt-0.5 flex items-center gap-2 text-xs font-medium text-warning">
                              {outOfStock ? 'Out of stock' : `Only ${quantity(preview.quantity - preview.shortBy)} in stock`}
                              <button type="button" onClick={() => setSubstituteFor(line)} className="rounded border border-warning/40 px-1.5 py-px hover:bg-warning/15">
                                Substitutes
                              </button>
                            </div>
                          )}
                        </td>
                        <td className="px-2 py-1.5 align-top">
                          <select
                            value={line.batchId}
                            onChange={(event) => updateLine(line.key, { batchId: event.target.value })}
                            aria-label={`Batch for ${line.item.productName}`}
                            disabled={line.item.batches.length === 0}
                            className={cn(cell, 'px-1 font-mono text-xs')}
                          >
                            <option value="">
                              {line.item.batches[0] ? `${line.item.batches[0].batchNo} · ${expiryLabel(line.item.batches[0].expiryDate)}${line.item.batches.length > 1 ? ' (auto)' : ''}` : 'No stock'}
                            </option>
                            {line.item.batches.length > 1 &&
                              line.item.batches.map((batch) => (
                                <option key={batch.productBatchId} value={batch.productBatchId}>
                                  {batch.batchNo} · {expiryLabel(batch.expiryDate)} · {quantity(batch.quantityOnHand)}
                                </option>
                              ))}
                          </select>
                          {preview.allocations.length > 1 && (
                            <div className="mt-0.5 text-[11px] text-muted-foreground">
                              {preview.allocations.map((allocation) => `${quantity(allocation.quantity)} × ${allocation.batch.batchNo}`).join(' + ')}
                            </div>
                          )}
                        </td>
                        <td className="px-2 py-1.5 align-top">
                          <input
                            ref={(element) => {
                              if (element) quantityRefs.current.set(line.key, element);
                              else quantityRefs.current.delete(line.key);
                            }}
                            value={line.quantity}
                            onChange={(event) => updateLine(line.key, { quantity: event.target.value })}
                            onKeyDown={(event) => handleQuantityKey(event, index)}
                            onFocus={(event) => event.target.select()}
                            aria-label={`Quantity of ${line.item.productName}`}
                            title="Units. Type 2s for two strips."
                            className={cn(cell, 'text-right tabular-nums', (short || preview.quantity <= 0) && 'border-warning')}
                          />
                          {breakdown && <div className="mt-0.5 text-right text-[11px] text-muted-foreground">{breakdown}</div>}
                        </td>
                        <td className="px-2 py-1.5 text-right align-top tabular-nums">{money(preview.unitPrice)}</td>
                        <td className="px-2 py-1.5 align-top">
                          <input
                            value={line.discount}
                            inputMode="decimal"
                            placeholder={discountPercent ? String(discountPercent) : '0'}
                            onChange={(event) => updateLine(line.key, { discount: event.target.value })}
                            aria-label={`Discount on ${line.item.productName}`}
                            className={cn(cell, 'text-right tabular-nums')}
                          />
                        </td>
                        <td className="px-2 py-1.5 text-right align-top font-medium tabular-nums">{money(preview.total)}</td>
                        <td className="px-1 py-1.5 align-top">
                          <button type="button" onClick={() => removeLine(line.key)} aria-label={`Remove ${line.item.productName}`} className="rounded p-1 text-muted-foreground hover:bg-destructive/10 hover:text-destructive">
                            <Trash2 className="h-3.5 w-3.5" />
                          </button>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
            <div className="flex flex-wrap gap-x-4 gap-y-1 border-t border-border bg-muted/30 px-3 py-1.5 text-xs text-muted-foreground">
              <KeyHint keys="F2">Item</KeyHint>
              <KeyHint keys="F7">Patient</KeyHint>
              <KeyHint keys="F4">Hold</KeyHint>
              <KeyHint keys="F6">Held bills</KeyHint>
              <KeyHint keys="F8">Payment</KeyHint>
              <KeyHint keys="F9">Reprint</KeyHint>
              <KeyHint keys="F10">Collect and print</KeyHint>
            </div>
          </section>
        </div>

        {/* ---- Right: totals and payment ---- */}
        <aside className="flex min-h-0 flex-col gap-3 overflow-y-auto rounded-lg border border-border bg-background p-3">
          <dl className="text-sm">
            <div className="flex justify-between py-0.5">
              <dt className="text-muted-foreground">Items</dt>
              <dd className="tabular-nums">{lines.length}</dd>
            </div>
            <div className="flex justify-between py-0.5">
              <dt className="text-muted-foreground">Subtotal</dt>
              <dd className="tabular-nums">{money(bill.gross)}</dd>
            </div>
            <div className="flex items-center justify-between py-0.5">
              <dt className="flex items-center gap-2 text-muted-foreground">
                Discount
                <input
                  value={billDiscount}
                  inputMode="decimal"
                  placeholder="0"
                  aria-label="Bill discount percent"
                  onChange={(event) => setBillDiscount(event.target.value)}
                  className="h-7 w-14 rounded border border-input bg-background px-1.5 text-right text-sm tabular-nums outline-none focus:border-primary"
                />
                %
              </dt>
              <dd className="tabular-nums">− {money(bill.discount)}</dd>
            </div>
            <div className="flex justify-between py-0.5">
              <dt className="text-muted-foreground">Round off</dt>
              <dd className="tabular-nums">{money(bill.roundOff)}</dd>
            </div>
            <div className="mt-2 flex items-baseline justify-between border-t border-border pt-2">
              <dt className="text-muted-foreground">To pay</dt>
              <dd className="text-3xl font-semibold tabular-nums text-foreground">₹{money(bill.net)}</dd>
            </div>
            <p className="text-right text-xs text-muted-foreground">Includes GST {money(bill.tax)}</p>
          </dl>

          <div>
            <div className="grid grid-cols-4 gap-1.5">
              {TENDERS.map(({ method, label }) => (
                <button
                  key={method}
                  type="button"
                  onClick={() => chooseTender(method)}
                  aria-pressed={!split && primary === method}
                  className={cn(
                    'rounded-md border px-2 py-1.5 text-sm font-medium transition-colors',
                    !split && primary === method ? 'border-primary bg-primary text-primary-foreground' : 'border-border text-muted-foreground hover:bg-accent hover:text-foreground',
                  )}
                >
                  {label}
                </button>
              ))}
            </div>

            {split ? (
              <div className="mt-2 grid grid-cols-2 gap-2">
                {TENDERS.map(({ method, label }, index) => (
                  <label key={method} className="flex flex-col gap-0.5 text-xs text-muted-foreground">
                    {label}
                    <input
                      ref={index === 0 ? receivedRef : undefined}
                      value={tender[method]}
                      inputMode="decimal"
                      onChange={(event) => setTender((current) => ({ ...current, [method]: event.target.value }))}
                      onKeyDown={(event) => event.key === 'Enter' && collect()}
                      className={cn(cell, 'text-right tabular-nums')}
                    />
                  </label>
                ))}
              </div>
            ) : (
              <label className="mt-2 flex items-center justify-between gap-3 text-sm">
                <span className="text-muted-foreground">Received</span>
                <input
                  ref={receivedRef}
                  value={tender[primary]}
                  inputMode="decimal"
                  placeholder={onCredit || toAdmission ? '0.00' : money(bill.net)}
                  aria-label="Amount received"
                  onChange={(event) => setTender({ ...EMPTY_TENDER, [primary]: event.target.value })}
                  onKeyDown={(event) => event.key === 'Enter' && collect()}
                  className="h-9 w-32 rounded border border-input bg-background px-2 text-right text-base tabular-nums outline-none focus:border-primary focus:ring-2 focus:ring-primary/20"
                />
              </label>
            )}

            {(split ? nonCash > 0 : primary !== 'Cash') && (
              <label className="mt-2 flex items-center justify-between gap-3 text-sm">
                <span className="text-muted-foreground">Reference no.</span>
                <input value={reference} maxLength={100} onChange={(event) => setReference(event.target.value)} className={cn(cell, 'w-40')} />
              </label>
            )}

            <div className="mt-2 flex justify-between text-sm">
              <span className="text-muted-foreground">Change</span>
              <span className={cn('tabular-nums', change > 0 && 'font-semibold text-success')}>{money(change)}</span>
            </div>
            {due > 0 && (
              <div className="flex justify-between text-sm">
                <span className="text-muted-foreground">{toAdmission ? 'To admission' : onCredit ? 'On credit' : 'Short by'}</span>
                <span className={cn('tabular-nums font-medium', toAdmission || onCredit ? 'text-foreground' : 'text-destructive')}>{money(due)}</span>
              </div>
            )}

            <div className="mt-2 flex flex-col gap-1 text-xs text-muted-foreground">
              <label className="flex items-center gap-2">
                <input type="checkbox" checked={split} onChange={(event) => { setSplit(event.target.checked); setTender(EMPTY_TENDER); }} />
                Split across payment modes
              </label>
              {admission && (
                <label className="flex items-center gap-2 text-foreground">
                  <input type="checkbox" checked={toAdmission} onChange={(event) => { setToAdmission(event.target.checked); if (event.target.checked) setOnCredit(false); }} />
                  IP credit: put the balance on {admission.admissionNumber}
                </label>
              )}
              {patient && !toAdmission && (
                <label className="flex items-center gap-2">
                  <input type="checkbox" checked={onCredit} onChange={(event) => setOnCredit(event.target.checked)} />
                  Give credit for the balance
                </label>
              )}
            </div>
          </div>

          {error && (
            <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
              {error}
            </p>
          )}

          <div className="mt-auto flex flex-col gap-2">
            <Button className="h-11 text-base" onClick={collect} disabled={!canBill || saleMutation.isPending || lines.length === 0}>
              {saleMutation.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Printer className="h-4 w-4" />}
              Collect and print
            </Button>
            <Button variant="outline" onClick={hold} disabled={holdMutation.isPending || lines.length === 0}>
              <PauseCircle className="h-4 w-4" />
              Hold bill
            </Button>
            {!patient && (
              <p className="flex items-center gap-1.5 text-xs text-muted-foreground">
                <UserRound className="h-3.5 w-3.5" />
                Walk-in sale: paid in full, no credit.
              </p>
            )}
          </div>
        </aside>
      </div>

      <SubstitutesDialog
        item={substituteFor?.item ?? null}
        storeId={storeId}
        onClose={() => setSubstituteFor(null)}
        onChoose={(substitute) => {
          if (substituteFor) updateLine(substituteFor.key, { item: substitute, batchId: '' });
          setSubstituteFor(null);
        }}
      />
      <HeldBillsDialog open={showHeld} bills={heldBills ?? []} onRecall={(held) => void recall(held)} onDiscard={(held) => void discard(held)} onClose={() => setShowHeld(false)} />
      <BillSavedDialog
        sale={savedBill}
        onPrint={() => {
          if (savedBill) print(savedBill);
          setSavedBill(null);
          setTimeout(() => itemRef.current?.focus(), 300);
        }}
        onNewBill={() => {
          setSavedBill(null);
          setTimeout(() => itemRef.current?.focus(), 0);
        }}
      />

      {printBill && (
        <div className="print-target hidden print:block">
          <BillPrint sale={printBill} settings={settings} />
        </div>
      )}
    </PharmacyPage>
  );
}
