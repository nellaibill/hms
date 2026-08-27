import { useQuery } from '@tanstack/react-query';
import { employeesApi } from '../../../services/apiClient';

export function useEmployeeQuery(id: string | undefined) {
  return useQuery({
    queryKey: ['employees', 'detail', id],
    queryFn: () => employeesApi.getEmployeeById(id as string),
    enabled: Boolean(id),
  });
}
