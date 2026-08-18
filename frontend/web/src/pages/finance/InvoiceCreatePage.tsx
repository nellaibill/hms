import type { Patient } from '@hms/shared';
import { ArrowLeft, FilePlus2 } from 'lucide-react';
import { useRef, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import {
  BillingStep,
  PatientPicker,
  defaultBillingFormValues,
  useCreateInvoiceMutation,
  type BillingStepHandle,
} from '../../features/billing';

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

  async function handleSave() {
    if (!patient || !billingRef.current) return;
    setSaveError(null);

    const valid = await billingRef.current.validate();
    if (!valid) return;

    const values = billingRef.current.getValues();
    try {
      // No real "new visit" concept exists yet for a returning patient (OPD encounter
      // tracking isn't built) — fall back to their last registration id the same way the
      // registration wizard's own save call does, rather than fabricating one.
      const billing = await createInvoiceMutation.mutateAsync({
        patientId: patient.id,
        visitId: patient.currentRegistration?.id ?? patient.id,
        values,
        patient: { name: `${patient.firstName} ${patient.lastName}`, uhid: patient.uhid },
      });

      if (!billing) {
        setSaveError('Add at least one billing item before saving.');
        return;
      }
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
        {/* Left-aligned, not centered — matches PatientRegistrationForm, which renders
            this same BillingStep component in the registration wizard. */}
        <div className="flex w-full max-w-6xl flex-col gap-4">
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
                  <Button variant="outline" size="sm" onClick={() => setPatient(null)}>
                    Change patient
                  </Button>
                </CardContent>
              </Card>

              <BillingStep ref={billingRef} defaultValues={defaultBillingFormValues} />

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
    </div>
  );
}
