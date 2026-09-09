import { ApiError, NetworkError, type Patient } from '@hms/shared';
import { ArrowLeft, FilePlus2, Loader2 } from 'lucide-react';
import { useEffect, useMemo, useRef, useState } from 'react';
import { Link, useBlocker } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import {
  BillingStep,
  InvoiceDetailCard,
  isConsultationEntryActive,
  LabDetailsCard,
  PatientPicker,
  RecordPaymentDialog,
  VoidInvoiceDialog,
  defaultBillingFormValues,
  describeBillingItem,
  emptyConsultation,
  useCreateInvoiceMutation,
  usePatientInvoicesQuery,
  useRecordPaymentMutation,
  useVoidInvoiceMutation,
  type Billing,
  type BillingFormValues,
  type BillingItem,
  type BillingStepHandle,
  type ConsultationBillingFormValues,
  type PaymentMethod,
} from '../../features/billing';
import { usePatientVisitsQuery } from '../../features/patients';

/**
 * Turns a thrown createInvoiceMutation error into what a receptionist actually needs to see —
 * the real server message (e.g. a specific validation failure) rather than a generic "please
 * try again" that gives no clue what went wrong or how to fix it. Mirrors the
 * ApiError/NetworkError pattern PatientRegistrationForm already uses (see
 * features/patients/apiErrorDisplay.ts) rather than a patients-specific import, since this is
 * unrelated to that feature.
 */
function describeSaveError(error: unknown): string {
  if (error instanceof ApiError) return error.message;
  if (error instanceof NetworkError) {
    return 'Could not reach the server — this invoice was NOT saved. Check your connection and try again.';
  }
  return 'Something went wrong while saving this invoice. Please try again.';
}

/**
 * OPD Billing Entry — the "Manual Invoice Entry" screen from the Finance & Billing domain
 * (docs/ScreenInventory.md), scoped to OPD (Consultation/Radiology/Laboratory/Procedure —
 * there's no IPD billing type yet). Closes the gap where only a patient's very first visit
 * (billed inline during New Patient Registration) could be billed at all — this lets
 * reception bill any *existing* patient's later visit (a follow-up lab test, a repeat
 * X-ray, a walk-in consultation) the same way. Reuses BillingStep unchanged rather than
 * forking it, so the two entry points can never drift out of sync.
 */
export default function InvoiceCreatePage() {
  const [patient, setPatient] = useState<Patient | null>(null);
  const [saveError, setSaveError] = useState<string | null>(null);
  const [saveErrorDetails, setSaveErrorDetails] = useState<string[]>([]);
  const billingRef = useRef<BillingStepHandle>(null);
  const createInvoiceMutation = useCreateInvoiceMutation();

  // Set once Save succeeds — swaps the editable billing form for the real invoice view
  // (InvoiceDetailCard, same component the standalone Invoice Detail page uses) rendered
  // right here, so billing a patient never has to leave this page. Kept in local state
  // rather than a query subscription: this page doesn't otherwise know the invoice's id
  // until the create call returns, and Record Payment/Void below update it directly from
  // their own mutation results rather than refetching.
  const [savedInvoice, setSavedInvoice] = useState<Billing | null>(null);
  const [itemPendingPayment, setItemPendingPayment] = useState<BillingItem | null>(null);
  const [isVoiding, setIsVoiding] = useState(false);
  const recordPaymentMutation = useRecordPaymentMutation();
  const voidInvoiceMutation = useVoidInvoiceMutation();

  // Registration Details already captured Department/Consultant/Consultation Type for this
  // patient's visit — re-asking for the same three fields here would just be redundant data
  // entry for the common case (billing the visit that was just registered). Prefilled from the
  // patient's most recent visit only; a patient with none (never went through Registration
  // Details, or was created before the visit feature existed) falls back to one blank row,
  // same as before this change.
  const { data: visits, isPending: visitsPending } = usePatientVisitsQuery(patient?.id);
  const latestVisit = visits?.[0];

  // Guards against double-billing a consultation this specific visit already had billed —
  // without this, re-opening OPD Billing Entry for the same visit (e.g. to add a Laboratory
  // charge, or any time after the day it was originally billed) would still prefill
  // Consultation Billing with the visit's doctor rows, inviting reception to save them again
  // as a second, duplicate consultation charge. Keyed to the visit's own id (Invoice.VisitId),
  // not "any invoice for this patient today" — a patient can have more than one visit, and a
  // consultation billed yesterday (or any earlier day) needs the same guard a same-day one
  // does. Deliberately whole-visit, not per-consultant: InvoiceLineItem.MarkPaid() clears
  // ConsultantId once a line item is paid (see docs/DecisionLog.md's Billing ADR on that fix),
  // so a paid item's consultant can no longer be matched individually — but billing every
  // consultant on a visit together in one invoice is the realistic common case anyway, so this
  // is the meaningful guard rather than a needless simplification. A voided invoice doesn't
  // count — that consultation was never actually billed.
  const { data: patientInvoices, isPending: invoicesPending } = usePatientInvoicesQuery(patient?.id);
  const consultationAlreadyBilledForVisit = useMemo(() => {
    if (!latestVisit) return false;
    return (patientInvoices ?? []).some(
      (invoice) =>
        !invoice.isVoided &&
        invoice.visitId === latestVisit.visitId &&
        invoice.items.some((item) => item.billingType === 'Consultation'),
    );
  }, [patientInvoices, latestVisit]);

  const billingDefaultValues = useMemo<BillingFormValues>(() => {
    if (consultationAlreadyBilledForVisit || !latestVisit || latestVisit.consultations.length === 0) {
      return defaultBillingFormValues;
    }
    const consultation: ConsultationBillingFormValues[] = latestVisit.consultations.map((c) => ({
      ...emptyConsultation,
      departmentId: c.departmentId,
      consultantId: c.consultantId,
      // Left blank (not required at Registration Details) rather than guessed — the charge
      // effect in ConsultationBillingCard only fires once a real type is selected, and forcing
      // one here could bill the wrong category.
      consultationTypeId: c.consultationTypeId ?? '',
      // Locks Department/Consultant in ConsultationBillingCard — see fromVisit's own comment
      // in billingValidation.ts for why Consultation Type isn't included in that lock.
      fromVisit: true,
    }));
    return { ...defaultBillingFormValues, consultation };
  }, [latestVisit, consultationAlreadyBilledForVisit]);

  // Guards against losing an in-progress, unsaved invoice — a receptionist part-way through
  // billing several items who accidentally hits back/closes the tab previously lost
  // everything with no warning. isDirtyRef mirrors isDirty for the blocker's shouldBlock
  // function (refs read synchronously, so handleSave can clear it immediately before its own
  // navigate() call — a plain state read there could still see the stale `true` from before
  // this render committed).
  const [isDirty, setIsDirty] = useState(false);
  const isDirtyRef = useRef(false);
  useEffect(() => {
    isDirtyRef.current = isDirty;
  }, [isDirty]);

  // In-app navigation (sidebar links, "Back to Accounts and Finance") — react-router's own
  // blocker, only reachable via the RouterProvider/data-router setup this app uses.
  const blocker = useBlocker(() => isDirtyRef.current);

  // Actual tab close/refresh/URL-bar navigation — react-router's blocker can't see these,
  // only the browser's native beforeunload prompt can.
  useEffect(() => {
    if (!isDirty) return;
    function handleBeforeUnload(event: BeforeUnloadEvent) {
      event.preventDefault();
      event.returnValue = '';
    }
    window.addEventListener('beforeunload', handleBeforeUnload);
    return () => window.removeEventListener('beforeunload', handleBeforeUnload);
  }, [isDirty]);

  // "Change patient" resets local state rather than navigating, so the router blocker above
  // never sees it — a separate, lighter-weight confirmation for this one case.
  const [confirmChangePatient, setConfirmChangePatient] = useState(false);

  function handleChangePatientClick() {
    if (isDirty) {
      setConfirmChangePatient(true);
    } else {
      setPatient(null);
    }
  }

  const showUnsavedDialog = blocker.state === 'blocked' || confirmChangePatient;

  function handleConfirmDiscard() {
    if (blocker.state === 'blocked') {
      blocker.proceed();
    }
    if (confirmChangePatient) {
      setPatient(null);
      setConfirmChangePatient(false);
      setIsDirty(false);
      isDirtyRef.current = false;
    }
  }

  function handleCancelDiscard() {
    if (blocker.state === 'blocked') {
      blocker.reset();
    }
    setConfirmChangePatient(false);
  }

  // Save-time counterpart to the prefill guard above — that guard only stops a duplicate row
  // from being auto-filled; it does nothing once reception clicks "Add another Consultation"
  // and fills it in by hand, which would otherwise save a genuine second consultation charge
  // for a visit that's already billed one with no check anywhere, client or server. This is a
  // warning, not a hard block: billing more than one real consultant on the same visit together
  // is the realistic common case (same reasoning the prefill guard's own comment gives), so
  // outright refusing to save would break that legitimate path — this just makes the accidental
  // case (re-adding a row out of habit after the prefill left it blank) take an explicit extra
  // click instead of silently saving.
  const [duplicateConsultationValues, setDuplicateConsultationValues] = useState<BillingFormValues | null>(null);

  async function handleSave() {
    if (!patient || !billingRef.current) return;
    setSaveError(null);
    setSaveErrorDetails([]);

    const valid = await billingRef.current.validate();
    if (!valid) return;

    const values = billingRef.current.getValues();
    const addsAnotherConsultationCharge = consultationAlreadyBilledForVisit && (values.consultation ?? []).some(isConsultationEntryActive);
    if (addsAnotherConsultationCharge) {
      setDuplicateConsultationValues(values);
      return;
    }

    await saveInvoice(values);
  }

  async function saveInvoice(values: BillingFormValues) {
    if (!patient) return;
    try {
      // Use the patient's actual visit (populates Registration Type/Department/Consultant(s)
      // on the Recent Patient Bills ledger via the Billing↔Patients join in
      // InvoiceService.GetRecentAsync) when Registration Details captured one; a returning
      // patient billed with no registration on record has no visit to attach, so this falls
      // back to the patient id itself — mirrors CreateInvoiceRequest.VisitId's own doc comment.
      const billing = await createInvoiceMutation.mutateAsync({
        patientId: patient.id,
        visitId: latestVisit?.visitId ?? patient.id,
        values,
        patient: { name: `${patient.firstName} ${patient.lastName}`, uhid: patient.uhid },
      });

      if (!billing) {
        setSaveError('Add at least one billing item before saving.');
        return;
      }
      // Clear the dirty flag so the unsaved-changes blocker doesn't fire once the form below
      // is replaced by the read-only invoice view — there's nothing left to lose.
      setIsDirty(false);
      isDirtyRef.current = false;
      setSavedInvoice(billing);
    } catch (error) {
      setSaveError(describeSaveError(error));
      setSaveErrorDetails(error instanceof ApiError ? (error.validationErrors?.map((issue) => issue.message) ?? []) : []);
    }
  }

  function handleConfirmDuplicateConsultation() {
    const values = duplicateConsultationValues;
    setDuplicateConsultationValues(null);
    if (values) void saveInvoice(values);
  }

  function handleCancelDuplicateConsultation() {
    setDuplicateConsultationValues(null);
  }

  function handleConfirmPayment(method: PaymentMethod) {
    if (!savedInvoice || !itemPendingPayment) return;
    recordPaymentMutation.mutate(
      { billingId: savedInvoice.id, itemId: itemPendingPayment.id, method },
      { onSuccess: (updated) => { setSavedInvoice(updated); setItemPendingPayment(null); } },
    );
  }

  function handleConfirmVoid(reason: string) {
    if (!savedInvoice) return;
    voidInvoiceMutation.mutate(
      { billingId: savedInvoice.id, reason },
      { onSuccess: (updated) => { setSavedInvoice(updated); setIsVoiding(false); } },
    );
  }

  // Resets the page for the next patient without leaving it — same reasoning as saving
  // itself staying on this page: a receptionist billing several walk-ins in a row shouldn't
  // have to navigate back to Accounts and Finance and re-open OPD Billing Entry each time.
  function handleBillAnotherPatient() {
    setPatient(null);
    setSavedInvoice(null);
    setSaveError(null);
    setSaveErrorDetails([]);
  }

  return (
    <div className="flex flex-1 flex-col">
      <div className="px-6 pt-4 lg:px-8">
        <Link to="/finance/accounts" className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
          <ArrowLeft className="h-4 w-4" />
          Back to Accounts and Finance
        </Link>
      </div>

      {/* Centered, brand-colored banner — matches the Page banner style used
          across module pages (Theme & Branding → Section headers). */}
      <div className="relative mt-3 flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        <div className="flex items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
            <FilePlus2 className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight">OPD Billing Entry</h1>
        </div>
        <p className="max-w-2xl text-sm text-page-banner-foreground/85">
          Bill an existing patient's visit — Consultation, Radiology, Laboratory, or Procedure.
        </p>
      </div>

      <div className="flex flex-1 flex-col gap-4 p-6 lg:p-8">
        {/* Full width, not centered — matches PatientRegistrationForm, which renders
            this same BillingStep component in the registration wizard. */}
        <div className="flex w-full flex-col gap-4">
          {!patient ? (
            <PatientPicker onSelect={setPatient} />
          ) : (
            <>
              <Card>
                <CardContent className="flex items-center justify-between gap-3 py-4">
                  <div className="flex flex-col gap-0.5">
                    <span className="text-xs text-muted-foreground">Billing for</span>
                    <span className="text-base font-semibold text-foreground">
                      {patient.title} {patient.firstName} {patient.lastName}{' '}
                      <span className="font-normal text-muted-foreground">· {patient.uhid}</span>
                    </span>
                  </div>
                  <Button variant="outline" size="sm" onClick={handleChangePatientClick}>
                    Change patient
                  </Button>
                </CardContent>
              </Card>

              {savedInvoice ? (
                // Saved — shows the real invoice view right here (same InvoiceDetailCard the
                // standalone Invoice Detail page renders) instead of navigating there, so
                // billing this patient never leaves this page. Record Payment/Void still work,
                // same as on that page.
                <>
                  <p role="status" className="rounded-md bg-success/10 px-3 py-2 text-sm text-success">
                    Invoice {savedInvoice.invoiceNumber ?? savedInvoice.id} saved.
                  </p>
                  {savedInvoice.items.some((item) => item.billingType === 'Laboratory') ? (
                    <Tabs defaultValue="summary">
                      <TabsList>
                        <TabsTrigger value="summary">Summary</TabsTrigger>
                        <TabsTrigger value="lab-details">Lab Details</TabsTrigger>
                      </TabsList>
                      <TabsContent value="summary" className="mt-4">
                        <InvoiceDetailCard billing={savedInvoice} onRecordPayment={setItemPendingPayment} onVoidInvoice={() => setIsVoiding(true)} />
                      </TabsContent>
                      <TabsContent value="lab-details" className="mt-4">
                        <LabDetailsCard billing={savedInvoice} />
                      </TabsContent>
                    </Tabs>
                  ) : (
                    <InvoiceDetailCard billing={savedInvoice} onRecordPayment={setItemPendingPayment} onVoidInvoice={() => setIsVoiding(true)} />
                  )}
                  <div className="flex justify-end">
                    <Button onClick={handleBillAnotherPatient}>Bill Another Patient</Button>
                  </div>
                </>
              ) : (
                <>
                  {visitsPending || invoicesPending ? (
                    <div className="flex items-center justify-center gap-2 py-10 text-sm text-muted-foreground">
                      <Loader2 className="h-4 w-4 animate-spin" />
                      Loading this patient's visit details…
                    </div>
                  ) : (
                    // key forces a fresh BillingStep (and a fresh RHF form) per patient -
                    // defaultValues is only ever read once by useForm, so without this, picking
                    // a different patient after the form already mounted would keep showing the
                    // first patient's prefill.
                    // onSave/isSaving/saveError render inside BillingSummaryCard's sticky
                    // sidebar (not below the two-column grid) — that grid's overall height
                    // follows whichever billing category card is currently expanded, so a
                    // button placed below it would visibly jump down the page every time a
                    // section opened. The summary sidebar doesn't grow that way, so the action
                    // that actually depends on it stays anchored there instead.
                    <BillingStep
                      key={patient.id}
                      ref={billingRef}
                      defaultValues={billingDefaultValues}
                      onDirtyChange={setIsDirty}
                      onSave={handleSave}
                      isSaving={createInvoiceMutation.isPending}
                      saveError={saveError}
                      saveErrorDetails={saveErrorDetails}
                    />
                  )}
                </>
              )}
            </>
          )}
        </div>
      </div>

      <Dialog open={showUnsavedDialog} onOpenChange={(open) => !open && handleCancelDiscard()}>
        <DialogContent aria-labelledby="discard-invoice-title">
          <DialogHeader>
            <DialogTitle id="discard-invoice-title">Discard this invoice?</DialogTitle>
            <DialogDescription>
              You've entered billing details that haven't been saved yet. Leaving now will lose everything entered for this
              invoice.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={handleCancelDiscard}>
              Keep editing
            </Button>
            <Button variant="destructive" onClick={handleConfirmDiscard}>
              Discard and leave
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={duplicateConsultationValues !== null} onOpenChange={(open) => !open && handleCancelDuplicateConsultation()}>
        <DialogContent aria-labelledby="duplicate-consultation-title">
          <DialogHeader>
            <DialogTitle id="duplicate-consultation-title">Consultation already billed for this visit</DialogTitle>
            <DialogDescription>
              This visit already has a consultation charge on a saved invoice. Saving now will add another consultation
              charge on top of it — only continue if a different or additional consultation genuinely needs billing.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={handleCancelDuplicateConsultation}>
              Cancel
            </Button>
            <Button onClick={handleConfirmDuplicateConsultation} disabled={createInvoiceMutation.isPending}>
              {createInvoiceMutation.isPending ? 'Saving…' : 'Continue and Save'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {itemPendingPayment && (
        <RecordPaymentDialog
          serviceLabel={`${itemPendingPayment.billingType} — ${describeBillingItem(itemPendingPayment).serviceLabel}`}
          amount={itemPendingPayment.total}
          isSaving={recordPaymentMutation.isPending}
          onConfirm={handleConfirmPayment}
          onCancel={() => setItemPendingPayment(null)}
        />
      )}

      {isVoiding && savedInvoice && (
        <VoidInvoiceDialog
          invoiceLabel={`Invoice ${savedInvoice.invoiceNumber ?? savedInvoice.id}`}
          isSaving={voidInvoiceMutation.isPending}
          onConfirm={handleConfirmVoid}
          onCancel={() => setIsVoiding(false)}
        />
      )}
    </div>
  );
}
