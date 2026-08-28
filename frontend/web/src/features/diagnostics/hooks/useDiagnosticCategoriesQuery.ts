import type { DiagnosticCategoryListQuery } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { diagnosticCategoriesApi } from '../../../services/apiClient';

export const diagnosticCategoriesQueryKey = (query: DiagnosticCategoryListQuery) => ['diagnostics', 'categories', 'list', query] as const;

export function useDiagnosticCategoriesQuery(query: DiagnosticCategoryListQuery = {}) {
  return useQuery({
    queryKey: diagnosticCategoriesQueryKey(query),
    queryFn: () => diagnosticCategoriesApi.getDiagnosticCategories(query),
    placeholderData: (previous) => previous,
  });
}
