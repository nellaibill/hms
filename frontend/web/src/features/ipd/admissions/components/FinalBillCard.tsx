import { ApiError } from '@hms/shared';
import { FileText, Loader2, Receipt } from 'lucide-react';
import { Link, useNavigate } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { useAdmissionAdvancesQuery } from '../hooks/useAdmissionAdvances';
import { useAdmissionChargesQuery } from '../hooks/useAdmissionCharges';
import { useGenerateFinalBillMutation } from '../hooks/useIPDBilling';

interface FinalBillCardProps {
  admissionId: string;
  finalInvoiceId?: string | null;
}

/**
 * Converts this discharged admission's AdmissionCharge ledger into a real
 * HMS.Modules.Billing Invoice — the actual payment-collection point IPD has never had (see
 * ADR-066). Only rendered once discharged (see AdmissionViewPage.tsx, same gate
 * DischargeSummaryEntryCard already uses). Deliberately does not rebuild any payment UI —
 * once generated, routes straight to the existing Invoice Detail page.
 */
export function FinalBillCard({ admissionId, finalInvoiceId }: FinalBillCardProps) {
  const navigate = useNavigate();
  const chargesQuery = useAdmissionChargesQuery(admissionId);
  const advancesQuery = useAdmissionAdvancesQuery(admissionId);
  const generateMutation = useGenerateFinalBillMutation(admissionId);

  if (finalInvoiceId) {
    return (
      <Button asChild variant="outline" size="sm" className="w-fit gap-1.5">
        <Link to={`/finance/accounts/${finalInvoiceId}`}>
          <FileText className="h-4 w-4" />
          View Final Bill
        </Link>
      </Button>
    );
  }

  const chargesTotal = (chargesQuery.data ?? []).reduce((sum, charge) => sum + charge.amount, 0);
  const advanceTotal = (advancesQuery.data ?? []).reduce((sum, advance) => sum + advance.amount, 0);
  // Display-only — the generated Invoice is never adjusted by the advance (HMS.Modules.Billing
  // has no partial-payment/credit concept to net it against, see ADR-067). This just tells
  // staff how much more (or how much refund, unhandled by this system) is actually owed.
  const balance = chargesTotal - advanceTotal;
  const isPending = chargesQuery.isPending || advancesQuery.isPending;
  const apiError = generateMutation.error instanceof ApiError ? generateMutation.error : null;

  function handleGenerate() {
    generateMutation.mutate(undefined, {
      onSuccess: (response) => navigate(`/finance/accounts/${response.invoiceId}`),
    });
  }

  return (
    <div className="flex flex-col gap-2">
      {isPending && (
        <div className="flex items-center gap-2 text-sm text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" />
          Loading charges…
        </div>
      )}

      {chargesQuery.isSuccess && advancesQuery.isSuccess && (
        <div className="text-sm text-muted-foreground">
          <p>
            Charges total: <span className="font-mono font-medium text-foreground">₹{chargesTotal.toFixed(2)}</span>
          </p>
          <p>
            Advance collected: <span className="font-mono font-medium text-foreground">₹{advanceTotal.toFixed(2)}</span>
          </p>
          <p>
            {balance >= 0 ? 'Balance due' : 'Refund due (not processed by this system)'}:{' '}
            <span className="font-mono font-medium text-foreground">₹{Math.abs(balance).toFixed(2)}</span>
          </p>
        </div>
      )}

      {apiError && <p className="text-sm text-destructive">{apiError.message}</p>}

      <Button
        type="button"
        className="w-fit gap-1.5"
        disabled={generateMutation.isPending || isPending || (chargesQuery.data ?? []).length === 0}
        onClick={handleGenerate}
      >
        <Receipt className="h-4 w-4" />
        {generateMutation.isPending ? 'Generating…' : 'Generate Final Bill'}
      </Button>
    </div>
  );
}
