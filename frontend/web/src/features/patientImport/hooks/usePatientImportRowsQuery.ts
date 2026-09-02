import type { ImportRowListQuery, PagedImportRows } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { patientImportApi } from '../../../services/apiClient';

export const patientImportRowsQueryKey = (batchId: string, query: ImportRowListQuery) =>
  ['patient-import', 'rows', batchId, query] as const;

export function usePatientImportRowsQuery(batchId: string | null, query: ImportRowListQuery, options?: { enabled?: boolean }) {
  return useQuery({
    queryKey: patientImportRowsQueryKey(batchId ?? '', query),
    queryFn: (): Promise<PagedImportRows> => patientImportApi.getRows(batchId!, query),
    enabled: (options?.enabled ?? true) && batchId !== null,
    placeholderData: (previous) => previous,
  });
}
