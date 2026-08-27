import { useQuery } from '@tanstack/react-query';
import { SearchableSelect } from '@/components/ui/searchable-select';
import { mastersApi } from '../services/apiClient';

interface DesignationSelectProps {
  id: string;
  value: string;
  onValueChange: (value: string) => void;
  ariaLabel?: string;
  disabled?: boolean;
}

/**
 * Designation picker for HR Employee forms, backed by the real GET /api/v1/masters/designations
 * list — Designation lives in Masters (see features/masters/configs/designation.ts) and is
 * reached through the generic MastersApi, the same way DepartmentSelect goes through the
 * dedicated DepartmentsApi. Shares the ['masters','designation','select-list'] cache with
 * useDesignationDirectory so this feature never issues a redundant designation fetch.
 */
export function DesignationSelect({ id, value, onValueChange, ariaLabel = 'Designation', disabled }: DesignationSelectProps) {
  const { data } = useQuery({
    queryKey: ['masters', 'designation', 'select-list'],
    queryFn: () => mastersApi.list('designation', { pageSize: 100, isActive: true }),
  });

  const options = (data?.items ?? []).map((designation) => ({
    value: String(designation.id),
    label: `${String(designation.name)} (${String(designation.code)})`,
    keywords: String(designation.code),
  }));

  return (
    <SearchableSelect
      id={id}
      value={value}
      onValueChange={onValueChange}
      options={options}
      placeholder="Select designation…"
      searchPlaceholder="Search by name or code…"
      ariaLabel={ariaLabel}
      disabled={disabled}
    />
  );
}
