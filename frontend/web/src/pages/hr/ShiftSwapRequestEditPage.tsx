import { ApiError, type SwapRequestFormValues } from '@hms/shared';
import { ArrowLeft, Loader2, Repeat } from 'lucide-react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { ShiftSwapRequestForm, useSwapRequestQuery, useUpdateSwapRequestMutation } from '../../features/shiftSwapRequests';

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
          approvedDate: values.approvedDate || undefined,
          approvedBy: values.approvedBy || undefined,
          remarks: values.remarks || undefined,
        },
      },
      { onSuccess: () => navigate(`/admin/hr/shift-swap-requests/${id}`) },
    );
  }

  return (
    <div className="flex flex-1 flex-col">
      <div className="px-6 pt-4 lg:px-8">
        <Link
          to={`/admin/hr/shift-swap-requests/${id}`}
          className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground"
        >
          <ArrowLeft className="h-4 w-4" />
          Back to request
        </Link>
      </div>

      <div className="mt-3 flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        <div className="flex items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
            <Repeat className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight">Edit Shift Swap Request</h1>
        </div>
        <p className="text-sm text-page-banner-foreground/85">Update this swap request's details or status.</p>
      </div>

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
            requestedDate: request.requestedDate,
            approvedDate: request.approvedDate ?? '',
            approvedBy: request.approvedBy ?? '',
            remarks: request.remarks ?? '',
          }}
          onSubmit={handleSubmit}
        />
      </div>
    </div>
  );
}
