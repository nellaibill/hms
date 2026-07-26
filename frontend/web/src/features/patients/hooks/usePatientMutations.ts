import type { CreatePatientRequest, IdProofType, UpdatePatientRequest } from '@hms/shared';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { patientsApi } from '../../../services/apiClient';

function useInvalidatePatients() {
  const queryClient = useQueryClient();
  return () => queryClient.invalidateQueries({ queryKey: ['patients'] });
}

export function useCreatePatientMutation() {
  const invalidatePatients = useInvalidatePatients();
  return useMutation({
    mutationFn: (request: CreatePatientRequest) => patientsApi.createPatient(request),
    onSuccess: invalidatePatients,
  });
}

export function useUpdatePatientMutation() {
  const invalidatePatients = useInvalidatePatients();
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdatePatientRequest }) => patientsApi.updatePatient(id, request),
    onSuccess: invalidatePatients,
  });
}

export function useDeletePatientMutation() {
  const invalidatePatients = useInvalidatePatients();
  return useMutation({
    mutationFn: (id: string) => patientsApi.deletePatient(id),
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
