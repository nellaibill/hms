import type { LeaveTypeResponse } from '@hms/shared';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { useAuth } from '@/features/auth/AuthContext';

interface LeaveTypeTableProps {
  leaveTypes: LeaveTypeResponse[];
  onEditRequested: (leaveType: LeaveTypeResponse) => void;
  onDeleteRequested: (leaveType: LeaveTypeResponse) => void;
}

export function LeaveTypeTable({ leaveTypes, onEditRequested, onDeleteRequested }: LeaveTypeTableProps) {
  const { hasPermission } = useAuth();
  const canEdit = hasPermission('workforce-admin.edit');
  const canDelete = hasPermission('workforce-admin.delete');

  return (
    <div className="overflow-hidden rounded-lg border border-border">
      <table className="w-full text-sm">
        <thead className="bg-muted/50 text-left text-xs font-medium uppercase tracking-wide text-muted-foreground">
          <tr>
            <th className="px-4 py-2.5">Code</th>
            <th className="px-4 py-2.5">Name</th>
            <th className="px-4 py-2.5">Max Days/Year</th>
            <th className="px-4 py-2.5">Paid</th>
            <th className="px-4 py-2.5">Status</th>
            <th className="px-4 py-2.5 text-right">Actions</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {leaveTypes.map((leaveType) => (
            <tr key={leaveType.id} className="hover:bg-muted/30">
              <td className="px-4 py-3 font-mono text-xs text-muted-foreground">{leaveType.code}</td>
              <td className="px-4 py-3 font-medium text-foreground">{leaveType.name}</td>
              <td className="px-4 py-3 text-muted-foreground">{leaveType.maxDaysPerYear ?? 'Unlimited'}</td>
              <td className="px-4 py-3">
                <Badge variant={leaveType.isPaid ? 'success' : 'secondary'}>{leaveType.isPaid ? 'Paid' : 'Unpaid'}</Badge>
              </td>
              <td className="px-4 py-3">
                <Badge variant={leaveType.isActive ? 'success' : 'secondary'}>{leaveType.isActive ? 'Active' : 'Inactive'}</Badge>
              </td>
              <td className="px-4 py-3">
                <div className="flex justify-end gap-1.5">
                  {canEdit && (
                    <Button variant="ghost" size="sm" onClick={() => onEditRequested(leaveType)}>
                      Edit
                    </Button>
                  )}
                  {canDelete && (
                    <Button variant="ghost" size="sm" className="text-destructive hover:text-destructive" onClick={() => onDeleteRequested(leaveType)}>
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
