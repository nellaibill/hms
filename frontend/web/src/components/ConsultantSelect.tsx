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
  /** Filter-toolbar mode: prepends an "All consultants"-style option whose value is '' (the
   * caller maps '' to an unset filter). Also lifts the department-first requirement — with no
   * department chosen ("All departments"), every active consultant is listed, since a filter
   * has no reason to force a department before narrowing by consultant. */
  allOptionLabel?: string;
}

/** Consultant picker shared across every form that references a ConsultantId (Patient
 * Registration, IPD Admission), backed by the real GET /api/v1/masters/consultants list. */
export function ConsultantSelect({ id, value, onValueChange, departmentId, ariaLabel = 'Consultant', disabled, allOptionLabel }: ConsultantSelectProps) {
  const canList = Boolean(departmentId) || Boolean(allOptionLabel);
  const { data } = useQuery({
    queryKey: ['consultants', 'select-list', departmentId],
    queryFn: () => consultantsApi.getConsultants({ pageSize: 100, isActive: true, departmentId }),
    enabled: canList,
  });

  // Doctors especially can share a display name (two "Dr. Sharma"s) — Specialization is the
  // best available disambiguator now that Code is gone (there's no other guaranteed-unique,
  // human-readable field left on this entity).
  const options = [
    ...(allOptionLabel ? [{ value: '', label: allOptionLabel }] : []),
    ...(data?.items ?? []).map((consultant) => ({
      value: consultant.id,
      label: consultant.specialization ? `${consultant.name} — ${consultant.specialization}` : consultant.name,
    })),
  ];

  return (
    <SearchableSelect
      id={id}
      value={value}
      onValueChange={onValueChange}
      options={options}
      placeholder={allOptionLabel ?? (departmentId ? 'Select consultant…' : 'Select a department first…')}
      searchPlaceholder="Search by name…"
      ariaLabel={ariaLabel}
      disabled={disabled || !canList}
    />
  );
}
