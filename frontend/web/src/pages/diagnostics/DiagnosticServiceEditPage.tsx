import { ApiError, type DiagnosticServiceFormValues } from '@hms/shared';
import { Loader2, Settings2 } from 'lucide-react';
import { useMemo } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { PageBanner } from '@/components/PageBanner';
import { RequirePermission } from '@/features/auth/RequirePermission';
import {
  DiagnosticServiceForm,
  toDiagnosticServiceFormValues,
  toDiagnosticServiceRequest,
  useDiagnosticServiceQuery,
  usePrimeDiagnosticPackageCache,
  useUpdateDiagnosticServiceMutation,
  type PackageUsingService,
} from '@/features/diagnostics';

export default function DiagnosticServiceEditPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: service, isPending, isError } = useDiagnosticServiceQuery(id);
  const packages = usePrimeDiagnosticPackageCache();
  const mutation = useUpdateDiagnosticServiceMutation();

  // ADR-140: a lab test's edit screen lists the packages it's part of.
  const packagesUsingThis = useMemo<PackageUsingService[] | undefined>(() => {
    if (!service || service.serviceType !== 'Laboratory' || !packages) return undefined;
    return packages
      .filter((pkg) => pkg.isActive && pkg.items.some((item) => item.serviceId === service.id))
      .map((pkg) => ({ id: pkg.id, name: pkg.name, totalPrice: pkg.totalPrice, costPrice: pkg.costPrice }))
      .sort((a, b) => a.name.localeCompare(b.name));
  }, [packages, service]);

  if (isPending) {
    return (
      <div className="flex flex-1 items-center justify-center gap-2 p-6 text-sm text-muted-foreground">
        <Loader2 className="h-4 w-4 animate-spin" />
        Loading service…
      </div>
    );
  }

  if (isError || !service) {
    return (
      <div className="p-6">
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          Service not found.
        </p>
      </div>
    );
  }

  function handleSubmit(values: DiagnosticServiceFormValues) {
    mutation.mutate({ id: id as string, request: toDiagnosticServiceRequest(values) }, { onSuccess: () => navigate('/diagnostics/lab/services') });
  }

  const kind = service.serviceType === 'Radiology' ? 'Radiology' : service.isOutsourced ? 'Lab test · outsourced' : 'Lab test';

  return (
    <RequirePermission permission="diagnostics.edit">
      <div className="flex flex-1 flex-col">
        <PageBanner icon={Settings2} title={service.name} subtitle={`${kind} — update its price and costs.`} backTo="/diagnostics/lab/services" backLabel="Back to services" />

        <div className="flex flex-1 flex-col gap-6 p-4 sm:p-6 lg:p-8">
          <DiagnosticServiceForm
            mode="edit"
            submitLabel="Save changes"
            isSubmitting={mutation.isPending}
            apiError={mutation.error instanceof ApiError ? mutation.error : null}
            defaultValues={toDiagnosticServiceFormValues(service)}
            packagesUsingThis={packagesUsingThis}
            onSubmit={handleSubmit}
          />
        </div>
      </div>
    </RequirePermission>
  );
}
