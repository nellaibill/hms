import { ApiError, type DiagnosticServiceFormValues } from '@hms/shared';
import { Loader2, Settings2, Trash2 } from 'lucide-react';
import { useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { PageBanner } from '@/components/PageBanner';
import { Button } from '@/components/ui/button';
import { useAuth } from '@/features/auth/AuthContext';
import { RequirePermission } from '@/features/auth/RequirePermission';
import {
  ConfirmDeleteDialog,
  DiagnosticServiceForm,
  toDiagnosticServiceFormValues,
  toDiagnosticServiceRequest,
  useDiagnosticServiceQuery,
  usePrimeDiagnosticPackageCache,
  useDeleteDiagnosticServiceMutation,
  useUpdateDiagnosticServiceMutation,
  type PackageUsingService,
} from '@/features/diagnostics';
import { priceListPath, priceListTabFor } from '@/features/priceList/priceListRows';

export default function DiagnosticServiceEditPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: service, isPending, isError } = useDiagnosticServiceQuery(id);
  const packages = usePrimeDiagnosticPackageCache();
  const mutation = useUpdateDiagnosticServiceMutation();
  const deleteMutation = useDeleteDiagnosticServiceMutation();
  const { hasPermission } = useAuth();
  const [confirmingDelete, setConfirmingDelete] = useState(false);

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

  const backTo = priceListPath(priceListTabFor(service));

  function handleSubmit(values: DiagnosticServiceFormValues) {
    mutation.mutate({ id: id as string, request: toDiagnosticServiceRequest(values) }, { onSuccess: () => navigate(priceListPath(priceListTabFor(values))) });
  }

  function handleDelete() {
    deleteMutation.mutate(id as string, { onSuccess: () => navigate(backTo) });
  }

  const kind = service.serviceType === 'Radiology' ? 'Radiology' : service.isOutsourced ? 'Lab test · outsourced' : 'Lab test';

  return (
    <RequirePermission permission="diagnostics.edit">
      <div className="flex flex-1 flex-col">
        <PageBanner
          icon={Settings2}
          title={service.name}
          subtitle={`${kind} — update its price and costs.`}
          backTo={backTo}
          backLabel="Back to Price List"
          rightActions={
            hasPermission('diagnostics.delete') ? (
              <Button variant="outline" size="sm" className="gap-1.5" onClick={() => setConfirmingDelete(true)}>
                <Trash2 className="h-4 w-4" />
                Delete
              </Button>
            ) : undefined
          }
        />

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

        {confirmingDelete && (
          <ConfirmDeleteDialog
            title={`Delete ${service.name}?`}
            isPending={deleteMutation.isPending}
            errorMessage={deleteMutation.error instanceof Error ? deleteMutation.error.message : null}
            onConfirm={handleDelete}
            onCancel={() => setConfirmingDelete(false)}
          >
            It's removed from the Price List and from billing, and old bills that used it lose its name. To stop billing it but keep it on
            record, switch its Status to Inactive instead.
          </ConfirmDeleteDialog>
        )}
      </div>
    </RequirePermission>
  );
}
