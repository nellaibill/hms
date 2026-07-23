import { ApiError, type UserProfileFormValues } from '@hms/shared';
import { useNavigate } from 'react-router-dom';
import { UserForm, useCreateUserMutation } from '../../features/users';

export default function UserCreatePage() {
  const navigate = useNavigate();
  const mutation = useCreateUserMutation();

  function handleSubmit(values: UserProfileFormValues) {
    mutation.mutate(
      {
        firstName: values.firstName,
        lastName: values.lastName,
        email: values.email,
        phoneNumber: values.phoneNumber || undefined,
      },
      {
        onSuccess: (user) => navigate(`/users/${user.id}`),
      },
    );
  }

  return (
    <section>
      <h1>New User</h1>
      <UserForm
        submitLabel="Create User"
        isSubmitting={mutation.isPending}
        apiError={mutation.error instanceof ApiError ? mutation.error : null}
        onSubmit={handleSubmit}
      />
    </section>
  );
}
