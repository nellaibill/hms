import { useMutation, useQueryClient } from '@tanstack/react-query';
import { createRole, updateRole } from '../apiRoleRepository';
import type { RoleFormValues } from '../types';

function useInvalidateRoles() {
  const queryClient = useQueryClient();
  return () => queryClient.invalidateQueries({ queryKey: ['roles'] });
}

export function useCreateRoleMutation() {
  const invalidateRoles = useInvalidateRoles();
  return useMutation({
    mutationFn: async (values: RoleFormValues) => createRole(values),
    onSuccess: invalidateRoles,
  });
}

export function useUpdateRoleMutation() {
  const invalidateRoles = useInvalidateRoles();
  return useMutation({
    mutationFn: async ({ id, values }: { id: string; values: RoleFormValues }) => updateRole(id, values),
    onSuccess: invalidateRoles,
  });
}
