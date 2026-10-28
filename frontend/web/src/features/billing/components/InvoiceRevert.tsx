import type { InvoiceRevertRequestResponse } from '@hms/shared';
import { CircleCheck, CircleX, Clock, RotateCcw } from 'lucide-react';
import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { useToast } from '@/components/ui/toast-context';
import { useAuth } from '@/features/auth/AuthContext';
import { formatCurrency } from '../billingCalculations';
import {
  useApproveRevertRequestMutation,
  useInvoiceRevertStatusQuery,
  useRejectRevertRequestMutation,
  useRequestInvoiceRevertMutation,
} from '../hooks/useInvoiceRevert';
import type { Billing } from '../types';
import { RequestRevertDialog, RevertDecisionDialog } from './RevertRequestDialogs';

function formatDateTime(value: string | null | undefined) {
  return value ? new Date(value).toLocaleString('en-IN') : '';
}

/** "Request Revert" on an OP bill (ADR-106) — shown only when the server says this bill can be
 * reverted (an OP bill made today, not voided, no request already pending) and the user can edit
 * bills. */
export function RequestRevertButton({ billing }: { billing: Billing }) {
  const { hasPermission } = useAuth();
  const { toast } = useToast();
  const { data: status } = useInvoiceRevertStatusQuery(billing.id);
  const mutation = useRequestInvoiceRevertMutation();
  const [isOpen, setIsOpen] = useState(false);

  if (!hasPermission('finance-billing.edit') || !status?.canRequest) return null;

  const paidAmount = billing.items.filter((item) => item.paymentStatus === 'Paid').reduce((sum, item) => sum + item.total, 0);

  function handleConfirm(reason: string) {
    mutation.mutate(
      { invoiceId: billing.id, reason },
      {
        onSuccess: () => {
          setIsOpen(false);
          toast({ title: 'Revert request sent', description: 'The Super Admin will approve or reject it.', variant: 'success' });
        },
      },
    );
  }

  return (
    <>
      <Button
        size="sm"
        variant="outline"
        className="gap-1.5"
        onClick={() => {
          mutation.reset();
          setIsOpen(true);
        }}
      >
        <RotateCcw className="h-4 w-4" />
        Request Revert
      </Button>
      {isOpen && (
        <RequestRevertDialog
          invoiceLabel={`Invoice ${billing.invoiceNumber ?? billing.id}`}
          netAmount={billing.netAmount}
          paidAmount={paidAmount}
          isSaving={mutation.isPending}
          error={mutation.error}
          onConfirm={handleConfirm}
          onCancel={() => setIsOpen(false)}
        />
      )}
    </>
  );
}

/** Approve/Reject buttons for a Pending revert request — rendered only for a Super Admin. Shared
 * by the invoice page's banner and the Revert Requests list. */
export function RevertDecisionButtons({ request }: { request: InvoiceRevertRequestResponse }) {
  const { user } = useAuth();
  const { toast } = useToast();
  const approve = useApproveRevertRequestMutation();
  const reject = useRejectRevertRequestMutation();
  const [decision, setDecision] = useState<'approve' | 'reject' | null>(null);

  if (user?.role !== 'superAdmin' || request.status !== 'Pending') return null;

  const mutation = decision === 'reject' ? reject : approve;

  function handleConfirm(notes: string) {
    const onSuccess = () => {
      const approved = decision === 'approve';
      setDecision(null);
      toast({
        title: approved ? `Invoice ${request.invoiceNumber} reverted` : 'Revert request rejected',
        description: approved && request.paidAmount > 0 ? `Refund ${formatCurrency(request.paidAmount)} to ${request.patientName}.` : undefined,
        variant: 'success',
      });
    };
    if (decision === 'reject') reject.mutate({ id: request.id, notes }, { onSuccess });
    else approve.mutate({ id: request.id, notes }, { onSuccess });
  }

  function open(next: 'approve' | 'reject') {
    approve.reset();
    reject.reset();
    setDecision(next);
  }

  return (
    <>
      <div className="flex flex-wrap gap-2">
        <Button size="sm" variant="destructive" onClick={() => open('approve')}>
          Approve
        </Button>
        <Button size="sm" variant="outline" onClick={() => open('reject')}>
          Reject
        </Button>
      </div>
      {decision && (
        <RevertDecisionDialog
          decision={decision}
          request={request}
          isSaving={mutation.isPending}
          error={mutation.error}
          onConfirm={handleConfirm}
          onCancel={() => setDecision(null)}
        />
      )}
    </>
  );
}

/** The invoice page's revert status: a pending request (with the Super Admin's Approve/Reject),
 * who approved a reverted bill, or why the last request was rejected. Nothing when the bill has
 * never had a revert request. */
export function InvoiceRevertBanner({ billing }: { billing: Billing }) {
  const { data: status } = useInvoiceRevertStatusQuery(billing.id);
  const latest = status?.requests[0];
  if (!latest) return null;

  if (latest.status === 'Pending') {
    return (
      <div className="flex flex-wrap items-start justify-between gap-3 rounded-md border border-warning/40 bg-warning/10 px-3 py-2 text-sm">
        <div className="flex min-w-[14rem] flex-1 items-start gap-2">
          <Clock className="mt-0.5 h-4 w-4 shrink-0 text-warning" />
          <div className="flex flex-col">
            <span className="font-medium text-foreground">Revert waiting for Super Admin approval</span>
            <span className="text-muted-foreground">
              Requested{latest.requestedByName ? ` by ${latest.requestedByName}` : ''} on {formatDateTime(latest.requestedAt)}
            </span>
            <span className="text-foreground">Reason: {latest.reason}</span>
          </div>
        </div>
        <RevertDecisionButtons request={latest} />
      </div>
    );
  }

  if (latest.status === 'Approved') {
    return (
      <div className="flex items-start gap-2 rounded-md border border-border bg-muted/30 px-3 py-2 text-sm">
        <CircleCheck className="mt-0.5 h-4 w-4 shrink-0 text-muted-foreground" />
        <span className="text-muted-foreground">
          Revert requested{latest.requestedByName ? ` by ${latest.requestedByName}` : ''} and approved
          {latest.decidedByName ? ` by ${latest.decidedByName}` : ''} on {formatDateTime(latest.decidedAt)}
          {latest.paidAmount > 0 ? ` · ${formatCurrency(latest.paidAmount)} to refund` : ''}
          {latest.decisionNotes ? ` · ${latest.decisionNotes}` : ''}
        </span>
      </div>
    );
  }

  return (
    <div className="flex items-start gap-2 rounded-md border border-border bg-muted/30 px-3 py-2 text-sm">
      <CircleX className="mt-0.5 h-4 w-4 shrink-0 text-muted-foreground" />
      <div className="flex flex-col">
        <span className="font-medium text-foreground">
          Last revert request rejected{latest.decidedByName ? ` by ${latest.decidedByName}` : ''} on {formatDateTime(latest.decidedAt)}
        </span>
        {latest.decisionNotes && <span className="text-muted-foreground">{latest.decisionNotes}</span>}
      </div>
    </div>
  );
}
