import { useQuery } from '@tanstack/react-query';
import { diagnosticPackagesApi } from '../../../services/apiClient';

export const diagnosticPackageQueryKey = (id: string | undefined) => ['diagnostics', 'packages', 'detail', id] as const;

/** Single package, with its `items` array (each resolving to a DiagnosticService id) —
 * backs both LabPackageDetailPage and the Invoice "Lab Details" tab's package breakdown. */
export function useDiagnosticPackageQuery(id: string | undefined) {
  return useQuery({
    queryKey: diagnosticPackageQueryKey(id),
    queryFn: () => diagnosticPackagesApi.getDiagnosticPackageById(id as string),
    enabled: Boolean(id),
  });
}
