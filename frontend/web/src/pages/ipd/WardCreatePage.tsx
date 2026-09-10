import { ApiError, type WardFormValues } from '@hms/shared';
import { BedDouble } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { PageBanner } from '@/components/PageBanner';
import { WardForm, useCreateWardMutation } from '../../features/ipd/wards';
import { RequirePermission } from '../../features/auth/RequirePermission';

export default function WardCreatePage() {
  const navigate = useNavigate();
  const mutation = useCreateWardMutation();

  function handleSubmit(values: WardFormValues) {
    mutation.mutate(values, {
      onSuccess: () => navigate('/clinical/ipd/wards'),
    });
  }

  return (
    <RequirePermission permission="clinical-care.create">
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={BedDouble}
        title="New Ward"
        subtitle="Create a new hospital ward."
        backTo="/clinical/ipd/wards"
        backLabel="Back to wards"
      />

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
        <WardForm
          mode="create"
          submitLabel="Create Ward"
          isSubmitting={mutation.isPending}
          apiError={mutation.error instanceof ApiError ? mutation.error : null}
          onSubmit={handleSubmit}
        />
      </div>
    </div>
    </RequirePermission>
  );
}
