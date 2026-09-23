import { ApiError } from '@hms/shared';
import { Loader2, Plus, X } from 'lucide-react';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { SearchableSelect, type SearchableSelectOption } from '@/components/ui/searchable-select';
import { formatCurrency } from '@/features/billing/billingCalculations';
import { useDiagnosticPackagesQuery, useDiagnosticServices } from '@/features/diagnostics';
import { LabStatusBadge } from '@/features/laboratory';
import { generateClientId } from '@/lib/id';
import { useLabOrdersQuery, usePostLabOrderMutation } from '../hooks/useLabOrders';

interface LabOrdersPanelProps {
  admissionId: string;
}

type ItemType = 'service' | 'package';

interface DraftLine {
  key: string;
  itemType: ItemType;
  itemId: string;
}

/** `svc:<id>` / `pkg:<id>` — disambiguates a single flat SearchableSelect's value into
 * itemType/itemId, same convention as Billing's LaboratoryBillingCard. */
function toOptionValue(itemType: ItemType, itemId: string): string {
  return `${itemType === 'package' ? 'pkg' : 'svc'}:${itemId}`;
}

function parseOptionValue(value: string): { itemType: ItemType; itemId: string } | null {
  if (value.startsWith('svc:')) return { itemType: 'service', itemId: value.slice(4) };
  if (value.startsWith('pkg:')) return { itemType: 'package', itemId: value.slice(4) };
  return null;
}

function emptyLine(): DraftLine {
  return { key: generateClientId(), itemType: 'service', itemId: '' };
}

/**
 * Places a real HMS.Modules.Laboratory LabOrder directly against this admission — no invoice
 * required (see IPDLabOrderService.PlaceOrderAsync, ADR-064). Deliberately reuses the existing
 * `/diagnostics/lab/orders/:id` page (LabOrderDetailPage) for sample collection/result entry/
 * verification/report release rather than rebuilding any of that workflow here — this panel is
 * only the ordering surface plus a status-at-a-glance list.
 */
export function LabOrdersPanel({ admissionId }: LabOrdersPanelProps) {
  const { data: orders, isPending, isError } = useLabOrdersQuery(admissionId);
  const createMutation = usePostLabOrderMutation(admissionId);
  const { services, isLoading: isLoadingServices } = useDiagnosticServices('Laboratory');
  const packagesQuery = useDiagnosticPackagesQuery({ isActive: true, pageSize: 200, sort: 'name' });
  const packages = packagesQuery.data?.items ?? [];
  const isLoadingItems = isLoadingServices || packagesQuery.isPending;

  const [lines, setLines] = useState<DraftLine[]>([emptyLine()]);

  const serviceOptions: SearchableSelectOption[] = services.map((service) => ({
    value: toOptionValue('service', service.id),
    label: `${service.name} — ${formatCurrency(service.price)}`,
    keywords: service.name,
  }));
  const packageOptions: SearchableSelectOption[] = packages.map((pkg) => ({
    value: toOptionValue('package', pkg.id),
    label: `${pkg.name} — ${formatCurrency(pkg.totalPrice)}`,
    keywords: pkg.name,
  }));

  function updateLine(key: string, value: string) {
    const parsed = parseOptionValue(value);
    if (!parsed) return;
    setLines((prev) => prev.map((line) => (line.key === key ? { key, ...parsed } : line)));
  }

  function removeLine(key: string) {
    setLines((prev) => (prev.length > 1 ? prev.filter((line) => line.key !== key) : prev));
  }

  function handleSubmit() {
    const validLines = lines.filter((line) => line.itemId);
    if (validLines.length === 0) return;

    createMutation.mutate(
      {
        lines: validLines.map((line) =>
          line.itemType === 'package' ? { packageId: line.itemId } : { serviceId: line.itemId },
        ),
      },
      { onSuccess: () => setLines([emptyLine()]) },
    );
  }

  const apiError = createMutation.error instanceof ApiError ? createMutation.error : null;

  return (
    <div className="flex flex-col gap-4">
      {isPending && (
        <div className="flex items-center gap-2 py-6 text-sm text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" />
          Loading lab orders…
        </div>
      )}

      {isError && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          Failed to load lab orders.
        </p>
      )}

      {!isPending && !isError && (
        <div className="overflow-x-auto rounded-lg border border-border">
          <table className="w-full text-sm">
            <thead className="bg-muted/50 text-left text-xs font-medium uppercase tracking-wide text-muted-foreground">
              <tr>
                <th className="px-3 py-2.5">Order #</th>
                <th className="px-3 py-2.5">Tests</th>
                <th className="px-3 py-2.5">Priority</th>
                <th className="px-3 py-2.5">Status</th>
                <th className="px-3 py-2.5">Placed</th>
                <th className="px-3 py-2.5 text-right">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {(orders ?? []).length === 0 && (
                <tr>
                  <td colSpan={6} className="px-4 py-6 text-center text-sm text-muted-foreground">
                    No lab orders placed yet.
                  </td>
                </tr>
              )}
              {orders?.map((order) => (
                <tr key={order.id}>
                  <td className="px-3 py-3 text-sm font-medium text-foreground">{order.labOrderNumber}</td>
                  <td className="px-3 py-3 text-sm text-foreground">{order.items.map((item) => item.testName).join(', ')}</td>
                  <td className="px-3 py-3 text-sm text-muted-foreground">{order.priority}</td>
                  <td className="px-3 py-3 text-sm">
                    <LabStatusBadge status={order.overallStatus} />
                  </td>
                  <td className="px-3 py-3 text-sm text-muted-foreground">{new Date(order.createdAt).toLocaleString('en-IN')}</td>
                  <td className="px-3 py-3 text-right">
                    <Button asChild variant="ghost" size="sm">
                      <Link to={`/diagnostics/lab/orders/${order.id}`}>View Details</Link>
                    </Button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <div className="flex flex-col gap-3 rounded-lg border border-dashed border-border p-4">
        {apiError && (
          <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
            {apiError.message}
          </p>
        )}

        {lines.map((line, index) => {
          const filteredOptions = line.itemType === 'package' ? packageOptions : serviceOptions;
          return (
            <div key={line.key} className={index < lines.length - 1 ? 'flex flex-col gap-2 border-b border-dashed border-border pb-3' : 'flex flex-col gap-2'}>
              <div className="inline-flex w-fit overflow-hidden rounded-md border border-input">
                <button
                  type="button"
                  onClick={() => setLines((prev) => prev.map((l) => (l.key === line.key ? { ...l, itemType: 'service', itemId: '' } : l)))}
                  className={`px-2.5 py-1 text-xs font-medium transition-colors ${
                    line.itemType === 'service' ? 'bg-primary text-primary-foreground' : 'bg-background text-muted-foreground hover:bg-accent'
                  }`}
                >
                  Services
                </button>
                <button
                  type="button"
                  onClick={() => setLines((prev) => prev.map((l) => (l.key === line.key ? { ...l, itemType: 'package', itemId: '' } : l)))}
                  className={`px-2.5 py-1 text-xs font-medium transition-colors ${
                    line.itemType === 'package' ? 'bg-primary text-primary-foreground' : 'bg-background text-muted-foreground hover:bg-accent'
                  }`}
                >
                  Packages
                </button>
              </div>

              <div className="flex flex-wrap items-center gap-3">
                <div className="min-w-[240px] flex-1">
                  <SearchableSelect
                    id={`lab-order-line-${line.key}`}
                    ariaLabel="Test or package"
                    value={line.itemId ? toOptionValue(line.itemType, line.itemId) : ''}
                    onValueChange={(value) => updateLine(line.key, value)}
                    options={filteredOptions}
                    placeholder={isLoadingItems ? 'Loading items…' : line.itemType === 'package' ? 'Select a package' : 'Select a test'}
                    searchPlaceholder={line.itemType === 'package' ? 'Search packages…' : 'Search tests…'}
                    disabled={isLoadingItems}
                  />
                </div>

                {lines.length > 1 && (
                  <Button type="button" variant="ghost" size="icon" aria-label="Remove this line" onClick={() => removeLine(line.key)}>
                    <X className="h-4 w-4" />
                  </Button>
                )}
              </div>
            </div>
          );
        })}

        <div className="flex flex-wrap items-center gap-2">
          <Button type="button" variant="outline" size="sm" className="gap-1.5" onClick={() => setLines((prev) => [...prev, emptyLine()])}>
            <Plus className="h-3.5 w-3.5" />
            Add another test
          </Button>
          <Button
            type="button"
            className="gap-1.5"
            disabled={createMutation.isPending || lines.every((line) => !line.itemId)}
            onClick={handleSubmit}
          >
            <Plus className="h-4 w-4" />
            {createMutation.isPending ? 'Placing…' : 'Place Lab Order'}
          </Button>
        </div>
      </div>
    </div>
  );
}
