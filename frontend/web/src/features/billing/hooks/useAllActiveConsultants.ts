import { useMemo } from 'react';
import { useQuery } from '@tanstack/react-query';
import { consultantsApi } from '@/services/apiClient';
import type { ServiceConsultant } from '../billingCatalog';

/**
 * Radiology/Laboratory/Procedure Billing's Consultant dropdown — the full active Consultant
 * roster, unfiltered by department. Unlike Consultation Billing (which has its own Department
 * field to scope by, via `ConsultantSelect`), these three categories have no natural
 * department to filter on: not every hospital has a dedicated Laboratory/Pathology department
 * configured (confirmed for the seeded 'lhs' tenant — it has "Radiology & Sonology" but no
 * lab department), so a department-scoped picker would be empty/broken for Laboratory/
 * Procedure regardless of which real department happened to exist. Shares its query key with
 * `ConsultantSelect`'s own unfiltered case so the two don't double-fetch.
 */
export function useAllActiveConsultants(): { consultants: ServiceConsultant[]; isLoading: boolean } {
  const { data, isLoading } = useQuery({
    queryKey: ['consultants', 'select-list', undefined],
    queryFn: () => consultantsApi.getConsultants({ pageSize: 100, isActive: true }),
  });

  const consultants = useMemo<ServiceConsultant[]>(
    () => (data?.items ?? []).map((consultant) => ({ id: consultant.id, name: consultant.name })),
    [data],
  );

  return { consultants, isLoading };
}
