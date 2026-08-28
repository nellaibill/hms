import { useQueries } from '@tanstack/react-query';
import { FlaskConical, Loader2 } from 'lucide-react';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { diagnosticPackageQueryKey, useDiagnosticServicesQuery } from '@/features/diagnostics';
import { diagnosticPackagesApi } from '../../../services/apiClient';
import type { Billing } from '../types';

interface LabDetailsCardProps {
  billing: Billing;
}

interface LabDetailRow {
  key: string;
  testName: string;
  packageName?: string;
}

/**
 * Invoice detail's read-only "Lab Details" tab — one row per test actually ordered: a plain
 * Laboratory line (packageId null) is one row; a package line expands to one row per test the
 * package contains (resolved via useDiagnosticPackageQuery per distinct packageId, batched with
 * useQueries since a bill can have more than one package line). Every row shows a static
 * "Pending" badge — this is read-only/derived per the mockup, which never shows a way to enter
 * a result, only a view. No result-entry UI, no sample tracking, no order-status transitions —
 * explicitly out of scope.
 */
export function LabDetailsCard({ billing }: LabDetailsCardProps) {
  const labItems = billing.items.filter((item) => item.billingType === 'Laboratory');
  const packageIds = Array.from(new Set(labItems.filter((item) => item.packageId).map((item) => item.packageId as string)));

  // All services (Laboratory + Radiology, no isActive filter) — a service referenced by an
  // already-saved bill or package should still resolve to a name even if later deactivated.
  const servicesQuery = useDiagnosticServicesQuery({ pageSize: 200, sort: 'name' });
  const packageQueries = useQueries({
    queries: packageIds.map((packageId) => ({
      queryKey: diagnosticPackageQueryKey(packageId),
      queryFn: () => diagnosticPackagesApi.getDiagnosticPackageById(packageId),
    })),
  });

  const isLoading = servicesQuery.isPending || packageQueries.some((query) => query.isPending);
  const servicesById = new Map((servicesQuery.data?.items ?? []).map((service) => [service.id, service.name]));
  const packagesById = new Map(
    packageQueries.filter((query) => query.data).map((query) => [query.data!.id, query.data!]),
  );

  const rows: LabDetailRow[] = labItems.flatMap((item) => {
    if (item.packageId) {
      const pkg = packagesById.get(item.packageId);
      if (!pkg) return [];
      return pkg.items.map((packageItem) => ({
        key: `${item.id}-${packageItem.id}`,
        testName: servicesById.get(packageItem.serviceId) ?? packageItem.serviceId,
        packageName: pkg.name,
      }));
    }
    return [
      {
        key: item.id,
        testName: item.serviceId ? (servicesById.get(item.serviceId) ?? item.serviceId) : 'Laboratory test',
      },
    ];
  });

  return (
    <Card>
      <CardHeader className="flex-row items-center gap-3 space-y-0">
        <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-accent text-accent-foreground">
          <FlaskConical className="h-5 w-5" />
        </span>
        <div className="flex flex-col gap-1">
          <CardTitle className="text-lg">Lab Details</CardTitle>
          <CardDescription>Every test ordered on this bill, including package contents.</CardDescription>
        </div>
      </CardHeader>
      <CardContent className="flex flex-col gap-3 pt-0">
        {isLoading && (
          <div className="flex items-center justify-center gap-2 py-10 text-sm text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" />
            Loading lab details…
          </div>
        )}

        {!isLoading && rows.length === 0 && <p className="text-sm text-muted-foreground">No laboratory tests on this bill.</p>}

        {!isLoading && rows.length > 0 && (
          <div className="flex flex-col divide-y divide-border rounded-md border border-border">
            {rows.map((row) => (
              <div key={row.key} className="flex flex-wrap items-center justify-between gap-3 px-4 py-3">
                <div className="flex flex-col gap-0.5">
                  <span className="text-sm font-medium text-foreground">{row.testName}</span>
                  {row.packageName && <span className="text-xs text-muted-foreground">Part of {row.packageName}</span>}
                </div>
                <Badge variant="warning">Pending</Badge>
              </div>
            ))}
          </div>
        )}
      </CardContent>
    </Card>
  );
}
