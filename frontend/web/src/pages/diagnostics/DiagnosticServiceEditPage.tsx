import { ApiError, type DiagnosticServiceFormValues } from '@hms/shared';
import { ArrowLeft, Loader2, Settings2 } from 'lucide-react';
import { Link, useNavigate, useParams } from 'react-router-dom';
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
          isActive: values.isActive,
        },
      },
      { onSuccess: () => navigate('/diagnostics/lab/services') },
    );
  }

  return (
    <RequirePermission permission="diagnostics.edit">
      <div className="flex flex-1 flex-col">
        <div className="px-6 pt-4 lg:px-8">
          <Link to="/diagnostics/lab/services" className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
            <ArrowLeft className="h-4 w-4" />
            Back to services
          </Link>
        </div>

        <div className="mt-3 flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
          <div className="flex items-center gap-3">
            <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
              <Settings2 className="h-5 w-5" />
            </span>
            <h1 className="text-xl font-semibold tracking-tight">Edit {service.name}</h1>
          </div>
          <p className="text-sm text-page-banner-foreground/85">Update this service's details.</p>
        </div>

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
              isActive: service.isActive,
            }}
            onSubmit={handleSubmit}
          />
        </div>
      </div>
    </RequirePermission>
  );
}
