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
import {
  DiscountDecisionButtons,
  Pagination,
  describeBillingItem,
  describeDiscountEntry,
  formatCurrency,
  useDiscountRequestsQuery,
} from '../../features/billing';

const PAGE_SIZE = 20;

const FILTERS: { value: InvoiceDiscountRequestStatus | undefined; label: string }[] = [
  { value: 'Pending', label: 'Pending' },
  { value: 'Approved', label: 'Approved' },
  { value: 'Rejected', label: 'Rejected' },
  { value: 'Cancelled', label: 'Cancelled' },
  { value: undefined, label: 'All' },
];

function StatusBadge({ status }: { status: InvoiceDiscountRequestStatus }) {
  if (status === 'Pending') return <Badge variant="warning">Pending</Badge>;
  if (status === 'Approved') return <Badge variant="success">Approved</Badge>;
  if (status === 'Cancelled') return <Badge variant="secondary">Cancelled</Badge>;
  return <Badge variant="outline">Rejected</Badge>;
}

function Decision({ request }: { request: InvoiceDiscountRequestResponse }) {
  if (request.status === 'Pending') return <DiscountDecisionButtons request={request} />;
  return (
    <div className="flex flex-col gap-0.5">
      <StatusBadge status={request.status} />
      <span className="text-xs text-muted-foreground">
        {request.decidedByName ?? (request.status === 'Cancelled' ? 'Bill voided' : 'Super Admin')}
        {request.decidedAt ? ` · ${new Date(request.decidedAt).toLocaleString('en-IN')}` : ''}
      </span>
      {request.decisionNotes && <span className="text-xs text-foreground">{request.decisionNotes}</span>}
    </div>
  );
}

function Lines({ request }: { request: InvoiceDiscountRequestResponse }) {
  return (
    <ul className="flex flex-col gap-0.5">
      {request.lines.map((line) => {
        const { serviceLabel } = describeBillingItem({
          id: line.lineItemId,
          billingType: line.billingType,
          departmentId: line.departmentId ?? undefined,
          consultantId: line.consultantId ?? undefined,
          serviceId: line.serviceId ?? undefined,
          packageId: line.packageId ?? undefined,
          quantity: line.quantity,
          unitPrice: line.unitPrice,
          discount: 0,
          discountApproved: false,
          paymentStatus: 'Pending',
          total: line.lineAmount,
        });
        return (
          <li key={line.lineItemId} className="flex justify-between gap-3 text-xs">
            <span className="text-muted-foreground">
              {line.billingType} — {serviceLabel} ({formatCurrency(line.lineAmount)})
            </span>
            <span className="shrink-0 font-medium text-destructive">− {formatCurrency(line.discount)}</span>
          </li>
        );
      })}
    </ul>
  );
}

/** The Accounts & Finance "Discount Requests" tab — bill discount requests (ADR-109). Pending
 * requests are the Super Admin's approval queue (oldest first); anyone with billing access can
 * read the list, but only a Super Admin sees Approve/Reject. */
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
        subtitle="Bill discount requests — raised by billing staff, approved or rejected by the Super Admin."
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
                    <th className="px-4 py-2.5">Lines</th>
                    <th className="px-4 py-2.5 text-right">Discount</th>
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
                      <td className="min-w-[14rem] px-4 py-3">
                        <Lines request={request} />
                      </td>
                      <td className="whitespace-nowrap px-4 py-3 text-right">
                        <div className="font-medium text-destructive">− {formatCurrency(request.totalDiscount)}</div>
                        <div className="text-xs text-muted-foreground">
                          {describeDiscountEntry(request.discountType, request.discountValue)} · bill {formatCurrency(request.grossAmount)}
                        </div>
                      </td>
                      <td className="min-w-[10rem] max-w-xs px-4 py-3 text-foreground">{request.reason}</td>
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
