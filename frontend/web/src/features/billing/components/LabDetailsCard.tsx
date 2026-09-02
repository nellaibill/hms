import { useQueries } from '@tanstack/react-query';
import { ExternalLink, FlaskConical, Loader2 } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { useAuth } from '@/features/auth/AuthContext';
import { diagnosticPackageQueryKey, useDiagnosticServicesQuery } from '@/features/diagnostics';
import { LabStatusBadge, useLabOrdersByPatientQuery, type LabOrderItem } from '@/features/laboratory';
import { diagnosticPackagesApi } from '../../../services/apiClient';
import type { Billing } from '../types';

interface LabDetailsCardProps {
  billing: Billing;
}

interface LabDetailRow {
  key: string;
  testName: string;
  packageName?: string;
  /** The matching HMS.Modules.Laboratory item, once a LabOrder exists for this invoice —
   * see the map-building comment below for how a row is matched to it. */
  labItem?: LabOrderItem;
}

/** Key a billing row and a LabOrderItem the same way, so a package's expanded service rows
 * line up with the LabOrder's own per-service items (both independently expand a package the
 * same way — one item per DiagnosticPackage.items entry, sharing that item's serviceId and the
 * line's packageId). */
function matchKey(packageId: string | undefined | null, serviceId: string | undefined | null): string {
  return `${packageId ?? 'none'}:${serviceId ?? 'none'}`;
}

/**
 * Invoice detail's "Lab Details" tab — one row per test actually ordered: a plain Laboratory
 * line (packageId null) is one row; a package line expands to one row per test the package
 * contains (resolved via useDiagnosticPackageQuery per distinct packageId, batched with
 * useQueries since a bill can have more than one package line). Each row's status badge is the
 * REAL per-test status from HMS.Modules.Laboratory — found via useLabOrdersByPatientQuery(
 * billing.patientId), matching the one LabOrder whose invoiceId equals this bill's id (a
 * LabOrder maps to exactly one Invoice, so this find is exact) and then matching each row to
 * its LabOrderItem by (packageId, serviceId). Falls back to the static "Pending" look only when
 * no LabOrder exists yet for this invoice (e.g. a brand-new bill where the best-effort Billing
 * -> Laboratory hook hasn't run yet or failed) — this also doubles as the Reception Notification
 * / Status Update requirement: a real status here IS the notification, no separate mechanism
 * needed.
 */
export function LabDetailsCard({ billing }: LabDetailsCardProps) {
  const { hasPermission } = useAuth();
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
  const labOrdersQuery = useLabOrdersByPatientQuery(billing.patientId);

  const isLoading = servicesQuery.isPending || packageQueries.some((query) => query.isPending) || labOrdersQuery.isPending;
  const servicesById = new Map((servicesQuery.data?.items ?? []).map((service) => [service.id, service.name]));
  const packagesById = new Map(
    packageQueries.filter((query) => query.data).map((query) => [query.data!.id, query.data!]),
  );

  const labOrder = labOrdersQuery.data?.find((order) => order.invoiceId === billing.id);
  const labItemsByKey = new Map((labOrder?.items ?? []).map((item) => [matchKey(item.packageId, item.serviceId), item]));

  const rows: LabDetailRow[] = labItems.flatMap((item) => {
    if (item.packageId) {
      const pkg = packagesById.get(item.packageId);
      if (!pkg) return [];
      return pkg.items.map((packageItem) => ({
        key: `${item.id}-${packageItem.id}`,
        testName: servicesById.get(packageItem.serviceId) ?? packageItem.serviceId,
        packageName: pkg.name,
        labItem: labItemsByKey.get(matchKey(item.packageId, packageItem.serviceId)),
      }));
    }
    return [
      {
        key: item.id,
        testName: item.serviceId ? (servicesById.get(item.serviceId) ?? item.serviceId) : 'Laboratory test',
        labItem: item.serviceId ? labItemsByKey.get(matchKey(null, item.serviceId)) : undefined,
      },
    ];
  });

  return (
    <Card>
      <CardHeader className="flex-row items-center gap-3 space-y-0">
        <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-accent text-accent-foreground">
          <FlaskConical className="h-5 w-5" />
        </span>
        <div className="flex flex-1 flex-col gap-1">
          <CardTitle className="text-lg">Lab Details</CardTitle>
          <CardDescription>Every test ordered on this bill, including package contents.</CardDescription>
        </div>
        {labOrder && hasPermission('diagnostics.view') && (
          <Link
            to={`/diagnostics/lab/orders/${labOrder.id}`}
            className="inline-flex shrink-0 items-center gap-1 text-xs font-medium text-primary hover:underline"
          >
            View in Lab Worklist
            <ExternalLink className="h-3.5 w-3.5" />
          </Link>
        )}
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
                {row.labItem ? <LabStatusBadge status={row.labItem.status} /> : <Badge variant="warning">Pending</Badge>}
              </div>
            ))}
          </div>
        )}
      </CardContent>
    </Card>
  );
}
