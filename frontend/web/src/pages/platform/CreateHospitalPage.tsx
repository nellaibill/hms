import { ApiError, type CreateHospitalFormValues } from '@hms/shared';
import { ArrowLeft, Building2 } from 'lucide-react';
import { Link, useNavigate } from 'react-router-dom';
import { HospitalForm, useCreateHospitalMutation } from '@/features/platformHospitals';

export default function CreateHospitalPage() {
  const navigate = useNavigate();
  const mutation = useCreateHospitalMutation();

  function handleSubmit(values: CreateHospitalFormValues) {
    mutation.mutate(values, {
      onSuccess: () => navigate('/platform/dashboard'),
    });
  }

  return (
    <div className="min-h-screen bg-muted/30">
      <div className="px-6 pt-4 lg:px-8">
        <Link to="/platform/dashboard" className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
          <ArrowLeft className="h-4 w-4" />
          Back to dashboard
        </Link>
      </div>

      <div className="mt-3 flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        <div className="flex items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
            <Building2 className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight">Register Hospital</h1>
        </div>
        <p className="text-sm text-page-banner-foreground/85">
          Provisions a new, fully isolated hospital database and its first Super Admin account.
        </p>
      </div>

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
