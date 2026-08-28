import type { Patient } from '@hms/shared';
import { ArrowLeft, FilePlus2, Loader2 } from 'lucide-react';
import { useEffect, useMemo, useRef, useState } from 'react';
import { Link, useBlocker, useNavigate } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import {
  BillingStep,
  PatientPicker,
  defaultBillingFormValues,
  emptyConsultation,
  useCreateInvoiceMutation,
  type BillingFormValues,
  type BillingStepHandle,
  type ConsultationBillingFormValues,
} from '../../features/billing';
import { usePatientVisitsQuery } from '../../features/patients';

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
  const navigate = useNavigate();
  const [patient, setPatient] = useState<Patient | null>(null);
  const [saveError, setSaveError] = useState<string | null>(null);
  const billingRef = useRef<BillingStepHandle>(null);
  const createInvoiceMutation = useCreateInvoiceMutation();

  // Registration Details already captured Department/Consultant/Consultation Type for this
  // patient's visit — re-asking for the same three fields here would just be redundant data
  // entry for the common case (billing the visit that was just registered). Prefilled from the
  // patient's most recent visit only; a patient with none (never went through Registration
  // Details, or was created before the visit feature existed) falls back to one blank row,
  // same as before this change.
  const { data: visits, isPending: visitsPending } = usePatientVisitsQuery(patient?.id);
  const billingDefaultValues = useMemo<BillingFormValues>(() => {
    const latestVisit = visits?.[0];
    if (!latestVisit || latestVisit.consultations.length === 0) {
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
    }));
    return { ...defaultBillingFormValues, consultation };
  }, [visits]);

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

  async function handleSave() {
    if (!patient || !billingRef.current) return;
    setSaveError(null);

    const valid = await billingRef.current.validate();
    if (!valid) return;

    const values = billingRef.current.getValues();
    try {
      // No visit/encounter concept exists on Patient at all anymore (Registration Details is
      // UI-only pending a future backend module) — key the invoice off the patient directly.
      const billing = await createInvoiceMutation.mutateAsync({
        patientId: patient.id,
        visitId: patient.id,
        values,
        patient: { name: `${patient.firstName} ${patient.lastName}`, uhid: patient.uhid },
      });

      if (!billing) {
        setSaveError('Add at least one billing item before saving.');
        return;
      }
      // Clear the dirty flag before navigating so the blocker above doesn't intercept this
      // very navigation — the invoice is safely saved, there's nothing left to lose.
      setIsDirty(false);
      isDirtyRef.current = false;
      navigate(`/finance/accounts/${billing.id}`);
    } catch {
      setSaveError('Could not save the invoice. Please try again.');
    }
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

              {visitsPending ? (
                <div className="flex items-center justify-center gap-2 py-10 text-sm text-muted-foreground">
                  <Loader2 className="h-4 w-4 animate-spin" />
                  Loading this patient's visit details…
                </div>
              ) : (
                // key forces a fresh BillingStep (and a fresh RHF form) per patient — defaultValues
                // is only ever read once by useForm, so without this, picking a different patient
                // after the form already mounted would keep showing the first patient's prefill.
                <BillingStep key={patient.id} ref={billingRef} defaultValues={billingDefaultValues} onDirtyChange={setIsDirty} />
              )}

              {saveError && (
                <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
                  {saveError}
                </p>
              )}

              <div className="flex justify-end">
                <Button onClick={handleSave} disabled={createInvoiceMutation.isPending}>
                  {createInvoiceMutation.isPending ? 'Saving…' : 'Save Invoice'}
                </Button>
              </div>
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
    </div>
  );
}
