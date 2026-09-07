import type { DiagnosticService, DiagnosticServiceType } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { useEffect, useMemo } from 'react';
import { diagnosticServicesApi } from '../../../services/apiClient';
import { primeDiagnosticServiceCache } from '../referenceCache';

export interface BillingServiceOption {
  id: string;
  name: string;
  price: number;
}

/** Walks every page at the server's maximum page size (PagedRequest.MaxPageSize = 100) rather
 * than fetching one page and hoping it fits — a single `pageSize: 200` request here used to
 * silently truncate once a tenant had more than 200 active services of one type, which the
 * real `lhs` tenant already does for Laboratory (265 active rows) after the tariff cost-data
 * load. Same "MaxPageSize truncates a single big-page request" fix already applied to
 * masterStoreFactory.ts's getAll() and apiBillingRepository.ts's getAllInvoicesForReport(). */
async function fetchAllDiagnosticServices(serviceType: DiagnosticServiceType): Promise<DiagnosticService[]> {
  const all: DiagnosticService[] = [];
  let page = 1;
  let totalPages = 1;
  do {
    const paged = await diagnosticServicesApi.getDiagnosticServices({ serviceType, isActive: true, page, pageSize: 100 });
    all.push(...paged.items);
    totalPages = paged.meta.totalPages;
    page++;
  } while (page <= totalPages);
  return all;
}

/**
 * Radiology/Laboratory Billing's Service dropdown, backed by the new typed DiagnosticService
 * catalog (replaces useDiagnosticTestServices for these two categories — Procedure stays on
 * the old DiagnosticTest master via useDiagnosticTestServices, untouched). Maps to the same
 * {id, name, price} shape useDiagnosticTestServices already returns, so RadiologyBillingCard's
 * only change is swapping this hook in — ServiceBillingCard/ServiceBillingRow need no changes
 * at all.
 */
export function useDiagnosticServices(serviceType: DiagnosticServiceType): { services: BillingServiceOption[]; isLoading: boolean } {
  const { data, isLoading } = useQuery({
    queryKey: ['diagnostics', 'services', 'all', serviceType],
    queryFn: () => fetchAllDiagnosticServices(serviceType),
  });

  // Primes the diagnostics reference cache describeBillingItem reads from — see
  // referenceCache.ts. Runs as an effect (not in queryFn) so it stays a pure side-effect of
  // this hook's own render rather than react-query internals.
  useEffect(() => {
    if (data) primeDiagnosticServiceCache(data);
  }, [data]);

  const services = useMemo<BillingServiceOption[]>(
    () => (data ?? []).map((service) => ({ id: service.id, name: service.name, price: service.price })).sort((a, b) => a.name.localeCompare(b.name)),
    [data],
  );

  return { services, isLoading };
}
