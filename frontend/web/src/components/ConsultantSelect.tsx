import { useQuery } from '@tanstack/react-query';
import { SearchableSelect } from '@/components/ui/searchable-select';
import { consultantsApi } from '../services/apiClient';

interface ConsultantSelectProps {
  id: string;
  value: string;
  onValueChange: (value: string) => void;
  ariaLabel?: string;
  disabled?: boolean;
}

/** Consultant picker shared across every form that references a ConsultantId (Patient
 * Registration), backed by the real GET /api/v1/masters/consultants list. */
export function ConsultantSelect({ id, value, onValueChange, ariaLabel = 'Consultant', disabled }: ConsultantSelectProps) {
  const { data } = useQuery({
    queryKey: ['consultants', 'select-list'],
    queryFn: () => consultantsApi.getConsultants({ pageSize: 100, isActive: true }),
  });

  // Always suffix the code — see DepartmentSelect's identical comment. Doctors especially
  // can share a display name (two "Dr. Sharma"s), and Code is the only guaranteed-unique field.
  const options = (data?.items ?? []).map((consultant) => ({
    value: consultant.id,
    label: `${consultant.name} (${consultant.code})`,
    keywords: consultant.code,
  }));

  return (
    <SearchableSelect
      id={id}
      value={value}
      onValueChange={onValueChange}
      options={options}
      placeholder="Select consultant…"
      searchPlaceholder="Search by name or code…"
      ariaLabel={ariaLabel}
      disabled={disabled}
    />
  );
}
