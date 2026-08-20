import { ApiError, type DispenseCartFormValues, type Patient } from '@hms/shared';
import { ArrowLeft, Pill } from 'lucide-react';
import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useToast } from '@/components/ui/toast-context';
import { PatientPicker } from '@/features/billing';
import { RequirePermission } from '@/features/auth/RequirePermission';
import { DispenseCartForm, useCreateDispenseCartMutation } from '@/features/pharmacy/dispenses';

export default function DispenseCreatePage() {
  const navigate = useNavigate();
  const { toast } = useToast();
  const [patient, setPatient] = useState<Patient | null>(null);
  const mutation = useCreateDispenseCartMutation();

  function handleSubmit(values: DispenseCartFormValues) {
    mutation.mutate(
      {
        patientId: values.patientId,
        admissionId: values.admissionId || undefined,
        lines: values.lines.map((line) => ({
          productId: line.productId,
          productBatchId: line.productBatchId,
          quantity: line.quantity,
          remarks: line.remarks || undefined,
        })),
      },
      {
        onSuccess: (cart) => {
          const itemCount = cart.lines.length;
          const base = `${itemCount} item${itemCount === 1 ? '' : 's'} dispensed to ${patient?.firstName} ${patient?.lastName} — total ₹${cart.totalAmount.toFixed(2)}.`;
          if (cart.billingFailed) {
            // The dispense itself always succeeds even when billing doesn't (see
            // DispenseService's best-effort billing step) — a real toast here, not a silent
            // drop, so staff know to post the charge manually via OPD Billing Entry.
            toast({
              title: 'Stock dispensed — billing failed',
              description: `${base} Could not create the invoice: ${cart.billingError ?? 'unknown error'}. Bill this manually via OPD Billing Entry.`,
              variant: 'warning',
            });
          } else {
            toast({
              title: 'Stock dispensed',
              description: `${base} Invoice ${cart.invoiceNumber} created.`,
              variant: 'success',
            });
          }
          navigate('/pharmacy/dispenses');
        },
        // On failure (409: expired batch / insufficient stock / invalid product-batch-patient
        // reference on any line — nothing in the cart is dispensed) the real backend message is
        // surfaced by DispenseCartForm via apiError — never swallowed into a generic message.
      },
    );
  }

  return (
    <RequirePermission permission="pharmacy.create">
      <div className="flex flex-1 flex-col">
        <div className="px-6 pt-4 lg:px-8">
          <Link to="/pharmacy/dispenses" className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
            <ArrowLeft className="h-4 w-4" />
            Back to dispenses
          </Link>
        </div>

        <div className="mt-3 flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
          <div className="flex items-center gap-3">
            <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
              <Pill className="h-5 w-5" />
            </span>
            <h1 className="text-xl font-semibold tracking-tight">Dispense Stock</h1>
          </div>
          <p className="max-w-2xl text-sm text-page-banner-foreground/85">Issue stock directly to a patient.</p>
        </div>

        <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
          {!patient && (
            <div className="flex flex-col gap-3">
              <h2 className="text-sm font-semibold uppercase tracking-wide text-muted-foreground">Patient</h2>
              <PatientPicker onSelect={setPatient} />
            </div>
          )}

          {patient && (
            <DispenseCartForm
              patient={patient}
              onChangePatient={() => setPatient(null)}
              isSubmitting={mutation.isPending}
              apiError={mutation.error instanceof ApiError ? mutation.error : null}
              onSubmit={handleSubmit}
            />
          )}
        </div>
      </div>
    </RequirePermission>
  );
}
