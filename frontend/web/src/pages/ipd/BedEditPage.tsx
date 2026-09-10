import { ApiError, type BedFormValues } from '@hms/shared';
import { BedSingle, Loader2 } from 'lucide-react';
import { useNavigate, useParams } from 'react-router-dom';
import { PageBanner } from '@/components/PageBanner';
import { BedForm, useBedQuery, useUpdateBedMutation } from '../../features/ipd/beds';
import { RequirePermission } from '../../features/auth/RequirePermission';

export default function BedEditPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: bed, isPending, isError } = useBedQuery(id);
  const mutation = useUpdateBedMutation();

  if (isPending) {
    return (
      <div className="flex flex-1 items-center justify-center gap-2 p-6 text-sm text-muted-foreground">
        <Loader2 className="h-4 w-4 animate-spin" />
        Loading bed…
      </div>
    );
  }

  if (isError || !bed) {
    return (
      <div className="p-6">
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          Bed not found.
        </p>
      </div>
    );
  }

  function handleSubmit(values: BedFormValues) {
    mutation.mutate(
      {
        id: id as string,
        request: {
          bedType: values.bedType,
          status: values.status,
          isActive: values.isActive,
          dailyCharge: values.dailyCharge,
        },
      },
      { onSuccess: () => navigate('/clinical/ipd/beds') },
    );
  }

  return (
    <RequirePermission permission="clinical-care.edit">
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={BedSingle}
        title={`Edit Bed ${bed.bedNumber}`}
        subtitle="Update this bed's type, status, and active state."
        backTo="/clinical/ipd/beds"
        backLabel="Back to beds"
      />

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
        <BedForm
          mode="edit"
          submitLabel="Save Changes"
          isSubmitting={mutation.isPending}
          apiError={mutation.error instanceof ApiError ? mutation.error : null}
          defaultValues={{
            wardId: bed.wardId,
            bedNumber: bed.bedNumber,
            bedType: bed.bedType,
            status: bed.status,
            isActive: bed.isActive,
            dailyCharge: bed.dailyCharge,
          }}
          onSubmit={handleSubmit}
        />
      </div>
    </div>
    </RequirePermission>
  );
}
