import { ApiError, type DiagnosticServiceFormValues } from '@hms/shared';
import { FlaskConical } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { PageBanner } from '@/components/PageBanner';
import { RequirePermission } from '@/features/auth/RequirePermission';
import { DiagnosticServiceForm, toDiagnosticServiceRequest, useCreateDiagnosticServiceMutation } from '@/features/diagnostics';

export default function DiagnosticServiceCreatePage() {
  const navigate = useNavigate();
  const mutation = useCreateDiagnosticServiceMutation();

  function handleSubmit(values: DiagnosticServiceFormValues) {
    mutation.mutate(
      toDiagnosticServiceRequest(values),
      { onSuccess: () => navigate('/diagnostics/lab/services') },
    );
  }

  return (
    <RequirePermission permission="diagnostics.create">
      <div className="flex flex-1 flex-col">
        <PageBanner
          icon={FlaskConical}
          title="New lab test or radiology service"
          subtitle="Set what the patient pays and what it costs us — the profit is worked out for you."
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
