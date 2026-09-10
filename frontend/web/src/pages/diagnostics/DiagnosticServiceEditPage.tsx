import { ApiError, type DiagnosticServiceFormValues } from '@hms/shared';
import { Loader2, Settings2 } from 'lucide-react';
import { useNavigate, useParams } from 'react-router-dom';
import { PageBanner } from '@/components/PageBanner';
import { RequirePermission } from '@/features/auth/RequirePermission';
import { DiagnosticServiceForm, useDiagnosticServiceQuery, useUpdateDiagnosticServiceMutation } from '@/features/diagnostics';

export default function DiagnosticServiceEditPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: service, isPending, isError } = useDiagnosticServiceQuery(id);
  const mutation = useUpdateDiagnosticServiceMutation();

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
    mutation.mutate(
      {
        id: id as string,
        request: {
          code: values.code,
          name: values.name,
          categoryId: values.categoryId,
          serviceType: values.serviceType,
          isOutsourced: values.isOutsourced,
          providerId: values.isOutsourced ? values.providerId || undefined : undefined,
          price: values.price,
          costPrice: values.costPrice,
          isActive: values.isActive,
        },
      },
      { onSuccess: () => navigate('/diagnostics/lab/services') },
    );
  }

  return (
    <RequirePermission permission="diagnostics.edit">
      <div className="flex flex-1 flex-col">
        <PageBanner
          icon={Settings2}
          title={`Edit ${service.name}`}
          subtitle="Update this service's details."
          backTo="/diagnostics/lab/services"
          backLabel="Back to services"
        />

        <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
          <DiagnosticServiceForm
            mode="edit"
            submitLabel="Save Changes"
            isSubmitting={mutation.isPending}
            apiError={mutation.error instanceof ApiError ? mutation.error : null}
            defaultValues={{
              code: service.code,
              name: service.name,
              categoryId: service.categoryId,
              serviceType: service.serviceType,
              isOutsourced: service.isOutsourced,
              providerId: service.providerId ?? '',
              price: service.price,
              costPrice: service.costPrice,
              isActive: service.isActive,
            }}
            onSubmit={handleSubmit}
          />
        </div>
      </div>
    </RequirePermission>
  );
}
