import { ApiError, type DiagnosticServiceFormValues } from '@hms/shared';
import { FlaskConical } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { PageBanner } from '@/components/PageBanner';
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
        costPrice: values.costPrice,
        isActive: values.isActive,
      },
      { onSuccess: () => navigate('/diagnostics/lab/services') },
    );
  }

  return (
    <RequirePermission permission="diagnostics.create">
      <div className="flex flex-1 flex-col">
        <PageBanner
          icon={FlaskConical}
          title="New Service"
          subtitle="Add a new test to the Laboratory/Radiology service catalog."
          backTo="/diagnostics/lab/services"
          backLabel="Back to services"
        />

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
