import type { AddAllergyRequest, CreatePatientRequest, CreatePatientVisitRequest, UpdatePatientRequest } from '@hms/shared';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { documentsApi, patientsApi } from '../../../services/apiClient';
import { patientDocumentsQueryKey } from './usePatientDocumentUrl';

function useInvalidatePatients() {
  const queryClient = useQueryClient();
  return () => queryClient.invalidateQueries({ queryKey: ['patients'] });
}

// Write failures propagate so the caller's apiError/toast handling can show them.

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

// Photo/ID-proof files are stored as Documents (HMS.Modules.Documents), not on the Patient
// row itself — there's no photo/idProof field on Patient to invalidate the patients cache
// for, so unlike every other mutation here, these two invalidate the patient-documents query
// (usePatientDocumentUrl) instead, so the profile photo/ID-proof view refreshes right away.
export function useUploadPatientPhotoMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, file }: { id: string; file: File }) =>
      documentsApi.uploadDocument(file, { ownerType: 'Patient', ownerId: id, documentType: 'Other' }),
    onSuccess: (_data, { id }) => queryClient.invalidateQueries({ queryKey: patientDocumentsQueryKey(id) }),
  });
}

export function useUploadPatientIdProofMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, file }: { id: string; file: File }) =>
      documentsApi.uploadDocument(file, { ownerType: 'Patient', ownerId: id, documentType: 'IdProof' }),
    onSuccess: (_data, { id }) => queryClient.invalidateQueries({ queryKey: patientDocumentsQueryKey(id) }),
  });
}

export function useAddPatientAllergyMutation() {
  const invalidatePatients = useInvalidatePatients();
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: AddAllergyRequest }) => patientsApi.addAllergy(id, request),
    onSuccess: invalidatePatients,
  });
}

export function useRemovePatientAllergyMutation() {
  const invalidatePatients = useInvalidatePatients();
  return useMutation({
    mutationFn: ({ id, allergyId }: { id: string; allergyId: string }) => patientsApi.removeAllergy(id, allergyId),
    onSuccess: invalidatePatients,
  });
}

// A visit isn't part of the Patient response (it's fetched separately, same as Documents), so
// unlike the allergy mutations above there's no `patients` query to invalidate here.
export function useCreatePatientVisitMutation() {
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: CreatePatientVisitRequest }) => patientsApi.createVisit(id, request),
  });
}
