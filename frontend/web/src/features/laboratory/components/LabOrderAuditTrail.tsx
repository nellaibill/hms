import { History } from 'lucide-react';
import { humanize } from '@/features/patients/humanize';
import type { LabOrder } from '../types';

interface AuditEntry {
  key: string;
  testName: string;
  eventType: string;
  actorId?: string | null;
  occurredAt: string;
  remarks?: string | null;
}

interface LabOrderAuditTrailProps {
  order: LabOrder;
}

/** Every item's event history, flattened across the whole order and sorted newest first —
 * doubles as both sample-status history and general audit trail (one append-only entry per
 * LabOrderItemEvent). */
export function LabOrderAuditTrail({ order }: LabOrderAuditTrailProps) {
  const entries: AuditEntry[] = order.items
    .flatMap((item) =>
      item.events.map((event) => ({
        key: event.id,
        testName: item.testName,
        eventType: event.eventType,
        actorId: event.actorId,
        occurredAt: event.occurredAt,
        remarks: event.remarks,
      })),
    )
    .sort((a, b) => new Date(b.occurredAt).getTime() - new Date(a.occurredAt).getTime());

  if (entries.length === 0) {
    return (
      <div className="flex flex-col items-center gap-2 rounded-lg border border-dashed border-border py-10 text-center text-sm text-muted-foreground">
        <History className="h-6 w-6" />
        No audit history recorded yet.
      </div>
    );
  }

  return (
    <div className="flex flex-col divide-y divide-border rounded-md border border-border">
      {entries.map((entry) => (
        <div key={entry.key} className="flex flex-wrap items-start justify-between gap-2 px-4 py-3">
          <div className="flex flex-col gap-0.5">
            <span className="text-sm font-medium text-foreground">{humanize(entry.eventType)}</span>
            <span className="text-xs text-muted-foreground">
              {entry.testName}
              {/* actorId is an Identity User id, not a Masters Consultant/Department reference
                  — there's no existing id->display-name resolver for users in this codebase
                  (ConsultantName/DepartmentName only resolve Masters entities), so it's shown
                  as a short id rather than misusing one of those components. */}
              {entry.actorId && <> · User {entry.actorId.slice(0, 8)}…</>}
            </span>
            {entry.remarks && <span className="text-xs text-muted-foreground">{entry.remarks}</span>}
          </div>
          <span className="whitespace-nowrap font-mono text-xs text-muted-foreground">{new Date(entry.occurredAt).toLocaleString('en-IN')}</span>
        </div>
      ))}
    </div>
  );
}
