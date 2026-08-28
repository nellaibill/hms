import { useEffect } from 'react';
import { primeDiagnosticPackageCache } from '../referenceCache';
import { useDiagnosticPackagesQuery } from './useDiagnosticPackagesQuery';

/**
 * Primes the diagnostics reference cache's package half (see referenceCache.ts) — mirrors
 * useDiagnosticServices' own service-side priming. Called wherever describeBillingItem needs
 * to resolve a Laboratory line's packageId to a display label without repeating the
 * fetch+prime boilerplate (InvoiceDetailCard, BillingSummaryCard, PatientDetails' billing
 * section).
 */
export function usePrimeDiagnosticPackageCache(): void {
  const { data } = useDiagnosticPackagesQuery({ isActive: true, pageSize: 200, sort: 'name' });
  useEffect(() => {
    if (data?.items) primeDiagnosticPackageCache(data.items);
  }, [data]);
}
