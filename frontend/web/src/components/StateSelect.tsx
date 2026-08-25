import { useQuery } from '@tanstack/react-query';
import { SearchableSelect } from '@/components/ui/searchable-select';
import { statesApi } from '../services/apiClient';

interface StateSelectProps {
  id: string;
  /** State id (a real Masters State record's Guid) — matches Address.StateId's Guid field. */
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

  const options = (data ?? []).map((state) => ({ value: state.id, label: state.name }));

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
