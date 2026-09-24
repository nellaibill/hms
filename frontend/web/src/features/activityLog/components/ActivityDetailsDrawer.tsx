import type { ActivityLogEntry } from '@hms/shared';
import {
  CalendarClock,
  Fingerprint,
  Globe,
  Link2,
  Loader2,
  Monitor,
  Package,
  ScrollText,
  Tag,
  User as UserIcon,
  Zap,
} from 'lucide-react';
import { Sheet, SheetContent } from '@/components/ui/sheet';
import { Button } from '@/components/ui/button';
import { actionLabel, buildChangeRows, formatDateTime } from '../activityLogUtils';
import { useActivityLogDetailQuery } from '../hooks/useActivityLogQueries';
import { ResultBadge } from './ResultBadge';

interface ActivityDetailsDrawerProps {
  entry: ActivityLogEntry | null;
  userName: (userId: string | null | undefined) => string;
  onClose: () => void;
}

function Row({ icon: Icon, label, children }: { icon: React.ElementType; label: string; children: React.ReactNode }) {
  return (
    <div className="grid grid-cols-[130px_1fr] items-start gap-2 text-sm">
      <span className="flex items-center gap-2 text-muted-foreground">
        <Icon className="h-4 w-4 shrink-0" />
        {label}
      </span>
      <span className="min-w-0 break-words font-medium text-foreground">{children}</span>
    </div>
  );
}

export function ActivityDetailsDrawer({ entry, userName, onClose }: ActivityDetailsDrawerProps) {
  const { data: detail, isPending, isError, error } = useActivityLogDetailQuery(entry?.id ?? null);

  // The list row already carries everything except IP/user agent/correlation id and the
  // old/new snapshots, so the header renders instantly while only those load.
  const shown = detail ?? entry;
  const changes = detail ? buildChangeRows(detail.oldValues, detail.newValues) : [];

  return (
    <Sheet open={entry !== null} onOpenChange={(open) => !open && onClose()}>
      <SheetContent side="right" className="flex w-full flex-col gap-4 overflow-y-auto p-5 sm:max-w-md">
        <h2 className="text-lg font-semibold text-foreground">Activity Details</h2>

        {shown && (
          <>
            <div className="rounded-md border border-success/30 bg-success/10 px-3 py-2">
              <p className="font-semibold text-foreground">
                {[shown.entityType, actionLabel(shown.action)].filter(Boolean).join(' ')}
              </p>
              {shown.description && <p className="text-sm text-muted-foreground">{shown.description}</p>}
            </div>

            <div className="flex flex-col gap-2.5">
              <Row icon={UserIcon} label="User">
                {userName(shown.userId)}
              </Row>
              <Row icon={CalendarClock} label="Date & Time">
                {formatDateTime(shown.createdAt)}
              </Row>
              <Row icon={Package} label="Module">
                {shown.module}
              </Row>
              <Row icon={Tag} label="Entity">
                {[shown.entityType, shown.entityId].filter(Boolean).join(' · ') || '—'}
              </Row>
              <Row icon={Zap} label="Action">
                {actionLabel(shown.action)}
              </Row>
              <Row icon={Fingerprint} label="Result">
                <ResultBadge isSuccess={shown.isSuccess} />
              </Row>
              <Row icon={Globe} label="IP Address">
                {detail?.ipAddress || (isPending ? '…' : '—')}
              </Row>
              <Row icon={Monitor} label="User Agent">
                {detail?.userAgent || (isPending ? '…' : '—')}
              </Row>
              <Row icon={Link2} label="Correlation ID">
                {detail?.correlationId || (isPending ? '…' : '—')}
              </Row>
            </div>

            <div className="flex flex-col gap-1.5">
              <h3 className="flex items-center gap-2 text-sm font-semibold text-foreground">
                <ScrollText className="h-4 w-4" />
                Description
              </h3>
              <p className="rounded-md bg-muted/50 px-3 py-2 text-sm text-foreground">{shown.description || '—'}</p>
            </div>

            <div className="flex flex-col gap-1.5">
              <h3 className="text-sm font-semibold text-foreground">Changes</h3>
              {isPending && (
                <div className="flex items-center gap-2 py-4 text-sm text-muted-foreground">
                  <Loader2 className="h-4 w-4 animate-spin" />
                  Loading changes…
                </div>
              )}
              {isError && (
                <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
                  {error instanceof Error ? error.message : 'Failed to load activity details.'}
                </p>
              )}
              {detail && changes.length === 0 && (
                <p className="rounded-md bg-muted/50 px-3 py-2 text-sm text-muted-foreground">No field changes were recorded for this event.</p>
              )}
              {changes.length > 0 && (
                <div className="overflow-x-auto rounded-md border border-border">
                  <table className="w-full text-sm">
                    <thead className="bg-sidebar-active text-left text-xs font-semibold text-sidebar-active-foreground">
                      <tr>
                        <th className="px-3 py-1.5">Field</th>
                        <th className="px-3 py-1.5">Before</th>
                        <th className="px-3 py-1.5">After</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-border">
                      {changes.map((row) => (
                        <tr key={row.field}>
                          <td className="px-3 py-1.5 font-medium">{row.field}</td>
                          <td className="break-words px-3 py-1.5 text-muted-foreground">{row.before}</td>
                          <td className="break-words px-3 py-1.5">{row.after}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          </>
        )}

        <div className="mt-auto flex justify-end pt-2">
          <Button variant="outline" onClick={onClose}>
            Close
          </Button>
        </div>
      </SheetContent>
    </Sheet>
  );
}
