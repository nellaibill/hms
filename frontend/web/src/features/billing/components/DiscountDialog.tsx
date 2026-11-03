import type { InvoiceDiscountType } from '@hms/shared';
import { ShieldCheck } from 'lucide-react';
import { useState } from 'react';
import { SuggestionTextarea } from '@/components/SuggestionTextarea';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { mergeSuggestions } from '@/lib/suggestions';
import { cn } from '@/lib/utils';
import {
  allocateDiscount,
  COMMON_DISCOUNT_REASONS,
  describeDiscountApprovers,
  resolveDiscountEntry,
  toPercentText,
  type DiscountAllocation,
  type DiscountableLine,
  type DiscountEntry,
} from '../billingDiscount';
import { formatCurrency } from '../billingCalculations';
import { useDiscountReasonSuggestionsQuery } from '../hooks/useInvoiceDiscount';

/** Bill line | Charge | Discount % | Discount ₹ | Net — Charge and Net collapse under the line
 * on a phone. */
const rowGridClassName =
  'grid grid-cols-[minmax(0,1fr)_4rem_5rem] items-center gap-x-2 px-3 py-2 sm:grid-cols-[minmax(0,1fr)_6rem_5.5rem_7rem_6.5rem] sm:gap-x-3';

export interface DiscountDialogResult {
  /** 'Percent' when every discounted line took the same typed % (30% on the whole bill, say) —
   * `value` is then that %; otherwise 'Amount' and `value` is the lines' ₹ total. */
  type: InvoiceDiscountType;
  value: number;
  reason: string;
  /** Each discounted line's ₹ discount, in whole rupees; lines left at zero are left out. This
   * is what the bill takes, however the discount was typed. */
  allocations: DiscountAllocation[];
}

interface DiscountDialogProps {
  /** The lines that can take a discount. */
  lines: DiscountableLine[];
  /** The bill's whole net amount before this discount — lines that can't be discounted included. */
  billTotal: number;
  /** A discount already on the form — its lines' amounts, or its %, are filled back in. */
  initial?: { type: InvoiceDiscountType; value: number; reason: string; allocations: DiscountAllocation[] };
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

const NO_DISCOUNT: DiscountEntry = { mode: 'amount', text: '' };

function toAmountText(amount: number): string {
  return amount > 0 ? String(amount) : '';
}

/** Holds what's typed to a sensible figure as it's typed: a ₹ amount above `max` is cut back to it
 * (a discount can never be more than the charge it comes off — whole rupees, so a ₹99.50 charge
 * caps at ₹99), and a % to 100 with two decimals at most. */
function capEntry(entry: DiscountEntry, max: number): DiscountEntry {
  const value = Number(entry.text);
  if (entry.mode === 'percent') {
    return { mode: 'percent', text: Number.isFinite(value) && value > 100 ? '100' : entry.text.replace(/(\.\d{2})\d+$/, '$1') };
  }
  return { mode: 'amount', text: Number.isFinite(value) && value > max ? String(Math.floor(max)) : entry.text };
}

function sumOfLines(lines: DiscountableLine[], entries: Record<string, DiscountEntry>): number {
  return lines.reduce((sum, line) => {
    const amount = resolveDiscountEntry(entries[line.id] ?? NO_DISCOUNT, line.amount);
    return Number.isNaN(amount) ? sum : sum + amount;
  }, 0);
}

/**
 * "Add discount" (ADR-109) — a discount against each bill line (consultation, radiology,
 * laboratory, procedure, injection…), typed as a % of the line's charge or a ₹ amount (ADR-113),
 * and a reason. Whichever box is typed in, the other shows what it comes to. When the bill has more
 * than one line, a "Whole bill" row takes one % — the same % off every line — or one ₹ amount,
 * split across the lines in proportion to their charges; any line can still be changed afterwards,
 * and the Whole bill row then shows the lines' total. Used both while making a bill
 * (BillingSummaryCard) and on a saved, unpaid bill (RequestDiscountButton).
 */
export function DiscountDialog({ lines, billTotal, initial, needsApproval, confirmLabel, isSaving, error, onConfirm, onRemove, onCancel }: DiscountDialogProps) {
  const [lineEntries, setLineEntries] = useState<Record<string, DiscountEntry>>(() => {
    const initialById = new Map(initial?.allocations.map((allocation) => [allocation.lineId, allocation.amount]));
    return Object.fromEntries(
      lines.map((line): [string, DiscountEntry] => {
        const amount = initialById.get(line.id) ?? 0;
        // A % discount comes back as that % on each line it covered.
        return [line.id, initial?.type === 'Percent' && amount > 0 ? { mode: 'percent', text: String(initial.value) } : { mode: 'amount', text: toAmountText(amount) }];
      }),
    );
  });
  const [wholeEntry, setWholeEntry] = useState<DiscountEntry>(() =>
    initial?.type === 'Percent' && lines.every((line) => lineEntries[line.id]?.mode === 'percent')
      ? { mode: 'percent', text: String(initial.value) }
      : { mode: 'amount', text: toAmountText(sumOfLines(lines, lineEntries)) },
  );
  const [reason, setReason] = useState(initial?.reason ?? '');
  const { data: usedReasons } = useDiscountReasonSuggestionsQuery();
  const reasonSuggestions = mergeSuggestions(usedReasons?.map((suggestion) => suggestion.reason) ?? [], COMMON_DISCOUNT_REASONS);
  const [attempted, setAttempted] = useState(false);

  const showWholeBill = lines.length > 1;
  const linesTotal = lines.reduce((sum, line) => sum + line.amount, 0);
  const entries = lines.map((line) => {
    const entry = lineEntries[line.id] ?? NO_DISCOUNT;
    const amount = resolveDiscountEntry(entry, line.amount);
    const problem = Number.isNaN(amount)
      ? entry.mode === 'percent'
        ? `Enter the discount for ${line.label} as a % from 0 to 100.`
        : `Enter the discount for ${line.label} in whole rupees.`
      : amount > line.amount
        ? `The discount for ${line.label} can’t be more than its charge (${formatCurrency(line.amount)}).`
        : null;
    return { line, entry, amount: problem ? 0 : amount, problem };
  });
  const wholeAmount = resolveDiscountEntry(wholeEntry, linesTotal);
  const totalDiscount = entries.reduce((sum, entry) => sum + entry.amount, 0);
  const trimmedReason = reason.trim();
  const discounted = entries.filter((entry) => entry.amount > 0);
  const discountedLines = discounted.map((entry) => entry.line);
  // Recorded as a % only when it really was one: every discounted line typed as the same %.
  const sharedPercent =
    discounted.length > 0 && discounted.every((entry) => entry.entry.mode === 'percent' && Number(entry.entry.text) === Number(discounted[0].entry.text))
      ? Number(discounted[0].entry.text)
      : null;

  const problem =
    entries.find((entry) => entry.problem)?.problem ??
    (Number.isNaN(wholeAmount)
      ? wholeEntry.mode === 'percent'
        ? 'Enter the whole-bill discount as a % from 0 to 100.'
        : 'Enter the whole-bill discount in whole rupees.'
      : wholeAmount > linesTotal
        ? `The whole-bill discount can’t be more than the bill’s total (${formatCurrency(linesTotal)}).`
        : totalDiscount === 0
          ? showWholeBill
            ? 'Enter a discount against at least one line, or for the whole bill.'
            : 'Enter the discount as a % or a ₹ amount.'
          : trimmedReason.length === 0
            ? 'A reason is required — the Super Admin decides on it.'
            : null);

  function handleWholeChange(typed: DiscountEntry) {
    const entry = capEntry(typed, linesTotal);
    setWholeEntry(entry);
    const amount = resolveDiscountEntry(entry, linesTotal);
    if (entry.mode === 'percent') {
      // The same % off every line.
      const text = Number.isNaN(amount) ? '' : entry.text;
      setLineEntries(Object.fromEntries(lines.map((line): [string, DiscountEntry] => [line.id, { mode: 'percent', text }])));
      return;
    }
    const shares = new Map(allocateDiscount(lines, Number.isNaN(amount) ? 0 : amount).map((share) => [share.lineId, share.amount]));
    setLineEntries(Object.fromEntries(lines.map((line): [string, DiscountEntry] => [line.id, { mode: 'amount', text: toAmountText(shares.get(line.id) ?? 0) }])));
  }

  function handleLineChange(id: string, typed: DiscountEntry) {
    const charge = lines.find((line) => line.id === id)?.amount ?? 0;
    const next = { ...lineEntries, [id]: capEntry(typed, charge) };
    setLineEntries(next);
    setWholeEntry({ mode: 'amount', text: toAmountText(sumOfLines(lines, next)) });
  }

  function handleConfirm() {
    setAttempted(true);
    if (problem) return;
    onConfirm({
      type: sharedPercent === null ? 'Amount' : 'Percent',
      value: sharedPercent ?? totalDiscount,
      reason: trimmedReason,
      allocations: discounted.map((entry) => ({ lineId: entry.line.id, amount: entry.amount })),
    });
  }

  return (
    <Dialog open onOpenChange={(open) => !open && onCancel()}>
      <DialogContent
        aria-labelledby="discount-dialog-title"
        className="flex max-h-[90vh] w-[calc(100vw-2rem)] max-w-3xl flex-col gap-4 overflow-y-auto"
        // Esc in the Reason box undoes a suggestion first (SuggestionTextarea) rather than closing.
        onEscapeKeyDown={(event) => {
          if (document.activeElement?.getAttribute('data-suggesting') === 'true') event.preventDefault();
        }}
      >
        <DialogHeader>
          <DialogTitle id="discount-dialog-title">Add discount</DialogTitle>
          <DialogDescription>
            {showWholeBill
              ? 'Enter a discount against each bill line as a % or a ₹ amount, or one for the whole bill, and the reason.'
              : 'Enter the discount as a % or a ₹ amount, and the reason.'}
          </DialogDescription>
        </DialogHeader>

        <div className="flex flex-col gap-2">
          <div className="flex flex-col divide-y divide-border rounded-md border border-border text-sm" role="table" aria-label="Discount by bill line">
            <div role="row" className={cn(rowGridClassName, 'bg-muted/50 text-xs font-medium text-muted-foreground')}>
              <span role="columnheader">Bill line</span>
              <span role="columnheader" className="hidden text-right sm:block">
                Charge
              </span>
              <span role="columnheader" className="text-right">
                Discount %
              </span>
              <span role="columnheader" className="text-right">
                Discount ₹
              </span>
              <span role="columnheader" className="hidden text-right sm:block">
                Net
              </span>
            </div>

            {showWholeBill && (
              <DiscountRow
                id="discount-whole-bill"
                label={`Whole bill (${lines.length} lines)`}
                charge={linesTotal}
                entry={wholeEntry}
                amount={totalDiscount}
                invalid={Number.isNaN(wholeAmount) || wholeAmount > linesTotal}
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
                entry={entry.entry}
                amount={entry.amount}
                invalid={entry.problem !== null}
                hint={needsApproval ? `${entry.line.approver ?? 'Super Admin'} approves` : undefined}
                onChange={(typed) => handleLineChange(entry.line.id, typed)}
                autoFocus={!showWholeBill && index === 0}
              />
            ))}
          </div>
          {showWholeBill && (
            <p className="text-xs text-muted-foreground">
              A % against <span className="font-medium text-foreground">Whole bill</span> comes off every line; a ₹ amount is shared across
              the lines in proportion to their charges. Change any line afterwards if needed.
            </p>
          )}
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="discount-reason">
            Reason <span className="font-normal text-muted-foreground">(required)</span>
          </Label>
          <SuggestionTextarea
            id="discount-reason"
            rows={2}
            maxLength={500}
            value={reason}
            onValueChange={setReason}
            suggestions={reasonSuggestions}
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
            {discountedLines.length > 0
              ? `Needs approval from ${describeDiscountApprovers(discountedLines)}.`
              : 'A discount needs approval — from the consultant for their consultation fee, the Super Admin for the rest.'}{' '}
            The bill is saved without payment and sent for approval; collect payment once every part is approved or rejected.
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
  /** What was typed, in the % box or the ₹ box. */
  entry: DiscountEntry;
  /** The ₹ discount the row comes to — shown in the box that wasn't typed in, and taken off the
   * charge for Net. */
  amount: number;
  /** Marks the box that was typed in. */
  invalid: boolean;
  /** A small note under the line's name — who approves its discount. */
  hint?: string;
  onChange: (entry: DiscountEntry) => void;
  /** The Whole bill row — shaded and bold. */
  emphasis?: boolean;
  autoFocus?: boolean;
}

function DiscountRow({ id, label, charge, entry, amount, invalid, hint, onChange, emphasis, autoFocus }: DiscountRowProps) {
  const net = charge - amount;
  const percentInvalid = invalid && entry.mode === 'percent';
  const amountInvalid = invalid && entry.mode === 'amount';
  return (
    <div role="row" className={cn(rowGridClassName, emphasis && 'bg-muted/30')}>
      <div role="cell" className="flex min-w-0 flex-col">
        <label htmlFor={id} className={cn('break-words text-foreground', emphasis && 'font-medium')}>
          {label}
        </label>
        {hint && <span className="text-xs text-muted-foreground">{hint}</span>}
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
          max={100}
          step={0.01}
          inputMode="decimal"
          value={entry.mode === 'percent' ? entry.text : toPercentText(amount, charge)}
          onChange={(event) => onChange({ mode: 'percent', text: event.target.value })}
          placeholder="%"
          aria-label={`${label} — discount %`}
          aria-invalid={percentInvalid || undefined}
          className={cn('h-9 text-right', percentInvalid && 'border-destructive focus-visible:ring-destructive')}
          autoFocus={autoFocus}
        />
      </div>
      <div role="cell">
        <Input
          type="number"
          min={0}
          max={charge}
          step={1}
          inputMode="numeric"
          value={entry.mode === 'amount' ? entry.text : toAmountText(amount)}
          onChange={(event) => onChange({ mode: 'amount', text: event.target.value })}
          placeholder="₹"
          aria-label={`${label} — discount ₹`}
          aria-invalid={amountInvalid || undefined}
          className={cn('h-9 text-right', amountInvalid && 'border-destructive focus-visible:ring-destructive')}
        />
      </div>
      <span role="cell" className={cn('hidden text-right sm:block', emphasis ? 'font-semibold text-foreground' : 'font-medium text-foreground')}>
        {formatCurrency(Math.max(net, 0))}
      </span>
    </div>
  );
}
