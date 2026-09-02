import { useQuery } from '@tanstack/react-query';
import { documentsApi } from '../../../services/apiClient';

/**
 * Real-API-backed employee document list — calls documentsApi.listDocuments directly with
 * ownerType: 'Staff', independently of the features/documents Document Management dashboard
 * (which now also calls the real API, via its own paged documentsApi.getDocuments).
 */
export function useEmployeeDocumentsQuery(employeeId: string | undefined) {
  return useQuery({
    queryKey: ['employeeDocuments', 'list', employeeId],
    queryFn: () => documentsApi.listDocuments({ ownerType: 'Staff', ownerId: employeeId as string }),
    enabled: Boolean(employeeId),
  });
}
