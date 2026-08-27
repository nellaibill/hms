import { useQuery } from '@tanstack/react-query';
import { rolesApi } from '../../../services/apiClient';

interface RoleOption {
  id: string;
  name: string;
}

// Users feature stays self-contained (calls the shared rolesApi directly rather than
// importing from features/roles' hooks) — no existing feature imports another feature's
// hooks, so this keeps the same isolation the rest of the app already follows.
export function useRolesForSelect() {
  return useQuery({
    queryKey: ['roles', 'select-list'],
    queryFn: async (): Promise<{ items: RoleOption[] }> => {
      const result = await rolesApi.getRoles({ pageSize: 100, isActive: true });
      return { items: result.items };
    },
  });
}
