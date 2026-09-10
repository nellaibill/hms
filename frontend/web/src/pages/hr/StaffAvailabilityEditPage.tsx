import { ApiError, type StaffAvailabilityFormValues } from '@hms/shared';
import { CalendarClock, Loader2 } from 'lucide-react';
import { useNavigate, useParams } from 'react-router-dom';
import { PageBanner } from '@/components/PageBanner';
import { StaffAvailabilityForm, useStaffAvailabilityRecordQuery, useUpdateStaffAvailabilityMutation } from '../../features/staffAvailability';

export default function StaffAvailabilityEditPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: record, isPending, isError } = useStaffAvailabilityRecordQuery(id);
  const mutation = useUpdateStaffAvailabilityMutation();

  if (isPending) {
    return (
      <div className="flex flex-1 items-center justify-center gap-2 p-6 text-sm text-muted-foreground">
        <Loader2 className="h-4 w-4 animate-spin" />
        Loading availability record…
      </div>
    );
  }

  if (isError || !record) {
    return (
      <div className="p-6">
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          Availability record not found.
        </p>
      </div>
    );
  }

  function handleSubmit(values: StaffAvailabilityFormValues) {
    mutation.mutate(
      { id: id as string, request: { ...values, reason: values.reason || undefined } },
      { onSuccess: () => navigate(`/admin/hr/staff-availability/${id}`) },
    );
  }

  return (
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={CalendarClock}
        title="Edit Availability Record"
        subtitle="Update this staff member's availability window."
        backTo={`/admin/hr/staff-availability/${id}`}
        backLabel="Back to record"
      />

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
        <StaffAvailabilityForm
          submitLabel="Save Changes"
          isSubmitting={mutation.isPending}
          apiError={mutation.error instanceof ApiError ? mutation.error : null}
          defaultValues={{
            staffId: record.staffId,
            startDate: record.startDate,
            endDate: record.endDate,
            availabilityStatus: record.availabilityStatus,
            reason: record.reason ?? '',
          }}
          onSubmit={handleSubmit}
        />
      </div>
    </div>
  );
}
