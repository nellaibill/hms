import { useQuery } from '@tanstack/react-query';
import { SearchableSelect } from '@/components/ui/searchable-select';
import { statesApi } from '../services/apiClient';

interface DistrictSelectProps {
  id: string;
  /** District id (a real Masters District record's Guid) — matches Address.DistrictId's Guid field. */
  value: string;
  onValueChange: (value: string) => void;
  /** The currently selected state's id (StateSelect's own value shape) — used directly to
   * fetch this state's districts, no name resolution needed. */
  stateId: string | undefined;
  ariaLabel?: string;
  disabled?: boolean;
}

/** District picker for Patient Registration's Address section, scoped to the selected state
 * — mirrors ConsultantSelect's "picking a child before its parent means everything shows up
 * at once" reasoning. Backed by the real GET /api/v1/masters/states/{stateId}/districts list. */
export function DistrictSelect({ id, value, onValueChange, stateId, ariaLabel = 'District', disabled }: DistrictSelectProps) {
  const { data: districts } = useQuery({
    queryKey: ['districts', 'select-list', stateId],
    queryFn: () => statesApi.getDistricts(stateId!),
    enabled: Boolean(stateId),
  });

  const options = (districts ?? []).map((district) => ({ value: district.id, label: district.name }));

  return (
    <SearchableSelect
      id={id}
      value={value}
      onValueChange={onValueChange}
      options={options}
      placeholder={stateId ? 'Select district…' : 'Select a state first…'}
      searchPlaceholder="Search district…"
      ariaLabel={ariaLabel}
      disabled={disabled || !stateId}
    />
  );
}
