import { useQuery } from '@tanstack/react-query';
import { usersApi } from '@/services/apiClient';

/**
 * Powers the Finalization section's Prepared/Checked/Approved-By pickers — reuses the same
 * low-sensitivity staff directory endpoint (GET /api/v1/users/directory) the Messaging
 * module's "start a conversation" picker calls (see
 * features/messaging/hooks/useStaffDirectoryQuery.ts), fetched once unfiltered (capped at
 * 100 server-side, see IUserService.GetStaffDirectoryAsync's own doc comment) since
 * SearchableSelect does its own client-side filtering rather than needing debounced
 * server-side search here.
 */
export function useStaffDirectoryQuery() {
  return useQuery({
    queryKey: ['staff-directory', ''],
    queryFn: () => usersApi.getStaffDirectory(),
  });
}
