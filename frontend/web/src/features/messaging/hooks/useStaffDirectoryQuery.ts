import { useQuery } from '@tanstack/react-query';
import { usersApi } from '@/services/apiClient';

/** Backs the "start a conversation" staff picker — a search-gated query (empty search
 * returns the first page of active staff, capped at 100 server-side, see
 * IUserService.GetStaffDirectoryAsync's own doc comment). */
export function useStaffDirectoryQuery(search: string) {
  return useQuery({
    queryKey: ['staff-directory', search],
    queryFn: () => usersApi.getStaffDirectory(search || undefined),
  });
}
