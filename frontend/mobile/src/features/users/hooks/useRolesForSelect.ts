import { useQuery } from '@tanstack/react-query';
import { rolesApi } from '../../../services/apiClient';

// Mirrors web's features/users/hooks/useRolesForSelect.ts — Users feature stays
// self-contained (calls the shared rolesApi directly) rather than importing from a
// features/roles module, matching how the rest of the app avoids cross-feature imports.
export function useRolesForSelect() {
  return useQuery({
    queryKey: ['roles', 'select-list'],
    queryFn: () => rolesApi.getRoles({ pageSize: 100, isActive: true }),
  });
}
