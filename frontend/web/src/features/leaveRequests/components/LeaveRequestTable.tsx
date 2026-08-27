import type { LeaveRequestResponse } from '@hms/shared';
import { Button } from '@/components/ui/button';
import { useAuth } from '@/features/auth/AuthContext';
import { LeaveRequestStatusBadge } from './LeaveRequestStatusBadge';

interface LeaveRequestTableProps {
  leaveRequests: LeaveRequestResponse[];
  onApproveRequested: (leaveRequest: LeaveRequestResponse) => void;
  onRejectRequested: (leaveRequest: LeaveRequestResponse) => void;
  onCancelRequested: (leaveRequest: LeaveRequestResponse) => void;
  isCancellingId: string | undefined;
}

export function LeaveRequestTable({
  leaveRequests,
  onApproveRequested,
  onRejectRequested,
  onCancelRequested,
  isCancellingId,
}: LeaveRequestTableProps) {
  const { hasPermission } = useAuth();
  const canEdit = hasPermission('workforce-admin.edit');

  return (
    <div className="overflow-x-auto rounded-lg border border-border">
      <table className="w-full text-sm">
        <thead className="bg-muted/50 text-left text-xs font-medium uppercase tracking-wide text-muted-foreground">
          <tr>
            <th className="px-4 py-2.5">Employee</th>
            <th className="px-4 py-2.5">Leave Type</th>
            <th className="px-4 py-2.5">Dates</th>
            <th className="px-4 py-2.5">Days</th>
            <th className="px-4 py-2.5">Reason</th>
            <th className="px-4 py-2.5">Status</th>
            <th className="px-4 py-2.5 text-right">Actions</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {leaveRequests.map((leaveRequest) => {
            const isPendingStatus = leaveRequest.status === 'Pending';
            return (
              <tr key={leaveRequest.id} className="hover:bg-muted/30">
                <td className="px-4 py-3">
                  <span className="font-medium text-foreground">{leaveRequest.employeeName}</span>{' '}
                  <span className="font-mono text-xs text-muted-foreground">({leaveRequest.employeeCode})</span>
                </td>
                <td className="px-4 py-3 text-muted-foreground">{leaveRequest.leaveTypeName}</td>
                <td className="px-4 py-3 font-mono text-xs text-muted-foreground">
                  {leaveRequest.startDate} – {leaveRequest.endDate}
                </td>
                <td className="px-4 py-3 text-muted-foreground">{leaveRequest.totalDays}</td>
                <td className="max-w-xs truncate px-4 py-3 text-muted-foreground" title={leaveRequest.reason}>
                  {leaveRequest.reason}
                </td>
                <td className="px-4 py-3">
                  <LeaveRequestStatusBadge status={leaveRequest.status} />
                </td>
                <td className="px-4 py-3">
                  <div className="flex justify-end gap-1.5">
                    {canEdit && isPendingStatus && (
                      <Button variant="ghost" size="sm" onClick={() => onApproveRequested(leaveRequest)}>
                        Approve
                      </Button>
                    )}
                    {canEdit && isPendingStatus && (
                      <Button variant="ghost" size="sm" className="text-destructive hover:text-destructive" onClick={() => onRejectRequested(leaveRequest)}>
                        Reject
                      </Button>
                    )}
                    {canEdit && isPendingStatus && (
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => onCancelRequested(leaveRequest)}
                        disabled={isCancellingId === leaveRequest.id}
                      >
                        Cancel
                      </Button>
                    )}
                  </div>
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}
