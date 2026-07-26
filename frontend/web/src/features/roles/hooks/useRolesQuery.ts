import { useQuery } from '@tanstack/react-query';
import { listMockRoles, type PagedRoles } from '../mockRolesStore';
import type { RoleListQuery } from '../types';

export const rolesQueryKey = (query: RoleListQuery) => ['roles', 'list', query] as const;

export function useRolesQuery(query: RoleListQuery) {
  return useQuery<PagedRoles>({
    queryKey: rolesQueryKey(query),
    queryFn: () => listMockRoles(query),
    placeholderData: (previous) => previous,
  });
}
