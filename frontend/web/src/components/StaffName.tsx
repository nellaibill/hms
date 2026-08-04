import { NetworkError } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { listMockUsers } from '@/features/users/mockUsersStore';
import { usersApi } from '../services/apiClient';

interface StaffNameProps {
  staffId: string;
}

/**
 * Resolves a StaffId (bare GUID — no Staff module exists yet, see StaffSelect's own doc
 * comment) to a display name via the same cached ['users','select-list'] query StaffSelect
 * populates, so list/detail views don't show raw GUIDs. Falls back to a truncated id while
 * loading or if the id isn't a known user.
 */
export function StaffName({ staffId }: StaffNameProps) {
  const { data } = useQuery({
    queryKey: ['users', 'select-list'],
    queryFn: async () => {
      try {
        return await usersApi.getUsers({ pageSize: 100, isActive: true });
      } catch (err) {
        if (err instanceof NetworkError) {
          return listMockUsers({ pageSize: 100, isActive: true });
        }
        throw err;
      }
    },
  });

  const user = data?.items.find((item) => item.id === staffId);
  if (user) {
    return <>{user.firstName} {user.lastName}</>;
  }

  return <span className="font-mono text-xs text-muted-foreground">{staffId.slice(0, 8)}…</span>;
}
