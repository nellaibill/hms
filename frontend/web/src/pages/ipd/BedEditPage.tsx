import { ApiError, type BedFormValues } from '@hms/shared';
import { ArrowLeft, BedSingle, Loader2 } from 'lucide-react';
import { Link, useNavigate, useParams } from 'react-router-dom';
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
      <div className="px-6 pt-4 lg:px-8">
        <Link to="/clinical/ipd/beds" className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
          <ArrowLeft className="h-4 w-4" />
          Back to beds
        </Link>
      </div>

      <div className="mt-3 flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        <div className="flex items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
            <BedSingle className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight">Edit Bed {bed.bedNumber}</h1>
        </div>
        <p className="text-sm text-page-banner-foreground/85">Update this bed's type, status, and active state.</p>
      </div>

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
