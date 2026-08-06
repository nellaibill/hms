import { useQuery } from '@tanstack/react-query';
import { listMockDocuments, type DocumentListQuery } from '../mockDocumentsStore';

export const documentsQueryKey = (query: DocumentListQuery) => ['documents', 'list', query] as const;

export function useDocumentsQuery(query: DocumentListQuery) {
  return useQuery({
    queryKey: documentsQueryKey(query),
    queryFn: () => listMockDocuments(query),
    placeholderData: (previous) => previous,
  });
}
