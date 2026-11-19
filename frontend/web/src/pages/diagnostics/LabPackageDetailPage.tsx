import { Loader2, PackageSearch, Plus, X } from 'lucide-react';
import { useMemo, useState } from 'react';
import { useParams } from 'react-router-dom';
import { PageBanner } from '@/components/PageBanner';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { SearchableSelect } from '@/components/ui/searchable-select';
import { useAuth } from '@/features/auth/AuthContext';
import {
  DiagnosticPackageFormDialog,
  formatRupees,
  PackagePriceCard,
  testsRunningCostTotal,
  useAddPackageItemMutation,
  useAllDiagnosticServicesQuery,
  useDiagnosticPackageQuery,
  useRemovePackageItemMutation,
} from '@/features/diagnostics';

/**
 * A lab package's page (ADR-140): its details (edited in DiagnosticPackageFormDialog), the tests
 * it includes with each test's own price and running cost, and — beside them — the package price
 * and profit, set the way the hospital's tariff sheet writes them.
 */
export default function LabPackageDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { hasPermission } = useAuth();
  const canEdit = hasPermission('diagnostics.edit');

  const { data: pkg, isPending, isError } = useDiagnosticPackageQuery(id);
  // The whole catalog (not one page of 100), so every included test resolves to its name.
  const labQuery = useAllDiagnosticServicesQuery('Laboratory');
  const radiologyQuery = useAllDiagnosticServicesQuery('Radiology');
  const addItemMutation = useAddPackageItemMutation();
  const removeItemMutation = useRemovePackageItemMutation();

  const [showEditDialog, setShowEditDialog] = useState(false);
  const [selectedServiceId, setSelectedServiceId] = useState('');
  const [addError, setAddError] = useState<string | null>(null);

  const services = useMemo(() => [...(labQuery.data ?? []), ...(radiologyQuery.data ?? [])], [labQuery.data, radiologyQuery.data]);
  const servicesById = useMemo(() => new Map(services.map((service) => [service.id, service])), [services]);
  const servicesLoading = labQuery.isPending || radiologyQuery.isPending;

  if (isPending) {
    return (
      <div className="flex flex-1 items-center justify-center gap-2 p-6 text-sm text-muted-foreground">
        <Loader2 className="h-4 w-4 animate-spin" />
        Loading package…
      </div>
    );
  }

  if (isError || !pkg) {
    return (
      <div className="p-6">
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          Package not found.
        </p>
      </div>
    );
  }

  const includedTests = pkg.items.map((item) => ({ item, service: servicesById.get(item.serviceId) }));
  const resolvedTests = includedTests.flatMap(({ service }) => (service ? [service] : []));
  const testsTotalPrice = resolvedTests.reduce((sum, service) => sum + service.price, 0);
  const testsRunningCost = resolvedTests.length === includedTests.length ? testsRunningCostTotal(resolvedTests) : null;
  const testsRunningTotalShown = resolvedTests.reduce((sum, service) => sum + service.costPrice, 0);

  const includedServiceIds = new Set(pkg.items.map((item) => item.serviceId));
  const addOptions = services
    .filter((service) => service.isActive && !includedServiceIds.has(service.id))
    .sort((a, b) => a.name.localeCompare(b.name))
    .map((service) => ({
      value: service.id,
      label: `${service.name} — ${formatRupees(service.price)}`,
      keywords: service.code,
    }));

  function handleAddTest() {
    if (!id) return;
    if (!selectedServiceId) {
      setAddError('Select a test to add.');
      return;
    }
    setAddError(null);
    addItemMutation.mutate({ packageId: id, request: { serviceId: selectedServiceId } }, { onSuccess: () => setSelectedServiceId('') });
  }

  function handleRemoveItem(itemId: string) {
    if (!id) return;
    removeItemMutation.mutate({ packageId: id, itemId });
  }

  return (
    <div className="flex flex-1 flex-col">
      <PageBanner icon={PackageSearch} title={pkg.name} subtitle={`Lab package · ${pkg.code}`} backTo="/diagnostics/lab/packages" backLabel="Back to packages" />

      <div className="flex flex-1 flex-col gap-6 p-4 sm:p-6 lg:p-8">
        <div className="mx-auto grid w-full max-w-6xl items-start gap-6 lg:grid-cols-[minmax(0,1fr)_380px]">
          <div className="flex min-w-0 flex-col gap-6">
            <Card>
              <CardHeader className="flex-row items-center justify-between space-y-0">
                <div className="flex flex-col gap-1">
                  <CardTitle className="text-base">Package details</CardTitle>
                  <CardDescription>Name, code, how it reads on the bill, and status.</CardDescription>
                </div>
                {canEdit && (
                  <Button variant="outline" size="sm" onClick={() => setShowEditDialog(true)}>
                    Edit details
                  </Button>
                )}
              </CardHeader>
              <CardContent className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <div className="flex flex-col gap-0.5">
                  <span className="text-xs text-muted-foreground">Name</span>
                  <span className="text-sm font-medium text-foreground">{pkg.name}</span>
                </div>
                <div className="flex flex-col gap-0.5">
                  <span className="text-xs text-muted-foreground">Code</span>
                  <span className="font-mono text-sm text-foreground">{pkg.code}</span>
                </div>
                <div className="flex flex-col gap-0.5 sm:col-span-2">
                  <span className="text-xs text-muted-foreground">Shown on the bill and report as</span>
                  <span className="text-sm text-foreground">{pkg.description || '—'}</span>
                </div>
                <div className="flex flex-col gap-0.5">
                  <span className="text-xs text-muted-foreground">Status</span>
                  <Badge variant={pkg.isActive ? 'success' : 'secondary'} className="w-fit">
                    {pkg.isActive ? 'Active' : 'Inactive'}
                  </Badge>
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle className="text-base">Tests in this package</CardTitle>
                <CardDescription>
                  {pkg.items.length} test{pkg.items.length === 1 ? '' : 's'}. Each keeps its own price for when it's billed alone.
                </CardDescription>
              </CardHeader>
              <CardContent className="flex flex-col gap-3">
                {pkg.items.length === 0 && <p className="text-sm text-muted-foreground">No tests in this package yet — add one below.</p>}

                {pkg.items.length > 0 && (
                  <div className="overflow-x-auto rounded-md border border-border">
                    <table className="w-full min-w-[460px] text-sm">
                      <thead>
                        <tr className="border-b border-border bg-muted/50 text-left text-xs text-muted-foreground">
                          <th scope="col" className="px-4 py-2 font-medium">Test</th>
                          <th scope="col" className="px-4 py-2 text-right font-medium">Price alone</th>
                          <th scope="col" className="px-4 py-2 text-right font-medium">Running cost</th>
                          {canEdit && (
                            <th scope="col" className="w-12 px-2 py-2">
                              <span className="sr-only">Remove</span>
                            </th>
                          )}
                        </tr>
                      </thead>
                      <tbody>
                        {includedTests.map(({ item, service }) => (
                          <tr key={item.id} className="border-b border-border last:border-0">
                            <td className="px-4 py-2.5 font-medium">{service?.name ?? (servicesLoading ? 'Loading…' : item.serviceId)}</td>
                            <td className="px-4 py-2.5 text-right tabular-nums">{service ? formatRupees(service.price) : '—'}</td>
                            <td className="px-4 py-2.5 text-right tabular-nums">
                              {service ? (service.costPrice > 0 ? formatRupees(service.costPrice) : <span className="text-muted-foreground">Not costed</span>) : '—'}
                            </td>
                            {canEdit && (
                              <td className="px-2 py-1.5">
                                <Button
                                  variant="ghost"
                                  size="icon"
                                  aria-label={`Remove ${service?.name ?? 'test'}`}
                                  disabled={removeItemMutation.isPending}
                                  onClick={() => handleRemoveItem(item.id)}
                                >
                                  <X className="h-4 w-4" />
                                </Button>
                              </td>
                            )}
                          </tr>
                        ))}
                      </tbody>
                      <tfoot>
                        <tr className="bg-muted/50 font-semibold">
                          <td className="px-4 py-2.5">If billed one by one</td>
                          <td className="px-4 py-2.5 text-right tabular-nums">{formatRupees(testsTotalPrice)}</td>
                          <td className="px-4 py-2.5 text-right tabular-nums">{formatRupees(testsRunningTotalShown)}</td>
                          {canEdit && <td />}
                        </tr>
                      </tfoot>
                    </table>
                  </div>
                )}

                {canEdit && (
                  <div className="flex flex-wrap items-end gap-3 border-t border-dashed border-border pt-4">
                    <div className="flex min-w-[240px] flex-1 flex-col gap-1.5">
                      <span className="text-sm font-medium text-foreground">Add a test</span>
                      <SearchableSelect
                        id="add-test-to-package"
                        ariaLabel="Add a test to the package"
                        value={selectedServiceId}
                        onValueChange={(value) => {
                          setSelectedServiceId(value);
                          setAddError(null);
                        }}
                        options={addOptions}
                        placeholder={servicesLoading ? 'Loading tests…' : 'Search tests…'}
                        searchPlaceholder="Search tests…"
                        disabled={servicesLoading}
                      />
                      {addError && <p className="text-sm text-destructive">{addError}</p>}
                    </div>
                    <Button type="button" className="gap-1.5" onClick={handleAddTest} disabled={addItemMutation.isPending}>
                      <Plus className="h-4 w-4" />
                      {addItemMutation.isPending ? 'Adding…' : 'Add'}
                    </Button>
                  </div>
                )}
              </CardContent>
            </Card>
          </div>

          <PackagePriceCard pkg={pkg} testsTotalPrice={testsTotalPrice} testsRunningCost={testsRunningCost} canEdit={canEdit} />
        </div>
      </div>

      {showEditDialog && <DiagnosticPackageFormDialog pkg={pkg} onClose={() => setShowEditDialog(false)} />}
    </div>
  );
}
