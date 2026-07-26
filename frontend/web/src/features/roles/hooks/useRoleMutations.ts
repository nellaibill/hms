import { useMutation, useQueryClient } from '@tanstack/react-query';
import { createMockRole, updateMockRole } from '../mockRolesStore';
import type { RoleFormValues } from '../types';

function useInvalidateRoles() {
  const queryClient = useQueryClient();
  return () => queryClient.invalidateQueries({ queryKey: ['roles'] });
}

export function useCreateRoleMutation() {
  const invalidateRoles = useInvalidateRoles();
  return useMutation({
    mutationFn: async (values: RoleFormValues) => createMockRole(values),
    onSuccess: invalidateRoles,
  });
}

export function useUpdateRoleMutation() {
  const invalidateRoles = useInvalidateRoles();
  return useMutation({
    mutationFn: async ({ id, values }: { id: string; values: RoleFormValues }) => updateMockRole(id, values),
    onSuccess: invalidateRoles,
  });
}
