import { ApiError, type UserProfileFormValues } from '@hms/shared';
import { useNavigate, useParams } from 'react-router-dom';
import { UserForm, useUpdateUserMutation, useUserQuery } from '../../features/users';

export default function UserEditPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: user, isPending, isError } = useUserQuery(id);
  const mutation = useUpdateUserMutation();

  if (isPending) {
    return <p>Loading user…</p>;
  }

  if (isError || !user) {
    return <p className="form-error-banner">User not found.</p>;
  }

  function handleSubmit(values: UserProfileFormValues) {
    mutation.mutate(
      {
        id: id as string,
        request: {
          firstName: values.firstName,
          lastName: values.lastName,
          email: values.email,
          phoneNumber: values.phoneNumber || undefined,
        },
      },
      {
        onSuccess: () => navigate(`/users/${id}`),
      },
    );
  }

  return (
    <section>
      <h1>Edit User</h1>
      <UserForm
        submitLabel="Save Changes"
        isSubmitting={mutation.isPending}
        apiError={mutation.error instanceof ApiError ? mutation.error : null}
        defaultValues={{
          firstName: user.firstName,
          lastName: user.lastName,
          email: user.email,
          phoneNumber: user.phoneNumber ?? '',
        }}
        onSubmit={handleSubmit}
      />
    </section>
  );
}
