import type { InvoiceDiscountRequestStatus } from '@hms/shared';
import { Loader2 } from 'lucide-react';
import { useState } from 'react';
import { Card, CardContent } from '@/components/ui/card';
import { useAuth } from '@/features/auth/AuthContext';
import { useDiagnosticServices, usePrimeDiagnosticPackageCache } from '@/features/diagnostics';
import { useMasterOptionsQuery } from '@/features/masters';
import { cn } from '@/lib/utils';
import { formatCurrency } from '../billingCalculations';
import { useMyDiscountApprovalsQuery } from '../hooks/useInvoiceDiscount';
import { DiscountApprovalParts } from './InvoiceDiscount';
import { Pagination } from './Pagination';

const PAGE_SIZE = 20;

const FILTERS: { value: InvoiceDiscountRequestStatus | undefined; label: string }[] = [
  { value: 'Pending', label: 'Waiting' },
  { value: 'Approved', label: 'Approved' },
  { value: 'Rejected', label: 'Rejected' },
  { value: undefined, label: 'All' },
];

/**
 * A consultant's own discount approvals (ADR-111) — the OPD page's "Discount Approvals" tab for a
 * consultant login. Lists the bills where the counter asked for a discount on this consultant's
 * consultation fee, waiting ones first (oldest first), each with Approve/Reject. Only the
 * consultant's own part is shown; the rest of the bill's discount (if any) is the Super Admin's.
 * Invoice numbers are plain text: a consultant usually can't open the billing pages.
 */
export function MyDiscountApprovals() {
  const { scopedConsultantId } = useAuth();
  const [status, setStatus] = useState<InvoiceDiscountRequestStatus | undefined>('Pending');
  const [page, setPage] = useState(1);
  // Primes the reference caches describeBillingItem reads, so each line shows its service name.
  useMasterOptionsQuery('department');
  useMasterOptionsQuery('consultant');
  useMasterOptionsQuery('consultationType');
  useMasterOptionsQuery('diagnosticTest');
  useDiagnosticServices('Radiology');
  useDiagnosticServices('Laboratory');
  usePrimeDiagnosticPackageCache();

  const { data, isPending, isError, error } = useMyDiscountApprovalsQuery({ page, pageSize: PAGE_SIZE, status }, Boolean(scopedConsultantId));

  if (!scopedConsultantId) return null;

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex flex-wrap gap-1 rounded-md border border-border p-1" role="group" aria-label="Filter by status">
          {FILTERS.map((filter) => (
            <button
              key={filter.label}
              type="button"
              aria-pressed={status === filter.value}
              onClick={() => {
                setStatus(filter.value);
                setPage(1);
              }}
              className={cn(
                'rounded px-3 py-1.5 text-sm font-medium transition-colors',
                status === filter.value ? 'bg-sidebar-active text-sidebar-active-foreground' : 'text-muted-foreground hover:bg-accent/50 hover:text-foreground',
              )}
            >
              {filter.label}
            </button>
          ))}
        </div>
        <p className="text-sm text-muted-foreground">Discounts the counter asked for on your consultation fee.</p>
      </div>

      {isPending && (
        <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" />
          Loading discount approvals…
        </div>
      )}

      {isError && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {error instanceof Error ? error.message : 'Failed to load discount approvals.'}
        </p>
      )}

      {!isPending && !isError && data && data.items.length === 0 && (
        <Card className="border-dashed">
          <CardContent className="flex flex-col items-center gap-2 py-16 text-center">
            <p className="text-sm font-medium text-foreground">
              {status === 'Pending' ? 'No discounts waiting for you' : `No ${status ? status.toLowerCase() : ''} discount approvals`}
            </p>
            <p className="text-sm text-muted-foreground">When the counter asks for a discount on your fee, it shows here for you to approve or reject.</p>
          </CardContent>
        </Card>
      )}

      {!isPending && !isError && data && data.items.length > 0 && (
        <div className="flex flex-col gap-3">
          {data.items.map((request) => (
            <Card key={request.id}>
              <CardContent className="flex flex-col gap-3 p-4">
                <div className="flex flex-wrap items-start justify-between gap-2">
                  <div className="flex flex-col">
                    <span className="font-medium text-foreground">{request.patientName}</span>
                    <span className="text-xs text-muted-foreground">
                      {request.patientUhid} · Invoice <span className="font-mono">{request.invoiceNumber}</span> · bill {formatCurrency(request.grossAmount)}
                    </span>
                  </div>
                  <span className="text-xs text-muted-foreground">
                    Requested{request.requestedByName ? ` by ${request.requestedByName}` : ''} on {new Date(request.requestedAt).toLocaleString('en-IN')}
                  </span>
                </div>
                <div className="text-sm">
                  <span className="text-muted-foreground">Reason: </span>
                  <span className="text-foreground">{request.reason}</span>
                </div>
                <div className="rounded-md border border-border bg-muted/30 px-3 py-2">
                  <DiscountApprovalParts request={request} showLines onlyConsultantId={scopedConsultantId} />
                </div>
              </CardContent>
            </Card>
          ))}
          <Pagination meta={data.meta} onPageChange={setPage} />
        </div>
      )}
    </div>
  );
}
