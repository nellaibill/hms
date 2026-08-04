import type { PagedUsers, UserListQuery } from '@hms/shared';
import { NetworkError } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { usersApi } from '../../../services/apiClient';
import { listMockUsers } from '../mockUsersStore';

export const usersQueryKey = (query: UserListQuery) => ['users', 'list', query] as const;

/** Result carries `source` so the UI can flag when it's showing offline demo data. */
export type PagedUsersResult = PagedUsers & { source: 'live' | 'mock' };

export function useUsersQuery(query: UserListQuery) {
  return useQuery({
    queryKey: usersQueryKey(query),
    queryFn: async (): Promise<PagedUsersResult> => {
      try {
        const result = await usersApi.getUsers(query);
        return { ...result, source: 'live' };
      } catch (err) {
        // Only fall back when the backend is genuinely unreachable — a real ApiError
        // (backend up, request rejected) should still surface normally.
        if (err instanceof NetworkError) {
          return { ...listMockUsers(query), source: 'mock' };
        }
        throw err;
      }
    },
    placeholderData: (previous) => previous,
  });
}
