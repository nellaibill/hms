import { useQuery } from '@tanstack/react-query';
import { documentsApi } from '../../../services/apiClient';
import { patientDocumentsQueryKey } from './usePatientDocumentUrl';

/** Passages returned per search — enough to cover a patient's handful of documents without
 * burying the best matches under weak ones. */
const RESULT_LIMIT = 8;

/**
 * Semantic (RAG) search over one patient's indexed documents — backs the AI Search tab.
 * Runs only for a submitted, non-blank question (not on every keystroke: each search is an
 * embedding call to the configured model). Scoped server-side to ownerType=Patient/ownerId,
 * and the server additionally filters by what the caller may read.
 */
export function usePatientDocumentSearchQuery(patientId: string, question: string) {
  const q = question.trim();
  return useQuery({
    // Under the ['patient-documents', patientId] prefix, so an upload or delete (which
    // invalidates that prefix) re-runs an open search instead of showing a deleted file.
    queryKey: [...patientDocumentsQueryKey(patientId), 'search', q],
    queryFn: () => documentsApi.searchDocuments({ q, ownerType: 'Patient', ownerId: patientId, limit: RESULT_LIMIT }),
    enabled: q.length > 0,
    // The same question on the same patient returns the same passages until documents change;
    // no point re-embedding it on every tab revisit.
    staleTime: 5 * 60 * 1000,
    retry: false,
  });
}
