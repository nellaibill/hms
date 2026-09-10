import { ApiError, type SwapRequestFormValues } from '@hms/shared';
import { Repeat } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { PageBanner } from '@/components/PageBanner';
import { ShiftSwapRequestForm, toUtcIso, useCreateSwapRequestMutation } from '../../features/shiftSwapRequests';

export default function ShiftSwapRequestCreatePage() {
  const navigate = useNavigate();
  const mutation = useCreateSwapRequestMutation();

  function handleSubmit(values: SwapRequestFormValues) {
    mutation.mutate(
      {
        ...values,
        requestedDate: toUtcIso(values.requestedDate),
        approvedDate: values.approvedDate ? toUtcIso(values.approvedDate) : undefined,
        approvedBy: values.approvedBy || undefined,
        remarks: values.remarks || undefined,
      },
      { onSuccess: (request) => navigate(`/admin/hr/shift-swap-requests/${request.id}`) },
    );
  }

  return (
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={Repeat}
        title="New Shift Swap Request"
        subtitle="Request a swap between two shift assignments."
        backTo="/admin/hr/shift-swap-requests"
        backLabel="Back to shift swap requests"
      />

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
