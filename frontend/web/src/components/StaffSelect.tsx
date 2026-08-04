import { useQuery } from '@tanstack/react-query';
import { SearchableSelect } from '@/components/ui/searchable-select';
import { usersApi } from '../services/apiClient';

interface StaffSelectProps {
  id: string;
  value: string;
  onValueChange: (value: string) => void;
  ariaLabel?: string;
  disabled?: boolean;
}

/**
 * Staff picker shared across every HR (Duty Roster) form that references a StaffId
 * (Staff Availability, Shift Assignment, Shift Swap Request). No Staff/Department module
 * exists yet (see HMS.Modules.HR.Domain.ShiftAssignment's own doc comment), so this reuses
 * the existing, real GET /api/v1/users list as the closest stand-in for "who is staff" —
 * the same reuse pattern UserForm already applies to roleId via useRolesForSelect.
 */
export function StaffSelect({ id, value, onValueChange, ariaLabel = 'Staff', disabled }: StaffSelectProps) {
  const { data } = useQuery({
    queryKey: ['users', 'select-list'],
    queryFn: () => usersApi.getUsers({ pageSize: 100, isActive: true }),
  });

  const options = (data?.items ?? []).map((user) => ({
    value: user.id,
    label: `${user.firstName} ${user.lastName}`,
    keywords: `${user.username} ${user.roleName} ${user.email}`,
  }));

  return (
    <SearchableSelect
      id={id}
      value={value}
      onValueChange={onValueChange}
      options={options}
      placeholder="Select staff…"
      searchPlaceholder="Search by name, username, or role…"
      ariaLabel={ariaLabel}
      disabled={disabled}
    />
  );
}
