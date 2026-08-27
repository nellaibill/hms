import type { CreateEmployeeRequest, UpdateEmployeeRequest } from '@hms/shared';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { employeesApi } from '../../../services/apiClient';

function useInvalidateEmployees() {
  const queryClient = useQueryClient();
  return () => queryClient.invalidateQueries({ queryKey: ['employees'] });
}

export function useCreateEmployeeMutation() {
  const invalidateEmployees = useInvalidateEmployees();
  return useMutation({
    mutationFn: (request: CreateEmployeeRequest) => employeesApi.createEmployee(request),
    onSuccess: invalidateEmployees,
  });
}

export function useUpdateEmployeeMutation() {
  const invalidateEmployees = useInvalidateEmployees();
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateEmployeeRequest }) => employeesApi.updateEmployee(id, request),
    onSuccess: invalidateEmployees,
  });
}

export function useDeleteEmployeeMutation() {
  const invalidateEmployees = useInvalidateEmployees();
  return useMutation({
    mutationFn: (id: string) => employeesApi.deleteEmployee(id),
    onSuccess: invalidateEmployees,
  });
}

export function useActivateEmployeeMutation() {
  const invalidateEmployees = useInvalidateEmployees();
  return useMutation({
    mutationFn: (id: string) => employeesApi.activateEmployee(id),
    onSuccess: invalidateEmployees,
  });
}

export function useDeactivateEmployeeMutation() {
  const invalidateEmployees = useInvalidateEmployees();
  return useMutation({
    mutationFn: (id: string) => employeesApi.deactivateEmployee(id),
    onSuccess: invalidateEmployees,
  });
}
