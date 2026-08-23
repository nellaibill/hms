import { useQuery } from '@tanstack/react-query';
import { SearchableSelect } from '@/components/ui/searchable-select';
import { statesApi } from '../services/apiClient';

interface DistrictSelectProps {
  id: string;
  /** District *name* (not id) — matches Patient.District's plain string field, same
   * name-based shape as StateSelect's value. */
  value: string;
  onValueChange: (value: string) => void;
  /** The currently selected state's *name* (StateSelect's own value shape) — resolved to an
   * id internally (via the same states list StateSelect fetches, so react-query dedupes the
   * request) since the backend's district list is fetched by state id, not name. */
  stateName: string | undefined;
  ariaLabel?: string;
  disabled?: boolean;
}

/** District picker for Patient Registration's Address section, scoped to the selected state
 * — mirrors ConsultantSelect's "picking a child before its parent means everything shows up
 * at once" reasoning. Backed by the real GET /api/v1/masters/states/{stateId}/districts list. */
export function DistrictSelect({ id, value, onValueChange, stateName, ariaLabel = 'District', disabled }: DistrictSelectProps) {
  const { data: states } = useQuery({
    queryKey: ['states', 'select-list'],
    queryFn: () => statesApi.getStates(),
  });
  const stateId = states?.find((state) => state.name === stateName)?.id;

  const { data: districts } = useQuery({
    queryKey: ['districts', 'select-list', stateId],
    queryFn: () => statesApi.getDistricts(stateId!),
    enabled: Boolean(stateId),
  });

  const options = (districts ?? []).map((district) => ({ value: district.name, label: district.name }));

  return (
    <SearchableSelect
      id={id}
      value={value}
      onValueChange={onValueChange}
      options={options}
      placeholder={stateName ? 'Select district…' : 'Select a state first…'}
      searchPlaceholder="Search district…"
      ariaLabel={ariaLabel}
      disabled={disabled || !stateId}
    />
  );
}
