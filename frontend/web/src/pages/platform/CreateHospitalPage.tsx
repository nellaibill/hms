import { ApiError, type CreateHospitalFormValues } from '@hms/shared';
import { Building2 } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { PageBanner } from '@/components/PageBanner';
import { HospitalForm, useCreateHospitalMutation } from '@/features/platformHospitals';

export default function CreateHospitalPage() {
  const navigate = useNavigate();
  const mutation = useCreateHospitalMutation();

  function handleSubmit({ superAdminConfirmPassword, ...request }: CreateHospitalFormValues) {
    // superAdminConfirmPassword only exists to validate that the password was typed
    // correctly — it's not part of CreateHospitalRequest and must not be sent to the API.
    void superAdminConfirmPassword;
    mutation.mutate(request, {
      onSuccess: () => navigate('/platform/dashboard'),
    });
  }

  return (
    <div className="min-h-screen bg-muted/30">
      <PageBanner
        icon={Building2}
        title="Register Hospital"
        subtitle="Provisions a new, fully isolated hospital database and its first Super Admin account."
        backTo="/platform/dashboard"
        backLabel="Back to dashboard"
      />

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
        <HospitalForm
          submitLabel="Create Hospital"
          isSubmitting={mutation.isPending}
          apiError={mutation.error instanceof ApiError ? mutation.error : null}
          onSubmit={handleSubmit}
        />
      </div>
    </div>
  );
}
