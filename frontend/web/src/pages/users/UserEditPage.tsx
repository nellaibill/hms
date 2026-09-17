import { ApiError } from '@hms/shared';
import { Loader2, UserCog } from 'lucide-react';
import { useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { PageBanner } from '@/components/PageBanner';
import {
  DeleteUserDialog,
  UploadProfilePhotoDialog,
  UserEditForm,
  type UserEditFormSubmitValues,
  useDeleteUserMutation,
  useUpdateUserMutation,
  useUploadProfilePhotoMutation,
  useUserQuery,
} from '../../features/users';
import { RequirePermission } from '../../features/auth/RequirePermission';

export default function UserEditPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: user, isPending, isError } = useUserQuery(id);
  const mutation = useUpdateUserMutation();
  const uploadPhotoMutation = useUploadProfilePhotoMutation();
  const deleteMutation = useDeleteUserMutation();
  const [isUploadingPhoto, setIsUploadingPhoto] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);

  if (isPending) {
    return (
      <div className="flex flex-1 items-center justify-center gap-2 p-6 text-sm text-muted-foreground">
        <Loader2 className="h-4 w-4 animate-spin" />
        Loading user…
      </div>
    );
  }

  if (isError || !user) {
    return (
      <div className="p-6">
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          User not found.
        </p>
      </div>
    );
  }

  function handleSubmit(values: UserEditFormSubmitValues) {
    mutation.mutate(
      {
        id: id as string,
        request: {
          username: values.username,
          firstName: values.firstName,
          lastName: values.lastName,
          email: values.email,
          phoneNumber: values.phoneNumber,
          roleId: values.roleId,
          consultantId: values.consultantId,
        },
      },
      {
        onSuccess: () => navigate(`/users/${id}`),
      },
    );
  }

  function handleUploadPhoto(file: File) {
    if (!id) return;
    uploadPhotoMutation.mutate({ id, file }, { onSuccess: () => setIsUploadingPhoto(false) });
  }

  function handleDelete() {
    if (!id) return;
    deleteMutation.mutate(id, { onSuccess: () => navigate('/users') });
  }

  return (
    <RequirePermission permission="identity-administration.edit">
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={UserCog}
        title="Edit User"
        subtitle="Update user information, role, and consultant mapping."
        backTo={`/users/${id}`}
        backLabel="Back to user"
      />

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
        <UserEditForm
          user={user}
          isSubmitting={mutation.isPending}
          apiError={mutation.error instanceof ApiError ? mutation.error : null}
          onSubmit={handleSubmit}
          onUploadPhoto={() => setIsUploadingPhoto(true)}
          onDelete={() => setIsDeleting(true)}
          onCancel={() => navigate(`/users/${id}`)}
        />
      </div>

      {isUploadingPhoto && (
        <UploadProfilePhotoDialog
          user={user}
          isSubmitting={uploadPhotoMutation.isPending}
          apiError={uploadPhotoMutation.error instanceof ApiError ? uploadPhotoMutation.error : null}
          onSubmit={handleUploadPhoto}
          onCancel={() => {
            uploadPhotoMutation.reset();
            setIsUploadingPhoto(false);
          }}
        />
      )}

      {isDeleting && (
        <DeleteUserDialog
          user={user}
          isDeleting={deleteMutation.isPending}
          onConfirm={handleDelete}
          onCancel={() => {
            deleteMutation.reset();
            setIsDeleting(false);
          }}
        />
      )}
    </div>
    </RequirePermission>
  );
}
