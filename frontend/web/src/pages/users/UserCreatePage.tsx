import { ApiError, type UserProfileFormValues } from '@hms/shared';
import { ArrowLeft, UserPlus } from 'lucide-react';
import { Link, useNavigate } from 'react-router-dom';
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
    <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
      <div>
        <Link to="/users" className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
          <ArrowLeft className="h-4 w-4" />
          Back to users
        </Link>
        <div className="mt-3 flex items-start gap-3 border-b border-border pb-4">
          <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-md bg-primary/10 text-primary">
            <UserPlus className="h-5 w-5" />
          </span>
          <div>
            <h1 className="text-xl font-semibold tracking-tight text-foreground">New User</h1>
            <p className="mt-1 text-sm text-muted-foreground">Create a new system account.</p>
          </div>
        </div>
      </div>

      <UserForm
        submitLabel="Create User"
        isSubmitting={mutation.isPending}
        apiError={mutation.error instanceof ApiError ? mutation.error : null}
        onSubmit={handleSubmit}
      />
    </div>
  );
}
