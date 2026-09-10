import { ApiError, type ShiftAssignmentFormValues } from '@hms/shared';
import { CalendarCheck2, Loader2 } from 'lucide-react';
import { useNavigate, useParams } from 'react-router-dom';
import { PageBanner } from '@/components/PageBanner';
import { ShiftAssignmentForm, useShiftAssignmentQuery, useUpdateShiftAssignmentMutation } from '../../features/shiftAssignments';

export default function ShiftAssignmentEditPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: assignment, isPending, isError } = useShiftAssignmentQuery(id);
  const mutation = useUpdateShiftAssignmentMutation();

  if (isPending) {
    return (
      <div className="flex flex-1 items-center justify-center gap-2 p-6 text-sm text-muted-foreground">
        <Loader2 className="h-4 w-4 animate-spin" />
        Loading shift assignment…
      </div>
    );
  }

  if (isError || !assignment) {
    return (
      <div className="p-6">
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          Shift assignment not found.
        </p>
      </div>
    );
  }

  function handleSubmit(values: ShiftAssignmentFormValues) {
    mutation.mutate(
      { id: id as string, request: { ...values, remarks: values.remarks || undefined } },
      { onSuccess: () => navigate(`/admin/hr/shift-assignments/${id}`) },
    );
  }

  return (
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={CalendarCheck2}
        title="Edit Shift Assignment"
        subtitle="Update this staff assignment."
        backTo={`/admin/hr/shift-assignments/${id}`}
        backLabel="Back to assignment"
      />

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
        <ShiftAssignmentForm
          submitLabel="Save Changes"
          isSubmitting={mutation.isPending}
          apiError={mutation.error instanceof ApiError ? mutation.error : null}
          defaultValues={{
            staffId: assignment.staffId,
            departmentId: assignment.departmentId,
            shiftId: assignment.shiftId,
            rosterDate: assignment.rosterDate,
            status: assignment.status,
            remarks: assignment.remarks ?? '',
          }}
          onSubmit={handleSubmit}
        />
      </div>
    </div>
  );
}
