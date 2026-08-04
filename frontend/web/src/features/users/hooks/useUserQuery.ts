import { NetworkError } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { usersApi } from '../../../services/apiClient';
import { getMockUserById } from '../mockUsersStore';

export function useUserQuery(id: string | undefined) {
  return useQuery({
    queryKey: ['users', 'detail', id],
    queryFn: async () => {
      try {
        return await usersApi.getUserById(id as string);
      } catch (err) {
        if (err instanceof NetworkError) {
          const mock = getMockUserById(id as string);
          if (mock) {
            return mock;
          }
        }
        throw err;
      }
    },
    enabled: Boolean(id),
  });
}
