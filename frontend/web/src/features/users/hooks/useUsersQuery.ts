import type { UserListQuery } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { usersApi } from '../../../services/apiClient';

export const usersQueryKey = (query: UserListQuery) => ['users', 'list', query] as const;

export function useUsersQuery(query: UserListQuery) {
  return useQuery({
    queryKey: usersQueryKey(query),
    queryFn: () => usersApi.getUsers(query),
    placeholderData: (previous) => previous,
  });
}
