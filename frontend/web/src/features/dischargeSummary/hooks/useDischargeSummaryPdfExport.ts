import type { Admission, Consultant, DischargeSummary, Patient } from '@hms/shared';
import { useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import defaultLogoUrl from '@/assets/logo.png';
import { branding } from '@/config/branding';
import { resolveBrandLogoUrl } from '@/features/branding/brandLogo';
import { useBrandingQuery } from '@/features/branding/hooks/useBrandingQuery';
import { loadImageForPdf } from '@/lib/loadImageForPdf';
import { consultantsApi, statesApi } from '@/services/apiClient';
import { exportDischargeSummaryPdf, type LetterheadDoctor } from '../exportDischargeSummaryPdf';
import { useStaffDirectoryQuery } from './useStaffDirectoryQuery';

const LOOKUP_STALE_MS = 5 * 60 * 1000;

function toLetterheadDoctor(consultant: Consultant): LetterheadDoctor {
  return { name: consultant.name, specialization: consultant.specialization };
}

/**
 * Gathers what the discharge summary letterhead needs beyond the summary itself: the Print
 * logo (rasterized for jsPDF), the active consultant roster for the footer band, the
 * admission's consultant for the header panel, and the patient's district/state names. Each
 * lookup shares its query key with the component that already caches it (ConsultantSelect,
 * StateName, DistrictName), and any lookup that fails just drops that part of the letterhead —
 * the PDF itself still downloads.
 */
export function useDischargeSummaryPdfExport() {
  const queryClient = useQueryClient();
  const { data: brandingConfig } = useBrandingQuery();
  const directoryQuery = useStaffDirectoryQuery();
  const [isExporting, setIsExporting] = useState(false);

  async function resolvePatientAddress(patient: Patient): Promise<string> {
    const { addressLine1, addressLine2, addressLine3, stateId, districtId, pincode } = patient.address;
    const [states, districts] = stateId
      ? await Promise.all([
          queryClient
            .fetchQuery({ queryKey: ['states', 'select-list'], queryFn: () => statesApi.getStates(), staleTime: LOOKUP_STALE_MS })
            .catch(() => []),
          queryClient
            .fetchQuery({ queryKey: ['districts', 'select-list', stateId], queryFn: () => statesApi.getDistricts(stateId), staleTime: LOOKUP_STALE_MS })
            .catch(() => []),
        ])
      : [[], []];
    const place = [districts.find((d) => d.id === districtId)?.name, states.find((s) => s.id === stateId)?.name].filter(Boolean).join(', ');
    const placeWithPincode = [place, pincode].filter(Boolean).join(' - ');
    return [addressLine1, addressLine2, addressLine3, placeWithPincode].filter((part) => part?.trim()).join(', ');
  }

  async function resolveConsultants(admission: Admission): Promise<{ consultant: LetterheadDoctor | null; consultants: LetterheadDoctor[] }> {
    const active = await queryClient
      .fetchQuery({
        queryKey: ['consultants', 'select-list', undefined],
        queryFn: () => consultantsApi.getConsultants({ pageSize: 100, isActive: true }),
        staleTime: LOOKUP_STALE_MS,
      })
      .then((page) => page.items)
      .catch(() => [] as Consultant[]);
    // An inactive consultant (since left the hospital) isn't in the active roster — look them up directly.
    const treating =
      active.find((c) => c.id === admission.consultantId) ??
      (admission.consultantId ? await consultantsApi.getConsultantById(admission.consultantId).catch(() => null) : null);
    return {
      consultant: treating ? toLetterheadDoctor(treating) : admission.consultantName ? { name: admission.consultantName } : null,
      consultants: active.map(toLetterheadDoctor),
    };
  }

  async function exportPdf(summary: DischargeSummary, patient: Patient, admission: Admission) {
    setIsExporting(true);
    try {
      const [logo, { consultant, consultants }, patientAddress] = await Promise.all([
        loadImageForPdf(resolveBrandLogoUrl(brandingConfig, 'print') ?? defaultLogoUrl),
        resolveConsultants(admission),
        resolvePatientAddress(patient),
      ]);
      exportDischargeSummaryPdf(
        summary,
        patient,
        admission,
        directoryQuery.data ?? [],
        {
          hospitalName: brandingConfig?.hospitalName ?? branding.hospitalName,
          address: brandingConfig?.address,
          phoneNumber: brandingConfig?.phoneNumber,
          logo,
          consultant,
          consultants,
        },
        patientAddress,
      );
    } finally {
      setIsExporting(false);
    }
  }

  return { exportPdf, isExporting };
}
