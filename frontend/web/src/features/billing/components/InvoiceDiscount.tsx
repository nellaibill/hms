import type { InvoiceDiscountApprovalResponse, InvoiceDiscountRequestResponse, InvoiceDiscountRequestStatus } from '@hms/shared';
import { BadgePercent, CircleCheck, CircleX, Clock, Plus, UserRound, Wallet, X } from 'lucide-react';
import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { useToast } from '@/components/ui/toast-context';
import { useAuth } from '@/features/auth/AuthContext';
import { resolveRecordLabel, useMasterOptionsQuery } from '@/features/masters';
import { describeBillingItem, formatCurrency, toDiscountableLines } from '../billingCalculations';
import { describeDiscountEntry, type DiscountableLine } from '../billingDiscount';
import {
  useApproveDiscountMutation,
  useInvoiceDiscountStatusQuery,
  usePayInvoiceMutation,
  useRejectDiscountMutation,
  useRequestInvoiceDiscountMutation,
} from '../hooks/useInvoiceDiscount';
import { PAYMENT_METHODS, PAYMENT_METHOD_LABELS, type Billing, type BillingItem, type PaymentMethod } from '../types';
import { DiscountDialog, type DiscountDialogResult } from './DiscountDialog';

const textareaClassName =
  'flex w-full rounded-md border border-input bg-background px-3 py-2 text-sm shadow-sm placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2';

const DISCOUNTABLE_TYPES: ReadonlySet<BillingItem['billingType']> = new Set(['Consultation', 'Radiology', 'Laboratory', 'Procedure', 'Injection', 'File']);

function formatDateTime(value: string | null | undefined) {
  return value ? new Date(value).toLocaleString('en-IN') : '';
}

function ErrorLine({ error }: { error: unknown }) {
  if (!error) return null;
  return (
    <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
      {error instanceof Error ? error.message : 'Something went wrong. Please try again.'}
    </p>
  );
}

/** Mirrors the server's InvoiceDiscountRules.CanTakeDiscount — an unpaid OP charge with a price
 * and no discount yet. */
function canTakeDiscount(item: BillingItem) {
  return DISCOUNTABLE_TYPES.has(item.billingType) && item.paymentStatus === 'Pending' && item.discount === 0 && item.quantity * item.unitPrice > 0;
}

/** What's still owed on the bill — every unpaid line's total. */
function amountDue(billing: Billing) {
  return billing.items.filter((item) => item.paymentStatus !== 'Paid').reduce((sum, item) => sum + item.total, 0);
}

/** "Request Discount" on a saved, unpaid bill (ADR-109) — shown when the server says a discount
 * can be raised and the user can make bills. A Super Admin's applies at once; anyone else's goes
 * to the consultant for their fee and the Super Admin for the rest (ADR-111). */
export function RequestDiscountButton({ billing }: { billing: Billing }) {
  const { hasPermission, user } = useAuth();
  const { toast } = useToast();
  const { data: status } = useInvoiceDiscountStatusQuery(billing.id);
  const mutation = useRequestInvoiceDiscountMutation();
  const [isOpen, setIsOpen] = useState(false);
  const isSuperAdmin = user?.role === 'superAdmin';

  if (!hasPermission('finance-billing.create') || !status?.canRequest) return null;

  const lines: DiscountableLine[] = toDiscountableLines(billing.items.filter(canTakeDiscount));

  function handleConfirm(result: DiscountDialogResult) {
    mutation.mutate(
      {
        invoiceId: billing.id,
        type: result.type,
        value: result.value,
        reason: result.reason,
        lines: result.allocations.map((allocation) => ({ lineItemId: allocation.lineId, discount: allocation.amount })),
      },
      {
        onSuccess: () => {
          setIsOpen(false);
          toast({
            title: isSuperAdmin ? 'Discount applied' : 'Discount sent for approval',
            description: isSuperAdmin ? undefined : 'Collect payment once every part of it is approved or rejected.',
            variant: 'success',
          });
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
        <BadgePercent className="h-4 w-4" />
        Request Discount
      </Button>
      {isOpen && (
        <DiscountDialog
          lines={lines}
          billTotal={amountDue(billing)}
          needsApproval={!isSuperAdmin}
          confirmLabel={isSuperAdmin ? 'Apply discount' : 'Send for Approval'}
          isSaving={mutation.isPending}
          error={mutation.error}
          onConfirm={handleConfirm}
          onCancel={() => setIsOpen(false)}
        />
      )}
    </>
  );
}

/** The request's lines, or only one approval part's when `approvalId` is given. */
function DiscountLines({ request, approvalId }: { request: InvoiceDiscountRequestResponse; approvalId?: string }) {
  const lines = approvalId ? request.lines.filter((line) => line.approvalId === approvalId) : request.lines;
  return (
    <ul className="flex flex-col gap-0.5 text-xs">
      {lines.map((line) => {
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
          <li key={line.lineItemId} className="flex justify-between gap-3">
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

/** "Dr. S. Karthikeyan" for a consultant's part, "Super Admin" for the rest (ADR-111). */
function describeDiscountApprover(approval: InvoiceDiscountApprovalResponse): string {
  return approval.approverType === 'Consultant' && approval.consultantId ? resolveRecordLabel('consultant', approval.consultantId) : 'Super Admin';
}

/** A Super Admin may decide any waiting part; a consultant's login only the part for their own fees. */
function useCanDecide() {
  const { user, scopedConsultantId } = useAuth();
  return (approval: InvoiceDiscountApprovalResponse) =>
    approval.status === 'Pending' &&
    (user?.role === 'superAdmin' || (approval.approverType === 'Consultant' && Boolean(scopedConsultantId) && approval.consultantId === scopedConsultantId));
}

interface DiscountDecisionDialogProps {
  decision: 'approve' | 'reject';
  request: InvoiceDiscountRequestResponse;
  approval: InvoiceDiscountApprovalResponse;
  /** A Super Admin deciding a consultant's part for them. */
  onBehalf: boolean;
  isSaving: boolean;
  error: unknown;
  onConfirm: (notes: string) => void;
  onCancel: () => void;
}

/** Approving or rejecting one approval part of a discount request. Rejecting needs a reason. */
function DiscountDecisionDialog({ decision, request, approval, onBehalf, isSaving, error, onConfirm, onCancel }: DiscountDecisionDialogProps) {
  const [notes, setNotes] = useState('');
  const trimmedNotes = notes.trim();
  const isApprove = decision === 'approve';
  const isConsultantPart = approval.approverType === 'Consultant';

  return (
    <Dialog open onOpenChange={(open) => !open && onCancel()}>
      <DialogContent aria-labelledby="discount-decision-title" className="max-w-lg">
        <DialogHeader>
          <DialogTitle id="discount-decision-title">{isApprove ? 'Approve discount?' : 'Reject discount?'}</DialogTitle>
          <DialogDescription>
            <strong className="text-foreground">Invoice {request.invoiceNumber}</strong> for {request.patientName}:{' '}
            {formatCurrency(approval.totalDiscount)} off{isConsultantPart ? ' the consultation fee' : ''}
            {request.discountType === 'Percent' ? ` (${describeDiscountEntry(request.discountType, request.discountValue)})` : ''}.{' '}
            {isApprove ? 'The discount goes on the bill.' : 'These lines stay at their full price.'}
          </DialogDescription>
        </DialogHeader>
        {onBehalf && (
          <p className="flex items-start gap-2 rounded-md border border-warning/40 bg-warning/10 px-3 py-2 text-sm text-foreground">
            <UserRound className="mt-0.5 h-4 w-4 shrink-0 text-warning" />
            This part is for {describeDiscountApprover(approval)} to decide — you’re deciding it on their behalf.
          </p>
        )}
        <div className="flex flex-col gap-2 rounded-md border border-border bg-muted/30 px-3 py-2 text-sm">
          <DiscountLines request={request} approvalId={approval.id} />
          <div>
            <span className="text-muted-foreground">Reason given: </span>
            <span className="text-foreground">{request.reason}</span>
          </div>
        </div>
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="discount-decision-notes">
            {isApprove ? 'Notes' : 'Reason for rejecting'}{' '}
            <span className="font-normal text-muted-foreground">{isApprove ? '(optional)' : '(required)'}</span>
          </Label>
          <textarea
            id="discount-decision-notes"
            rows={2}
            maxLength={500}
            value={notes}
            onChange={(event) => setNotes(event.target.value)}
            className={textareaClassName}
            autoFocus
          />
        </div>
        <ErrorLine error={error} />
        <DialogFooter>
          <Button variant="outline" onClick={onCancel} disabled={isSaving}>
            Cancel
          </Button>
          <Button onClick={() => onConfirm(trimmedNotes)} disabled={isSaving || (!isApprove && trimmedNotes.length === 0)}>
            {isSaving ? 'Saving…' : isApprove ? 'Approve Discount' : 'Reject Discount'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

/** Approve/Reject for one waiting approval part — rendered only for someone who may decide it
 * (the Super Admin, or the consultant whose fee it is). */
export function DiscountDecisionButtons({ request, approval }: { request: InvoiceDiscountRequestResponse; approval: InvoiceDiscountApprovalResponse }) {
  const { scopedConsultantId } = useAuth();
  const canDecide = useCanDecide();
  const { toast } = useToast();
  const approve = useApproveDiscountMutation();
  const reject = useRejectDiscountMutation();
  const [decision, setDecision] = useState<'approve' | 'reject' | null>(null);

  if (!canDecide(approval)) return null;

  const onBehalf = approval.approverType === 'Consultant' && approval.consultantId !== scopedConsultantId;
  const mutation = decision === 'reject' ? reject : approve;

  function handleConfirm(notes: string) {
    const onSuccess = () => {
      const approved = decision === 'approve';
      setDecision(null);
      toast({
        title: approved ? `Discount approved on invoice ${request.invoiceNumber}` : 'Discount rejected',
        description: approved ? `${formatCurrency(approval.totalDiscount)} off for ${request.patientName}.` : undefined,
        variant: 'success',
      });
    };
    if (decision === 'reject') reject.mutate({ approvalId: approval.id, notes }, { onSuccess });
    else approve.mutate({ approvalId: approval.id, notes }, { onSuccess });
  }

  function open(next: 'approve' | 'reject') {
    approve.reset();
    reject.reset();
    setDecision(next);
  }

  return (
    <>
      <div className="flex flex-wrap gap-2">
        <Button size="sm" onClick={() => open('approve')}>
          Approve
        </Button>
        <Button size="sm" variant="outline" onClick={() => open('reject')}>
          Reject
        </Button>
      </div>
      {decision && (
        <DiscountDecisionDialog
          decision={decision}
          request={request}
          approval={approval}
          onBehalf={onBehalf}
          isSaving={mutation.isPending}
          error={mutation.error}
          onConfirm={handleConfirm}
          onCancel={() => setDecision(null)}
        />
      )}
    </>
  );
}

const APPROVAL_STATUS_LABELS: Record<InvoiceDiscountRequestStatus, string> = {
  Pending: 'Waiting',
  Approved: 'Approved',
  Rejected: 'Rejected',
  Cancelled: 'Cancelled',
  PartlyApproved: 'Partly approved',
};

function ApprovalStatusIcon({ status }: { status: InvoiceDiscountRequestStatus }) {
  if (status === 'Pending') return <Clock className="mt-0.5 h-4 w-4 shrink-0 text-warning" />;
  if (status === 'Approved') return <CircleCheck className="mt-0.5 h-4 w-4 shrink-0 text-success" />;
  return <CircleX className="mt-0.5 h-4 w-4 shrink-0 text-muted-foreground" />;
}

/** Each approver's part of a discount request (ADR-111): who decides it, its amount, its status
 * or decision, and Approve/Reject for whoever may decide it. With `showLines`, each part lists
 * its lines. `onlyConsultantId` narrows it to one consultant's part — a consultant's own tab. */
export function DiscountApprovalParts({
  request,
  showLines = false,
  onlyConsultantId,
}: {
  request: InvoiceDiscountRequestResponse;
  showLines?: boolean;
  onlyConsultantId?: string;
}) {
  const approvals = onlyConsultantId ? request.approvals.filter((approval) => approval.consultantId === onlyConsultantId) : request.approvals;
  return (
    <ul className="flex flex-col gap-2">
      {approvals.map((approval) => (
        <li key={approval.id} className="flex flex-wrap items-start justify-between gap-2">
          <div className="flex min-w-[12rem] flex-1 items-start gap-2">
            <ApprovalStatusIcon status={approval.status} />
            <div className="flex flex-1 flex-col gap-0.5">
              <span className="text-sm text-foreground">
                <span className="font-medium">{describeDiscountApprover(approval)}</span>
                {approval.approverType === 'Consultant' ? ' (consultation fee)' : ''} · {formatCurrency(approval.totalDiscount)} ·{' '}
                {APPROVAL_STATUS_LABELS[approval.status]}
              </span>
              {approval.status !== 'Pending' && (approval.decidedByName || approval.decidedAt) && (
                <span className="text-xs text-muted-foreground">
                  {approval.decidedByName ? `by ${approval.decidedByName}` : ''}
                  {approval.decidedOnBehalf ? ' on their behalf' : ''}
                  {approval.decidedAt ? ` · ${formatDateTime(approval.decidedAt)}` : ''}
                  {approval.decisionNotes ? ` · ${approval.decisionNotes}` : ''}
                </span>
              )}
              {showLines && <DiscountLines request={request} approvalId={approval.id} />}
            </div>
          </div>
          <DiscountDecisionButtons request={request} approval={approval} />
        </li>
      ))}
    </ul>
  );
}

/** The invoice page's discount status: who still has to decide a waiting request (with
 * Approve/Reject for whoever may), or how it was decided. Nothing when the bill never had a
 * discount request. */
export function InvoiceDiscountBanner({ billing }: { billing: Billing }) {
  const { data: status } = useInvoiceDiscountStatusQuery(billing.id);
  // Names each consultant's part.
  useMasterOptionsQuery('consultant');
  const latest = status?.requests[0];
  if (!latest || latest.status === 'Cancelled') return null;

  const requested = (
    <span className="text-muted-foreground">
      Requested{latest.requestedByName ? ` by ${latest.requestedByName}` : ''} on {formatDateTime(latest.requestedAt)} · Reason: {latest.reason}
    </span>
  );

  if (latest.status === 'Pending') {
    return (
      <div className="flex flex-col gap-2 rounded-md border border-warning/40 bg-warning/10 px-3 py-2 text-sm">
        <div className="flex items-start gap-2">
          <Clock className="mt-0.5 h-4 w-4 shrink-0 text-warning" />
          <div className="flex flex-1 flex-col gap-1">
            <span className="font-medium text-foreground">
              Discount of {formatCurrency(latest.totalDiscount)} waiting for approval — collect payment once every part is decided
            </span>
            {requested}
          </div>
        </div>
        <DiscountApprovalParts request={latest} showLines />
      </div>
    );
  }

  const heading =
    latest.status === 'Approved'
      ? `Discount of ${formatCurrency(latest.totalDiscount)} approved`
      : latest.status === 'PartlyApproved'
        ? `${formatCurrency(latest.approvedDiscount)} of the ${formatCurrency(latest.totalDiscount)} discount approved — the rest stays at full price`
        : 'Discount request rejected — collect the full amount';

  return (
    <div className="flex flex-col gap-2 rounded-md border border-border bg-muted/30 px-3 py-2 text-sm">
      <div className="flex items-start gap-2">
        <ApprovalStatusIcon status={latest.status === 'PartlyApproved' ? 'Approved' : latest.status} />
        <div className="flex flex-1 flex-col gap-1">
          <span className="font-medium text-foreground">{heading}</span>
          {requested}
        </div>
      </div>
      <DiscountApprovalParts request={latest} />
    </div>
  );
}

interface PaymentRow {
  method: PaymentMethod | '';
  amount: string;
  referenceNumber: string;
}

/** Collects everything still owed on a bill in one go, split across methods if needed — the same
 * rules as paying while billing: the rows must add up to exactly the amount due. A fully
 * discounted bill (₹0 due) is settled with no rows. */
function CollectPaymentDialog({ billing, onPaid, onCancel }: { billing: Billing; onPaid: (updated: Billing) => void; onCancel: () => void }) {
  const due = amountDue(billing);
  const mutation = usePayInvoiceMutation();
  const [rows, setRows] = useState<PaymentRow[]>(due > 0 ? [{ method: '', amount: String(due), referenceNumber: '' }] : []);

  const total = rows.reduce((sum, row) => sum + (Number(row.amount) || 0), 0);
  const problem =
    due === 0
      ? null
      : rows.some((row) => !row.method)
        ? 'Pick a payment mode for every row.'
        : total !== due
          ? `The amounts must add up to ${formatCurrency(due)} (now ${formatCurrency(total)}).`
          : null;

  function update(index: number, patch: Partial<PaymentRow>) {
    setRows((previous) => previous.map((row, i) => (i === index ? { ...row, ...patch } : row)));
  }

  function handleConfirm() {
    if (problem) return;
    mutation.mutate(
      {
        billingId: billing.id,
        payments: rows.map((row) => ({ method: row.method as PaymentMethod, amount: Number(row.amount), referenceNumber: row.referenceNumber || null })),
      },
      { onSuccess: onPaid },
    );
  }

  return (
    <Dialog open onOpenChange={(open) => !open && onCancel()}>
      <DialogContent aria-labelledby="collect-payment-title" className="max-h-[90vh] max-w-lg overflow-y-auto">
        <DialogHeader>
          <DialogTitle id="collect-payment-title">Collect payment</DialogTitle>
          <DialogDescription>
            Invoice {billing.invoiceNumber ?? billing.id} — {formatCurrency(due)} due
            {billing.totalDiscount > 0 ? ` after a ${formatCurrency(billing.totalDiscount)} discount` : ''}.
          </DialogDescription>
        </DialogHeader>

        {due === 0 ? (
          <p className="text-sm text-muted-foreground">Nothing is owed on this bill — confirm to mark it settled.</p>
        ) : (
          <div className="flex flex-col gap-3">
            {rows.map((row, index) => (
              <div key={index} className="grid grid-cols-1 gap-2 rounded-md border border-border p-3 sm:grid-cols-2">
                <div className="flex flex-col gap-1">
                  <Label htmlFor={`collect-${index}-amount`}>Amount (₹)</Label>
                  <Input
                    id={`collect-${index}-amount`}
                    type="number"
                    min={0}
                    inputMode="decimal"
                    value={row.amount}
                    onChange={(event) => update(index, { amount: event.target.value })}
                  />
                </div>
                <div className="flex flex-col gap-1">
                  <div className="flex items-center justify-between">
                    <Label htmlFor={`collect-${index}-mode`}>Payment Mode</Label>
                    {rows.length > 1 && (
                      <Button
                        type="button"
                        variant="ghost"
                        size="icon"
                        aria-label="Remove this payment method"
                        className="h-6 w-6"
                        onClick={() => setRows((previous) => previous.filter((_, i) => i !== index))}
                      >
                        <X className="h-3.5 w-3.5" />
                      </Button>
                    )}
                  </div>
                  <Select value={row.method} onValueChange={(value) => update(index, { method: value as PaymentMethod, referenceNumber: value === 'Cash' ? '' : row.referenceNumber })}>
                    <SelectTrigger id={`collect-${index}-mode`}>
                      <SelectValue placeholder="Select payment mode" />
                    </SelectTrigger>
                    <SelectContent>
                      {PAYMENT_METHODS.map((option) => (
                        <SelectItem key={option} value={option}>
                          {PAYMENT_METHOD_LABELS[option]}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
                {row.method && row.method !== 'Cash' && (
                  <div className="flex flex-col gap-1 sm:col-span-2">
                    <Label htmlFor={`collect-${index}-reference`}>Reference / Transaction No. (Optional)</Label>
                    <Input
                      id={`collect-${index}-reference`}
                      value={row.referenceNumber}
                      onChange={(event) => update(index, { referenceNumber: event.target.value })}
                    />
                  </div>
                )}
              </div>
            ))}
            <Button
              type="button"
              variant="outline"
              size="sm"
              className="w-full gap-1.5"
              onClick={() => setRows((previous) => [...previous, { method: '', amount: String(Math.max(due - total, 0)), referenceNumber: '' }])}
            >
              <Plus className="h-4 w-4" />
              Add another payment method
            </Button>
            {problem && <p className="text-sm text-destructive">{problem}</p>}
          </div>
        )}

        <ErrorLine error={mutation.error} />
        <DialogFooter>
          <Button variant="outline" onClick={onCancel} disabled={mutation.isPending}>
            Cancel
          </Button>
          <Button onClick={handleConfirm} disabled={mutation.isPending || Boolean(problem)}>
            {mutation.isPending ? 'Collecting…' : due === 0 ? 'Mark Settled' : `Collect ${formatCurrency(due)}`}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

/** "Collect Payment" for everything still owed on a saved bill — needs only "finance-billing.create",
 * like taking payment while billing, so the front desk can collect once a discount is decided.
 * Hidden while a discount waits for approval, or once the bill is paid or voided. */
export function CollectPaymentButton({ billing, onPaid }: { billing: Billing; onPaid?: (updated: Billing) => void }) {
  const { hasPermission } = useAuth();
  const { toast } = useToast();
  const [isOpen, setIsOpen] = useState(false);
  const hasUnpaid = billing.items.some((item) => item.paymentStatus !== 'Paid');

  if (!hasPermission('finance-billing.create') || billing.isVoided || !hasUnpaid || billing.discountStatus === 'Pending') return null;

  return (
    <>
      <Button size="sm" className="gap-1.5" onClick={() => setIsOpen(true)}>
        <Wallet className="h-4 w-4" />
        Collect Payment
      </Button>
      {isOpen && (
        <CollectPaymentDialog
          billing={billing}
          onCancel={() => setIsOpen(false)}
          onPaid={(updated) => {
            setIsOpen(false);
            toast({
              title: updated.opNumber ? `Payment collected — OP No ${updated.opNumber}` : 'Payment collected',
              description: `${updated.patientName} (${updated.patientUhid})`,
              variant: 'success',
            });
            onPaid?.(updated);
          }}
        />
      )}
    </>
  );
}
