import { ApiError, type UserProfileFormValues } from '@hms/shared';
import { ArrowLeft, Loader2, UserCog } from 'lucide-react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { UserForm, useUpdateUserMutation, useUserQuery } from '../../features/users';
import { RequirePermission } from '../../features/auth/RequirePermission';

export default function UserEditPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: user, isPending, isError } = useUserQuery(id);
  const mutation = useUpdateUserMutation();

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

  function handleSubmit(values: UserProfileFormValues) {
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
        },
      },
      {
        onSuccess: () => navigate(`/users/${id}`),
      },
    );
  }

  return (
    <RequirePermission permission="identity-administration.edit">
    <div className="flex flex-1 flex-col">
      <div className="px-6 pt-4 lg:px-8">
        <Link to={`/users/${id}`} className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
          <ArrowLeft className="h-4 w-4" />
          Back to user
        </Link>
      </div>

      {/* Centered, brand-colored banner — matches the Page banner style used
          across module pages (Theme & Branding → Section headers). */}
      <div className="mt-3 flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        <div className="flex items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
            <UserCog className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight">
            Edit {user.firstName} {user.lastName}
          </h1>
        </div>
        <p className="text-sm text-page-banner-foreground/85">Update this user's profile details.</p>
      </div>

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
      <UserForm
        submitLabel="Save Changes"
        isSubmitting={mutation.isPending}
        apiError={mutation.error instanceof ApiError ? mutation.error : null}
        defaultValues={{
          username: user.username,
          firstName: user.firstName,
          lastName: user.lastName,
          email: user.email,
          phoneNumber: user.phoneNumber ?? '',
          roleId: user.roleId,
        }}
        onSubmit={handleSubmit}
      />
      </div>
    </div>
    </RequirePermission>
  );
}
