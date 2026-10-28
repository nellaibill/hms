import type { InvoiceRevertRequestResponse, InvoiceRevertRequestStatus } from '@hms/shared';
import { Loader2, Wallet } from 'lucide-react';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { PageBanner } from '@/components/PageBanner';
import { PatientNameLink } from '@/components/PatientNameLink';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent } from '@/components/ui/card';
import { useAuth } from '@/features/auth/AuthContext';
import { AccountsNavTabs } from '@/features/reports';
import { cn } from '@/lib/utils';
import { Pagination, RevertDecisionButtons, formatCurrency, useRevertRequestsQuery } from '../../features/billing';

const PAGE_SIZE = 20;

const FILTERS: { value: InvoiceRevertRequestStatus | undefined; label: string }[] = [
  { value: 'Pending', label: 'Pending' },
  { value: 'Approved', label: 'Approved' },
  { value: 'Rejected', label: 'Rejected' },
  { value: undefined, label: 'All' },
];

function StatusBadge({ status }: { status: InvoiceRevertRequestStatus }) {
  if (status === 'Pending') return <Badge variant="warning">Pending</Badge>;
  if (status === 'Approved') return <Badge variant="secondary">Reverted</Badge>;
  return <Badge variant="outline">Rejected</Badge>;
}

function Decision({ request }: { request: InvoiceRevertRequestResponse }) {
  if (request.status === 'Pending') return <RevertDecisionButtons request={request} />;
  return (
    <div className="flex flex-col gap-0.5">
      <StatusBadge status={request.status} />
      <span className="text-xs text-muted-foreground">
        {request.decidedByName ?? 'Super Admin'}
        {request.decidedAt ? ` · ${new Date(request.decidedAt).toLocaleString('en-IN')}` : ''}
      </span>
      {request.decisionNotes && <span className="text-xs text-foreground">{request.decisionNotes}</span>}
    </div>
  );
}

/** The Accounts & Finance "Revert Requests" tab — OP bill revert requests (ADR-106). Pending
 * requests are the Super Admin's approval queue (oldest first); anyone with billing access can
 * read the list, but only a Super Admin sees Approve/Reject. */
export default function RevertRequestsPage() {
  const { user } = useAuth();
  const isSuperAdmin = user?.role === 'superAdmin';
  const [status, setStatus] = useState<InvoiceRevertRequestStatus | undefined>('Pending');
  const [page, setPage] = useState(1);

  const { data, isPending, isError, error } = useRevertRequestsQuery({ page, pageSize: PAGE_SIZE, status });

  return (
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={Wallet}
        title="Accounts and Finance"
        subtitle="OP bill revert requests — raised by billing staff, approved or rejected by the Super Admin."
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
          {!isSuperAdmin && <p className="text-sm text-muted-foreground">Only a Super Admin can approve or reject these requests.</p>}
        </div>

        {isPending && (
          <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" />
            Loading revert requests…
          </div>
        )}

        {isError && (
          <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
            {error instanceof Error ? error.message : 'Failed to load revert requests.'}
          </p>
        )}

        {!isPending && !isError && data && data.items.length === 0 && (
          <Card className="border-dashed">
            <CardContent className="flex flex-col items-center gap-2 py-16 text-center">
              <p className="text-sm font-medium text-foreground">No {status ? status.toLowerCase() : ''} revert requests</p>
              <p className="text-sm text-muted-foreground">Billing staff raise one from an OP bill's page with "Request Revert".</p>
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
                    <th className="px-4 py-2.5 text-right">Amount</th>
                    <th className="px-4 py-2.5">Reason</th>
                    <th className="px-4 py-2.5">Requested</th>
                    <th className="px-4 py-2.5">Decision</th>
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
                      <td className="whitespace-nowrap px-4 py-3 text-right">
                        <div className="font-medium text-foreground">{formatCurrency(request.netAmount)}</div>
                        <div className="text-xs text-muted-foreground">{request.paidAmount > 0 ? `${formatCurrency(request.paidAmount)} paid` : 'Not paid'}</div>
                      </td>
                      <td className="min-w-[12rem] max-w-xs px-4 py-3 text-foreground">{request.reason}</td>
                      <td className="whitespace-nowrap px-4 py-3">
                        <div className="text-foreground">{request.requestedByName ?? '—'}</div>
                        <div className="font-mono text-xs text-muted-foreground">{new Date(request.requestedAt).toLocaleString('en-IN')}</div>
                      </td>
                      <td className="min-w-[10rem] px-4 py-3">
                        <Decision request={request} />
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
