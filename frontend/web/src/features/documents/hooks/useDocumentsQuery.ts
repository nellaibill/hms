import { useQuery } from '@tanstack/react-query';
import { documentsApi } from '../../../services/apiClient';
import { mapDocumentResponseToHmsDocument, toDocumentSearchQuery } from '../mapDocument';
import type { DocumentFilters } from '../types';

export const documentsQueryKey = (filters: DocumentFilters) => ['documents', 'list', filters] as const;

export function useDocumentsQuery(filters: DocumentFilters) {
  return useQuery({
    queryKey: documentsQueryKey(filters),
    queryFn: async () => {
      const { items } = await documentsApi.getDocuments(toDocumentSearchQuery(filters));
      return items.map(mapDocumentResponseToHmsDocument);
    },
    placeholderData: (previous) => previous,
  });
}
