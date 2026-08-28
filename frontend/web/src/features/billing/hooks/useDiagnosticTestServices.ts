import { useMemo } from 'react';
import { useMasterOptionsQuery } from '@/features/masters';
import type { BillingService } from '../billingCatalog';

/**
 * Laboratory/Radiology Billing's Service dropdown, backed by the real DiagnosticTest master
 * data (replaces the old hardcoded LABORATORY_SERVICES/RADIOLOGY_SERVICES arrays). Filters
 * the shared diagnosticTest catalog down to one service type and active records client-side —
 * there's no per-field filter on the generic Masters list endpoint, and the full catalog is
 * only ~250 rows, so fetching once and filtering in memory is simplest.
 */
export function useDiagnosticTestServices(serviceType: 'Laboratory' | 'Radiology'): { services: BillingService[]; isLoading: boolean } {
  const { data, isLoading } = useMasterOptionsQuery('diagnosticTest');

  const services = useMemo<BillingService[]>(
    () =>
      (data ?? [])
        .filter((record) => record.isActive && record.serviceType === serviceType)
        .map((record) => ({
          id: String(record.id),
          name: String(record.name),
          price: Number(record.price ?? 0),
        }))
        .sort((a, b) => a.name.localeCompare(b.name)),
    [data, serviceType],
  );

  return { services, isLoading };
}
