import type { ImportBatch } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { patientImportApi } from '../../../services/apiClient';

export const patientImportBatchQueryKey = (batchId: string) => ['patient-import', 'batch', batchId] as const;

const ACTIVE_STATUSES: ImportBatch['status'][] = ['Validating', 'Committing'];

/** Polls while the batch is still being processed in the background (Validating/Committing),
 * and stops once it settles (ReadyForReview/Completed/Failed) — backs the progress step of
 * PatientBulkImportPage. */
export function usePatientImportBatchQuery(batchId: string | null) {
  return useQuery({
    queryKey: patientImportBatchQueryKey(batchId ?? ''),
    queryFn: (): Promise<ImportBatch> => patientImportApi.getBatch(batchId!),
    enabled: batchId !== null,
    refetchInterval: (query) => (query.state.data && ACTIVE_STATUSES.includes(query.state.data.status) ? 2000 : false),
  });
}
