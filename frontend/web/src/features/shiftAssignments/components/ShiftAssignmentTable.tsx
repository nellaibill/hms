import type { ShiftAssignment } from '@hms/shared';
import { ArrowDown, ArrowUp } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { StaffName } from '@/components/StaffName';
import { useAuth } from '@/features/auth/AuthContext';
import { ShiftName } from './ShiftName';

interface ShiftAssignmentTableProps {
  assignments: ShiftAssignment[];
  sort: string;
  onSortChange: (sort: string) => void;
  onDeleteRequested: (assignment: ShiftAssignment) => void;
}

const columns: Array<{ field: string; label: string }> = [{ field: 'rosterDate', label: 'Roster Date' }];

const statusVariant: Record<ShiftAssignment['status'], 'success' | 'secondary' | 'destructive'> = {
  Scheduled: 'success',
  Completed: 'secondary',
  Cancelled: 'destructive',
};

export function ShiftAssignmentTable({ assignments, sort, onSortChange, onDeleteRequested }: ShiftAssignmentTableProps) {
  const currentField = sort.startsWith('-') ? sort.slice(1) : sort;
  const isDescending = sort.startsWith('-');
  const { hasPermission } = useAuth();
  const canEdit = hasPermission('workforce-admin.edit');
  const canDelete = hasPermission('workforce-admin.delete');

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
            <th className="px-4 py-2.5">Staff</th>
            <th className="px-4 py-2.5">Shift</th>
            {columns.map((column) => (
              <th key={column.field} className="px-4 py-2.5">
                <button type="button" onClick={() => toggleSort(column.field)} className="inline-flex items-center gap-1 hover:text-foreground">
                  {column.label}
                  {currentField === column.field &&
                    (isDescending ? <ArrowDown className="h-3.5 w-3.5" /> : <ArrowUp className="h-3.5 w-3.5" />)}
                </button>
              </th>
            ))}
            <th className="px-4 py-2.5">Status</th>
            <th className="px-4 py-2.5 text-right">Actions</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {assignments.map((assignment) => (
            <tr key={assignment.id} className="hover:bg-muted/30">
              <td className="px-4 py-3">
                <Link to={`/admin/hr/shift-assignments/${assignment.id}`} className="font-medium text-foreground hover:text-primary hover:underline">
                  <StaffName staffId={assignment.staffId} />
                </Link>
              </td>
              <td className="px-4 py-3 text-muted-foreground">
                <ShiftName shiftId={assignment.shiftId} />
              </td>
              <td className="px-4 py-3 font-mono text-xs text-muted-foreground">{assignment.rosterDate}</td>
              <td className="px-4 py-3">
                <Badge variant={statusVariant[assignment.status]}>{assignment.status}</Badge>
              </td>
              <td className="px-4 py-3">
                <div className="flex justify-end gap-1.5">
                  {canEdit && (
                    <Button asChild variant="ghost" size="sm">
                      <Link to={`/admin/hr/shift-assignments/${assignment.id}/edit`}>Edit</Link>
                    </Button>
                  )}
                  {canDelete && (
                    <Button variant="ghost" size="sm" className="text-destructive hover:text-destructive" onClick={() => onDeleteRequested(assignment)}>
                      Delete
                    </Button>
                  )}
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
