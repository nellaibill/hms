import type { SyncEventRow, SyncResolution, SyncStreamRow } from '@hms/shared';
import { Loader2 } from 'lucide-react';
import { useEffect, useState } from 'react';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Label } from '@/components/ui/label';
import { Sheet, SheetContent } from '@/components/ui/sheet';
import { cn } from '@/lib/utils';
import { OPERATION_LABELS, RESOLUTION_LABELS, formatDateTime } from '../cloudSyncUtils';
import { useSyncPatientHistoryQuery } from '../hooks/useCloudSyncQueries';
import { SyncStatusBadge } from './SyncTables';

/** One patient's sync history: every change (field names only) with every send attempt. */
export function SyncHistoryDrawer({ patientId, onClose }: { patientId: string | null; onClose: () => void }) {
  const { data, isPending, isError, error } = useSyncPatientHistoryQuery(patientId);
  const first = data?.[0];
  const title = first?.patientName ? `${first.patientName}${first.uhid ? ` (${first.uhid})` : ''}` : patientId ? `Patient ${patientId.slice(0, 8)}…` : '';

  return (
    <Sheet open={patientId !== null} onOpenChange={(open) => !open && onClose()}>
      <SheetContent side="right" className="flex w-full flex-col gap-4 overflow-y-auto p-5 sm:max-w-lg">
        <div>
          <h2 className="text-lg font-semibold text-foreground">Sync history</h2>
          <p className="text-sm text-muted-foreground">{title}</p>
          <p className="text-xs text-muted-foreground">Shows which fields changed, never their values.</p>
        </div>

        {isPending && (
          <div className="flex items-center gap-2 py-6 text-sm text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" />
            Loading history…
          </div>
        )}
        {isError && (
          <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
            {error instanceof Error ? error.message : 'Could not load the history.'}
          </p>
        )}
        {data && data.length === 0 && <p className="text-sm text-muted-foreground">No changes recorded for this patient.</p>}

        <ol className="flex flex-col gap-3">
          {data?.map((event) => (
            <HistoryItem key={event.eventId} event={event} />
          ))}
        </ol>
      </SheetContent>
    </Sheet>
  );
}

function HistoryItem({ event }: { event: SyncEventRow }) {
  return (
    <li className="rounded-md border border-border p-3 text-sm">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <span className="font-semibold text-foreground">
          #{event.sequenceNumber} · {OPERATION_LABELS[event.operationType] ?? event.operationType}
        </span>
        <SyncStatusBadge status={event.status} />
      </div>
      <p className="text-xs text-muted-foreground">Saved {formatDateTime(event.createdAt)}</p>
      {event.changedFields.length > 0 && <p className="mt-1 text-foreground">Changed: {event.changedFields.join(', ')}</p>}
      {event.syncedAt && <p className="text-xs text-muted-foreground">Reached the cloud {formatDateTime(event.syncedAt)}</p>}
      {event.resolution && (
        <p className="text-xs text-muted-foreground">
          {RESOLUTION_LABELS[event.resolution] ?? event.resolution} {event.resolvedAt ? `on ${formatDateTime(event.resolvedAt)}` : ''}
        </p>
      )}
      {(event.errorMessage || event.errorCode) && event.status !== 'Synced' && (
        <p className="mt-1 rounded bg-destructive/10 px-2 py-1 text-xs text-destructive">{event.errorMessage || event.errorCode}</p>
      )}
      {event.attempts && event.attempts.length > 0 && (
        <details className="mt-2">
          <summary className="cursor-pointer text-xs font-medium text-primary">
            {event.attempts.length} attempt{event.attempts.length === 1 ? '' : 's'}
          </summary>
          <ul className="mt-1 flex flex-col gap-1">
            {event.attempts.map((attempt) => (
              <li key={attempt.attemptNo} className="text-xs text-muted-foreground">
                {attempt.attemptNo}. {formatDateTime(attempt.finishedAt)} — <span className="font-medium text-foreground">{attempt.outcome}</span>
                {attempt.httpStatus ? ` (HTTP ${attempt.httpStatus})` : ''} · {attempt.durationMs} ms
                {attempt.errorMessage ? ` · ${attempt.errorMessage}` : ''}
              </li>
            ))}
          </ul>
        </details>
      )}
    </li>
  );
}

export interface ResolveTarget {
  eventId: string;
  patientId: string;
  status: SyncStreamRow['status'];
  label: string;
}

interface ResolveDialogProps {
  target: ResolveTarget | null;
  busy: boolean;
  onCancel: () => void;
  onConfirm: (resolution: SyncResolution, note: string) => void;
}

/** Conflicts: send the local version, or keep the cloud's. Needs-attention: skip the change. */
export function SyncResolveDialog({ target, busy, onCancel, onConfirm }: ResolveDialogProps) {
  const isConflict = target?.status === 'Conflict';
  const [choice, setChoice] = useState<SyncResolution>('LocalWins');
  const [note, setNote] = useState('');

  useEffect(() => {
    setChoice(target?.status === 'Conflict' ? 'LocalWins' : 'Skip');
    setNote('');
  }, [target]);

  const options: { value: SyncResolution; title: string; body: string }[] = isConflict
    ? [
        {
          value: 'LocalWins',
          title: 'Send this server’s version',
          body: 'The cloud copy is overwritten with the record as it is here. The cloud’s earlier copy is kept in its conflict log.',
        },
        {
          value: 'CloudWins',
          title: 'Keep the cloud’s version',
          body: 'The record here is replaced with the cloud’s copy, so both sides match. The local change is discarded.',
        },
      ]
    : [
        {
          value: 'Skip',
          title: 'Skip this change',
          body: 'The cloud never receives this one change. The patient’s next change still carries the full record, so the two sides catch up then.',
        },
      ];

  return (
    <Dialog open={target !== null} onOpenChange={(open) => !open && onCancel()}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>{isConflict ? 'Resolve conflict' : 'Skip blocked change'}</DialogTitle>
          <DialogDescription>
            {isConflict
              ? `${target?.label}: the cloud copy was changed outside sync, so nothing was overwritten. Choose which version to keep.`
              : `${target?.label}: this change keeps being refused by the cloud and is holding back the patient’s later changes.`}
          </DialogDescription>
        </DialogHeader>

        <div className="flex flex-col gap-2" role="radiogroup" aria-label="Resolution">
          {options.map((option) => (
            <button
              key={option.value}
              type="button"
              role="radio"
              aria-checked={choice === option.value}
              onClick={() => setChoice(option.value)}
              className={cn(
                'rounded-md border p-3 text-left text-sm transition-colors',
                choice === option.value ? 'border-primary bg-primary/5' : 'border-border hover:bg-muted/40',
              )}
            >
              <span className="block font-semibold text-foreground">{option.title}</span>
              <span className="text-muted-foreground">{option.body}</span>
            </button>
          ))}
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="sync-resolve-note">Reason (kept in the activity log)</Label>
          <textarea
            id="sync-resolve-note"
            value={note}
            maxLength={500}
            onChange={(e) => setNote(e.target.value)}
            className="min-h-[72px] rounded-md border border-input bg-background px-3 py-2 text-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
          />
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={onCancel} disabled={busy}>
            Cancel
          </Button>
          <Button variant={choice === 'LocalWins' ? 'default' : 'warning'} onClick={() => onConfirm(choice, note.trim())} disabled={busy}>
            {busy && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            Confirm
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

interface ConfirmDialogProps {
  open: boolean;
  title: string;
  description: string;
  confirmLabel: string;
  busy: boolean;
  onCancel: () => void;
  onConfirm: () => void;
}

export function SyncConfirmDialog({ open, title, description, confirmLabel, busy, onCancel, onConfirm }: ConfirmDialogProps) {
  return (
    <Dialog open={open} onOpenChange={(next) => !next && onCancel()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
          <DialogDescription>{description}</DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <Button variant="outline" onClick={onCancel} disabled={busy}>
            Cancel
          </Button>
          <Button onClick={onConfirm} disabled={busy}>
            {busy && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            {confirmLabel}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
