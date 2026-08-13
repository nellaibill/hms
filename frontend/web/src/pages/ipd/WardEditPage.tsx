import { ApiError, type WardFormValues } from '@hms/shared';
import { ArrowLeft, BedDouble, Loader2 } from 'lucide-react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { WardForm, useUpdateWardMutation, useWardQuery } from '../../features/ipd/wards';

export default function WardEditPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: ward, isPending, isError } = useWardQuery(id);
  const mutation = useUpdateWardMutation();

  if (isPending) {
    return (
      <div className="flex flex-1 items-center justify-center gap-2 p-6 text-sm text-muted-foreground">
        <Loader2 className="h-4 w-4 animate-spin" />
        Loading ward…
      </div>
    );
  }

  if (isError || !ward) {
    return (
      <div className="p-6">
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          Ward not found.
        </p>
      </div>
    );
  }

  function handleSubmit(values: WardFormValues) {
    mutation.mutate(
      {
        id: id as string,
        request: {
          name: values.name,
          departmentId: values.departmentId,
          wardType: values.wardType,
          isActive: values.isActive,
        },
      },
      { onSuccess: () => navigate('/clinical/ipd/wards') },
    );
  }

  return (
    <div className="flex flex-1 flex-col">
      <div className="px-6 pt-4 lg:px-8">
        <Link to="/clinical/ipd/wards" className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
          <ArrowLeft className="h-4 w-4" />
          Back to wards
        </Link>
      </div>

      <div className="mt-3 flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        <div className="flex items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
            <BedDouble className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight">Edit {ward.name}</h1>
        </div>
        <p className="text-sm text-page-banner-foreground/85">Update this ward's details.</p>
      </div>

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
        <WardForm
          mode="edit"
          submitLabel="Save Changes"
          isSubmitting={mutation.isPending}
          apiError={mutation.error instanceof ApiError ? mutation.error : null}
          defaultValues={{
            code: ward.code,
            name: ward.name,
            departmentId: ward.departmentId,
            wardType: ward.wardType,
            isActive: ward.isActive,
          }}
          onSubmit={handleSubmit}
        />
      </div>
    </div>
  );
}
