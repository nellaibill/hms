import type { InvoiceDiscountType } from '@hms/shared';
import { ShieldCheck } from 'lucide-react';
import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { cn } from '@/lib/utils';
import { allocateDiscount, type DiscountAllocation, type DiscountableLine } from '../billingDiscount';
import { formatCurrency } from '../billingCalculations';

const textareaClassName =
  'flex w-full rounded-md border border-input bg-background px-3 py-2 text-sm shadow-sm placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2';

/** Bill line | Charge | Discount (₹) | Net — the last three collapse under the line on a phone. */
const rowGridClassName = 'grid grid-cols-[minmax(0,1fr)_7rem] items-center gap-x-3 px-3 py-2 sm:grid-cols-[minmax(0,1fr)_6.5rem_8rem_6.5rem]';

export interface DiscountDialogResult {
  /** Always 'Amount' — each line's discount is a ₹ figure and `value` is their total. */
  type: InvoiceDiscountType;
  value: number;
  reason: string;
  /** Each discounted line's ₹ discount, in whole rupees; lines left at zero are left out. */
  allocations: DiscountAllocation[];
}

interface DiscountDialogProps {
  /** The lines that can take a discount. */
  lines: DiscountableLine[];
  /** The bill's whole net amount before this discount — lines that can't be discounted included. */
  billTotal: number;
  /** A discount already on the form — its lines' amounts are filled back in. */
  initial?: { reason: string; allocations: DiscountAllocation[] };
  /** True for anyone but a Super Admin — the discount then waits for approval. */
  needsApproval: boolean;
  confirmLabel: string;
  isSaving?: boolean;
  error?: unknown;
  onConfirm: (result: DiscountDialogResult) => void;
  /** Shown as "Remove discount" when a discount is already applied to the form. */
  onRemove?: () => void;
  onCancel: () => void;
}

/** Blank reads as no discount; anything that isn't a number reads as NaN. */
function parseAmount(text: string | undefined): number {
  return !text || text.trim() === '' ? 0 : Number(text);
}

function isValidAmount(amount: number): boolean {
  return Number.isInteger(amount) && amount >= 0;
}

function toAmountText(amount: number): string {
  return amount > 0 ? String(amount) : '';
}

function sumOfLines(lines: DiscountableLine[], texts: Record<string, string>): number {
  return lines.reduce((sum, line) => {
    const amount = parseAmount(texts[line.id]);
    return isValidAmount(amount) ? sum + amount : sum;
  }, 0);
}

/**
 * "Add discount" (ADR-109) — a ₹ discount against each bill line (consultation, radiology,
 * laboratory, procedure, injection…), and a reason. When the bill has more than one line, a
 * "Whole bill" row takes one amount and splits it across the lines in proportion to their
 * charges, after which any line can still be changed; the Whole bill figure always shows the lines'
 * total. Used both while making a bill (BillingSummaryCard) and on a saved, unpaid bill
 * (RequestDiscountButton).
 */
export function DiscountDialog({ lines, billTotal, initial, needsApproval, confirmLabel, isSaving, error, onConfirm, onRemove, onCancel }: DiscountDialogProps) {
  const [lineTexts, setLineTexts] = useState<Record<string, string>>(() => {
    const initialById = new Map(initial?.allocations.map((allocation) => [allocation.lineId, allocation.amount]));
    return Object.fromEntries(lines.map((line) => [line.id, toAmountText(initialById.get(line.id) ?? 0)]));
  });
  const [wholeText, setWholeText] = useState(() => toAmountText(sumOfLines(lines, lineTexts)));
  const [reason, setReason] = useState(initial?.reason ?? '');
  const [attempted, setAttempted] = useState(false);

  const showWholeBill = lines.length > 1;
  const linesTotal = lines.reduce((sum, line) => sum + line.amount, 0);
  const entries = lines.map((line) => {
    const amount = parseAmount(lineTexts[line.id]);
    const problem = !isValidAmount(amount)
      ? `Enter the discount for ${line.label} in whole rupees.`
      : amount > line.amount
        ? `The discount for ${line.label} can’t be more than its charge (${formatCurrency(line.amount)}).`
        : null;
    return { line, amount: problem ? 0 : amount, problem };
  });
  const wholeAmount = parseAmount(wholeText);
  const totalDiscount = entries.reduce((sum, entry) => sum + entry.amount, 0);
  const trimmedReason = reason.trim();

  const problem =
    entries.find((entry) => entry.problem)?.problem ??
    (!isValidAmount(wholeAmount)
      ? 'Enter the whole-bill discount in whole rupees.'
      : wholeAmount > linesTotal
        ? `The whole-bill discount can’t be more than the bill’s total (${formatCurrency(linesTotal)}).`
        : totalDiscount === 0
          ? showWholeBill
            ? 'Enter a discount against at least one line, or for the whole bill.'
            : 'Enter the discount amount.'
          : trimmedReason.length === 0
            ? 'A reason is required — the Super Admin decides on it.'
            : null);

  function handleWholeChange(text: string) {
    setWholeText(text);
    const amount = parseAmount(text);
    const shares = new Map(allocateDiscount(lines, isValidAmount(amount) ? amount : 0).map((share) => [share.lineId, share.amount]));
    setLineTexts(Object.fromEntries(lines.map((line) => [line.id, toAmountText(shares.get(line.id) ?? 0)])));
  }

  function handleLineChange(id: string, text: string) {
    const next = { ...lineTexts, [id]: text };
    setLineTexts(next);
    setWholeText(toAmountText(sumOfLines(lines, next)));
  }

  function handleConfirm() {
    setAttempted(true);
    if (problem) return;
    onConfirm({
      type: 'Amount',
      value: totalDiscount,
      reason: trimmedReason,
      allocations: entries.filter((entry) => entry.amount > 0).map((entry) => ({ lineId: entry.line.id, amount: entry.amount })),
    });
  }

  return (
    <Dialog open onOpenChange={(open) => !open && onCancel()}>
      <DialogContent
        aria-labelledby="discount-dialog-title"
        className="flex max-h-[90vh] w-[calc(100vw-2rem)] max-w-3xl flex-col gap-4 overflow-y-auto"
      >
        <DialogHeader>
          <DialogTitle id="discount-dialog-title">Add discount</DialogTitle>
          <DialogDescription>
            {showWholeBill
              ? 'Enter a ₹ discount against each bill line, or one amount for the whole bill, and the reason.'
              : 'Enter the ₹ discount and the reason.'}
          </DialogDescription>
        </DialogHeader>

        <div className="flex flex-col gap-2">
          <div className="flex flex-col divide-y divide-border rounded-md border border-border text-sm" role="table" aria-label="Discount by bill line">
            <div role="row" className={cn(rowGridClassName, 'hidden bg-muted/50 text-xs font-medium text-muted-foreground sm:grid')}>
              <span role="columnheader">Bill line</span>
              <span role="columnheader" className="text-right">
                Charge
              </span>
              <span role="columnheader" className="text-right">
                Discount (₹)
              </span>
              <span role="columnheader" className="text-right">
                Net
              </span>
            </div>

            {showWholeBill && (
              <DiscountRow
                id="discount-whole-bill"
                label={`Whole bill (${lines.length} lines)`}
                charge={linesTotal}
                value={wholeText}
                net={linesTotal - totalDiscount}
                invalid={!isValidAmount(wholeAmount) || wholeAmount > linesTotal}
                onChange={handleWholeChange}
                emphasis
                autoFocus
              />
            )}
            {entries.map((entry, index) => (
              <DiscountRow
                key={entry.line.id}
                id={`discount-line-${index}`}
                label={entry.line.label}
                charge={entry.line.amount}
                value={lineTexts[entry.line.id] ?? ''}
                net={entry.line.amount - entry.amount}
                invalid={entry.problem !== null}
                onChange={(text) => handleLineChange(entry.line.id, text)}
                autoFocus={!showWholeBill && index === 0}
              />
            ))}
          </div>
          {showWholeBill && (
            <p className="text-xs text-muted-foreground">
              An amount against <span className="font-medium text-foreground">Whole bill</span> is shared across the lines in proportion to
              their charges — change any line afterwards if needed.
            </p>
          )}
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="discount-reason">
            Reason <span className="font-normal text-muted-foreground">(required)</span>
          </Label>
          <textarea
            id="discount-reason"
            rows={2}
            maxLength={500}
            value={reason}
            onChange={(event) => setReason(event.target.value)}
            className={textareaClassName}
            placeholder="e.g. Staff family, senior citizen, financial hardship"
          />
        </div>

        <div className="flex flex-col gap-1 rounded-md border border-border bg-muted/30 px-3 py-2 text-sm">
          <div className="flex items-center justify-between gap-3">
            <span className="text-muted-foreground">Bill amount</span>
            <span className="text-foreground">{formatCurrency(billTotal)}</span>
          </div>
          <div className="flex items-center justify-between gap-3">
            <span className="text-muted-foreground">Discount</span>
            <span className="font-medium text-destructive">{totalDiscount > 0 ? `− ${formatCurrency(totalDiscount)}` : formatCurrency(0)}</span>
          </div>
          <div className="flex items-center justify-between gap-3 border-t border-border pt-1">
            <span className="font-semibold text-foreground">Net payable{needsApproval ? ' after approval' : ''}</span>
            <span className="font-bold text-primary">{formatCurrency(Math.max(billTotal - totalDiscount, 0))}</span>
          </div>
        </div>

        {needsApproval && (
          <p className="flex items-start gap-2 rounded-md border border-warning/40 bg-warning/10 px-3 py-2 text-sm text-foreground">
            <ShieldCheck className="mt-0.5 h-4 w-4 shrink-0 text-warning" />
            A discount needs Super Admin approval. The bill is saved without payment and sent for approval; collect payment once it’s
            approved or rejected.
          </p>
        )}

        {attempted && problem && (
          <p role="alert" className="text-sm text-destructive">
            {problem}
          </p>
        )}
        {Boolean(error) && (
          <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
            {error instanceof Error ? error.message : 'Something went wrong. Please try again.'}
          </p>
        )}

        <DialogFooter className="mt-1 gap-2 sm:justify-between">
          {onRemove ? (
            <Button variant="ghost" className="text-destructive hover:text-destructive" onClick={onRemove} disabled={isSaving}>
              Remove discount
            </Button>
          ) : (
            <span />
          )}
          <div className="flex flex-col-reverse gap-2 sm:flex-row">
            <Button variant="outline" onClick={onCancel} disabled={isSaving}>
              Cancel
            </Button>
            <Button onClick={handleConfirm} disabled={isSaving}>
              {isSaving ? 'Saving…' : confirmLabel}
            </Button>
          </div>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

interface DiscountRowProps {
  id: string;
  label: string;
  charge: number;
  value: string;
  net: number;
  invalid: boolean;
  onChange: (text: string) => void;
  /** The Whole bill row — shaded and bold. */
  emphasis?: boolean;
  autoFocus?: boolean;
}

function DiscountRow({ id, label, charge, value, net, invalid, onChange, emphasis, autoFocus }: DiscountRowProps) {
  return (
    <div role="row" className={cn(rowGridClassName, emphasis && 'bg-muted/30')}>
      <div role="cell" className="flex min-w-0 flex-col">
        <label htmlFor={id} className={cn('break-words text-foreground', emphasis && 'font-medium')}>
          {label}
        </label>
        <span className="text-xs text-muted-foreground sm:hidden">
          {formatCurrency(charge)} → {formatCurrency(Math.max(net, 0))}
        </span>
      </div>
      <span role="cell" className="hidden text-right text-muted-foreground sm:block">
        {formatCurrency(charge)}
      </span>
      <div role="cell">
        <Input
          id={id}
          type="number"
          min={0}
          max={charge}
          step={1}
          inputMode="numeric"
          value={value}
          onChange={(event) => onChange(event.target.value)}
          placeholder="0"
          aria-invalid={invalid || undefined}
          className={cn('h-9 text-right', invalid && 'border-destructive focus-visible:ring-destructive')}
          autoFocus={autoFocus}
        />
      </div>
      <span role="cell" className={cn('hidden text-right sm:block', emphasis ? 'font-semibold text-foreground' : 'font-medium text-foreground')}>
        {formatCurrency(Math.max(net, 0))}
      </span>
    </div>
  );
}
