import { type Admission, ApiError, type CounterItemResponse } from '@hms/shared';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { BedDouble, Loader2, Send, Trash2 } from 'lucide-react';
import { useRef, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { Switch } from '@/components/ui/switch';
import { useToast } from '@/components/ui/toast-context';
import { useAuth } from '@/features/auth/AuthContext';
import { quantity, toNumber } from '@/features/pharmacy/common/format';
import { LookupInput, type LookupInputHandle } from '@/features/pharmacy/common/LookupInput';
import { PharmacyPage } from '@/features/pharmacy/shell/PharmacyPage';
import { cn } from '@/lib/utils';
import { admissionsApi, pharmacyApi } from '@/services/apiClient';

interface IndentLine {
  item: CounterItemResponse;
  quantity: string;
  instructions: string;
}

const input = 'h-9 w-full rounded-md border border-input bg-background px-2 text-sm outline-none focus:border-primary focus:ring-2 focus:ring-primary/20';
const caption = 'text-[11px] font-medium uppercase tracking-wide text-muted-foreground';

async function searchAdmissions(term: string): Promise<Admission[]> {
  return (await admissionsApi.getAdmissions({ search: term || undefined, status: 'Admitted', pageSize: 20 })).items;
}

/**
 * Raises an indent: the ward's list of what an admitted patient needs from the pharmacy.
 * Items are picked from the pharmacy's own catalog, so the pharmacy receives exact products
 * rather than handwriting, and the ward can see what is in stock before asking for it.
 */
export default function IndentEntryPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { toast } = useToast();
  const { hasPermission } = useAuth();
  const [params] = useSearchParams();
  const admissionId = params.get('admission');

  const [admission, setAdmission] = useState<Admission | null>(null);
  const [lines, setLines] = useState<IndentLine[]>([]);
  const [isUrgent, setIsUrgent] = useState(false);
  const [remarks, setRemarks] = useState('');
  const [error, setError] = useState<string | null>(null);
  const itemRef = useRef<LookupInputHandle>(null);
  const quantityRefs = useRef(new Map<string, HTMLInputElement>());

  // Opened from a patient's admission screen: the patient is already known.
  useQuery({
    queryKey: ['pharmacy', 'indent-entry', 'admission', admissionId],
    queryFn: async () => {
      const found = await admissionsApi.getAdmissionById(admissionId!);
      setAdmission(found);
      return found;
    },
    enabled: Boolean(admissionId),
  });

  const mutation = useMutation({
    mutationFn: () =>
      pharmacyApi.createIndent({
        admissionId: admission!.id,
        isUrgent,
        remarks: remarks.trim() || undefined,
        lines: lines.map((line) => ({ productId: line.item.productId, quantity: toNumber(line.quantity), instructions: line.instructions.trim() || undefined })),
      }),
    onSuccess: (indent) => {
      void queryClient.invalidateQueries({ queryKey: ['pharmacy', 'indents'] });
      toast({ title: `Indent ${indent.indentNo} sent to the pharmacy`, description: `${indent.patientName} · ${indent.lineCount} items`, variant: 'success' });
      // Ward staff go back to where they came from; the pharmacy goes to its queue.
      if (hasPermission('pharmacy.view')) navigate('/pharmacy/indents');
      else navigate(-1);
    },
    onError: (failure) => setError(failure instanceof ApiError ? (failure.validationErrors?.[0]?.message ?? failure.message) : 'Could not send the indent.'),
  });

  function addItem(item: CounterItemResponse) {
    setError(null);
    if (!lines.some((line) => line.item.productId === item.productId)) {
      setLines((current) => [...current, { item, quantity: '', instructions: '' }]);
    }
    setTimeout(() => quantityRefs.current.get(item.productId)?.focus(), 0);
  }

  function submit() {
    if (!admission) {
      setError('Choose the admitted patient.');
      return;
    }
    if (lines.length === 0 || lines.some((line) => toNumber(line.quantity) <= 0)) {
      setError('Add at least one item, with a quantity on every line.');
      return;
    }
    setError(null);
    mutation.mutate();
  }

  const update = (productId: string, patch: Partial<IndentLine>) => setLines((current) => current.map((line) => (line.item.productId === productId ? { ...line, ...patch } : line)));

  return (
    <PharmacyPage
      title="New indent"
      subtitle="Ask the pharmacy for an admitted patient's medicines."
      actions={
        <Button size="sm" onClick={submit} disabled={mutation.isPending}>
          {mutation.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Send className="h-4 w-4" />}
          Send to pharmacy
        </Button>
      }
    >
      <section className="grid gap-3 rounded-lg border border-border bg-background p-3 md:grid-cols-[minmax(0,2fr)_minmax(0,2fr)_auto]">
        <div className="flex flex-col gap-1">
          <span className={caption}>Admitted patient</span>
          {admission ? (
            <div className="flex items-center gap-3">
              <BedDouble className="h-5 w-5 shrink-0 text-primary" />
              <div className="min-w-0 flex-1">
                <p className="truncate font-medium text-foreground">{admission.patientName}</p>
                <p className="truncate text-xs text-muted-foreground">
                  {[admission.uhid, admission.admissionNumber, admission.wardName, admission.bedNumber, admission.consultantName].filter(Boolean).join(' · ')}
                </p>
              </div>
              {!admissionId && (
                <Button size="sm" variant="outline" onClick={() => setAdmission(null)}>
                  Change
                </Button>
              )}
            </div>
          ) : (
            <LookupInput<Admission>
              queryKey={['pharmacy', 'indent-entry', 'admissions']}
              search={searchAdmissions}
              getKey={(candidate) => candidate.id}
              onSelect={(next) => { setAdmission(next); itemRef.current?.focus(); }}
              ariaLabel="Admitted patient"
              placeholder="Patient name, UHID or admission no.…"
              minChars={0}
              autoFocus
              emptyText="No admitted patient found."
              renderItem={(candidate) => (
                <div className="flex items-baseline justify-between gap-3">
                  <span className="font-medium">{candidate.patientName}</span>
                  <span className="shrink-0 text-xs text-muted-foreground">
                    {[candidate.admissionNumber, candidate.wardName, candidate.bedNumber].filter(Boolean).join(' · ')}
                  </span>
                </div>
              )}
            />
          )}
        </div>
        <label className="flex flex-col gap-1">
          <span className={caption}>Note to the pharmacy</span>
          <input value={remarks} maxLength={500} onChange={(event) => setRemarks(event.target.value)} className={cn(input, 'h-10')} />
        </label>
        <label className="flex items-center gap-2 self-end pb-2 text-sm text-foreground">
          <Switch checked={isUrgent} onCheckedChange={setIsUrgent} aria-label="Urgent" />
          Urgent
        </label>
      </section>

      <LookupInput<CounterItemResponse>
        ref={itemRef}
        queryKey={['pharmacy', 'indent-entry', 'items']}
        search={(term) => pharmacyApi.searchCatalog(term)}
        getKey={(item) => item.productId}
        onSelect={addItem}
        ariaLabel="Add item"
        placeholder="Add a medicine: type brand or generic…"
        emptyText="No item found in the pharmacy's catalog."
        renderItem={(item) => (
          <div className="flex items-center justify-between gap-3">
            <span className="min-w-0">
              <span className="font-medium">{item.productName}</span>
              <span className="block truncate text-xs text-muted-foreground">{item.genericName ?? '—'}</span>
            </span>
            <span className={cn('shrink-0 text-xs tabular-nums', item.availableQuantity > 0 ? 'text-success' : 'text-destructive')}>
              {item.availableQuantity > 0 ? `${quantity(item.availableQuantity)} in stock` : 'Out of stock'}
            </span>
          </div>
        )}
      />

      <section className="overflow-hidden rounded-lg border border-border bg-background">
        <table className="w-full text-sm">
          <thead className="bg-sidebar-active text-left text-[11px] font-medium uppercase tracking-wide text-sidebar-active-foreground">
            <tr>
              <th className="w-10 px-3 py-2">#</th>
              <th className="px-3 py-2">Item</th>
              <th className="px-3 py-2 text-right">In stock</th>
              <th className="w-32 px-3 py-2 text-right">Quantity (units)</th>
              <th className="px-3 py-2">Instructions</th>
              <th className="w-10 px-3 py-2" />
            </tr>
          </thead>
          <tbody className="divide-y divide-border">
            {lines.length === 0 && (
              <tr>
                <td colSpan={6} className="px-3 py-12 text-center text-muted-foreground">
                  Nothing on the indent yet.
                </td>
              </tr>
            )}
            {lines.map((line, index) => (
              <tr key={line.item.productId}>
                <td className="px-3 py-2 text-muted-foreground">{index + 1}</td>
                <td className="px-3 py-2">
                  <div className="font-medium text-foreground">{line.item.productName}</div>
                  <div className="text-xs text-muted-foreground">{line.item.genericName ?? ''}</div>
                </td>
                <td className={cn('px-3 py-2 text-right tabular-nums', line.item.availableQuantity > 0 ? 'text-muted-foreground' : 'font-medium text-destructive')}>
                  {line.item.availableQuantity > 0 ? quantity(line.item.availableQuantity) : 'Out of stock'}
                </td>
                <td className="px-3 py-1.5">
                  <input
                    ref={(element) => {
                      if (element) quantityRefs.current.set(line.item.productId, element);
                      else quantityRefs.current.delete(line.item.productId);
                    }}
                    value={line.quantity}
                    inputMode="decimal"
                    aria-label={`Units of ${line.item.productName}`}
                    onChange={(event) => update(line.item.productId, { quantity: event.target.value })}
                    onKeyDown={(event) => event.key === 'Enter' && itemRef.current?.focus()}
                    className={cn(input, 'h-8 text-right tabular-nums')}
                  />
                </td>
                <td className="px-3 py-1.5">
                  <input value={line.instructions} maxLength={200} placeholder="1-0-1 after food" onChange={(event) => update(line.item.productId, { instructions: event.target.value })} className={cn(input, 'h-8')} />
                </td>
                <td className="px-2 py-2">
                  <button type="button" onClick={() => setLines((current) => current.filter((candidate) => candidate.item.productId !== line.item.productId))} aria-label={`Remove ${line.item.productName}`} className="rounded p-1 text-muted-foreground hover:bg-destructive/10 hover:text-destructive">
                    <Trash2 className="h-3.5 w-3.5" />
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </section>

      {error && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {error}
        </p>
      )}
    </PharmacyPage>
  );
}
