import { useQuery } from '@tanstack/react-query';
import { SearchableSelect } from '@/components/ui/searchable-select';
import { wardsApi } from '../services/apiClient';

interface WardSelectProps {
  id: string;
  value: string;
  onValueChange: (value: string) => void;
  ariaLabel?: string;
  disabled?: boolean;
}

/** Ward picker shared across IPD forms that reference a WardId (New Admission, Bed
 * Transfer, Bed Master), backed by the real GET /api/v1/ipd/wards list. */
export function WardSelect({ id, value, onValueChange, ariaLabel = 'Ward', disabled }: WardSelectProps) {
  const { data } = useQuery({
    queryKey: ['ipd', 'wards', 'select-list'],
    queryFn: () => wardsApi.getWards({ pageSize: 100, isActive: true }),
  });

  const options = (data?.items ?? []).map((ward) => ({
    value: ward.id,
    label: `${ward.name} (${ward.code})`,
    keywords: ward.code,
  }));

  return (
    <SearchableSelect
      id={id}
      value={value}
      onValueChange={onValueChange}
      options={options}
      placeholder="Select ward…"
      searchPlaceholder="Search by name or code…"
      ariaLabel={ariaLabel}
      disabled={disabled}
    />
  );
}
