import { ArrowLeft, Loader2, PackageSearch, Plus, X } from 'lucide-react';
import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { SearchableSelect } from '@/components/ui/searchable-select';
import { useAuth } from '@/features/auth/AuthContext';
import {
  DiagnosticPackageFormDialog,
  useAddPackageItemMutation,
  useDiagnosticPackageQuery,
  useDiagnosticServicesQuery,
  useRemovePackageItemMutation,
} from '@/features/diagnostics';

/**
 * Central Laboratory's Package detail page — Package Information (with an Edit button opening
 * DiagnosticPackageFormDialog), the Included Tests list (each with a Remove action), and an
 * "Add Test to Package" SearchableSelect + Add button, mirroring the mockup exactly.
 */
export default function LabPackageDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { hasPermission } = useAuth();
  const canEdit = hasPermission('diagnostics.edit');

  const { data: pkg, isPending, isError } = useDiagnosticPackageQuery(id);
  // Loaded once (all active Laboratory+Radiology services) to resolve each item's serviceId to
  // a name/price and to build the "Add Test to Package" options list — the full catalog is a
  // few hundred rows at most, so fetching once and filtering client-side is simplest, same
  // reasoning useDiagnosticTestServices/useDiagnosticServices already use elsewhere.
  const servicesQuery = useDiagnosticServicesQuery({ isActive: true, pageSize: 200, sort: 'name' });
  const addItemMutation = useAddPackageItemMutation();
  const removeItemMutation = useRemovePackageItemMutation();

  const [showEditDialog, setShowEditDialog] = useState(false);
  const [selectedServiceId, setSelectedServiceId] = useState('');
  const [addError, setAddError] = useState<string | null>(null);

  const servicesById = new Map((servicesQuery.data?.items ?? []).map((service) => [service.id, service]));

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

  const includedServiceIds = new Set(pkg.items.map((item) => item.serviceId));
  const addOptions = (servicesQuery.data?.items ?? [])
    .filter((service) => !includedServiceIds.has(service.id))
    .map((service) => ({
      value: service.id,
      label: `${service.name} — ₹${service.price.toLocaleString('en-IN')} (${service.serviceType})`,
      keywords: service.code,
    }));

  function handleAddTest() {
    if (!id) return;
    if (!selectedServiceId) {
      setAddError('Select a test to add.');
      return;
    }
    setAddError(null);
    addItemMutation.mutate(
      { packageId: id, request: { serviceId: selectedServiceId } },
      { onSuccess: () => setSelectedServiceId('') },
    );
  }

  function handleRemoveItem(itemId: string) {
    if (!id) return;
    removeItemMutation.mutate({ packageId: id, itemId });
  }

  return (
    <div className="flex flex-1 flex-col">
      <div className="px-6 pt-4 lg:px-8">
        <Link to="/diagnostics/lab/packages" className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
          <ArrowLeft className="h-4 w-4" />
          Back to packages
        </Link>
      </div>

      <div className="mt-3 flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        <div className="flex items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
            <PackageSearch className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight">{pkg.name}</h1>
        </div>
        <p className="max-w-2xl text-sm text-page-banner-foreground/85">Package {pkg.code}</p>
      </div>

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
        <div className="mx-auto w-full max-w-3xl flex flex-col gap-6">
          <Card>
            <CardHeader className="flex-row items-center justify-between space-y-0">
              <div className="flex flex-col gap-1">
                <CardTitle className="text-base">Package Information</CardTitle>
                <CardDescription>Code, pricing, and status.</CardDescription>
              </div>
              {canEdit && (
                <Button variant="outline" size="sm" onClick={() => setShowEditDialog(true)}>
                  Edit
                </Button>
              )}
            </CardHeader>
            <CardContent className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <div className="flex flex-col gap-0.5">
                <span className="text-xs text-muted-foreground">Code</span>
                <span className="font-mono text-sm text-foreground">{pkg.code}</span>
              </div>
              <div className="flex flex-col gap-0.5">
                <span className="text-xs text-muted-foreground">Name</span>
                <span className="text-sm font-medium text-foreground">{pkg.name}</span>
              </div>
              <div className="flex flex-col gap-0.5 sm:col-span-2">
                <span className="text-xs text-muted-foreground">Description</span>
                <span className="text-sm text-foreground">{pkg.description || '—'}</span>
              </div>
              <div className="flex flex-col gap-0.5">
                <span className="text-xs text-muted-foreground">Total Price</span>
                <span className="text-sm font-medium text-foreground">₹{pkg.totalPrice.toLocaleString('en-IN')}</span>
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
              <CardTitle className="text-base">Included Tests</CardTitle>
              <CardDescription>{pkg.items.length} test{pkg.items.length === 1 ? '' : 's'} in this package.</CardDescription>
            </CardHeader>
            <CardContent className="flex flex-col gap-3">
              {pkg.items.length === 0 && <p className="text-sm text-muted-foreground">No tests in this package yet — add one below.</p>}

              {pkg.items.length > 0 && (
                <div className="flex flex-col divide-y divide-border rounded-md border border-border">
                  {pkg.items.map((item) => {
                    const service = servicesById.get(item.serviceId);
                    return (
                      <div key={item.id} className="flex items-center justify-between gap-3 px-4 py-3">
                        <div className="flex flex-col gap-0.5">
                          <span className="text-sm font-medium text-foreground">{service?.name ?? item.serviceId}</span>
                          <span className="text-xs text-muted-foreground">
                            {service ? `${service.serviceType} · ₹${service.price.toLocaleString('en-IN')}` : ''}
                          </span>
                        </div>
                        {canEdit && (
                          <Button
                            variant="ghost"
                            size="icon"
                            aria-label={`Remove ${service?.name ?? 'test'}`}
                            disabled={removeItemMutation.isPending}
                            onClick={() => handleRemoveItem(item.id)}
                          >
                            <X className="h-4 w-4" />
                          </Button>
                        )}
                      </div>
                    );
                  })}
                </div>
              )}

              {canEdit && (
                <div className="flex flex-wrap items-end gap-3 border-t border-dashed border-border pt-4">
                  <div className="flex min-w-[260px] flex-1 flex-col gap-1.5">
                    <span className="text-sm font-medium text-foreground">Add Test to Package</span>
                    <SearchableSelect
                      id="add-test-to-package"
                      ariaLabel="Add test to package"
                      value={selectedServiceId}
                      onValueChange={(value) => {
                        setSelectedServiceId(value);
                        setAddError(null);
                      }}
                      options={addOptions}
                      placeholder={servicesQuery.isPending ? 'Loading tests…' : 'Search tests…'}
                      searchPlaceholder="Search tests…"
                      disabled={servicesQuery.isPending}
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
      </div>

      {showEditDialog && <DiagnosticPackageFormDialog pkg={pkg} onClose={() => setShowEditDialog(false)} />}
    </div>
  );
}
