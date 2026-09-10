import { ApiError, type SwapRequestFormValues } from '@hms/shared';
import { Loader2, Repeat } from 'lucide-react';
import { useNavigate, useParams } from 'react-router-dom';
import { PageBanner } from '@/components/PageBanner';
import {
  ShiftSwapRequestForm,
  toDateTimeLocalInput,
  toUtcIso,
  useSwapRequestQuery,
  useUpdateSwapRequestMutation,
} from '../../features/shiftSwapRequests';

export default function ShiftSwapRequestEditPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: request, isPending, isError } = useSwapRequestQuery(id);
  const mutation = useUpdateSwapRequestMutation();

  if (isPending) {
    return (
      <div className="flex flex-1 items-center justify-center gap-2 p-6 text-sm text-muted-foreground">
        <Loader2 className="h-4 w-4 animate-spin" />
        Loading shift swap request…
      </div>
    );
  }

  if (isError || !request) {
    return (
      <div className="p-6">
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          Shift swap request not found.
        </p>
      </div>
    );
  }

  function handleSubmit(values: SwapRequestFormValues) {
    mutation.mutate(
      {
        id: id as string,
        request: {
          ...values,
          requestedDate: toUtcIso(values.requestedDate),
          approvedDate: values.approvedDate ? toUtcIso(values.approvedDate) : undefined,
          approvedBy: values.approvedBy || undefined,
          remarks: values.remarks || undefined,
        },
      },
      { onSuccess: () => navigate(`/admin/hr/shift-swap-requests/${id}`) },
    );
  }

  return (
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={Repeat}
        title="Edit Shift Swap Request"
        subtitle="Update this swap request's details or status."
        backTo={`/admin/hr/shift-swap-requests/${id}`}
        backLabel="Back to request"
      />

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
        <ShiftSwapRequestForm
          submitLabel="Save Changes"
          isSubmitting={mutation.isPending}
          apiError={mutation.error instanceof ApiError ? mutation.error : null}
          defaultValues={{
            requestedByStaffId: request.requestedByStaffId,
            requestedToStaffId: request.requestedToStaffId,
            currentShiftAssignmentId: request.currentShiftAssignmentId,
            requestedShiftAssignmentId: request.requestedShiftAssignmentId,
            status: request.status,
            requestedDate: toDateTimeLocalInput(request.requestedDate),
            approvedDate: request.approvedDate ? toDateTimeLocalInput(request.approvedDate) : '',
            approvedBy: request.approvedBy ?? '',
            remarks: request.remarks ?? '',
          }}
          onSubmit={handleSubmit}
        />
      </div>
    </div>
  );
}
