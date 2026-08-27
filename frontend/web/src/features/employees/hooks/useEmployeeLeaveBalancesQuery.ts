import { useQuery } from '@tanstack/react-query';
import { employeesApi } from '../../../services/apiClient';

export function useEmployeeLeaveBalancesQuery(employeeId: string | undefined) {
  return useQuery({
    queryKey: ['employees', 'leave-balances', employeeId],
    queryFn: () => employeesApi.getEmployeeLeaveBalances(employeeId as string),
    enabled: Boolean(employeeId),
  });
}
