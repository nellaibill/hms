import { ApiError } from '@hms/shared';
import { UserPlus } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { PageBanner } from '@/components/PageBanner';
import { UserCreateForm, type UserCreateFormSubmitValues, useCreateUserMutation } from '../../features/users';
import { RequirePermission } from '../../features/auth/RequirePermission';

export default function UserCreatePage() {
  const navigate = useNavigate();
  const mutation = useCreateUserMutation();

  function handleSubmit(values: UserCreateFormSubmitValues) {
    mutation.mutate(
      {
        username: values.username,
        firstName: values.firstName,
        lastName: values.lastName,
        email: values.email,
        phoneNumber: values.phoneNumber,
        roleId: values.roleId,
        consultantId: values.consultantId,
      },
      {
        onSuccess: (user) => navigate(`/users/${user.id}`),
      },
    );
  }

  return (
    <RequirePermission permission="identity-administration.create">
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={UserPlus}
        title="New User"
        subtitle="Create a new system account."
        backTo="/users"
        backLabel="Back to users"
      />

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
        <UserCreateForm
          isSubmitting={mutation.isPending}
          apiError={mutation.error instanceof ApiError ? mutation.error : null}
          onSubmit={handleSubmit}
          onCancel={() => navigate('/users')}
        />
      </div>
    </div>
    </RequirePermission>
  );
}
