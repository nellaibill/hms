import { useQuery } from '@tanstack/react-query';
import { diagnosticServicesApi } from '../../../services/apiClient';

export const diagnosticServiceQueryKey = (id: string | undefined) => ['diagnostics', 'services', 'detail', id] as const;

export function useDiagnosticServiceQuery(id: string | undefined) {
  return useQuery({
    queryKey: diagnosticServiceQueryKey(id),
    queryFn: () => diagnosticServicesApi.getDiagnosticServiceById(id as string),
    enabled: Boolean(id),
  });
}
