import type { DiagnosticPackage, DiagnosticService } from '@hms/shared';

/**
 * A tiny synchronous id->label cache for DiagnosticService/DiagnosticPackage, mirroring
 * features/masters/engine/registry.ts's primeReferenceCache/resolveRecordLabel pattern —
 * but standalone, since DiagnosticService/DiagnosticPackage are typed DTOs, not registered in
 * the generic Masters engine (see dtos/diagnostics' own file comments for why). Primed inside
 * the queryFn of useDiagnosticServices/usePrimeDiagnosticPackageCache (like Masters'
 * useMasterOptionsQuery), so the cache already holds the new data on the render that delivers
 * it — priming in an effect ran after that render, and a report's rows memo kept the old (or
 * missing) running costs. Read synchronously by billingCalculations.ts's describeBillingItem/
 * resolveItemCostPrice, which are called during render and can't await a fetch themselves.
 */
const serviceCache = new Map<string, DiagnosticService>();
const packageCache = new Map<string, DiagnosticPackage>();

export function primeDiagnosticServiceCache(services: DiagnosticService[]): void {
  for (const service of services) serviceCache.set(service.id, service);
}

export function primeDiagnosticPackageCache(packages: DiagnosticPackage[]): void {
  for (const pkg of packages) packageCache.set(pkg.id, pkg);
}

export function resolveDiagnosticServiceLabel(id: string | undefined | null): string {
  if (!id) return '—';
  return serviceCache.get(id)?.name ?? id;
}

/** Resolves a DiagnosticService id to its CostPrice, mirroring features/masters/engine/
 * registry.ts's resolveRecordCostPrice — returns null (not 0) both when the record hasn't
 * resolved yet and when CostPrice is genuinely 0 ("not yet costed"), so a caller never
 * mistakes either case for "free." */
export function resolveDiagnosticServiceCostPrice(id: string | undefined | null): number | null {
  if (!id) return null;
  const cost = serviceCache.get(id)?.costPrice;
  return typeof cost === 'number' && cost > 0 ? cost : null;
}

/** A DiagnosticPackage's running cost for profit reporting: the package's own CostPrice when
 * set; otherwise the total of its tests' running costs, but only when every test has one — a
 * partial total would understate the cost. Null (never 0) when neither is known, the same
 * "not yet costed" rule as resolveDiagnosticServiceCostPrice. Reads the service half of this
 * cache too, so both halves must be primed (useDiagnosticServices('Laboratory') +
 * usePrimeDiagnosticPackageCache). */
export function resolveDiagnosticPackageCostPrice(id: string | undefined | null): number | null {
  if (!id) return null;
  const pkg = packageCache.get(id);
  if (!pkg) return null;
  if (typeof pkg.costPrice === 'number' && pkg.costPrice > 0) return pkg.costPrice;
  if (pkg.items.length === 0) return null;

  let total = 0;
  for (const item of pkg.items) {
    const cost = resolveDiagnosticServiceCostPrice(item.serviceId);
    if (cost === null) return null;
    total += cost;
  }
  return total;
}

export function resolveDiagnosticPackageLabel(id: string | undefined | null): string {
  if (!id) return '—';
  const pkg = packageCache.get(id);
  return pkg ? `${pkg.name} (Package)` : id;
}
