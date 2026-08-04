import { NetworkError } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { SearchableSelect } from '@/components/ui/searchable-select';
import { listMockShifts } from '@/features/shifts/mockShiftsStore';
import { shiftsApi } from '../../../services/apiClient';

interface ShiftSelectProps {
  id: string;
  value: string;
  onValueChange: (value: string) => void;
  ariaLabel?: string;
}

/** Shift picker for forms referencing ShiftId (Shift Assignment), backed by the real
 * GET /api/v1/shifts list built in Phase 1. */
export function ShiftSelect({ id, value, onValueChange, ariaLabel = 'Shift' }: ShiftSelectProps) {
  const { data } = useQuery({
    queryKey: ['shifts', 'select-list'],
    queryFn: async () => {
      try {
        return await shiftsApi.getShifts({ pageSize: 100, isActive: true });
      } catch (err) {
        if (err instanceof NetworkError) {
          return listMockShifts({ pageSize: 100, isActive: true });
        }
        throw err;
      }
    },
  });

  const options = (data?.items ?? []).map((shift) => ({
    value: shift.id,
    label: `${shift.code} — ${shift.name}`,
    keywords: `${shift.startTime} ${shift.endTime}`,
  }));

  return (
    <SearchableSelect
      id={id}
      value={value}
      onValueChange={onValueChange}
      options={options}
      placeholder="Select shift…"
      searchPlaceholder="Search by code or name…"
      ariaLabel={ariaLabel}
    />
  );
}
