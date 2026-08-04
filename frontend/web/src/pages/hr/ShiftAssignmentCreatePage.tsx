import { ApiError, type ShiftAssignmentFormValues } from '@hms/shared';
import { ArrowLeft, CalendarCheck2 } from 'lucide-react';
import { Link, useNavigate } from 'react-router-dom';
import { ShiftAssignmentForm, useCreateShiftAssignmentMutation } from '../../features/shiftAssignments';

export default function ShiftAssignmentCreatePage() {
  const navigate = useNavigate();
  const mutation = useCreateShiftAssignmentMutation();

  function handleSubmit(values: ShiftAssignmentFormValues) {
    mutation.mutate(
      { ...values, remarks: values.remarks || undefined },
      { onSuccess: (assignment) => navigate(`/admin/hr/shift-assignments/${assignment.id}`) },
    );
  }

  return (
    <div className="flex flex-1 flex-col">
      <div className="px-6 pt-4 lg:px-8">
        <Link to="/admin/hr/shift-assignments" className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
          <ArrowLeft className="h-4 w-4" />
          Back to shift assignments
        </Link>
      </div>

      <div className="mt-3 flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        <div className="flex items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
            <CalendarCheck2 className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight">New Shift Assignment</h1>
        </div>
        <p className="text-sm text-page-banner-foreground/85">Assign a staff member to a shift on a specific date.</p>
      </div>

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
        <ShiftAssignmentForm
          submitLabel="Create Assignment"
          isSubmitting={mutation.isPending}
          apiError={mutation.error instanceof ApiError ? mutation.error : null}
          onSubmit={handleSubmit}
        />
      </div>
    </div>
  );
}
