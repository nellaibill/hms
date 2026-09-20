import type { ActivityLogEntry } from '@hms/shared';
import { Eye } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { actionLabel, entityLabel, formatDateTime } from '../activityLogUtils';
import { ResultBadge } from './ResultBadge';

interface ActivityLogTableProps {
  entries: ActivityLogEntry[];
  userName: (userId: string | null | undefined) => string;
  onView: (entry: ActivityLogEntry) => void;
  selectedId: string | null;
}

const HEAD = 'whitespace-nowrap px-3 py-2 text-left text-xs font-semibold text-foreground';
const CELL = 'px-3 py-2 align-middle';

export function ActivityLogTable({ entries, userName, onView, selectedId }: ActivityLogTableProps) {
  return (
    <div className="overflow-x-auto rounded-md border border-border">
      <table className="w-full min-w-[860px] text-sm">
        <thead className="bg-muted/50">
          <tr>
            <th className={HEAD}>Date &amp; Time</th>
            <th className={HEAD}>User</th>
            <th className={HEAD}>Module</th>
            <th className={HEAD}>Action</th>
            <th className={HEAD}>Entity</th>
            <th className={HEAD}>Description</th>
            <th className={HEAD}>Result</th>
            <th className={`${HEAD} text-center`}>View</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {entries.map((entry) => (
            <tr key={entry.id} className={entry.id === selectedId ? 'bg-primary/5' : 'hover:bg-muted/30'}>
              <td className={`${CELL} whitespace-nowrap`}>{formatDateTime(entry.createdAt)}</td>
              <td className={`${CELL} whitespace-nowrap`}>{userName(entry.userId)}</td>
              <td className={CELL}>{entry.module}</td>
              <td className={CELL}>{actionLabel(entry.action)}</td>
              <td className={`${CELL} whitespace-nowrap`}>{entityLabel(entry)}</td>
              <td className={`${CELL} max-w-[280px] truncate`} title={entry.description ?? undefined}>
                {entry.description || '—'}
              </td>
              <td className={CELL}>
                <ResultBadge isSuccess={entry.isSuccess} />
              </td>
              <td className={`${CELL} text-center`}>
                <Button
                  type="button"
                  variant="ghost"
                  size="icon"
                  className="h-7 w-7 text-primary"
                  aria-label={`View details of ${actionLabel(entry.action)} on ${entry.module}`}
                  onClick={() => onView(entry)}
                >
                  <Eye className="h-4 w-4" />
                </Button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
