import type { DiagnosticServiceType } from '@hms/shared';
import { useEffect, useMemo } from 'react';
import { primeDiagnosticServiceCache } from '../referenceCache';
import { useDiagnosticServicesQuery } from './useDiagnosticServicesQuery';

export interface BillingServiceOption {
  id: string;
  name: string;
  price: number;
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
  const { data, isLoading } = useDiagnosticServicesQuery({ serviceType, isActive: true, pageSize: 200 });

  // Primes the diagnostics reference cache describeBillingItem reads from — see
  // referenceCache.ts. Runs as an effect (not in queryFn) so it stays a pure side-effect of
  // this hook's own render rather than react-query internals.
  useEffect(() => {
    if (data?.items) primeDiagnosticServiceCache(data.items);
  }, [data]);

  const services = useMemo<BillingServiceOption[]>(
    () => (data?.items ?? []).map((service) => ({ id: service.id, name: service.name, price: service.price })).sort((a, b) => a.name.localeCompare(b.name)),
    [data],
  );

  return { services, isLoading };
}
