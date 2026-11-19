import type { DiagnosticPackage, DiagnosticService, DiagnosticServiceRateOption } from '@hms/shared';

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

/** A radiology service's rate option, by id (ADR-140). Undefined until the service is cached, or
 * when the option was removed since. */
export function resolveDiagnosticRateOption(serviceId: string | undefined | null, rateOptionId: string | undefined | null): DiagnosticServiceRateOption | undefined {
  if (!serviceId || !rateOptionId) return undefined;
  return serviceCache.get(serviceId)?.rateOptions?.find((option) => option.id === rateOptionId);
}

/**
 * A radiology line's cost, split the way the tariff sheet does (ADR-140): the doctor's share and
 * the hospital's running cost of the rate billed — the rate option's own when one was picked
 * (and still exists), else the service's standard rate — plus the bedside add-on's doctor's
 * share for a bedside scan. Null when the service isn't cached yet or the rate isn't costed
 * (both parts 0), the same "never a fabricated 0" rule as resolveDiagnosticServiceCostPrice.
 */
export function resolveDiagnosticServiceCost(
  serviceId: string | undefined | null,
  rateOptionId?: string | null,
  isBedside?: boolean,
): { doctorShare: number; runningCost: number } | null {
  if (!serviceId) return null;
  const service = serviceCache.get(serviceId);
  if (!service) return null;
  const option = rateOptionId ? service.rateOptions?.find((candidate) => candidate.id === rateOptionId) : undefined;
  const doctorShare = option ? option.doctorCharge : (service.doctorCharge ?? 0);
  const runningCost = option ? option.costPrice : service.costPrice;
  if (!(doctorShare > 0) && !(runningCost > 0)) return null;
  const bedsideDoctorShare = isBedside ? (service.bedsideDoctorCharge ?? 0) : 0;
  return { doctorShare: doctorShare + bedsideDoctorShare, runningCost };
}
