import { ApiError, type ShiftAssignmentFormValues } from '@hms/shared';
import { CalendarCheck2 } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { PageBanner } from '@/components/PageBanner';
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
      <PageBanner
        icon={CalendarCheck2}
        title="New Shift Assignment"
        subtitle="Assign a staff member to a shift on a specific date."
        backTo="/admin/hr/shift-assignments"
        backLabel="Back to shift assignments"
      />

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
