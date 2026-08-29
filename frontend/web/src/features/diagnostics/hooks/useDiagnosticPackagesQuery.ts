import type { DiagnosticPackageListQuery } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { diagnosticPackagesApi } from '../../../services/apiClient';

export const diagnosticPackagesQueryKey = (query: DiagnosticPackageListQuery) => ['diagnostics', 'packages', 'list', query] as const;

export function useDiagnosticPackagesQuery(query: DiagnosticPackageListQuery = {}) {
  return useQuery({
    queryKey: diagnosticPackagesQueryKey(query),
    queryFn: () => diagnosticPackagesApi.getDiagnosticPackages(query),
    placeholderData: (previous) => previous,
  });
}
