import { useQuery } from '@tanstack/react-query';
import { documentsApi } from '../../../services/apiClient';

export function useDocumentSummaryQuery() {
  return useQuery({
    queryKey: ['documents', 'summary'],
    queryFn: () => documentsApi.getSummary(),
  });
}
