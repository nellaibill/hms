import { useQuery } from '@tanstack/react-query';
import { usersApi } from '../services/apiClient';

interface StaffNameProps {
  staffId: string;
}

/**
 * Resolves a StaffId (bare GUID — no Staff module exists yet, see StaffSelect's own doc
 * comment) to a display name, so list/detail views don't show raw GUIDs. Reads the
 * low-sensitivity staff directory (GET /users/directory, any signed-in user) rather than the
 * admin Users list, so a role without identity-administration.view — e.g. Receptionist on a
 * patient's "updated by" line — still sees the name. Shares the ['staff-directory', ''] cache
 * entry with the other directory pickers. Falls back to a truncated id while loading or if
 * the id isn't a known active user.
 */
export function StaffName({ staffId }: StaffNameProps) {
  const { data } = useQuery({
    queryKey: ['staff-directory', ''],
    queryFn: () => usersApi.getStaffDirectory(),
  });

  const user = data?.find((item) => item.id === staffId);
  if (user) {
    return <>{user.firstName} {user.lastName}</>;
  }

  return <span className="font-mono text-xs text-muted-foreground">{staffId.slice(0, 8)}…</span>;
}
