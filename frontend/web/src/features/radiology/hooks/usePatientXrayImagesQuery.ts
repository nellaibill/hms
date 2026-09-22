import type { DocumentResponse } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { documentsApi } from '../../../services/apiClient';

/** Formats the AI model can read — DICOM and PDF are not supported yet. */
const ANALYZABLE_CONTENT_TYPES = new Set(['image/jpeg', 'image/png', 'image/webp']);

export const patientXrayImagesQueryKey = (patientId: string) => ['radiology', 'patientImages', patientId] as const;

/** A patient's stored image documents (the generic Documents repository, owner type Patient) —
 * the "patient folder" the radiology page reads from. */
export function usePatientXrayImagesQuery(patientId: string | undefined) {
  return useQuery({
    queryKey: patientXrayImagesQueryKey(patientId ?? ''),
    enabled: Boolean(patientId),
    queryFn: async (): Promise<DocumentResponse[]> => {
      const documents = await documentsApi.listDocuments({ ownerType: 'Patient', ownerId: patientId! });
      return documents
        .filter((document) => ANALYZABLE_CONTENT_TYPES.has(document.contentType.toLowerCase()) && !document.isArchived && document.status === 'Available')
        .sort((a, b) => b.createdAt.localeCompare(a.createdAt));
    },
  });
}
