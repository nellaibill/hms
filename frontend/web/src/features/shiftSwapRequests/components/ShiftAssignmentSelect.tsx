import { useQuery } from '@tanstack/react-query';
import { SearchableSelect } from '@/components/ui/searchable-select';
import { shiftAssignmentsApi } from '../../../services/apiClient';

interface ShiftAssignmentSelectProps {
  id: string;
  value: string;
  onValueChange: (value: string) => void;
  ariaLabel?: string;
}

/** Shift assignment picker for the Shift Swap Request form's Current/Requested assignment
 * fields, backed by the real GET /api/v1/shift-assignments list built in Phase 4. */
export function ShiftAssignmentSelect({ id, value, onValueChange, ariaLabel = 'Shift assignment' }: ShiftAssignmentSelectProps) {
  const { data } = useQuery({
    queryKey: ['shiftAssignments', 'select-list'],
    queryFn: () => shiftAssignmentsApi.getShiftAssignments({ pageSize: 100 }),
  });

  const options = (data?.items ?? []).map((assignment) => ({
    value: assignment.id,
    label: `${assignment.rosterDate} — ${assignment.status}`,
    keywords: assignment.id,
  }));

  return (
    <SearchableSelect
      id={id}
      value={value}
      onValueChange={onValueChange}
      options={options}
      placeholder="Select shift assignment…"
      searchPlaceholder="Search by date or status…"
      ariaLabel={ariaLabel}
    />
  );
}
