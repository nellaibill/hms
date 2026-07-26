import type { CreatePatientRequest, IdProofType, UpdatePatientRequest } from '@hms/shared';
import { NetworkError } from '@hms/shared';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { patientsApi } from '../../../services/apiClient';
import { createMockPatient, deleteMockPatient, updateMockPatient } from '../mockPatientsStore';

function useInvalidatePatients() {
  const queryClient = useQueryClient();
  return () => queryClient.invalidateQueries({ queryKey: ['patients'] });
}

export function useCreatePatientMutation() {
  const invalidatePatients = useInvalidatePatients();
  return useMutation({
    mutationFn: async (request: CreatePatientRequest) => {
      try {
        return await patientsApi.createPatient(request);
      } catch (err) {
        if (err instanceof NetworkError) {
          return createMockPatient(request);
        }
        throw err;
      }
    },
    onSuccess: invalidatePatients,
  });
}

export function useUpdatePatientMutation() {
  const invalidatePatients = useInvalidatePatients();
  return useMutation({
    mutationFn: async ({ id, request }: { id: string; request: UpdatePatientRequest }) => {
      try {
        return await patientsApi.updatePatient(id, request);
      } catch (err) {
        if (err instanceof NetworkError) {
          return updateMockPatient(id, request);
        }
        throw err;
      }
    },
    onSuccess: invalidatePatients,
  });
}

export function useDeletePatientMutation() {
  const invalidatePatients = useInvalidatePatients();
  return useMutation({
    mutationFn: async (id: string) => {
      try {
        await patientsApi.deletePatient(id);
      } catch (err) {
        if (err instanceof NetworkError) {
          deleteMockPatient(id);
          return;
        }
        throw err;
      }
    },
    onSuccess: invalidatePatients,
  });
}

export function useUploadPatientPhotoMutation() {
  const invalidatePatients = useInvalidatePatients();
  return useMutation({
    mutationFn: ({ id, file }: { id: string; file: File }) => patientsApi.uploadPhoto(id, file),
    onSuccess: invalidatePatients,
  });
}

export function useUploadPatientIdProofMutation() {
  const invalidatePatients = useInvalidatePatients();
  return useMutation({
    mutationFn: ({ id, idProofType, file }: { id: string; idProofType: IdProofType; file: File }) =>
      patientsApi.uploadIdProof(id, idProofType, file),
    onSuccess: invalidatePatients,
  });
}
