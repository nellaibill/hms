import type { ProcedureListQuery } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { billingApi } from '@/services/apiClient';

export const opdProceduresQueryKey = (query: ProcedureListQuery) => ['opd', 'procedures', 'list', query] as const;

/** OPD Procedures List tab — one row per Procedure invoice line item, across every invoice. */
export function useOpdProceduresQuery(query: ProcedureListQuery) {
  return useQuery({
    queryKey: opdProceduresQueryKey(query),
    queryFn: () => billingApi.getProcedureLineItems(query),
    placeholderData: (previous) => previous,
  });
}
