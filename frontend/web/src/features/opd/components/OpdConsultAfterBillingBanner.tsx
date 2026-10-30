import { CircleCheck, Info, Loader2, Stethoscope } from 'lucide-react';
import { Link, useNavigate } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { invoicePathForConsult } from '../consultBillingLinks';
import { useStartConsultationMutation } from '../hooks/useStartConsultationMutation';
import { useVisitPaymentStatusesQuery } from '../hooks/useVisitPaymentStatusesQuery';

interface OpdConsultAfterBillingBannerProps {
  consultationId: string;
  patientId: string;
  visitId: string;
  /** The invoice already on screen — left out of the "next unpaid bill" link. */
  currentInvoiceId?: string;
}

/**
 * Shown on OPD Billing Entry / Invoice Detail when they were opened from an OPD "Consult" click
 * on an unpaid visit (?consultationId=, see consultBillingLinks.ts). Tracks the visit's Payment
 * Status — the same one the OPD Patient List shows, refetched after every save/payment since
 * those invalidate ['billings'] — and offers "Continue to Consultation" once it's Paid.
 */
export function OpdConsultAfterBillingBanner({ consultationId, patientId, visitId, currentInvoiceId }: OpdConsultAfterBillingBannerProps) {
  const navigate = useNavigate();
  const startConsultation = useStartConsultationMutation();
  const { getStatus, getUnpaidInvoiceIds } = useVisitPaymentStatusesQuery([patientId]);
  const status = getStatus(patientId, visitId);

  if (status === 'Loading') return null;

  if (status === 'Paid') {
    return (
      <div role="status" className="flex flex-wrap items-center justify-between gap-3 rounded-md border border-success/30 bg-success/10 px-3 py-2 text-sm">
        <span className="flex items-center gap-2 text-foreground">
          <CircleCheck className="h-4 w-4 shrink-0 text-success" />
          This visit is fully paid — the consultation can go ahead.
        </span>
        <div className="flex flex-col items-end gap-1">
          <Button
            size="sm"
            className="gap-1.5"
            disabled={startConsultation.isPending}
            onClick={() =>
              startConsultation.mutate(consultationId, {
                onSuccess: () => navigate(`/clinical/opd/consultations/${consultationId}`),
              })
            }
          >
            {startConsultation.isPending ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <Stethoscope className="h-3.5 w-3.5" />}
            Continue to Consultation
          </Button>
          {startConsultation.isError && (
            <span role="alert" className="text-xs text-destructive">
              {startConsultation.error instanceof Error ? startConsultation.error.message : 'Could not start the consultation.'}
            </span>
          )}
        </div>
      </div>
    );
  }

  const nextUnpaidInvoiceId = getUnpaidInvoiceIds(patientId, visitId).find((id) => id !== currentInvoiceId);

  return (
    <div className="flex flex-wrap items-center justify-between gap-3 rounded-md border border-info/30 bg-info/10 px-3 py-2 text-sm">
      <span className="flex items-center gap-2 text-foreground">
        <Info className="h-4 w-4 shrink-0 text-info" />
        Opened from OPD Consult — the consultation can start once this visit's Payment Status is Paid.
      </span>
      <div className="flex items-center gap-3">
        {nextUnpaidInvoiceId && (
          <Link to={invoicePathForConsult(nextUnpaidInvoiceId, consultationId)} className="text-primary hover:underline">
            Next unpaid bill
          </Link>
        )}
        <Link to="/clinical/opd" className="text-primary hover:underline">
          Back to OPD
        </Link>
      </div>
    </div>
  );
}
