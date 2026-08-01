import { useQuery } from '@tanstack/react-query';
import { rolesApi } from '../../../services/apiClient';

// Users feature stays self-contained (calls the shared rolesApi directly rather than
// importing from features/roles) — no existing feature imports another feature's hooks,
// so this keeps the same isolation the rest of the app already follows.
export function useRolesForSelect() {
  return useQuery({
    queryKey: ['roles', 'select-list'],
    queryFn: () => rolesApi.getRoles({ pageSize: 100, isActive: true }),
  });
}
