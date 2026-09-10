import { ApiError, type StaffAvailabilityFormValues } from '@hms/shared';
import { CalendarClock } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { PageBanner } from '@/components/PageBanner';
import { StaffAvailabilityForm, useCreateStaffAvailabilityMutation } from '../../features/staffAvailability';

export default function StaffAvailabilityCreatePage() {
  const navigate = useNavigate();
  const mutation = useCreateStaffAvailabilityMutation();

  function handleSubmit(values: StaffAvailabilityFormValues) {
    mutation.mutate(
      { ...values, reason: values.reason || undefined },
      { onSuccess: (record) => navigate(`/admin/hr/staff-availability/${record.id}`) },
    );
  }

  return (
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={CalendarClock}
        title="New Availability Record"
        subtitle="Record when a staff member is available or unavailable."
        backTo="/admin/hr/staff-availability"
        backLabel="Back to staff availability"
      />

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
        <StaffAvailabilityForm
          submitLabel="Create Record"
          isSubmitting={mutation.isPending}
          apiError={mutation.error instanceof ApiError ? mutation.error : null}
          onSubmit={handleSubmit}
        />
      </div>
    </div>
  );
}
