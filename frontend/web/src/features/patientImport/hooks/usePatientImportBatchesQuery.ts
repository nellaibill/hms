import type { ImportBatchListQuery, PagedImportBatches } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { patientImportApi } from '../../../services/apiClient';

export const patientImportBatchesQueryKey = (query: ImportBatchListQuery) => ['patient-import', 'batches', query] as const;

/** Import History — every past and in-progress batch, newest first. */
export function usePatientImportBatchesQuery(query: ImportBatchListQuery = {}) {
  return useQuery({
    queryKey: patientImportBatchesQueryKey(query),
    queryFn: (): Promise<PagedImportBatches> => patientImportApi.getBatches(query),
    placeholderData: (previous) => previous,
  });
}
