import { ApiError, type DispenseCartFormValues, type Patient } from '@hms/shared';
import { Pill } from 'lucide-react';
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { PageBanner } from '@/components/PageBanner';
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
        <PageBanner
          icon={Pill}
          title="Dispense Stock"
          subtitle="Issue stock directly to a patient."
          backTo="/pharmacy/dispenses"
          backLabel="Back to dispenses"
        />

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
