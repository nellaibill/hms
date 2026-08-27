import type { AttendanceResponse } from '@hms/shared';
import { Button } from '@/components/ui/button';
import { useAuth } from '@/features/auth/AuthContext';
import { AttendanceStatusBadge } from './AttendanceStatusBadge';

interface AttendanceTableProps {
  records: AttendanceResponse[];
  onEditRequested: (record: AttendanceResponse) => void;
  onCheckOutRequested: (record: AttendanceResponse) => void;
}

function formatTime(value: string | null | undefined): string {
  if (!value) return '—';
  return new Date(value).toLocaleTimeString('en-IN', { hour: '2-digit', minute: '2-digit' });
}

const todayIso = () => new Date().toISOString().slice(0, 10);

export function AttendanceTable({ records, onEditRequested, onCheckOutRequested }: AttendanceTableProps) {
  const { hasPermission } = useAuth();
  const canEdit = hasPermission('workforce-admin.edit');
  const today = todayIso();

  return (
    <div className="overflow-x-auto rounded-lg border border-border">
      <table className="w-full text-sm">
        <thead className="bg-muted/50 text-left text-xs font-medium uppercase tracking-wide text-muted-foreground">
          <tr>
            <th className="px-4 py-2.5">Employee</th>
            <th className="px-4 py-2.5">Date</th>
            <th className="px-4 py-2.5">Check-in</th>
            <th className="px-4 py-2.5">Check-out</th>
            <th className="px-4 py-2.5">Status</th>
            <th className="px-4 py-2.5">Remarks</th>
            <th className="px-4 py-2.5 text-right">Actions</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {records.map((record) => {
            // Check-out only makes sense on today's own record, and only once checked in —
            // historical dates are manual-correction-only (Edit), per this page's own design.
            const canCheckOut = record.attendanceDate === today && Boolean(record.checkInTime) && !record.checkOutTime;
            return (
              <tr key={record.id} className="hover:bg-muted/30">
                <td className="px-4 py-3">
                  <span className="font-medium text-foreground">{record.employeeName}</span>{' '}
                  <span className="font-mono text-xs text-muted-foreground">({record.employeeCode})</span>
                </td>
                <td className="px-4 py-3 font-mono text-xs text-muted-foreground">{record.attendanceDate}</td>
                <td className="px-4 py-3 text-muted-foreground">{formatTime(record.checkInTime)}</td>
                <td className="px-4 py-3 text-muted-foreground">{formatTime(record.checkOutTime)}</td>
                <td className="px-4 py-3">
                  <AttendanceStatusBadge status={record.status} />
                </td>
                <td className="px-4 py-3 text-muted-foreground">{record.remarks ?? '—'}</td>
                <td className="px-4 py-3">
                  <div className="flex justify-end gap-1.5">
                    {canEdit && canCheckOut && (
                      <Button variant="ghost" size="sm" onClick={() => onCheckOutRequested(record)}>
                        Check Out
                      </Button>
                    )}
                    {canEdit && (
                      <Button variant="ghost" size="sm" onClick={() => onEditRequested(record)}>
                        Edit
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
