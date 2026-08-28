import type { DiagnosticProviderListQuery } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { diagnosticProvidersApi } from '../../../services/apiClient';

export const diagnosticProvidersQueryKey = (query: DiagnosticProviderListQuery) => ['diagnostics', 'providers', 'list', query] as const;

export function useDiagnosticProvidersQuery(query: DiagnosticProviderListQuery = {}) {
  return useQuery({
    queryKey: diagnosticProvidersQueryKey(query),
    queryFn: () => diagnosticProvidersApi.getDiagnosticProviders(query),
    placeholderData: (previous) => previous,
  });
}
