import type { EmployeeListQuery } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { employeesApi } from '../../../services/apiClient';

export const employeesQueryKey = (query: EmployeeListQuery) => ['employees', 'list', query] as const;

export function useEmployeesQuery(query: EmployeeListQuery, options?: { enabled?: boolean }) {
  return useQuery({
    queryKey: employeesQueryKey(query),
    queryFn: () => employeesApi.getEmployees(query),
    placeholderData: (previous) => previous,
    enabled: options?.enabled ?? true,
  });
}
