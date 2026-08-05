import type { WeeklyRoster } from '@hms/shared';
import { ArrowDown, ArrowUp } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { DepartmentName } from '@/components/DepartmentName';

interface WeeklyRosterTableProps {
  rosters: WeeklyRoster[];
  sort: string;
  onSortChange: (sort: string) => void;
  onDeleteRequested: (roster: WeeklyRoster) => void;
  onPublishRequested: (roster: WeeklyRoster) => void;
  isPublishingId: string | undefined;
}

const columns: Array<{ field: string; label: string }> = [{ field: 'weekStartDate', label: 'Week Start' }];

export function WeeklyRosterTable({ rosters, sort, onSortChange, onDeleteRequested, onPublishRequested, isPublishingId }: WeeklyRosterTableProps) {
  const currentField = sort.startsWith('-') ? sort.slice(1) : sort;
  const isDescending = sort.startsWith('-');

  function toggleSort(field: string) {
    if (currentField !== field) {
      onSortChange(field);
      return;
    }
    onSortChange(isDescending ? field : `-${field}`);
  }

  return (
    <div className="overflow-hidden rounded-lg border border-border">
      <table className="w-full text-sm">
        <thead className="bg-muted/50 text-left text-xs font-medium uppercase tracking-wide text-muted-foreground">
          <tr>
            {columns.map((column) => (
              <th key={column.field} className="px-4 py-2.5">
                <button type="button" onClick={() => toggleSort(column.field)} className="inline-flex items-center gap-1 hover:text-foreground">
                  {column.label}
                  {currentField === column.field &&
                    (isDescending ? <ArrowDown className="h-3.5 w-3.5" /> : <ArrowUp className="h-3.5 w-3.5" />)}
                </button>
              </th>
            ))}
            <th className="px-4 py-2.5">Department</th>
            <th className="px-4 py-2.5">Status</th>
            <th className="px-4 py-2.5 text-right">Actions</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {rosters.map((roster) => (
            <tr key={roster.id} className="hover:bg-muted/30">
              <td className="px-4 py-3">
                <Link to={`/admin/hr/weekly-rosters/${roster.id}`} className="font-medium text-foreground hover:text-primary hover:underline">
                  {roster.weekStartDate}
                </Link>
              </td>
              <td className="px-4 py-3 text-foreground">
                <DepartmentName departmentId={roster.departmentId} />
              </td>
              <td className="px-4 py-3">
                <Badge variant={roster.published ? 'success' : 'secondary'}>{roster.published ? 'Published' : 'Draft'}</Badge>
              </td>
              <td className="px-4 py-3">
                <div className="flex justify-end gap-1.5">
                  {!roster.published && (
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => onPublishRequested(roster)}
                      disabled={isPublishingId === roster.id}
                    >
                      {isPublishingId === roster.id ? 'Publishing…' : 'Publish'}
                    </Button>
                  )}
                  <Button asChild variant="ghost" size="sm">
                    <Link to={`/admin/hr/weekly-rosters/${roster.id}/edit`}>Edit</Link>
                  </Button>
                  <Button variant="ghost" size="sm" className="text-destructive hover:text-destructive" onClick={() => onDeleteRequested(roster)}>
                    Delete
                  </Button>
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
