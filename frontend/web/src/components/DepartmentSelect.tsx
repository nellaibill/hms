import { useQuery } from '@tanstack/react-query';
import { SearchableSelect } from '@/components/ui/searchable-select';
import { departmentsApi } from '../services/apiClient';

interface DepartmentSelectProps {
  id: string;
  value: string;
  onValueChange: (value: string) => void;
  ariaLabel?: string;
  disabled?: boolean;
}

/** Department picker shared across every HR form that references a DepartmentId
 * (Weekly Roster, Shift Assignment), backed by the real GET /api/v1/masters/departments list. */
export function DepartmentSelect({ id, value, onValueChange, ariaLabel = 'Department', disabled }: DepartmentSelectProps) {
  const { data } = useQuery({
    queryKey: ['departments', 'select-list'],
    queryFn: () => departmentsApi.getDepartments({ pageSize: 100, isActive: true }),
  });

  // Always suffix the code, even though most department names are unique in practice —
  // Code is the only field guaranteed unique (enforced at creation), so this is the one
  // label that reliably tells two same-named departments apart (e.g. two "Intensive Care
  // Unit" wards with different codes) rather than only disambiguating when duplicates
  // happen to be noticed.
  const options = (data?.items ?? []).map((department) => ({
    value: department.id,
    label: `${department.name} (${department.code})`,
    keywords: department.code,
  }));

  return (
    <SearchableSelect
      id={id}
      value={value}
      onValueChange={onValueChange}
      options={options}
      placeholder="Select department…"
      searchPlaceholder="Search by name or code…"
      ariaLabel={ariaLabel}
      disabled={disabled}
    />
  );
}
