import type { SyncEventRow, SyncStreamRow } from '@hms/shared';
import { Eye, GitMerge, RotateCcw, SkipForward } from 'lucide-react';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
  EVENT_STATUS_LABELS,
  EVENT_STATUS_TONES,
  OPERATION_LABELS,
  formatDateTime,
  formatRelative,
  patientLabel,
} from '../cloudSyncUtils';

const HEAD = 'whitespace-nowrap px-3 py-2 text-left text-xs font-semibold';
const CELL = 'px-3 py-2 align-middle';

export function SyncStatusBadge({ status }: { status: SyncStreamRow['status'] }) {
  return (
    <Badge variant={EVENT_STATUS_TONES[status] ?? 'outline'} className="whitespace-nowrap">
      {EVENT_STATUS_LABELS[status] ?? status}
    </Badge>
  );
}

interface RowActions {
  canEdit: boolean;
  busy: boolean;
  onHistory: (patientId: string) => void;
  onRetry: (eventId: string) => void;
  onResolve: (event: { eventId: string; patientId: string; status: SyncStreamRow['status']; label: string }) => void;
}

/** One row per patient, blocked patients first. */
export function SyncPatientsTable({ rows, canEdit, busy, onHistory, onRetry, onResolve }: { rows: SyncStreamRow[] } & RowActions) {
  return (
    <div className="overflow-x-auto rounded-md border border-border">
      <table className="w-full min-w-[920px] text-sm">
        <thead className="bg-sidebar-active text-sidebar-active-foreground">
          <tr>
            <th className={HEAD}>Patient</th>
            <th className={HEAD}>Status</th>
            <th className={`${HEAD} text-right`}>Waiting</th>
            <th className={`${HEAD} text-right`}>Last synced #</th>
            <th className={`${HEAD} text-right`}>Retries</th>
            <th className={HEAD}>Last attempt</th>
            <th className={HEAD}>Error</th>
            <th className={`${HEAD} text-center`}>Actions</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {rows.map((row) => {
            const blocked = row.status === 'Failed' || row.status === 'NeedsAttention';
            return (
              <tr key={row.patientId} className="hover:bg-muted/30">
                <td className={`${CELL} max-w-[260px]`}>
                  <span className="block truncate font-medium text-foreground" title={row.patientId}>
                    {patientLabel(row)}
                  </span>
                  {row.patientDeleted && <span className="text-xs text-muted-foreground">Deleted patient</span>}
                </td>
                <td className={CELL}>
                  <SyncStatusBadge status={row.status} />
                </td>
                <td className={`${CELL} text-right tabular-nums`}>{row.pendingEvents}</td>
                <td className={`${CELL} text-right tabular-nums`}>
                  {row.lastSyncedSequence} / {row.lastSequence}
                </td>
                <td className={`${CELL} text-right tabular-nums`}>{row.retryCount}</td>
                <td className={`${CELL} whitespace-nowrap`} title={formatDateTime(row.lastAttemptAt)}>
                  {formatRelative(row.lastAttemptAt)}
                  {row.status === 'Failed' && row.nextAttemptAt && (
                    <span className="block text-xs text-muted-foreground">next {formatRelative(row.nextAttemptAt)}</span>
                  )}
                </td>
                <td className={`${CELL} max-w-[260px] truncate`} title={row.lastErrorMessage ?? row.lastErrorCode ?? undefined}>
                  {row.lastErrorMessage || row.lastErrorCode || '—'}
                </td>
                <td className={`${CELL} whitespace-nowrap text-center`}>
                  <Button variant="ghost" size="icon" className="h-7 w-7 text-primary" aria-label={`History of ${patientLabel(row)}`} onClick={() => onHistory(row.patientId)}>
                    <Eye className="h-4 w-4" />
                  </Button>
                  {canEdit && blocked && row.headEventId && (
                    <Button variant="ghost" size="icon" className="h-7 w-7" disabled={busy} aria-label={`Retry ${patientLabel(row)}`} onClick={() => onRetry(row.headEventId as string)}>
                      <RotateCcw className="h-4 w-4" />
                    </Button>
                  )}
                  {canEdit && (row.status === 'Conflict' || row.status === 'NeedsAttention') && row.headEventId && (
                    <Button
                      variant="ghost"
                      size="icon"
                      className="h-7 w-7"
                      disabled={busy}
                      aria-label={row.status === 'Conflict' ? `Resolve conflict for ${patientLabel(row)}` : `Skip the blocked change for ${patientLabel(row)}`}
                      onClick={() => onResolve({ eventId: row.headEventId as string, patientId: row.patientId, status: row.status, label: patientLabel(row) })}
                    >
                      {row.status === 'Conflict' ? <GitMerge className="h-4 w-4" /> : <SkipForward className="h-4 w-4" />}
                    </Button>
                  )}
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}

/** Failed, needs-attention and conflicting events across all patients, oldest first. */
export function SyncFailedEventsTable({ rows, canEdit, busy, onHistory, onRetry, onResolve }: { rows: SyncEventRow[] } & RowActions) {
  return (
    <div className="overflow-x-auto rounded-md border border-border">
      <table className="w-full min-w-[960px] text-sm">
        <thead className="bg-sidebar-active text-sidebar-active-foreground">
          <tr>
            <th className={HEAD}>Patient</th>
            <th className={`${HEAD} text-right`}>#</th>
            <th className={HEAD}>Change</th>
            <th className={HEAD}>Status</th>
            <th className={`${HEAD} text-right`}>Retries</th>
            <th className={HEAD}>Saved</th>
            <th className={HEAD}>Error</th>
            <th className={`${HEAD} text-center`}>Actions</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {rows.map((row) => (
            <tr key={row.eventId} className="hover:bg-muted/30">
              <td className={`${CELL} max-w-[240px] truncate font-medium`} title={row.patientId}>
                {patientLabel(row)}
              </td>
              <td className={`${CELL} text-right tabular-nums`}>{row.sequenceNumber}</td>
              <td className={`${CELL} max-w-[220px] truncate`} title={row.changedFields.join(', ')}>
                {OPERATION_LABELS[row.operationType] ?? row.operationType}
                {row.changedFields.length > 0 && <span className="text-muted-foreground"> · {row.changedFields.join(', ')}</span>}
              </td>
              <td className={CELL}>
                <SyncStatusBadge status={row.status} />
                {row.status === 'Failed' && row.nextRetryAt && <span className="block text-xs text-muted-foreground">next {formatRelative(row.nextRetryAt)}</span>}
              </td>
              <td className={`${CELL} text-right tabular-nums`}>{row.retryCount}</td>
              <td className={`${CELL} whitespace-nowrap`} title={formatDateTime(row.createdAt)}>
                {formatRelative(row.createdAt)}
              </td>
              <td className={`${CELL} max-w-[260px] truncate`} title={row.errorMessage ?? row.errorCode ?? undefined}>
                {row.errorMessage || row.errorCode || '—'}
              </td>
              <td className={`${CELL} whitespace-nowrap text-center`}>
                <Button variant="ghost" size="icon" className="h-7 w-7 text-primary" aria-label={`History of ${patientLabel(row)}`} onClick={() => onHistory(row.patientId)}>
                  <Eye className="h-4 w-4" />
                </Button>
                {canEdit && (row.status === 'Failed' || row.status === 'NeedsAttention') && (
                  <Button variant="ghost" size="icon" className="h-7 w-7" disabled={busy} aria-label={`Retry change ${row.sequenceNumber}`} onClick={() => onRetry(row.eventId)}>
                    <RotateCcw className="h-4 w-4" />
                  </Button>
                )}
                {canEdit && (row.status === 'Conflict' || row.status === 'NeedsAttention') && (
                  <Button
                    variant="ghost"
                    size="icon"
                    className="h-7 w-7"
                    disabled={busy}
                    aria-label={row.status === 'Conflict' ? `Resolve conflict on change ${row.sequenceNumber}` : `Skip change ${row.sequenceNumber}`}
                    onClick={() => onResolve({ eventId: row.eventId, patientId: row.patientId, status: row.status, label: patientLabel(row) })}
                  >
                    {row.status === 'Conflict' ? <GitMerge className="h-4 w-4" /> : <SkipForward className="h-4 w-4" />}
                  </Button>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
