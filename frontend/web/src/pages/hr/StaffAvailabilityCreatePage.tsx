import { ApiError, type StaffAvailabilityFormValues } from '@hms/shared';
import { ArrowLeft, CalendarClock } from 'lucide-react';
import { Link, useNavigate } from 'react-router-dom';
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
      <div className="px-6 pt-4 lg:px-8">
        <Link to="/admin/hr/staff-availability" className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
          <ArrowLeft className="h-4 w-4" />
          Back to staff availability
        </Link>
      </div>

      <div className="mt-3 flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        <div className="flex items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
            <CalendarClock className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight">New Availability Record</h1>
        </div>
        <p className="text-sm text-page-banner-foreground/85">Record when a staff member is available or unavailable.</p>
      </div>

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
