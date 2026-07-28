import { ApiError, type UserProfileFormValues } from '@hms/shared';
import { ArrowLeft, Loader2, UserCog } from 'lucide-react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { UserForm, useUpdateUserMutation, useUserQuery } from '../../features/users';

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
    <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
      <div>
        <Link to={`/users/${id}`} className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
          <ArrowLeft className="h-4 w-4" />
          Back to user
        </Link>
        <div className="mt-3 flex items-start gap-3 border-b border-border pb-4">
          <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-md bg-primary/10 text-primary">
            <UserCog className="h-5 w-5" />
          </span>
          <div>
            <h1 className="text-xl font-semibold tracking-tight text-foreground">
              Edit {user.firstName} {user.lastName}
            </h1>
            <p className="mt-1 text-sm text-muted-foreground">Update this user's profile details.</p>
          </div>
        </div>
      </div>

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
    </div>
  );
}
