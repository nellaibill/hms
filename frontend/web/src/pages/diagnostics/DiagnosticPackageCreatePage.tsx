import { ApiError, type DiagnosticPackageFormValues } from '@hms/shared';
import { ArrowLeft, PackageSearch } from 'lucide-react';
import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Label } from '@/components/ui/label';
import { SearchableSelect } from '@/components/ui/searchable-select';
import { RequirePermission } from '@/features/auth/RequirePermission';
import { DiagnosticPackageForm, useCreateDiagnosticPackageMutation, useDiagnosticServicesQuery } from '@/features/diagnostics';

/**
 * Judgment call: the mockup's own flow (and the task spec) has the create screen collect only
 * the package's own fields (Code/Name/Description/Total Price/Active), with tests added
 * afterward on the package's detail page. But CreateDiagnosticPackageRequestValidator rejects
 * an empty ServiceIds array ("A package must contain at least one test") — a package can't
 * actually be created with zero tests. This page reconciles the two by asking for exactly one
 * "first test" up front (required, separate from DiagnosticPackageForm's own fields/schema so
 * DiagnosticPackageForm stays reusable/unmodified for Edit, which has no such concept) and
 * routing straight to the detail page afterward, where "Add Test to Package" covers every test
 * after the first — matching the spec's intent as closely as the backend contract allows.
 */
export default function DiagnosticPackageCreatePage() {
  const navigate = useNavigate();
  const mutation = useCreateDiagnosticPackageMutation();
  const servicesQuery = useDiagnosticServicesQuery({ isActive: true, pageSize: 200, sort: 'name' });
  const [firstServiceId, setFirstServiceId] = useState('');
  const [firstServiceError, setFirstServiceError] = useState<string | null>(null);

  const serviceOptions = (servicesQuery.data?.items ?? []).map((service) => ({
    value: service.id,
    label: `${service.name} — ₹${service.price.toLocaleString('en-IN')} (${service.serviceType})`,
    keywords: service.code,
  }));

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
        isActive: values.isActive,
        serviceIds: [firstServiceId],
      },
      { onSuccess: (created) => navigate(`/diagnostics/lab/packages/${created.id}`) },
    );
  }

  return (
    <RequirePermission permission="diagnostics.create">
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
            <h1 className="text-xl font-semibold tracking-tight">New Package</h1>
          </div>
          <p className="text-sm text-page-banner-foreground/85">Add a new bundled test package.</p>
        </div>

        <div className="flex flex-1 flex-col gap-5 p-6 lg:p-8">
          <div className="mx-auto w-full max-w-3xl">
            <Card>
              <CardHeader>
                <CardTitle className="text-base">First Test</CardTitle>
              </CardHeader>
              <CardContent className="flex flex-col gap-1.5">
                <Label htmlFor="dpk-first-service">Test to include (more can be added after the package is created)</Label>
                <SearchableSelect
                  id="dpk-first-service"
                  ariaLabel="Test to include"
                  value={firstServiceId}
                  onValueChange={(value) => {
                    setFirstServiceId(value);
                    setFirstServiceError(null);
                  }}
                  options={serviceOptions}
                  placeholder={servicesQuery.isPending ? 'Loading tests…' : 'Select a test…'}
                  searchPlaceholder="Search tests…"
                  disabled={servicesQuery.isPending}
                />
                {firstServiceError && <p className="text-sm text-destructive">{firstServiceError}</p>}
              </CardContent>
            </Card>
          </div>

          <DiagnosticPackageForm
            mode="create"
            submitLabel="Create Package"
            isSubmitting={mutation.isPending}
            apiError={mutation.error instanceof ApiError ? mutation.error : null}
            onSubmit={handleSubmit}
          />
        </div>
      </div>
    </RequirePermission>
  );
}
