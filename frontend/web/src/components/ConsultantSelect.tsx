import { useQuery } from '@tanstack/react-query';
import { SearchableSelect } from '@/components/ui/searchable-select';
import { consultantsApi } from '../services/apiClient';

interface ConsultantSelectProps {
  id: string;
  value: string;
  onValueChange: (value: string) => void;
  /** Scopes the consultant list to this department — required, since picking a consultant
   * before a department is chosen means every consultant across every department shows up
   * at once (confirmed live: a 36-doctor, 18-department list with no way to tell which
   * doctor belongs to which department). Mirrors ProductBatchSelect's productId shape. */
  departmentId: string | undefined;
  ariaLabel?: string;
  disabled?: boolean;
}

/** Consultant picker shared across every form that references a ConsultantId (Patient
 * Registration, IPD Admission), backed by the real GET /api/v1/masters/consultants list. */
export function ConsultantSelect({ id, value, onValueChange, departmentId, ariaLabel = 'Consultant', disabled }: ConsultantSelectProps) {
  const { data } = useQuery({
    queryKey: ['consultants', 'select-list', departmentId],
    queryFn: () => consultantsApi.getConsultants({ pageSize: 100, isActive: true, departmentId }),
    enabled: Boolean(departmentId),
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
      placeholder={departmentId ? 'Select consultant…' : 'Select a department first…'}
      searchPlaceholder="Search by name or code…"
      ariaLabel={ariaLabel}
      disabled={disabled || !departmentId}
    />
  );
}
