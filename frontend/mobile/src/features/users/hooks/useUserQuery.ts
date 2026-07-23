import { useQuery } from '@tanstack/react-query';
import { usersApi } from '../../../services/apiClient';

export function useUserQuery(id: string | undefined) {
  return useQuery({
    queryKey: ['users', 'detail', id],
    queryFn: () => usersApi.getUserById(id as string),
    enabled: Boolean(id),
  });
}
