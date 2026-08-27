import { useQuery } from '@tanstack/react-query';
import { SearchableSelect, type SearchableSelectOption } from '@/components/ui/searchable-select';
import { employeesApi } from '../services/apiClient';

interface EmployeeSelectProps {
  id: string;
  value: string;
  onValueChange: (value: string) => void;
  ariaLabel?: string;
  disabled?: boolean;
  /** Exclude one employee id from the options — e.g. an employee can't be their own
   * reporting manager. */
  excludeId?: string;
  /** Prepends a "— None —" option (value ""), for optional employee references like
   * Employee.reportingManagerId. */
  includeNoneOption?: boolean;
}

/**
 * Employee picker shared across HR forms that reference an EmployeeId (Employee's own
 * Reporting Manager field, Attendance, Leave Requests), backed by the real
 * GET /api/v1/employees list. Shares the ['employees','select-list'] cache across every
 * consumer.
 */
export function EmployeeSelect({
  id,
  value,
  onValueChange,
  ariaLabel = 'Employee',
  disabled,
  excludeId,
  includeNoneOption,
}: EmployeeSelectProps) {
  const { data } = useQuery({
    queryKey: ['employees', 'select-list'],
    queryFn: () => employeesApi.getEmployees({ pageSize: 200, isActive: true }),
  });

  const options: SearchableSelectOption[] = (data?.items ?? [])
    .filter((employee) => employee.id !== excludeId)
    .map((employee) => ({
      value: employee.id,
      label: `${employee.firstName} ${employee.lastName} (${employee.employeeCode})`,
      keywords: `${employee.employeeCode} ${employee.email}`,
    }));

  const allOptions = includeNoneOption ? [{ value: '', label: '— None —' }, ...options] : options;

  return (
    <SearchableSelect
      id={id}
      value={value}
      onValueChange={onValueChange}
      options={allOptions}
      placeholder="Select employee…"
      searchPlaceholder="Search by name, code, or email…"
      ariaLabel={ariaLabel}
      disabled={disabled}
    />
  );
}
