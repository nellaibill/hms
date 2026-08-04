import { NetworkError } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { listMockShifts } from '@/features/shifts/mockShiftsStore';
import { shiftsApi } from '../../../services/apiClient';

interface ShiftNameProps {
  shiftId: string;
}

/** Resolves a ShiftId to its code/name via the same cached ['shifts','select-list'] query
 * ShiftSelect populates, so list/detail views don't show a raw GUID. */
export function ShiftName({ shiftId }: ShiftNameProps) {
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

  const shift = data?.items.find((item) => item.id === shiftId);
  if (shift) {
    return <>{shift.code} — {shift.name}</>;
  }

  return <span className="font-mono text-xs text-muted-foreground">{shiftId.slice(0, 8)}…</span>;
}
