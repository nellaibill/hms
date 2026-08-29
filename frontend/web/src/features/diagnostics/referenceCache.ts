import type { DiagnosticPackage, DiagnosticService } from '@hms/shared';

/**
 * A tiny synchronous id->label cache for DiagnosticService/DiagnosticPackage, mirroring
 * features/masters/engine/registry.ts's primeReferenceCache/resolveRecordLabel pattern —
 * but standalone, since DiagnosticService/DiagnosticPackage are typed DTOs, not registered in
 * the generic Masters engine (see dtos/diagnostics' own file comments for why). Primed by
 * useDiagnosticServicesQuery/useDiagnosticPackagesQuery as their react-query fetches resolve;
 * read synchronously by billingCalculations.ts's describeBillingItem, which (like the Masters
 * version) is called during render and can't await a fetch itself.
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

export function resolveDiagnosticPackageLabel(id: string | undefined | null): string {
  if (!id) return '—';
  const pkg = packageCache.get(id);
  return pkg ? `${pkg.name} (Package)` : id;
}
