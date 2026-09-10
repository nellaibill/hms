import { ApiError, type BedFormValues } from '@hms/shared';
import { BedSingle } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { PageBanner } from '@/components/PageBanner';
import { BedForm, useCreateBedMutation } from '../../features/ipd/beds';
import { RequirePermission } from '../../features/auth/RequirePermission';

export default function BedCreatePage() {
  const navigate = useNavigate();
  const mutation = useCreateBedMutation();

  function handleSubmit(values: BedFormValues) {
    mutation.mutate(values, {
      onSuccess: () => navigate('/clinical/ipd/beds'),
    });
  }

  return (
    <RequirePermission permission="clinical-care.create">
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={BedSingle}
        title="New Bed"
        subtitle="Add a new bed to a ward."
        backTo="/clinical/ipd/beds"
        backLabel="Back to beds"
      />

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
        <BedForm
          mode="create"
          submitLabel="Create Bed"
          isSubmitting={mutation.isPending}
          apiError={mutation.error instanceof ApiError ? mutation.error : null}
          onSubmit={handleSubmit}
        />
      </div>
    </div>
    </RequirePermission>
  );
}
