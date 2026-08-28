import { ApiError, type DiagnosticServiceFormValues } from '@hms/shared';
import { ArrowLeft, FlaskConical } from 'lucide-react';
import { Link, useNavigate } from 'react-router-dom';
import { RequirePermission } from '@/features/auth/RequirePermission';
import { DiagnosticServiceForm, useCreateDiagnosticServiceMutation } from '@/features/diagnostics';

export default function DiagnosticServiceCreatePage() {
  const navigate = useNavigate();
  const mutation = useCreateDiagnosticServiceMutation();

  function handleSubmit(values: DiagnosticServiceFormValues) {
    mutation.mutate(
      {
        code: values.code,
        name: values.name,
        categoryId: values.categoryId,
        serviceType: values.serviceType,
        isOutsourced: values.isOutsourced,
        providerId: values.isOutsourced ? values.providerId || undefined : undefined,
        price: values.price,
        isActive: values.isActive,
      },
      { onSuccess: () => navigate('/diagnostics/lab/services') },
    );
  }

  return (
    <RequirePermission permission="diagnostics.create">
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
              <FlaskConical className="h-5 w-5" />
            </span>
            <h1 className="text-xl font-semibold tracking-tight">New Service</h1>
          </div>
          <p className="text-sm text-page-banner-foreground/85">Add a new test to the Laboratory/Radiology service catalog.</p>
        </div>

        <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
          <DiagnosticServiceForm
            mode="create"
            submitLabel="Create Service"
            isSubmitting={mutation.isPending}
            apiError={mutation.error instanceof ApiError ? mutation.error : null}
            onSubmit={handleSubmit}
          />
        </div>
      </div>
    </RequirePermission>
  );
}
