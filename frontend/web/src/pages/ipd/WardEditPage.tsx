import { ApiError, type WardFormValues } from '@hms/shared';
import { BedDouble, Loader2 } from 'lucide-react';
import { useNavigate, useParams } from 'react-router-dom';
import { PageBanner } from '@/components/PageBanner';
import { WardForm, useUpdateWardMutation, useWardQuery } from '../../features/ipd/wards';
import { RequirePermission } from '../../features/auth/RequirePermission';

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
    <RequirePermission permission="clinical-care.edit">
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={BedDouble}
        title={`Edit ${ward.name}`}
        subtitle="Update this ward's details."
        backTo="/clinical/ipd/wards"
        backLabel="Back to wards"
      />

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
    </RequirePermission>
  );
}
