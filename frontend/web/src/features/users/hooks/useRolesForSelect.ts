import { NetworkError } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { rolesApi } from '../../../services/apiClient';
import { listMockRoles } from '../../roles/mockRolesStore';

interface RoleOption {
  id: string;
  name: string;
}

// Users feature stays self-contained (calls the shared rolesApi directly rather than
// importing from features/roles' hooks) — no existing feature imports another feature's
// hooks, so this keeps the same isolation the rest of the app already follows. The
// NetworkError fallback below reaches into features/roles' mock data store directly (not
// its hooks) for the same reason apiRoleRepository.ts falls back to it — without this, the
// Role field here is a hard blocker on creating/editing any user whenever the backend is
// unreachable (e.g. the static GitHub Pages deploy), since the dropdown has no options.
export function useRolesForSelect() {
  return useQuery({
    queryKey: ['roles', 'select-list'],
    queryFn: async (): Promise<{ items: RoleOption[] }> => {
      try {
        const result = await rolesApi.getRoles({ pageSize: 100, isActive: true });
        return { items: result.items };
      } catch (err) {
        if (err instanceof NetworkError) {
          return { items: listMockRoles({ pageSize: 100, status: 'Active' }).items };
        }
        throw err;
      }
    },
  });
}
