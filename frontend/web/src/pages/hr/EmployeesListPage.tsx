import type { EmployeeResponse, EmployeeType, EmploymentStatus } from '@hms/shared';
import { Loader2, Users } from 'lucide-react';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { Card, CardContent } from '@/components/ui/card';
import { Pagination } from '@/components/Pagination';
import { useDebouncedValue } from '../../hooks/useDebouncedValue';
import {
  DeleteEmployeeDialog,
  EmployeeListToolbar,
  EmployeeTable,
  useActivateEmployeeMutation,
  useDeactivateEmployeeMutation,
  useDeleteEmployeeMutation,
  useEmployeesQuery,
} from '../../features/employees';

export default function EmployeesListPage() {
  const [search, setSearch] = useState('');
  const [departmentId, setDepartmentId] = useState<string | undefined>(undefined);
  const [designationId, setDesignationId] = useState<string | undefined>(undefined);
  const [employeeType, setEmployeeType] = useState<string | undefined>(undefined);
  const [employmentStatus, setEmploymentStatus] = useState<string | undefined>(undefined);
  const [isActive, setIsActive] = useState<boolean | undefined>(undefined);
  const [page, setPage] = useState(1);
  const [sort, setSort] = useState('employeeCode');
  const [employeePendingDelete, setEmployeePendingDelete] = useState<EmployeeResponse | null>(null);

  const debouncedSearch = useDebouncedValue(search);

  const { data, isPending, isError, error } = useEmployeesQuery({
    page,
    pageSize: 20,
    sort,
    search: debouncedSearch || undefined,
    departmentId,
    designationId,
    employeeType: employeeType as EmployeeType | undefined,
    employmentStatus: employmentStatus as EmploymentStatus | undefined,
    isActive,
  });

  const deleteMutation = useDeleteEmployeeMutation();
  const activateMutation = useActivateEmployeeMutation();
  const deactivateMutation = useDeactivateEmployeeMutation();

  const isTogglingId = activateMutation.isPending
    ? (activateMutation.variables as string | undefined)
    : deactivateMutation.isPending
      ? (deactivateMutation.variables as string | undefined)
      : undefined;

  function resetPage() {
    setPage(1);
  }

  function handleToggleActive(employee: EmployeeResponse) {
    if (employee.isActive) {
      deactivateMutation.mutate(employee.id);
    } else {
      activateMutation.mutate(employee.id);
    }
  }

  function handleConfirmDelete() {
    if (!employeePendingDelete) {
      return;
    }
    deleteMutation.mutate(employeePendingDelete.id, {
      onSuccess: () => setEmployeePendingDelete(null),
    });
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
            <Users className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight">Employee Management</h1>
        </div>
        <p className="max-w-2xl text-sm text-page-banner-foreground/85">Staff directory — the hospital's employee master records.</p>
      </div>

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
        <EmployeeListToolbar
          search={search}
          onSearchChange={(value) => {
            setSearch(value);
            resetPage();
          }}
          departmentId={departmentId}
          onDepartmentIdChange={(value) => {
            setDepartmentId(value);
            resetPage();
          }}
          designationId={designationId}
          onDesignationIdChange={(value) => {
            setDesignationId(value);
            resetPage();
          }}
          employeeType={employeeType}
          onEmployeeTypeChange={(value) => {
            setEmployeeType(value);
            resetPage();
          }}
          employmentStatus={employmentStatus}
          onEmploymentStatusChange={(value) => {
            setEmploymentStatus(value);
            resetPage();
          }}
          isActive={isActive}
          onIsActiveChange={(value) => {
            setIsActive(value);
            resetPage();
          }}
        />

        {isPending && (
          <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" />
            Loading employees…
          </div>
        )}

        {isError && (
          <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
            {error instanceof Error ? error.message : 'Failed to load employees.'}
          </p>
        )}

        {!isPending && !isError && data && data.items.length === 0 && (
          <Card className="border-dashed">
            <CardContent className="flex flex-col items-center gap-2 py-16 text-center">
              <p className="text-sm font-medium text-foreground">No employees found</p>
              <p className="text-sm text-muted-foreground">
                {debouncedSearch ? `No results for "${debouncedSearch}".` : 'Create the first employee record to get started.'}
              </p>
            </CardContent>
          </Card>
        )}

        {!isPending && !isError && data && data.items.length > 0 && (
          <div className="flex flex-col gap-3">
            <EmployeeTable
              employees={data.items}
              sort={sort}
              onSortChange={(value) => {
                setSort(value);
                resetPage();
              }}
              onDeleteRequested={setEmployeePendingDelete}
              onToggleActive={handleToggleActive}
              isTogglingId={isTogglingId}
            />
            <Pagination meta={data.meta} onPageChange={setPage} />
          </div>
        )}

        {employeePendingDelete && (
          <DeleteEmployeeDialog
            employee={employeePendingDelete}
            isDeleting={deleteMutation.isPending}
            onConfirm={handleConfirmDelete}
            onCancel={() => setEmployeePendingDelete(null)}
          />
        )}
      </div>
    </div>
  );
}
