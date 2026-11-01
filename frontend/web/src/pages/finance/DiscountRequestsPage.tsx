import type { InvoiceDiscountRequestResponse, InvoiceDiscountRequestStatus } from '@hms/shared';
import { Loader2, Wallet } from 'lucide-react';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { PageBanner } from '@/components/PageBanner';
import { PatientNameLink } from '@/components/PatientNameLink';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent } from '@/components/ui/card';
import { useAuth } from '@/features/auth/AuthContext';
import { useDiagnosticServices, usePrimeDiagnosticPackageCache } from '@/features/diagnostics';
import { useMasterOptionsQuery } from '@/features/masters';
import { AccountsNavTabs } from '@/features/reports';
import { cn } from '@/lib/utils';
import { DiscountApprovalParts, Pagination, describeDiscountEntry, formatCurrency, useDiscountRequestsQuery } from '../../features/billing';

const PAGE_SIZE = 20;

const FILTERS: { value: InvoiceDiscountRequestStatus | undefined; label: string }[] = [
  { value: 'Pending', label: 'Pending' },
  { value: 'Approved', label: 'Approved' },
  { value: 'PartlyApproved', label: 'Partly approved' },
  { value: 'Rejected', label: 'Rejected' },
  { value: 'Cancelled', label: 'Cancelled' },
  { value: undefined, label: 'All' },
];

function StatusBadge({ status }: { status: InvoiceDiscountRequestStatus }) {
  if (status === 'Pending') return <Badge variant="warning">Pending</Badge>;
  if (status === 'Approved') return <Badge variant="success">Approved</Badge>;
  if (status === 'PartlyApproved') return <Badge variant="success">Partly approved</Badge>;
  if (status === 'Cancelled') return <Badge variant="secondary">Cancelled</Badge>;
  return <Badge variant="outline">Rejected</Badge>;
}

function Status({ request }: { request: InvoiceDiscountRequestResponse }) {
  return (
    <div className="flex flex-col gap-0.5">
      <StatusBadge status={request.status} />
      {request.status === 'PartlyApproved' && (
        <span className="text-xs text-muted-foreground">{formatCurrency(request.approvedDiscount)} approved</span>
      )}
      {request.status === 'Cancelled' && <span className="text-xs text-muted-foreground">Bill voided</span>}
    </div>
  );
}

/** The Accounts & Finance "Discount Requests" tab — bill discount requests (ADR-109). Pending
 * requests are the approval queue (oldest first). Each request lists its approval parts
 * (ADR-111): a consultant's for their consultation fee, the Super Admin's for the rest. Anyone
 * with billing access can read the list; the Super Admin sees Approve/Reject on every waiting
 * part, a consultant's part included (decided on their behalf). */
export default function DiscountRequestsPage() {
  const { user } = useAuth();
  const isSuperAdmin = user?.role === 'superAdmin';
  const [status, setStatus] = useState<InvoiceDiscountRequestStatus | undefined>('Pending');
  const [page, setPage] = useState(1);
  // Primes the reference caches describeBillingItem reads, so each line shows its service name.
  useMasterOptionsQuery('diagnosticTest');
  useMasterOptionsQuery('department');
  useMasterOptionsQuery('consultant');
  useMasterOptionsQuery('consultationType');
  useDiagnosticServices('Radiology');
  useDiagnosticServices('Laboratory');
  usePrimeDiagnosticPackageCache();

  const { data, isPending, isError, error } = useDiscountRequestsQuery({ page, pageSize: PAGE_SIZE, status });

  return (
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={Wallet}
        title="Accounts and Finance"
        subtitle="Bill discount requests — raised by billing staff, decided by the consultant for their fee and the Super Admin for the rest."
        backTo="/finance/accounts"
        backLabel="Back to Accounts and Finance"
      />

      <div className="flex flex-1 flex-col gap-4 p-6 lg:p-8">
        <AccountsNavTabs />

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
          {!isSuperAdmin && (
            <p className="text-sm text-muted-foreground">Only the Super Admin, or the consultant whose fee it is, can approve or reject these.</p>
          )}
        </div>

        {isPending && (
          <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" />
            Loading discount requests…
          </div>
        )}

        {isError && (
          <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
            {error instanceof Error ? error.message : 'Failed to load discount requests.'}
          </p>
        )}

        {!isPending && !isError && data && data.items.length === 0 && (
          <Card className="border-dashed">
            <CardContent className="flex flex-col items-center gap-2 py-16 text-center">
              <p className="text-sm font-medium text-foreground">No {status ? status.toLowerCase() : ''} discount requests</p>
              <p className="text-sm text-muted-foreground">Billing staff raise one with "Add discount" while billing, or "Request Discount" on an unpaid bill.</p>
            </CardContent>
          </Card>
        )}

        {!isPending && !isError && data && data.items.length > 0 && (
          <div className="flex flex-col gap-3">
            <div className="overflow-x-auto rounded-lg border border-border">
              <table className="w-full text-sm">
                <thead className="bg-sidebar-active text-left text-xs font-medium uppercase tracking-wide text-sidebar-active-foreground">
                  <tr>
                    <th className="px-4 py-2.5">Invoice</th>
                    <th className="px-4 py-2.5">Patient</th>
                    <th className="px-4 py-2.5">Approvals</th>
                    <th className="px-4 py-2.5 text-right">Discount</th>
                    <th className="px-4 py-2.5">Reason</th>
                    <th className="px-4 py-2.5">Requested</th>
                    <th className="px-4 py-2.5">Status</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-border">
                  {data.items.map((request) => (
                    <tr key={request.id} className="align-top hover:bg-muted/30">
                      <td className="whitespace-nowrap px-4 py-3">
                        <Link to={`/finance/accounts/${request.invoiceId}`} className="font-mono text-xs text-primary hover:underline">
                          {request.invoiceNumber}
                        </Link>
                      </td>
                      <td className="px-4 py-3">
                        <PatientNameLink patientId={request.patientId}>{request.patientName}</PatientNameLink>
                        <div className="text-xs text-muted-foreground">{request.patientUhid}</div>
                      </td>
                      <td className="min-w-[20rem] px-4 py-3">
                        <DiscountApprovalParts request={request} showLines />
                      </td>
                      <td className="whitespace-nowrap px-4 py-3 text-right">
                        <div className="font-medium text-destructive">− {formatCurrency(request.totalDiscount)}</div>
                        <div className="text-xs text-muted-foreground">
                          {request.discountType === 'Percent' && `${describeDiscountEntry(request.discountType, request.discountValue)} · `}bill{' '}
                          {formatCurrency(request.grossAmount)}
                        </div>
                      </td>
                      <td className="min-w-[10rem] max-w-xs px-4 py-3 text-foreground">{request.reason}</td>
                      <td className="whitespace-nowrap px-4 py-3">
                        <div className="text-foreground">{request.requestedByName ?? '—'}</div>
                        <div className="font-mono text-xs text-muted-foreground">{new Date(request.requestedAt).toLocaleString('en-IN')}</div>
                      </td>
                      <td className="whitespace-nowrap px-4 py-3">
                        <Status request={request} />
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <Pagination meta={data.meta} onPageChange={setPage} />
          </div>
        )}
      </div>
    </div>
  );
}
