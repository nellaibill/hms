import type { CreateUserRequest, SetPasswordRequest, UpdateUserRequest } from '@hms/shared';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { usersApi } from '../../../services/apiClient';

function useInvalidateUsers() {
  const queryClient = useQueryClient();
  return () => queryClient.invalidateQueries({ queryKey: ['users'] });
}

export function useCreateUserMutation() {
  const invalidateUsers = useInvalidateUsers();
  return useMutation({
    mutationFn: (request: CreateUserRequest) => usersApi.createUser(request),
    onSuccess: invalidateUsers,
  });
}

export function useUpdateUserMutation() {
  const invalidateUsers = useInvalidateUsers();
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateUserRequest }) => usersApi.updateUser(id, request),
    onSuccess: invalidateUsers,
  });
}

export function useDeleteUserMutation() {
  const invalidateUsers = useInvalidateUsers();
  return useMutation({
    mutationFn: (id: string) => usersApi.deleteUser(id),
    onSuccess: invalidateUsers,
  });
}

export function useActivateUserMutation() {
  const invalidateUsers = useInvalidateUsers();
  return useMutation({
    mutationFn: (id: string) => usersApi.activateUser(id),
    onSuccess: invalidateUsers,
  });
}

export function useDeactivateUserMutation() {
  const invalidateUsers = useInvalidateUsers();
  return useMutation({
    mutationFn: (id: string) => usersApi.deactivateUser(id),
    onSuccess: invalidateUsers,
  });
}

export function useSetPasswordMutation() {
  const invalidateUsers = useInvalidateUsers();
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: SetPasswordRequest }) => usersApi.setPassword(id, request),
    onSuccess: invalidateUsers,
  });
}
