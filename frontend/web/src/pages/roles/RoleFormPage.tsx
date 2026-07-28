import { ArrowLeft, Loader2, Pencil, ShieldCheck } from 'lucide-react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import {
  buildEmptyPermissions,
  RoleForm,
  useCreateRoleMutation,
  useRoleQuery,
  useUpdateRoleMutation,
  type Role,
  type RoleFormValues,
} from '../../features/roles';

interface RoleFormPageProps {
  mode: 'create' | 'edit' | 'view';
}

function toFormValues(role: Role): RoleFormValues {
  return {
    name: role.name,
    description: role.description,
    status: role.status,
    permissions: role.permissions,
  };
}

const emptyDefaults: RoleFormValues = {
  name: '',
  description: '',
  status: 'Active',
  permissions: buildEmptyPermissions(),
};

export default function RoleFormPage({ mode }: RoleFormPageProps) {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const isNew = mode === 'create';

  const { data: role, isPending, isError } = useRoleQuery(isNew ? undefined : id);
  const createMutation = useCreateRoleMutation();
  const updateMutation = useUpdateRoleMutation();

  if (!isNew && isPending) {
    return (
      <div className="flex flex-1 items-center justify-center gap-2 p-6 text-sm text-muted-foreground">
        <Loader2 className="h-4 w-4 animate-spin" />
        Loading role…
      </div>
    );
  }

  if (!isNew && (isError || !role)) {
    return (
      <div className="p-6">
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          Role not found.
        </p>
      </div>
    );
  }

  function handleSubmit(values: RoleFormValues) {
    if (isNew) {
      createMutation.mutate(values, { onSuccess: (created) => navigate(`/admin/roles/${created.id}`) });
    } else {
      updateMutation.mutate({ id: id as string, values }, { onSuccess: () => navigate(`/admin/roles/${id}`) });
    }
  }

  const heading = isNew ? 'New Role' : mode === 'view' ? role!.name : `Edit ${role!.name}`;
  const subtitle = isNew
    ? 'Define a role name, status, and its module-level permissions.'
    : mode === 'view'
      ? 'Role details and assigned permissions.'
      : "Update this role's details and permissions.";
  const backTo = isNew || mode === 'view' ? '/admin/roles' : `/admin/roles/${id}`;

  return (
    <div className="flex flex-1 flex-col gap-4 p-6 lg:p-8">
      <div>
        <Link to={backTo} className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
          <ArrowLeft className="h-4 w-4" />
          Back to roles
        </Link>
        <div className="mt-2 flex items-start justify-between gap-3 border-b border-border pb-3">
          <div className="flex items-start gap-3">
            <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-primary/10 text-primary">
              <ShieldCheck className="h-5 w-5" />
            </span>
            <div>
              <h1 className="text-xl font-semibold tracking-tight text-foreground">{heading}</h1>
              <p className="mt-1 text-sm text-muted-foreground">{subtitle}</p>
            </div>
          </div>
          {mode === 'view' && (
            <Button asChild variant="outline" className="gap-1.5">
              <Link to={`/admin/roles/${id}/edit`}>
                <Pencil className="h-4 w-4" />
                Edit
              </Link>
            </Button>
          )}
        </div>
      </div>

      <RoleForm
        mode={mode}
        defaultValues={isNew ? emptyDefaults : toFormValues(role!)}
        isSubmitting={createMutation.isPending || updateMutation.isPending}
        onSubmit={handleSubmit}
        onCancel={() => navigate(backTo)}
      />
    </div>
  );
}
