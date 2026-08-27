import { ATTENDANCE_STATUSES } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { LogIn, Plus } from 'lucide-react';
import { EmployeeSelect } from '@/components/EmployeeSelect';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { SearchableSelect } from '@/components/ui/searchable-select';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { useAuth } from '@/features/auth/AuthContext';
import { departmentsApi } from '@/services/apiClient';

interface AttendanceListToolbarProps {
  employeeId: string | undefined;
  onEmployeeIdChange: (value: string | undefined) => void;
  departmentId: string | undefined;
  onDepartmentIdChange: (value: string | undefined) => void;
  status: string | undefined;
  onStatusChange: (value: string | undefined) => void;
  dateFrom: string;
  onDateFromChange: (value: string) => void;
  dateTo: string;
  onDateToChange: (value: string) => void;
  onTodayRequested: () => void;
  onCheckInRequested: () => void;
  onNewAttendanceRequested: () => void;
}

export function AttendanceListToolbar({
  employeeId,
  onEmployeeIdChange,
  departmentId,
  onDepartmentIdChange,
  status,
  onStatusChange,
  dateFrom,
  onDateFromChange,
  dateTo,
  onDateToChange,
  onTodayRequested,
  onCheckInRequested,
  onNewAttendanceRequested,
}: AttendanceListToolbarProps) {
  const { hasPermission } = useAuth();
  const canCreate = hasPermission('workforce-admin.create');

  // Plain SearchableSelect (not the form-oriented DepartmentSelect) so an "All departments"
  // option can be offered — mirrors EmployeeListToolbar's own reasoning.
  const departmentsQuery = useQuery({
    queryKey: ['departments', 'select-list'],
    queryFn: () => departmentsApi.getDepartments({ pageSize: 100, isActive: true }),
  });
  const departmentOptions = [
    { value: '', label: 'All departments' },
    ...(departmentsQuery.data?.items ?? []).map((department) => ({
      value: department.id,
      label: `${department.name} (${department.code})`,
      keywords: department.code,
    })),
  ];

  return (
    <div className="flex flex-wrap items-end gap-3">
      <div className="w-56">
        <Label htmlFor="filter-employee" className="mb-1.5 block text-xs text-muted-foreground">
          Employee
        </Label>
        <EmployeeSelect
          id="filter-employee"
          value={employeeId ?? ''}
          onValueChange={(value) => onEmployeeIdChange(value || undefined)}
          includeNoneOption
          noneLabel="All employees"
          ariaLabel="Filter by employee"
        />
      </div>

      <div className="w-48">
        <Label htmlFor="filter-department" className="mb-1.5 block text-xs text-muted-foreground">
          Department
        </Label>
        <SearchableSelect
          id="filter-department"
          value={departmentId ?? ''}
          onValueChange={(value) => onDepartmentIdChange(value || undefined)}
          options={departmentOptions}
          placeholder="All departments"
          searchPlaceholder="Search by name or code…"
          ariaLabel="Filter by department"
        />
      </div>

      <div className="w-40">
        <Label htmlFor="filter-status" className="mb-1.5 block text-xs text-muted-foreground">
          Status
        </Label>
        <Select value={status ?? 'all'} onValueChange={(value) => onStatusChange(value === 'all' ? undefined : value)}>
          <SelectTrigger id="filter-status" aria-label="Filter by status">
            <SelectValue placeholder="All statuses" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All statuses</SelectItem>
            {ATTENDANCE_STATUSES.map((s) => (
              <SelectItem key={s} value={s}>
                {s}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      <div className="w-40">
        <Label htmlFor="filter-dateFrom" className="mb-1.5 block text-xs text-muted-foreground">
          From
        </Label>
        <Input id="filter-dateFrom" type="date" value={dateFrom} onChange={(event) => onDateFromChange(event.target.value)} />
      </div>

      <div className="w-40">
        <Label htmlFor="filter-dateTo" className="mb-1.5 block text-xs text-muted-foreground">
          To
        </Label>
        <Input id="filter-dateTo" type="date" value={dateTo} onChange={(event) => onDateToChange(event.target.value)} />
      </div>

      <Button type="button" variant="outline" onClick={onTodayRequested}>
        Today
      </Button>

      <div className="ml-auto flex gap-2">
        {canCreate && (
          <Button type="button" variant="outline" className="gap-1.5" onClick={onCheckInRequested}>
            <LogIn className="h-4 w-4" />
            Check In
          </Button>
        )}
        {canCreate && (
          <Button type="button" className="gap-1.5" onClick={onNewAttendanceRequested}>
            <Plus className="h-4 w-4" />
            New Attendance
          </Button>
        )}
      </div>
    </div>
  );
}
