import { History } from 'lucide-react';
import { useStaffNameMap } from '@/features/messaging/hooks/useStaffNameMap';
import { humanize } from '@/features/patients/humanize';
import type { LabOrder } from '../types';

interface AuditEntry {
  key: string;
  /** The test the event belongs to — absent for order-level events (report generate/release). */
  testName?: string;
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
 * LabOrderItemEvent). Report generation and release are recorded on the order itself rather than
 * as item events, so they're added here from the order's own stamps. */
export function LabOrderAuditTrail({ order }: LabOrderAuditTrailProps) {
  const { nameById } = useStaffNameMap();

  const itemEntries: AuditEntry[] = order.items.flatMap((item) =>
    item.events.map((event) => ({
      key: event.id,
      testName: item.testName,
      eventType: event.eventType,
      actorId: event.actorId,
      occurredAt: event.occurredAt,
      remarks: event.remarks,
    })),
  );
  const reportEntries: AuditEntry[] = [];
  if (order.reportGeneratedAt) {
    reportEntries.push({ key: 'report-generated', eventType: 'ReportGenerated', actorId: order.reportGeneratedBy, occurredAt: order.reportGeneratedAt });
  }
  if (order.reportReleasedAt) {
    reportEntries.push({ key: 'report-released', eventType: 'ReportReleased', actorId: order.reportReleasedBy, occurredAt: order.reportReleasedAt });
  }
  const entries = [...itemEntries, ...reportEntries].sort((a, b) => new Date(b.occurredAt).getTime() - new Date(a.occurredAt).getTime());

  // actorId is an Identity User id — resolved through the same low-sensitivity staff directory
  // Messaging uses (no admin permission needed). That directory only lists active users (up to
  // its 100-entry cap), so anyone outside it still falls back to a short id.
  const actorLabel = (actorId: string) => nameById.get(actorId) ?? `User ${actorId.slice(0, 8)}…`;

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
              {[entry.testName ?? 'Whole order', entry.actorId && actorLabel(entry.actorId)].filter(Boolean).join(' · ')}
            </span>
            {entry.remarks && <span className="text-xs text-muted-foreground">{entry.remarks}</span>}
          </div>
          <span className="whitespace-nowrap font-mono text-xs text-muted-foreground">{new Date(entry.occurredAt).toLocaleString('en-IN')}</span>
        </div>
      ))}
    </div>
  );
}
