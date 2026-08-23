import { useQuery } from '@tanstack/react-query';
import { SearchableSelect } from '@/components/ui/searchable-select';
import { statesApi } from '../services/apiClient';

interface StateSelectProps {
  id: string;
  /** State *name* (not id) — matches Patient.State's plain string field, so no bridging is
   * needed between this control and the form/backend contract. */
  value: string;
  onValueChange: (value: string) => void;
  ariaLabel?: string;
  disabled?: boolean;
}

/** State picker for Patient Registration's Address section, backed by the real
 * GET /api/v1/masters/states list (India only — see StatesController's own doc comment). */
export function StateSelect({ id, value, onValueChange, ariaLabel = 'State', disabled }: StateSelectProps) {
  const { data } = useQuery({
    queryKey: ['states', 'select-list'],
    queryFn: () => statesApi.getStates(),
  });

  const options = (data ?? []).map((state) => ({ value: state.name, label: state.name }));

  return (
    <SearchableSelect
      id={id}
      value={value}
      onValueChange={onValueChange}
      options={options}
      placeholder="Select state…"
      searchPlaceholder="Search state…"
      ariaLabel={ariaLabel}
      disabled={disabled}
    />
  );
}
