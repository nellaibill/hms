import { DEFAULT_PATIENT_FIELD_REQUIREMENTS, type PatientFieldRequirements } from '@hms/shared';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { patientsApi } from '../../../services/apiClient';

export const patientFieldSettingsQueryKey = ['patients', 'field-settings'] as const;

/** Settings → Patient Registration Fields (ADR-135): which registration fields this hospital
 * makes mandatory. */
export function usePatientFieldSettingsQuery() {
  return useQuery({
    queryKey: patientFieldSettingsQueryKey,
    queryFn: () => patientsApi.getFieldSettings(),
    // A Super Admin can change these at any time; the forms read them fresh on every visit.
    staleTime: 0,
  });
}

/**
 * For the New Patient Registration / Edit Patient forms: the hospital's settings, or — while
 * they load — nothing (the page waits, because the form reads them once when it mounts). If
 * they can't be loaded the defaults stand in rather than blocking registration; the API
 * enforces the real settings either way.
 */
export function usePatientFieldRequirements(): { requirements: PatientFieldRequirements | null } {
  const { data, isError } = usePatientFieldSettingsQuery();
  if (data) return { requirements: data };
  return { requirements: isError ? DEFAULT_PATIENT_FIELD_REQUIREMENTS : null };
}

export function useUpdatePatientFieldSettingsMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: PatientFieldRequirements) => patientsApi.updateFieldSettings(request),
    onSuccess: (saved) => queryClient.setQueryData(patientFieldSettingsQueryKey, saved),
  });
}
