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
        username: values.username,
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
    <div className="flex flex-1 flex-col">
      <div className="px-6 pt-4 lg:px-8">
        <Link to="/users" className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
          <ArrowLeft className="h-4 w-4" />
          Back to users
        </Link>
      </div>

      {/* Centered, brand-colored banner — matches the Page banner style used
          across module pages (Theme & Branding → Section headers). */}
      <div className="mt-3 flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        <div className="flex items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
            <UserPlus className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight">New User</h1>
        </div>
        <p className="text-sm text-page-banner-foreground/85">Create a new system account.</p>
      </div>

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
      <UserForm
        submitLabel="Create User"
        isSubmitting={mutation.isPending}
        apiError={mutation.error instanceof ApiError ? mutation.error : null}
        onSubmit={handleSubmit}
      />
      </div>
    </div>
  );
}
