import { useQuery } from '@tanstack/react-query';
import { documentsApi } from '../../../services/apiClient';
import { patientDocumentsQueryKey } from './usePatientDocumentUrl';

/** Every document on file for a patient, across all document types (photo/ID proof included) —
 * used by the Overview tab's Recent Documents list and the Documents tab's full list. Shares
 * the ['patient-documents', patientId] query-key prefix with usePatientDocumentUrl, so an
 * upload there invalidates this list too. */
export function usePatientDocumentsQuery(patientId: string | undefined) {
  return useQuery({
    queryKey: patientDocumentsQueryKey(patientId as string),
    queryFn: () => documentsApi.listDocuments({ ownerType: 'Patient', ownerId: patientId as string }),
    enabled: Boolean(patientId),
  });
}
