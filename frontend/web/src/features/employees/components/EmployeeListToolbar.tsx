import { EMPLOYEE_TYPES, EMPLOYMENT_STATUSES } from '@hms/shared';
import { Plus, Search } from 'lucide-react';
import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { SearchableSelect } from '@/components/ui/searchable-select';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { useAuth } from '@/features/auth/AuthContext';
import { departmentsApi, mastersApi } from '@/services/apiClient';

interface EmployeeListToolbarProps {
  search: string;
  onSearchChange: (value: string) => void;
  departmentId: string | undefined;
  onDepartmentIdChange: (value: string | undefined) => void;
  designationId: string | undefined;
  onDesignationIdChange: (value: string | undefined) => void;
  employeeType: string | undefined;
  onEmployeeTypeChange: (value: string | undefined) => void;
  employmentStatus: string | undefined;
  onEmploymentStatusChange: (value: string | undefined) => void;
  isActive: boolean | undefined;
  onIsActiveChange: (value: boolean | undefined) => void;
}

export function EmployeeListToolbar({
  search,
  onSearchChange,
  departmentId,
  onDepartmentIdChange,
  designationId,
  onDesignationIdChange,
  employeeType,
  onEmployeeTypeChange,
  employmentStatus,
  onEmploymentStatusChange,
  isActive,
  onIsActiveChange,
}: EmployeeListToolbarProps) {
  const { hasPermission } = useAuth();

  // Both filters use SearchableSelect directly (rather than the form-oriented
  // DepartmentSelect/DesignationSelect components) so an "All departments"/"All
  // designations" option can be offered — those components always require a real selection,
  // which a form field needs but a clearable list filter doesn't. Same underlying cache keys,
  // so no extra network requests beyond what the rest of this page already triggers.
  const departmentsQuery = useQuery({
    queryKey: ['departments', 'select-list'],
    queryFn: () => departmentsApi.getDepartments({ pageSize: 100, isActive: true }),
  });
  const designationsQuery = useQuery({
    queryKey: ['masters', 'designation', 'select-list'],
    queryFn: () => mastersApi.list('designation', { pageSize: 100, isActive: true }),
  });

  const departmentOptions = [
    { value: '', label: 'All departments' },
    ...(departmentsQuery.data?.items ?? []).map((department) => ({
      value: department.id,
      label: `${department.name} (${department.code})`,
      keywords: department.code,
    })),
  ];

  const designationOptions = [
    { value: '', label: 'All designations' },
    ...(designationsQuery.data?.items ?? []).map((designation) => ({
      value: String(designation.id),
      label: `${String(designation.name)} (${String(designation.code)})`,
      keywords: String(designation.code),
    })),
  ];

  return (
    <div className="flex flex-wrap items-center gap-3">
      <div className="relative w-64">
        <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
        <Input
          type="search"
          placeholder="Search by code, name, or email…"
          value={search}
          onChange={(event) => onSearchChange(event.target.value)}
          aria-label="Search employees"
          className="pl-9"
        />
      </div>

      <div className="w-48">
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

      <div className="w-48">
        <SearchableSelect
          id="filter-designation"
          value={designationId ?? ''}
          onValueChange={(value) => onDesignationIdChange(value || undefined)}
          options={designationOptions}
          placeholder="All designations"
          searchPlaceholder="Search by name or code…"
          ariaLabel="Filter by designation"
        />
      </div>

      <Select value={employeeType ?? 'all'} onValueChange={(value) => onEmployeeTypeChange(value === 'all' ? undefined : value)}>
        <SelectTrigger className="w-40" aria-label="Filter by employee type">
          <SelectValue placeholder="All types" />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="all">All types</SelectItem>
          {EMPLOYEE_TYPES.map((type) => (
            <SelectItem key={type} value={type}>
              {type}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>

      <Select value={employmentStatus ?? 'all'} onValueChange={(value) => onEmploymentStatusChange(value === 'all' ? undefined : value)}>
        <SelectTrigger className="w-44" aria-label="Filter by employment status">
          <SelectValue placeholder="All employment statuses" />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="all">All employment statuses</SelectItem>
          {EMPLOYMENT_STATUSES.map((status) => (
            <SelectItem key={status} value={status}>
              {status}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>

      <Select
        value={isActive === undefined ? 'all' : String(isActive)}
        onValueChange={(value) => onIsActiveChange(value === 'all' ? undefined : value === 'true')}
      >
        <SelectTrigger className="w-36" aria-label="Filter by active status">
          <SelectValue placeholder="All statuses" />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="all">All statuses</SelectItem>
          <SelectItem value="true">Active only</SelectItem>
          <SelectItem value="false">Inactive only</SelectItem>
        </SelectContent>
      </Select>

      {hasPermission('workforce-admin.create') && (
        <Button asChild className="ml-auto gap-1.5">
          <Link to="/admin/hr/employees/new">
            <Plus className="h-4 w-4" />
            New Employee
          </Link>
        </Button>
      )}
    </div>
  );
}
