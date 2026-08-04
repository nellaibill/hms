import { ApiError, type SwapRequestFormValues } from '@hms/shared';
import { ArrowLeft, Repeat } from 'lucide-react';
import { Link, useNavigate } from 'react-router-dom';
import { ShiftSwapRequestForm, useCreateSwapRequestMutation } from '../../features/shiftSwapRequests';

export default function ShiftSwapRequestCreatePage() {
  const navigate = useNavigate();
  const mutation = useCreateSwapRequestMutation();

  function handleSubmit(values: SwapRequestFormValues) {
    mutation.mutate(
      {
        ...values,
        approvedDate: values.approvedDate || undefined,
        approvedBy: values.approvedBy || undefined,
        remarks: values.remarks || undefined,
      },
      { onSuccess: (request) => navigate(`/admin/hr/shift-swap-requests/${request.id}`) },
    );
  }

  return (
    <div className="flex flex-1 flex-col">
      <div className="px-6 pt-4 lg:px-8">
        <Link to="/admin/hr/shift-swap-requests" className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
          <ArrowLeft className="h-4 w-4" />
          Back to shift swap requests
        </Link>
      </div>

      <div className="mt-3 flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        <div className="flex items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
            <Repeat className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight">New Shift Swap Request</h1>
        </div>
        <p className="text-sm text-page-banner-foreground/85">Request a swap between two shift assignments.</p>
      </div>

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
        <ShiftSwapRequestForm
          submitLabel="Create Request"
          isSubmitting={mutation.isPending}
          apiError={mutation.error instanceof ApiError ? mutation.error : null}
          onSubmit={handleSubmit}
        />
      </div>
    </div>
  );
}
