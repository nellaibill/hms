import { ApiError, type DiagnosticPackageFormValues } from '@hms/shared';
import { PackageSearch } from 'lucide-react';
import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { PageBanner } from '@/components/PageBanner';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Label } from '@/components/ui/label';
import { SearchableSelect } from '@/components/ui/searchable-select';
import { RequirePermission } from '@/features/auth/RequirePermission';
import { DiagnosticPackageForm, formatRupees, testsRunningCostTotal, useAllDiagnosticServicesQuery, useCreateDiagnosticPackageMutation } from '@/features/diagnostics';
import { priceListPath } from '@/features/priceList/priceListRows';

/**
 * A package can't be created with zero tests (CreateDiagnosticPackageRequestValidator), so this
 * page asks for a first test up front and routes to the package's page afterward, where the
 * rest are added. The price-and-profit card follows the tariff sheet (ADR-140).
 */
export default function DiagnosticPackageCreatePage() {
  const navigate = useNavigate();
  const mutation = useCreateDiagnosticPackageMutation();
  // The whole catalog, not one page of 100 — the lab list alone is well over 100 tests.
  const labQuery = useAllDiagnosticServicesQuery('Laboratory');
  const radiologyQuery = useAllDiagnosticServicesQuery('Radiology');
  const [firstServiceId, setFirstServiceId] = useState('');
  const [firstServiceError, setFirstServiceError] = useState<string | null>(null);

  const services = useMemo(
    () => [...(labQuery.data ?? []), ...(radiologyQuery.data ?? [])].filter((service) => service.isActive).sort((a, b) => a.name.localeCompare(b.name)),
    [labQuery.data, radiologyQuery.data],
  );
  const serviceOptions = services.map((service) => ({
    value: service.id,
    label: `${service.name} — ${formatRupees(service.price)} (${service.serviceType === 'Laboratory' ? 'Lab test' : 'Radiology'})`,
    keywords: service.code,
  }));
  const firstService = services.find((service) => service.id === firstServiceId);
  const isLoading = labQuery.isPending || radiologyQuery.isPending;

  function handleSubmit(values: DiagnosticPackageFormValues) {
    if (!firstServiceId) {
      setFirstServiceError('Select at least one test to include.');
      return;
    }
    setFirstServiceError(null);
    mutation.mutate(
      {
        code: values.code,
        name: values.name,
        description: values.description || undefined,
        totalPrice: values.totalPrice,
        costPrice: values.costPrice,
        isActive: values.isActive,
        serviceIds: [firstServiceId],
      },
      { onSuccess: (created) => navigate(`/diagnostics/lab/packages/${created.id}`) },
    );
  }

  return (
    <RequirePermission permission="diagnostics.create">
      <div className="flex flex-1 flex-col">
        <PageBanner
          icon={PackageSearch}
          title="New lab package"
          subtitle="A bundle of tests at one price — set the price and the profit, as in the tariff sheet."
          backTo={priceListPath('packages')}
          backLabel="Back to Price List"
        />

        <div className="flex flex-1 flex-col gap-5 p-4 sm:p-6 lg:p-8">
          <DiagnosticPackageForm
            mode="create"
            submitLabel="Create package"
            isSubmitting={mutation.isPending}
            apiError={mutation.error instanceof ApiError ? mutation.error : null}
            testsRunningCost={firstService ? testsRunningCostTotal([firstService]) : undefined}
            onSubmit={handleSubmit}
          >
            <Card>
              <CardHeader>
                <CardTitle className="text-base">First test</CardTitle>
                <CardDescription>More tests can be added after the package is created.</CardDescription>
              </CardHeader>
              <CardContent className="flex flex-col gap-1.5">
                <Label htmlFor="dpk-first-service">Test to include</Label>
                <SearchableSelect
                  id="dpk-first-service"
                  ariaLabel="Test to include"
                  value={firstServiceId}
                  onValueChange={(value) => {
                    setFirstServiceId(value);
                    setFirstServiceError(null);
                  }}
                  options={serviceOptions}
                  placeholder={isLoading ? 'Loading tests…' : 'Select a test…'}
                  searchPlaceholder="Search tests…"
                  disabled={isLoading}
                />
                {firstServiceError && <p className="text-sm text-destructive">{firstServiceError}</p>}
              </CardContent>
            </Card>
          </DiagnosticPackageForm>
        </div>
      </div>
    </RequirePermission>
  );
}
