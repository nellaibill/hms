import type { DiagnosticServiceListQuery } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { diagnosticServicesApi } from '../../../services/apiClient';

export const diagnosticServicesQueryKey = (query: DiagnosticServiceListQuery) => ['diagnostics', 'services', 'list', query] as const;

export function useDiagnosticServicesQuery(query: DiagnosticServiceListQuery = {}) {
  return useQuery({
    queryKey: diagnosticServicesQueryKey(query),
    queryFn: () => diagnosticServicesApi.getDiagnosticServices(query),
    placeholderData: (previous) => previous,
  });
}
