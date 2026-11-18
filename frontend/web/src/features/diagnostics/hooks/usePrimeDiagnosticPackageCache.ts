import type { DiagnosticPackage } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { diagnosticPackagesApi } from '../../../services/apiClient';
import { primeDiagnosticPackageCache } from '../referenceCache';

/** Every package, inactive ones too (a bill line for a package retired since still needs its
 * label and running cost), walking pages at the server's maximum page size. */
async function fetchAllDiagnosticPackages(): Promise<DiagnosticPackage[]> {
  const all: DiagnosticPackage[] = [];
  let page = 1;
  let totalPages = 1;
  do {
    const paged = await diagnosticPackagesApi.getDiagnosticPackages({ page, pageSize: 100, sort: 'name' });
    all.push(...paged.items);
    totalPages = paged.meta.totalPages;
    page++;
  } while (page <= totalPages);
  return all;
}

/**
 * Primes the diagnostics reference cache's package half (see referenceCache.ts) — the package
 * counterpart of useDiagnosticServices' own priming. Called wherever describeBillingItem/
 * resolveItemCostPrice need a Laboratory line's packageId resolved (InvoiceDetailCard,
 * BillingSummaryCard, PatientDetails' billing section, and the Finance reports).
 *
 * Primes inside queryFn, so the cache is current on the render that delivers the data, and
 * returns that data: a report lists it in its rows memo's dependencies so package labels and
 * running costs recompute once they load.
 */
export function usePrimeDiagnosticPackageCache(): DiagnosticPackage[] | undefined {
  const { data } = useQuery({
    queryKey: ['diagnostics', 'packages', 'with-inactive'],
    queryFn: async () => {
      const all = await fetchAllDiagnosticPackages();
      primeDiagnosticPackageCache(all);
      return all;
    },
  });
  return data;
}
