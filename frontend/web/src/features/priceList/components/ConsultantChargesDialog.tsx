import { useState } from 'react';
import { Link } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { formatRupees } from '@/features/diagnostics/tariff';
import { useUpdateMasterMutation, type MasterRecord } from '@/features/masters';
import type { ConsultationTypeColumn } from '../priceListRows';

interface ConsultantChargesDialogProps {
  consultant: MasterRecord;
  columns: ConsultationTypeColumn[];
  onClose: () => void;
}

interface ChargeDraft {
  offered: boolean;
  charge: string;
}

type ChargeEntry = { consultationTypeId: string; consultantCharge: number | null };

function chargesOf(consultant: MasterRecord): ChargeEntry[] {
  return Array.isArray(consultant.consultationTypeCharges) ? (consultant.consultationTypeCharges as ChargeEntry[]) : [];
}

/** Saving a consultant needs their available days and visiting hours (the backend validator). */
function hasSchedule(consultant: MasterRecord): boolean {
  return Array.isArray(consultant.availableDays) && consultant.availableDays.length > 0 && Boolean(consultant.visitStartTime) && Boolean(consultant.visitEndTime);
}

/**
 * Edits one consultant's consultation types and doctor's charges from the Price List (ADR-140):
 * tick a type they offer, enter the doctor's charge, and see what the hospital keeps. Saves the
 * consultant's own record (the same update the consultant page makes), so consultation billing
 * reads the new charges exactly as before. Charges for inactive types, which the grid doesn't
 * show, are kept as they are.
 */
export function ConsultantChargesDialog({ consultant, columns, onClose }: ConsultantChargesDialogProps) {
  const mutation = useUpdateMasterMutation('consultant');
  const existing = chargesOf(consultant);
  const [drafts, setDrafts] = useState<Record<string, ChargeDraft>>(() =>
    Object.fromEntries(
      columns.map((column) => {
        const entry = existing.find((charge) => charge.consultationTypeId === column.id);
        return [column.id, { offered: Boolean(entry), charge: entry && entry.consultantCharge !== null ? String(entry.consultantCharge) : '' }];
      }),
    ),
  );
  const [error, setError] = useState<string | null>(null);
  const canSave = hasSchedule(consultant);
  const name = String(consultant.name ?? 'Consultant');

  function update(typeId: string, change: Partial<ChargeDraft>) {
    setDrafts((current) => ({ ...current, [typeId]: { ...current[typeId], ...change } }));
    setError(null);
  }

  function handleSave() {
    const shownIds = new Set(columns.map((column) => column.id));
    const kept = existing.filter((charge) => !shownIds.has(charge.consultationTypeId));
    const edited: ChargeEntry[] = [];
    for (const column of columns) {
      const draft = drafts[column.id];
      if (!draft?.offered) continue;
      const text = draft.charge.trim();
      const value = text === '' ? null : Number(text);
      if (value !== null && (!Number.isFinite(value) || value < 0)) {
        setError(`Enter a charge of 0 or more for ${column.name}.`);
        return;
      }
      edited.push({ consultationTypeId: column.id, consultantCharge: value });
    }
    const consultationTypeCharges = [...kept, ...edited];
    if (consultationTypeCharges.length === 0) {
      setError('Tick at least one consultation type.');
      return;
    }
    mutation.mutate({ id: consultant.id, values: { ...consultant, consultationTypeCharges } }, { onSuccess: onClose });
  }

  const apiMessage = mutation.error instanceof Error ? mutation.error.message : null;

  return (
    <Dialog open onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="max-w-2xl" aria-labelledby="consultant-charges-title">
        <DialogHeader>
          <DialogTitle id="consultant-charges-title">{name}</DialogTitle>
          <DialogDescription>Tick the consultation types this doctor offers and enter the doctor&apos;s charge for each. The hospital keeps the rest.</DialogDescription>
        </DialogHeader>

        {!canSave && (
          <p role="alert" className="rounded-md bg-amber-100 px-3 py-2 text-sm text-amber-900 dark:bg-amber-950 dark:text-amber-200">
            This doctor has no available days or visiting hours yet, which a save needs.{' '}
            <Link to={`/admin/masters/consultant/${consultant.id}/edit`} className="font-semibold underline">
              Set them on the consultant&apos;s page
            </Link>{' '}
            first.
          </p>
        )}

        <ul className="max-h-[60vh] divide-y divide-border overflow-y-auto rounded-md border border-border">
          {columns.map((column) => {
            const draft = drafts[column.id] ?? { offered: false, charge: '' };
            const charge = draft.charge.trim() === '' ? null : Number(draft.charge);
            const keeps = draft.offered && charge !== null && Number.isFinite(charge) && column.amount !== null ? column.amount - charge : null;
            const percent = keeps !== null && column.amount ? Math.round((keeps / column.amount) * 100) : null;
            const checkboxId = `cc-${column.id}`;
            return (
              <li key={column.id} className="flex flex-wrap items-center gap-x-4 gap-y-2 px-3 py-2.5 text-sm">
                <label htmlFor={checkboxId} className="flex min-w-[200px] flex-1 cursor-pointer items-start gap-2.5">
                  <input
                    id={checkboxId}
                    type="checkbox"
                    className="mt-0.5 h-5 w-5 shrink-0 accent-primary"
                    checked={draft.offered}
                    onChange={(event) => update(column.id, { offered: event.target.checked })}
                  />
                  <span className="flex flex-col">
                    <span className="font-medium">{column.name}</span>
                    <span className="text-xs text-muted-foreground">{column.amount !== null ? `Hospital charge ${formatRupees(column.amount)}` : 'No fixed rate'}</span>
                  </span>
                </label>
                <div className="flex items-center gap-3 pl-7 sm:pl-0">
                  <div className="flex flex-col gap-1">
                    <span className="text-[11px] text-muted-foreground">Doctor&apos;s charge ₹</span>
                    <Input
                      type="number"
                      min={0}
                      step="any"
                      aria-label={`Doctor's charge for ${column.name}`}
                      className="h-9 w-28"
                      value={draft.charge}
                      disabled={!draft.offered}
                      placeholder={draft.offered ? 'Not set' : '—'}
                      onChange={(event) => update(column.id, { charge: event.target.value })}
                    />
                  </div>
                  <div className="flex min-w-[96px] flex-col gap-1 text-right">
                    <span className="text-[11px] text-muted-foreground">Hospital keeps</span>
                    {keeps !== null ? (
                      <span className={keeps < 0 ? 'tabular-nums text-destructive' : 'tabular-nums text-blue-900 dark:text-blue-200'}>
                        {formatRupees(keeps)}
                        {percent !== null ? ` · ${percent}%` : ''}
                      </span>
                    ) : (
                      <span className="text-muted-foreground">—</span>
                    )}
                  </div>
                </div>
              </li>
            );
          })}
        </ul>

        {(error || apiMessage) && (
          <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
            {error ?? apiMessage}
          </p>
        )}

        <DialogFooter>
          <Button type="button" variant="outline" onClick={onClose} disabled={mutation.isPending}>
            Cancel
          </Button>
          <Button type="button" onClick={handleSave} disabled={!canSave || mutation.isPending}>
            {mutation.isPending ? 'Saving…' : 'Save charges'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
