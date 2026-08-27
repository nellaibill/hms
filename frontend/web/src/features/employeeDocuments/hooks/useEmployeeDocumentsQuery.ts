import { useQuery } from '@tanstack/react-query';
import { documentsApi } from '../../../services/apiClient';

/**
 * Real-API-backed employee document list — deliberately NOT the mock features/documents
 * module (listMockDocuments/mockDocumentsStore), which has never been wired to the real
 * /api/v1/documents endpoints. Calls documentsApi.listDocuments directly with
 * ownerType: 'Staff'.
 */
export function useEmployeeDocumentsQuery(employeeId: string | undefined) {
  return useQuery({
    queryKey: ['employeeDocuments', 'list', employeeId],
    queryFn: () => documentsApi.listDocuments({ ownerType: 'Staff', ownerId: employeeId as string }),
    enabled: Boolean(employeeId),
  });
}
