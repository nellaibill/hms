import type { CreateOpdDiagnosisRequest, OpdInvestigationDepartment } from '@hms/shared';
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { opdConsultationApi } from '@/services/apiClient';

/** Diagnosis catalog search for the consultation form's picker — served by the OPD consultation
 * API under the clinical-care permission, since the Masters diagnosis endpoints need an admin
 * permission a doctor doesn't have (regression report OPD-03). Server-side search, so it keeps
 * working once the catalog grows past one page. */
export function useConsultationDiagnosesQuery(search: string) {
  return useQuery({
    queryKey: ['opd-consultation', 'diagnoses', search],
    queryFn: () => opdConsultationApi.searchDiagnoses(search || undefined),
    placeholderData: keepPreviousData,
    staleTime: 60_000,
  });
}

/** Adds a diagnosis to the catalog from the consultation form (returns the existing entry when
 * the name is already there). */
export function useCreateConsultationDiagnosisMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateOpdDiagnosisRequest) => opdConsultationApi.createDiagnosis(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['opd-consultation', 'diagnoses'] });
      // The Masters admin list (Settings > Master Data > Diagnoses) shows the same catalog.
      queryClient.invalidateQueries({ queryKey: ['masters'] });
    },
  });
}

/** Every active Laboratory/Radiology catalog service for the investigation picker (OPD-01). */
export function useInvestigationServicesQuery(department: OpdInvestigationDepartment) {
  return useQuery({
    queryKey: ['opd-consultation', 'investigation-services', department],
    queryFn: () => opdConsultationApi.listInvestigationServices(department),
    staleTime: 5 * 60_000,
  });
}
