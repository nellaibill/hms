import { useQuery } from '@tanstack/react-query';
import { listRoles, type PagedRoles } from '../apiRoleRepository';
import type { RoleListQuery } from '../types';

export const rolesQueryKey = (query: RoleListQuery) => ['roles', 'list', query] as const;

export function useRolesQuery(query: RoleListQuery) {
  return useQuery<PagedRoles>({
    queryKey: rolesQueryKey(query),
    queryFn: () => listRoles(query),
    placeholderData: (previous) => previous,
  });
}
