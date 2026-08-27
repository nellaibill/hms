import type { AttendanceResponse, AttendanceStatus } from '@hms/shared';
import { CalendarClock, Loader2 } from 'lucide-react';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { Card, CardContent } from '@/components/ui/card';
import { Pagination } from '@/components/Pagination';
import {
  AttendanceFormDialog,
  AttendanceListToolbar,
  AttendanceTable,
  CheckInOutModal,
  useAttendanceQuery,
} from '../../features/attendance';

function todayIso() {
  return new Date().toISOString().slice(0, 10);
}

export default function AttendanceListPage() {
  const [employeeId, setEmployeeId] = useState<string | undefined>(undefined);
  const [departmentId, setDepartmentId] = useState<string | undefined>(undefined);
  const [status, setStatus] = useState<string | undefined>(undefined);
  const [dateFrom, setDateFrom] = useState(todayIso());
  const [dateTo, setDateTo] = useState(todayIso());
  const [page, setPage] = useState(1);

  const [checkModal, setCheckModal] = useState<{ mode: 'check-in' | 'check-out'; employeeId?: string } | null>(null);
  const [formDialog, setFormDialog] = useState<{ mode: 'create' | 'edit'; attendance?: AttendanceResponse } | null>(null);

  const { data, isPending, isError, error } = useAttendanceQuery({
    page,
    pageSize: 20,
    sort: '-attendanceDate',
    employeeId,
    departmentId,
    status: status as AttendanceStatus | undefined,
    dateFrom,
    dateTo,
  });

  function resetToToday() {
    const today = todayIso();
    setDateFrom(today);
    setDateTo(today);
    setPage(1);
  }

  return (
    <div className="flex flex-1 flex-col">
      <div className="px-6 pt-4 lg:px-8">
        <Link to="/admin/hr" className="text-sm text-muted-foreground hover:text-foreground">
          &larr; Back to HR
        </Link>
      </div>

      <div className="mt-3 flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        <div className="flex items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
            <CalendarClock className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight">Attendance</h1>
        </div>
        <p className="max-w-2xl text-sm text-page-banner-foreground/85">Daily check-in/check-out tracking and manual corrections.</p>
      </div>

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
        <AttendanceListToolbar
          employeeId={employeeId}
          onEmployeeIdChange={(value) => {
            setEmployeeId(value);
            setPage(1);
          }}
          departmentId={departmentId}
          onDepartmentIdChange={(value) => {
            setDepartmentId(value);
            setPage(1);
          }}
          status={status}
          onStatusChange={(value) => {
            setStatus(value);
            setPage(1);
          }}
          dateFrom={dateFrom}
          onDateFromChange={(value) => {
            setDateFrom(value);
            setPage(1);
          }}
          dateTo={dateTo}
          onDateToChange={(value) => {
            setDateTo(value);
            setPage(1);
          }}
          onTodayRequested={resetToToday}
          onCheckInRequested={() => setCheckModal({ mode: 'check-in' })}
          onNewAttendanceRequested={() => setFormDialog({ mode: 'create' })}
        />

        {isPending && (
          <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" />
            Loading attendance…
          </div>
        )}

        {isError && (
          <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
            {error instanceof Error ? error.message : 'Failed to load attendance records.'}
          </p>
        )}

        {!isPending && !isError && data && data.items.length === 0 && (
          <Card className="border-dashed">
            <CardContent className="flex flex-col items-center gap-2 py-16 text-center">
              <p className="text-sm font-medium text-foreground">No attendance records found</p>
              <p className="text-sm text-muted-foreground">Try a different date range, or check an employee in.</p>
            </CardContent>
          </Card>
        )}

        {!isPending && !isError && data && data.items.length > 0 && (
          <div className="flex flex-col gap-3">
            <AttendanceTable
              records={data.items}
              onEditRequested={(record) => setFormDialog({ mode: 'edit', attendance: record })}
              onCheckOutRequested={(record) => setCheckModal({ mode: 'check-out', employeeId: record.employeeId })}
            />
            <Pagination meta={data.meta} onPageChange={setPage} />
          </div>
        )}

        {checkModal && <CheckInOutModal mode={checkModal.mode} employeeId={checkModal.employeeId} onClose={() => setCheckModal(null)} />}

        {formDialog && (
          <AttendanceFormDialog mode={formDialog.mode} attendance={formDialog.attendance} onClose={() => setFormDialog(null)} />
        )}
      </div>
    </div>
  );
}
