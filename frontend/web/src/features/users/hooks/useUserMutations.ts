import type { CreateUserRequest, SetPasswordRequest, UpdateUserRequest } from '@hms/shared';
import { NetworkError } from '@hms/shared';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { usersApi } from '../../../services/apiClient';
import {
  activateMockUser,
  createMockUser,
  deactivateMockUser,
  deleteMockUser,
  setMockUserPassword,
  updateMockUser,
  uploadMockUserProfilePhoto,
} from '../mockUsersStore';

function useInvalidateUsers() {
  const queryClient = useQueryClient();
  return () => queryClient.invalidateQueries({ queryKey: ['users'] });
}

export function useCreateUserMutation() {
  const invalidateUsers = useInvalidateUsers();
  return useMutation({
    mutationFn: async (request: CreateUserRequest) => {
      try {
        return await usersApi.createUser(request);
      } catch (err) {
        if (err instanceof NetworkError) {
          return createMockUser(request);
        }
        throw err;
      }
    },
    onSuccess: invalidateUsers,
  });
}

export function useUpdateUserMutation() {
  const invalidateUsers = useInvalidateUsers();
  return useMutation({
    mutationFn: async ({ id, request }: { id: string; request: UpdateUserRequest }) => {
      try {
        return await usersApi.updateUser(id, request);
      } catch (err) {
        if (err instanceof NetworkError) {
          return updateMockUser(id, request);
        }
        throw err;
      }
    },
    onSuccess: invalidateUsers,
  });
}

export function useDeleteUserMutation() {
  const invalidateUsers = useInvalidateUsers();
  return useMutation({
    mutationFn: async (id: string) => {
      try {
        await usersApi.deleteUser(id);
      } catch (err) {
        if (err instanceof NetworkError) {
          deleteMockUser(id);
          return;
        }
        throw err;
      }
    },
    onSuccess: invalidateUsers,
  });
}

export function useActivateUserMutation() {
  const invalidateUsers = useInvalidateUsers();
  return useMutation({
    mutationFn: async (id: string) => {
      try {
        return await usersApi.activateUser(id);
      } catch (err) {
        if (err instanceof NetworkError) {
          return activateMockUser(id);
        }
        throw err;
      }
    },
    onSuccess: invalidateUsers,
  });
}

export function useDeactivateUserMutation() {
  const invalidateUsers = useInvalidateUsers();
  return useMutation({
    mutationFn: async (id: string) => {
      try {
        return await usersApi.deactivateUser(id);
      } catch (err) {
        if (err instanceof NetworkError) {
          return deactivateMockUser(id);
        }
        throw err;
      }
    },
    onSuccess: invalidateUsers,
  });
}

export function useSetPasswordMutation() {
  const invalidateUsers = useInvalidateUsers();
  return useMutation({
    mutationFn: async ({ id, request }: { id: string; request: SetPasswordRequest }) => {
      try {
        return await usersApi.setPassword(id, request);
      } catch (err) {
        if (err instanceof NetworkError) {
          return setMockUserPassword(id);
        }
        throw err;
      }
    },
    onSuccess: invalidateUsers,
  });
}

export function useUploadProfilePhotoMutation() {
  const invalidateUsers = useInvalidateUsers();
  return useMutation({
    mutationFn: async ({ id, file }: { id: string; file: File }) => {
      try {
        return await usersApi.uploadProfilePhoto(id, file);
      } catch (err) {
        if (err instanceof NetworkError) {
          return uploadMockUserProfilePhoto(id, file);
        }
        throw err;
      }
    },
    onSuccess: invalidateUsers,
  });
}
