import { Loader2, Pencil, ShieldCheck } from 'lucide-react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { PageBanner } from '@/components/PageBanner';
import { Button } from '@/components/ui/button';
import { useAuth } from '../../features/auth/AuthContext';
import { RequirePermission } from '../../features/auth/RequirePermission';
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
  const { hasPermission } = useAuth();
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
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={ShieldCheck}
        title={heading}
        subtitle={subtitle}
        backTo={backTo}
        backLabel="Back to roles"
        rightActions={
          mode === 'view' &&
          hasPermission('identity-administration.edit') && (
            <Button
              asChild
              variant="outline"
              className="gap-1.5 border-page-banner-foreground/30 bg-page-banner-foreground/10 text-page-banner-foreground hover:bg-page-banner-foreground/20"
            >
              <Link to={`/admin/roles/${id}/edit`}>
                <Pencil className="h-4 w-4" />
                Edit
              </Link>
            </Button>
          )
        }
      />

      <div className="flex flex-1 flex-col gap-4 p-6 lg:p-8">
      {mode === 'view' ? (
        <RoleForm
          mode={mode}
          defaultValues={toFormValues(role!)}
          isSubmitting={false}
          onSubmit={handleSubmit}
          onCancel={() => navigate(backTo)}
        />
      ) : (
        <RequirePermission permission={isNew ? 'identity-administration.create' : 'identity-administration.edit'}>
          <RoleForm
            mode={mode}
            defaultValues={isNew ? emptyDefaults : toFormValues(role!)}
            isSubmitting={createMutation.isPending || updateMutation.isPending}
            onSubmit={handleSubmit}
            onCancel={() => navigate(backTo)}
          />
        </RequirePermission>
      )}
      </div>
    </div>
  );
}
