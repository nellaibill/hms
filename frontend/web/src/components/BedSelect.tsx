import type { Bed } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { useEffect } from 'react';
import { SearchableSelect } from '@/components/ui/searchable-select';
import { bedsApi } from '../services/apiClient';

interface BedSelectProps {
  id: string;
  value: string;
  onValueChange: (value: string) => void;
  /** Scopes the option list to beds belonging to this ward — required, since a bed only
   * makes sense once a ward has been chosen (New Admission / Bed Transfer). */
  wardId: string | undefined;
  ariaLabel?: string;
  disabled?: boolean;
  /** Reports the fetched bed list up to the parent so it can look up the selected bed's
   * tariff for a summary card, without duplicating the available-beds query. */
  onBedsLoaded?: (beds: Bed[]) => void;
}

/** Available-bed picker for New Admission / Bed Transfer, backed by the real
 * GET /api/v1/ipd/beds/available?wardId= list — only shows beds that can actually be
 * assigned right now, scoped to the currently-selected ward. */
export function BedSelect({ id, value, onValueChange, wardId, ariaLabel = 'Bed', disabled, onBedsLoaded }: BedSelectProps) {
  const { data } = useQuery({
    queryKey: ['ipd', 'beds', 'available', wardId],
    queryFn: () => bedsApi.getAvailableBeds(wardId),
    enabled: Boolean(wardId),
  });

  useEffect(() => {
    if (data) {
      onBedsLoaded?.(data);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [data]);

  const options = (data ?? []).map((bed) => ({
    value: bed.id,
    label: `${bed.bedNumber} (${bed.bedType}) - ₹${bed.dailyCharge.toLocaleString('en-IN')}/day`,
    keywords: bed.bedType,
  }));

  return (
    <SearchableSelect
      id={id}
      value={value}
      onValueChange={onValueChange}
      options={options}
      placeholder={wardId ? 'Select bed…' : 'Select a ward first…'}
      searchPlaceholder="Search by bed number or type…"
      ariaLabel={ariaLabel}
      disabled={disabled || !wardId}
    />
  );
}
