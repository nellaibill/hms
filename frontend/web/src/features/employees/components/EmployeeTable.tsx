import type { EmployeeResponse } from '@hms/shared';
import { ArrowDown, ArrowUp } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { useDepartmentNameById } from '@/features/calendarEvents/hooks/useDepartmentDirectory';
import { useAuth } from '@/features/auth/AuthContext';
import { useDesignationNameById } from '../hooks/useDesignationDirectory';
import { EmploymentStatusBadge } from './EmploymentStatusBadge';

interface EmployeeTableProps {
  employees: EmployeeResponse[];
  sort: string;
  onSortChange: (sort: string) => void;
  onDeleteRequested: (employee: EmployeeResponse) => void;
  onToggleActive: (employee: EmployeeResponse) => void;
  isTogglingId: string | undefined;
}

const columns: Array<{ field: string; label: string }> = [
  { field: 'employeeCode', label: 'Code' },
  { field: 'lastName', label: 'Name' },
];

export function EmployeeTable({ employees, sort, onSortChange, onDeleteRequested, onToggleActive, isTogglingId }: EmployeeTableProps) {
  const currentField = sort.startsWith('-') ? sort.slice(1) : sort;
  const isDescending = sort.startsWith('-');
  const { hasPermission } = useAuth();
  const canEdit = hasPermission('workforce-admin.edit');
  const canDelete = hasPermission('workforce-admin.delete');

  // EmployeeResponse.departmentName/designationName are only populated on the single-record
  // GET (see EmployeeResponse's own doc comment) — always null on this paged list, so the
  // table resolves both columns from small, cached lookup maps instead.
  const departmentNameById = useDepartmentNameById();
  const designationNameById = useDesignationNameById();

  function toggleSort(field: string) {
    if (currentField !== field) {
      onSortChange(field);
      return;
    }
    onSortChange(isDescending ? field : `-${field}`);
  }

  return (
    <div className="overflow-x-auto rounded-lg border border-border">
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
            <th className="px-4 py-2.5">Designation</th>
            <th className="px-4 py-2.5">Type</th>
            <th className="px-4 py-2.5">Employment Status</th>
            <th className="px-4 py-2.5">Active</th>
            <th className="px-4 py-2.5 text-right">Actions</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {employees.map((employee) => (
            <tr key={employee.id} className="hover:bg-muted/30">
              <td className="px-4 py-3 font-mono text-xs text-muted-foreground">{employee.employeeCode}</td>
              <td className="px-4 py-3">
                <Link to={`/admin/hr/employees/${employee.id}`} className="font-medium text-foreground hover:text-primary hover:underline">
                  {employee.firstName} {employee.lastName}
                </Link>
              </td>
              <td className="px-4 py-3 text-muted-foreground">{departmentNameById.get(employee.departmentId) ?? '—'}</td>
              <td className="px-4 py-3 text-muted-foreground">{designationNameById.get(employee.designationId) ?? '—'}</td>
              <td className="px-4 py-3 text-muted-foreground">{employee.employeeType}</td>
              <td className="px-4 py-3">
                <EmploymentStatusBadge status={employee.employmentStatus} />
              </td>
              <td className="px-4 py-3">
                <Badge variant={employee.isActive ? 'success' : 'secondary'}>{employee.isActive ? 'Active' : 'Inactive'}</Badge>
              </td>
              <td className="px-4 py-3">
                <div className="flex justify-end gap-1.5">
                  {canEdit && (
                    <Button asChild variant="ghost" size="sm">
                      <Link to={`/admin/hr/employees/${employee.id}/edit`}>Edit</Link>
                    </Button>
                  )}
                  {canEdit && (
                    <Button variant="ghost" size="sm" onClick={() => onToggleActive(employee)} disabled={isTogglingId === employee.id}>
                      {employee.isActive ? 'Deactivate' : 'Activate'}
                    </Button>
                  )}
                  {canDelete && (
                    <Button variant="ghost" size="sm" className="text-destructive hover:text-destructive" onClick={() => onDeleteRequested(employee)}>
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
