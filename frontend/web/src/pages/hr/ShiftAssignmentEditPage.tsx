import { ApiError, type ShiftAssignmentFormValues } from '@hms/shared';
import { ArrowLeft, CalendarCheck2, Loader2 } from 'lucide-react';
import { Link, useNavigate, useParams } from 'react-router-dom';
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
      <div className="px-6 pt-4 lg:px-8">
        <Link to={`/admin/hr/shift-assignments/${id}`} className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
          <ArrowLeft className="h-4 w-4" />
          Back to assignment
        </Link>
      </div>

      <div className="mt-3 flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        <div className="flex items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
            <CalendarCheck2 className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight">Edit Shift Assignment</h1>
        </div>
        <p className="text-sm text-page-banner-foreground/85">Update this staff assignment.</p>
      </div>

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
