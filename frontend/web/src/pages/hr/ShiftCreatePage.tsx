import { ApiError, type ShiftFormValues } from '@hms/shared';
import { Clock } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { PageBanner } from '@/components/PageBanner';
import { ShiftForm, useCreateShiftMutation } from '../../features/shifts';

export default function ShiftCreatePage() {
  const navigate = useNavigate();
  const mutation = useCreateShiftMutation();

  function handleSubmit(values: ShiftFormValues) {
    mutation.mutate(values, {
      onSuccess: (shift) => navigate(`/admin/hr/shifts/${shift.id}`),
    });
  }

  return (
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={Clock}
        title="New Shift"
        subtitle="Create a new shift definition."
        backTo="/admin/hr/shifts"
        backLabel="Back to shifts"
      />

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
        <ShiftForm
          mode="create"
          submitLabel="Create Shift"
          isSubmitting={mutation.isPending}
          apiError={mutation.error instanceof ApiError ? mutation.error : null}
          onSubmit={handleSubmit}
        />
      </div>
    </div>
  );
}
