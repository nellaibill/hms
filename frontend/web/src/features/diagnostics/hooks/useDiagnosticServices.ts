import type { DiagnosticService, DiagnosticServiceType } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { useMemo } from 'react';
import { diagnosticServicesApi } from '../../../services/apiClient';
import { primeDiagnosticServiceCache } from '../referenceCache';

export interface BillingServiceOption {
  id: string;
  name: string;
  price: number;
  /** Radiology only (ADR-140): other rates for the same scan, billed instead of `price`. */
  rateOptions?: { id: string; name: string; price: number }[];
  /** Radiology only: what a bedside scan adds to the price; null when not offered. */
  bedsidePrice?: number | null;
}

/** Walks every page at the server's maximum page size (PagedRequest.MaxPageSize = 100) rather
 * than fetching one page and hoping it fits — a single `pageSize: 200` request here used to
 * silently truncate once a tenant had more than 200 active services of one type, which the
 * real `lhs` tenant already does for Laboratory (265 active rows) after the tariff cost-data
 * load. Same "MaxPageSize truncates a single big-page request" fix already applied to
 * masterStoreFactory.ts's getAll() and apiBillingRepository.ts's getAllInvoicesForReport(). */
//
// Fetches inactive services too: a bill line for a service deactivated since (e.g. the flat
// legacy "Package" rows the lab tariff refresh retired) still needs its name and running cost in
// reports. Only the billing dropdown below is limited to active services.
async function fetchAllDiagnosticServices(serviceType: DiagnosticServiceType): Promise<DiagnosticService[]> {
  const all: DiagnosticService[] = [];
  let page = 1;
  let totalPages = 1;
  do {
    const paged = await diagnosticServicesApi.getDiagnosticServices({ serviceType, page, pageSize: 100 });
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
/**
 * Every service of one type, active and inactive, fetched in full (not one page of 100) and
 * primed into the reference cache — for screens that need the whole DiagnosticService record
 * (price, running cost, doctor's share, rate options), like the Price List and the package page.
 * Shares its query (and cache) with useDiagnosticServices below.
 */
export function useAllDiagnosticServicesQuery(serviceType: DiagnosticServiceType) {
  return useQuery({
    // 'with-inactive' (not the old 'all' key) so a browser holding the earlier active-only
    // list in its query cache doesn't reuse it.
    queryKey: ['diagnostics', 'services', 'with-inactive', serviceType],
    queryFn: async () => {
      const all = await fetchAllDiagnosticServices(serviceType);
      // Primed here, before the data reaches any render, so the reference cache describeBillingItem/
      // resolveItemCostPrice read is already current when a report's rows memo recomputes on this
      // data — see referenceCache.ts. (An effect ran after that render, so a running cost
      // updated in the master didn't show in OP Report.)
      primeDiagnosticServiceCache(all);
      return all;
    },
  });
}

/**
 * Radiology/Laboratory Billing's Service dropdown, backed by the typed DiagnosticService
 * catalog. Active services only. A radiology service also carries its rate options and
 * bedside add-on (ADR-140), which the billing row offers once the service is picked.
 */
export function useDiagnosticServices(serviceType: DiagnosticServiceType): { services: BillingServiceOption[]; isLoading: boolean } {
  const { data, isLoading } = useAllDiagnosticServicesQuery(serviceType);

  const services = useMemo<BillingServiceOption[]>(
    () =>
      (data ?? [])
        .filter((service) => service.isActive)
        .map((service) => ({
          id: service.id,
          name: service.name,
          price: service.price,
          rateOptions: (service.rateOptions ?? []).map((option) => ({ id: option.id ?? '', name: option.name, price: option.price })).filter((option) => option.id),
          bedsidePrice: service.bedsidePrice ?? null,
        }))
        .sort((a, b) => a.name.localeCompare(b.name)),
    [data],
  );

  return { services, isLoading };
}
