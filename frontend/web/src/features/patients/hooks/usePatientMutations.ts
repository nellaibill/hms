import type { EmergencyContact, EmergencyContactInput, AddAllergyRequest, CreatePatientRequest, CreatePatientVisitRequest, UpdatePatientRequest } from '@hms/shared';
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

/** The patient's emergency contacts as loaded, and as edited — Edit Patient saves the
 * difference, since UpdatePatientRequest doesn't carry contacts. */
export interface EmergencyContactChanges {
  original: EmergencyContact[];
  edited: EmergencyContactInput[];
}

const contactKey = (contact: EmergencyContactInput) =>
  `${contact.relationship}|${contact.name.trim().toLowerCase()}|${contact.phone.trim()}`;

/**
 * Saves the patient, then their emergency contacts. A changed contact used to be dropped: the
 * update request has no contacts, and nothing else saved them, so an imported patient's
 * placeholder 9999999999 stayed while the record was marked verified (25 Nov staging test).
 * New or changed contacts are added before the old ones are removed, so a hospital that makes
 * the contact mandatory never sees the patient left without one.
 */
export function useUpdatePatientMutation() {
  const invalidatePatients = useInvalidatePatients();
  return useMutation({
    mutationFn: async ({ id, request, contacts }: { id: string; request: UpdatePatientRequest; contacts?: EmergencyContactChanges }) => {
      let patient = await patientsApi.updatePatient(id, request);
      if (contacts) {
        const originalKeys = new Set(contacts.original.map(contactKey));
        const editedKeys = new Set(contacts.edited.map(contactKey));
        for (const contact of contacts.edited) {
          if (!originalKeys.has(contactKey(contact))) {
            patient = await patientsApi.addEmergencyContact(id, { relationship: contact.relationship, name: contact.name.trim(), phone: contact.phone.trim() });
          }
        }
        for (const contact of contacts.original) {
          if (!editedKeys.has(contactKey(contact))) {
            patient = await patientsApi.removeEmergencyContact(id, contact.id);
          }
        }
      }
      return patient;
    },
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

// Soft delete (DocumentsController.Delete) — the row drops out of the list query via the
// backend's IsDeleted query filter, and invalidating the shared prefix also clears a deleted
// photo/ID proof from usePatientDocumentUrl.
export function useDeletePatientDocumentMutation(patientId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (documentId: string) => documentsApi.deleteDocument(documentId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: patientDocumentsQueryKey(patientId) }),
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
// unlike the allergy mutations above there's no `patients` query to invalidate here — instead
// invalidate ['patient-visits', id] (usePatientVisitsQuery), which is what the Overview tab's
// Recent Visits card and At-a-Glance stats actually read. Without this, the newly recorded visit
// stayed invisible on the page navigated back to until the 30s query staleTime lapsed.
export function useCreatePatientVisitMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: CreatePatientVisitRequest }) => patientsApi.createVisit(id, request),
    onSuccess: (_data, { id }) => queryClient.invalidateQueries({ queryKey: ['patient-visits', id] }),
  });
}
