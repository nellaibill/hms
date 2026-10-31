import type { InvoiceDiscountType } from '@hms/shared';
import { ShieldCheck } from 'lucide-react';
import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { cn } from '@/lib/utils';
import { allocateDiscount, DISCOUNT_TYPE_LABELS, discountTotalFor, type DiscountAllocation, type DiscountableLine } from '../billingDiscount';
import { formatCurrency } from '../billingCalculations';

const textareaClassName =
  'flex w-full rounded-md border border-input bg-background px-3 py-2 text-sm shadow-sm placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2';

export interface DiscountDialogResult {
  type: InvoiceDiscountType;
  value: number;
  reason: string;
  /** Each chosen line's share, in whole rupees — see billingDiscount.ts's allocateDiscount. */
  allocations: DiscountAllocation[];
}

interface DiscountDialogProps {
  /** The lines that can take a discount; all start ticked unless `initial` says otherwise. */
  lines: DiscountableLine[];
  /** The bill's whole net amount before this discount — lines that can't be discounted included. */
  billTotal: number;
  initial?: { type: InvoiceDiscountType; value: number; reason: string; lineIds: string[] };
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

/**
 * "Add discount" (ADR-109) — pick the bill lines it covers (all of them, or some), a ₹ amount or
 * a %, and a reason. The discount is spread over the ticked lines in proportion to their charges,
 * and the preview shows each line's share and the new net payable. Used both while making a bill
 * (BillingSummaryCard) and on a saved, unpaid bill (RequestDiscountButton).
 */
export function DiscountDialog({ lines, billTotal, initial, needsApproval, confirmLabel, isSaving, error, onConfirm, onRemove, onCancel }: DiscountDialogProps) {
  const [selectedIds, setSelectedIds] = useState<ReadonlySet<string>>(() => {
    const initialIds = initial?.lineIds.filter((id) => lines.some((line) => line.id === id)) ?? [];
    return new Set(initialIds.length > 0 ? initialIds : lines.map((line) => line.id));
  });
  const [type, setType] = useState<InvoiceDiscountType>(initial?.type ?? 'Amount');
  const [valueText, setValueText] = useState(initial ? String(initial.value) : '');
  const [reason, setReason] = useState(initial?.reason ?? '');
  const [attempted, setAttempted] = useState(false);

  const value = valueText === '' ? 0 : Number(valueText);
  const selectedLines = lines.filter((line) => selectedIds.has(line.id));
  const selectedTotal = selectedLines.reduce((sum, line) => sum + line.amount, 0);
  const allocations = allocateDiscount(selectedLines, type, value);
  const shareById = new Map(allocations.map((allocation) => [allocation.lineId, allocation.amount]));
  const totalDiscount = discountTotalFor(selectedTotal, type, value);
  const allSelected = selectedIds.size === lines.length;
  const trimmedReason = reason.trim();

  const problem =
    selectedLines.length === 0
      ? 'Tick at least one bill line to discount.'
      : !(value > 0) || Number.isNaN(value)
        ? `Enter the discount ${type === 'Percent' ? 'percentage' : 'amount'}.`
        : type === 'Percent' && value > 100
          ? 'A percentage discount can’t be more than 100%.'
          : type === 'Amount' && value > selectedTotal
            ? `The discount can’t be more than the ticked lines’ total (${formatCurrency(selectedTotal)}).`
            : trimmedReason.length === 0
              ? 'A reason is required — the Super Admin decides on it.'
              : totalDiscount === 0
                ? 'This discount rounds to ₹0 — enter a larger amount.'
                : null;

  function toggleLine(id: string) {
    setSelectedIds((previous) => {
      const next = new Set(previous);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  }

  function handleConfirm() {
    setAttempted(true);
    if (problem) return;
    onConfirm({ type, value, reason: trimmedReason, allocations });
  }

  return (
    <Dialog open onOpenChange={(open) => !open && onCancel()}>
      <DialogContent aria-labelledby="discount-dialog-title" className="max-h-[90vh] max-w-lg overflow-y-auto">
        <DialogHeader>
          <DialogTitle id="discount-dialog-title">Add discount</DialogTitle>
          <DialogDescription>Choose the bill lines the discount covers, how much, and why.</DialogDescription>
        </DialogHeader>

        <div className="flex flex-col gap-2">
          <span className="text-sm font-medium text-foreground">Apply to</span>
          <div className="flex flex-col divide-y divide-border rounded-md border border-border text-sm">
            <label className="flex cursor-pointer items-center justify-between gap-3 bg-muted/30 px-3 py-2">
              <span className="flex items-center gap-2 font-medium text-foreground">
                <input
                  type="checkbox"
                  className="h-4 w-4 rounded border-input accent-primary"
                  checked={allSelected}
                  onChange={(event) => setSelectedIds(new Set(event.target.checked ? lines.map((line) => line.id) : []))}
                />
                Whole bill ({lines.length} {lines.length === 1 ? 'line' : 'lines'})
              </span>
              <span className="text-muted-foreground">{formatCurrency(lines.reduce((sum, line) => sum + line.amount, 0))}</span>
            </label>
            {lines.map((line) => {
              const share = shareById.get(line.id);
              return (
                <label key={line.id} className="flex cursor-pointer items-center justify-between gap-3 px-3 py-2">
                  <span className="flex min-w-0 items-center gap-2 text-foreground">
                    <input
                      type="checkbox"
                      className="h-4 w-4 shrink-0 rounded border-input accent-primary"
                      checked={selectedIds.has(line.id)}
                      onChange={() => toggleLine(line.id)}
                    />
                    <span className="truncate">{line.label}</span>
                  </span>
                  <span className="flex shrink-0 flex-col items-end">
                    <span className="text-foreground">{formatCurrency(line.amount)}</span>
                    {share ? <span className="text-xs font-medium text-destructive">− {formatCurrency(share)}</span> : null}
                  </span>
                </label>
              );
            })}
          </div>
        </div>

        <div className="grid grid-cols-1 gap-3 sm:grid-cols-[auto_minmax(0,1fr)]">
          <div className="flex flex-col gap-1.5">
            <span className="text-sm font-medium text-foreground">Discount as</span>
            <div className="flex gap-1 rounded-md border border-border p-1" role="group" aria-label="Discount as">
              {(Object.keys(DISCOUNT_TYPE_LABELS) as InvoiceDiscountType[]).map((option) => (
                <button
                  key={option}
                  type="button"
                  aria-pressed={type === option}
                  onClick={() => setType(option)}
                  className={cn(
                    'rounded px-3 py-1.5 text-sm font-medium transition-colors',
                    type === option ? 'bg-sidebar-active text-sidebar-active-foreground' : 'text-muted-foreground hover:bg-accent/50 hover:text-foreground',
                  )}
                >
                  {DISCOUNT_TYPE_LABELS[option]}
                </button>
              ))}
            </div>
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="discount-value">{type === 'Percent' ? 'Percentage (%)' : 'Amount (₹)'}</Label>
            <Input
              id="discount-value"
              type="number"
              min={0}
              max={type === 'Percent' ? 100 : selectedTotal}
              inputMode="decimal"
              value={valueText}
              onChange={(event) => setValueText(event.target.value)}
              placeholder={type === 'Percent' ? 'e.g. 10' : 'e.g. 100'}
              autoFocus
            />
          </div>
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

        <DialogFooter className="gap-2 sm:justify-between">
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
