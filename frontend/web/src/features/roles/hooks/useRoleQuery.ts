import { useQuery } from '@tanstack/react-query';
import { getRoleById } from '../apiRoleRepository';

export function useRoleQuery(id: string | undefined) {
  return useQuery({
    queryKey: ['roles', 'detail', id],
    queryFn: () => getRoleById(id as string),
    enabled: Boolean(id),
  });
}
