import type { InvoiceDiscountRequestResponse } from '@hms/shared';
import { BadgePercent, CircleCheck, CircleX, Clock, Plus, Wallet, X } from 'lucide-react';
import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { useToast } from '@/components/ui/toast-context';
import { useAuth } from '@/features/auth/AuthContext';
import { describeBillingItem, formatCurrency } from '../billingCalculations';
import { describeDiscountEntry, type DiscountableLine } from '../billingDiscount';
import {
  useApproveDiscountRequestMutation,
  useInvoiceDiscountStatusQuery,
  usePayInvoiceMutation,
  useRejectDiscountRequestMutation,
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
 * can be raised and the user can make bills. A Super Admin's applies at once. */
export function RequestDiscountButton({ billing }: { billing: Billing }) {
  const { hasPermission, user } = useAuth();
  const { toast } = useToast();
  const { data: status } = useInvoiceDiscountStatusQuery(billing.id);
  const mutation = useRequestInvoiceDiscountMutation();
  const [isOpen, setIsOpen] = useState(false);
  const isSuperAdmin = user?.role === 'superAdmin';

  if (!hasPermission('finance-billing.create') || !status?.canRequest) return null;

  const lines: DiscountableLine[] = billing.items.filter(canTakeDiscount).map((item) => ({
    id: item.id,
    label: `${item.billingType} — ${describeBillingItem(item).serviceLabel}`,
    amount: item.quantity * item.unitPrice,
  }));

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
            description: isSuperAdmin ? undefined : 'Collect payment once the Super Admin approves or rejects it.',
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

function DiscountLines({ request }: { request: InvoiceDiscountRequestResponse }) {
  return (
    <ul className="flex flex-col gap-0.5 text-xs">
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

interface DiscountDecisionDialogProps {
  decision: 'approve' | 'reject';
  request: InvoiceDiscountRequestResponse;
  isSaving: boolean;
  error: unknown;
  onConfirm: (notes: string) => void;
  onCancel: () => void;
}

/** A Super Admin's approve/reject of a discount request. Rejecting needs a reason. */
function DiscountDecisionDialog({ decision, request, isSaving, error, onConfirm, onCancel }: DiscountDecisionDialogProps) {
  const [notes, setNotes] = useState('');
  const trimmedNotes = notes.trim();
  const isApprove = decision === 'approve';

  return (
    <Dialog open onOpenChange={(open) => !open && onCancel()}>
      <DialogContent aria-labelledby="discount-decision-title" className="max-w-lg">
        <DialogHeader>
          <DialogTitle id="discount-decision-title">{isApprove ? 'Approve discount?' : 'Reject discount request?'}</DialogTitle>
          <DialogDescription>
            <strong className="text-foreground">Invoice {request.invoiceNumber}</strong> for {request.patientName}:{' '}
            {formatCurrency(request.totalDiscount)} off ({describeDiscountEntry(request.discountType, request.discountValue)}).{' '}
            {isApprove ? 'The discount goes on the bill and the counter collects the reduced amount.' : 'The bill stays at its full price.'}
          </DialogDescription>
        </DialogHeader>
        <div className="flex flex-col gap-2 rounded-md border border-border bg-muted/30 px-3 py-2 text-sm">
          <DiscountLines request={request} />
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
            {isSaving ? 'Saving…' : isApprove ? 'Approve Discount' : 'Reject Request'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

/** Approve/Reject buttons for a Pending discount request — rendered only for a Super Admin.
 * Shared by the invoice page's banner and the Discount Requests list. */
export function DiscountDecisionButtons({ request }: { request: InvoiceDiscountRequestResponse }) {
  const { user } = useAuth();
  const { toast } = useToast();
  const approve = useApproveDiscountRequestMutation();
  const reject = useRejectDiscountRequestMutation();
  const [decision, setDecision] = useState<'approve' | 'reject' | null>(null);

  if (user?.role !== 'superAdmin' || request.status !== 'Pending') return null;

  const mutation = decision === 'reject' ? reject : approve;

  function handleConfirm(notes: string) {
    const onSuccess = () => {
      const approved = decision === 'approve';
      setDecision(null);
      toast({
        title: approved ? `Discount approved on invoice ${request.invoiceNumber}` : 'Discount request rejected',
        description: approved ? `${formatCurrency(request.totalDiscount)} off for ${request.patientName}.` : undefined,
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
          isSaving={mutation.isPending}
          error={mutation.error}
          onConfirm={handleConfirm}
          onCancel={() => setDecision(null)}
        />
      )}
    </>
  );
}

/** The invoice page's discount status: a pending request (with the Super Admin's Approve/Reject),
 * who approved the discount, or why the last request was rejected. Nothing when the bill never had
 * a discount request. */
export function InvoiceDiscountBanner({ billing }: { billing: Billing }) {
  const { data: status } = useInvoiceDiscountStatusQuery(billing.id);
  const latest = status?.requests[0];
  if (!latest || latest.status === 'Cancelled') return null;

  if (latest.status === 'Pending') {
    return (
      <div className="flex flex-wrap items-start justify-between gap-3 rounded-md border border-warning/40 bg-warning/10 px-3 py-2 text-sm">
        <div className="flex min-w-[14rem] flex-1 items-start gap-2">
          <Clock className="mt-0.5 h-4 w-4 shrink-0 text-warning" />
          <div className="flex flex-1 flex-col gap-1">
            <span className="font-medium text-foreground">
              Discount of {formatCurrency(latest.totalDiscount)} waiting for Super Admin approval — collect payment once it’s decided
            </span>
            <span className="text-muted-foreground">
              Requested{latest.requestedByName ? ` by ${latest.requestedByName}` : ''} on {formatDateTime(latest.requestedAt)} · Reason: {latest.reason}
            </span>
            <DiscountLines request={latest} />
          </div>
        </div>
        <DiscountDecisionButtons request={latest} />
      </div>
    );
  }

  if (latest.status === 'Approved') {
    return (
      <div className="flex items-start gap-2 rounded-md border border-border bg-muted/30 px-3 py-2 text-sm">
        <CircleCheck className="mt-0.5 h-4 w-4 shrink-0 text-success" />
        <span className="text-muted-foreground">
          Discount of {formatCurrency(latest.totalDiscount)} approved{latest.decidedByName ? ` by ${latest.decidedByName}` : ''} on{' '}
          {formatDateTime(latest.decidedAt)} · {latest.reason}
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
          Discount request rejected{latest.decidedByName ? ` by ${latest.decidedByName}` : ''} on {formatDateTime(latest.decidedAt)} — collect the
          full amount
        </span>
        {latest.decisionNotes && <span className="text-muted-foreground">{latest.decisionNotes}</span>}
      </div>
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
