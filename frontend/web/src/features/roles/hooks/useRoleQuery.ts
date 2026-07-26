import { useQuery } from '@tanstack/react-query';
import { getMockRoleById } from '../mockRolesStore';

export function useRoleQuery(id: string | undefined) {
  return useQuery({
    queryKey: ['roles', 'detail', id],
    queryFn: () => {
      const role = getMockRoleById(id as string);
      if (!role) {
        throw new Error('Role not found.');
      }
      return role;
    },
    enabled: Boolean(id),
  });
}
