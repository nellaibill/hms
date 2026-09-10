import { ApiError, type ShiftFormValues } from '@hms/shared';
import { Clock, Loader2 } from 'lucide-react';
import { useNavigate, useParams } from 'react-router-dom';
import { PageBanner } from '@/components/PageBanner';
import { ShiftForm, useShiftQuery, useUpdateShiftMutation } from '../../features/shifts';

export default function ShiftEditPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: shift, isPending, isError } = useShiftQuery(id);
  const mutation = useUpdateShiftMutation();

  if (isPending) {
    return (
      <div className="flex flex-1 items-center justify-center gap-2 p-6 text-sm text-muted-foreground">
        <Loader2 className="h-4 w-4 animate-spin" />
        Loading shift…
      </div>
    );
  }

  if (isError || !shift) {
    return (
      <div className="p-6">
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          Shift not found.
        </p>
      </div>
    );
  }

  function handleSubmit(values: ShiftFormValues) {
    mutation.mutate(
      {
        id: id as string,
        request: {
          name: values.name,
          startTime: values.startTime,
          endTime: values.endTime,
          breakMinutes: values.breakMinutes,
          graceMinutes: values.graceMinutes,
          isNightShift: values.isNightShift,
          isActive: values.isActive,
        },
      },
      { onSuccess: () => navigate(`/admin/hr/shifts/${id}`) },
    );
  }

  return (
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={Clock}
        title={`Edit ${shift.name}`}
        subtitle="Update this shift's timing and settings."
        backTo={`/admin/hr/shifts/${id}`}
        backLabel="Back to shift"
      />

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
        <ShiftForm
          mode="edit"
          submitLabel="Save Changes"
          isSubmitting={mutation.isPending}
          apiError={mutation.error instanceof ApiError ? mutation.error : null}
          defaultValues={{
            code: shift.code,
            name: shift.name,
            startTime: shift.startTime,
            endTime: shift.endTime,
            breakMinutes: shift.breakMinutes,
            graceMinutes: shift.graceMinutes,
            isNightShift: shift.isNightShift,
            isActive: shift.isActive,
          }}
          onSubmit={handleSubmit}
        />
      </div>
    </div>
  );
}
