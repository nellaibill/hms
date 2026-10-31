import { ApiError } from '@hms/shared';
import { ExternalLink, KeyRound, Loader2 } from 'lucide-react';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { Label } from '@/components/ui/label';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { useToast } from '@/components/ui/toast-context';
import { useAuth } from '@/features/auth/AuthContext';
import { useRolesForSelect } from '../hooks/useRolesForSelect';
import {
  useActivateUserMutation,
  useDeactivateUserMutation,
  useSetPasswordMutation,
  useUpdateUserMutation,
} from '../hooks/useUserMutations';
import { useUserQuery } from '../hooks/useUserQuery';
import { SetPasswordDialog } from './SetPasswordDialog';
import { StatusBadge } from './StatusBadge';

interface LinkedLoginDetailsProps {
  userId: string;
}

/**
 * The Login access card once a login exists (ADR-108): the few things staff actually change
 * on a colleague's login — role, password, on/off — without leaving the Employee or
 * Consultant page. Each action saves straight away through the same Users API (and
 * permissions) as Settings → Users; anything rarer is one click away there.
 */
export function LinkedLoginDetails({ userId }: LinkedLoginDetailsProps) {
  const { hasPermission } = useAuth();
  const canEdit = hasPermission('identity-administration.edit');
  const { toast } = useToast();
  const { data: user, isPending, isError } = useUserQuery(userId);
  const { data: roles } = useRolesForSelect();
  const updateMutation = useUpdateUserMutation();
  const setPasswordMutation = useSetPasswordMutation();
  const activateMutation = useActivateUserMutation();
  const deactivateMutation = useDeactivateUserMutation();
  const [pendingRoleId, setPendingRoleId] = useState<string | null>(null);
  const [isSettingPassword, setIsSettingPassword] = useState(false);

  if (isPending) {
    return (
      <p className="flex items-center gap-2 text-sm text-muted-foreground">
        <Loader2 className="h-4 w-4 animate-spin" />
        Loading login…
      </p>
    );
  }

  if (isError || !user) {
    return (
      <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
        The linked login couldn't be loaded. It may have been deleted from Settings → Users.
      </p>
    );
  }

  const roleId = pendingRoleId ?? user.roleId;
  const toggleMutation = user.isActive ? deactivateMutation : activateMutation;

  function handleUpdateRole() {
    if (!user || !pendingRoleId) return;
    updateMutation.mutate(
      {
        id: user.id,
        request: {
          username: user.username,
          firstName: user.firstName,
          lastName: user.lastName,
          email: user.email,
          phoneNumber: user.phoneNumber,
          roleId: pendingRoleId,
          consultantId: user.consultantId,
        },
      },
      {
        onSuccess: () => {
          setPendingRoleId(null);
          toast({ title: 'Role updated', variant: 'success' });
        },
        onError: (error) =>
          toast({ title: "Role wasn't updated", description: error.message, variant: 'error' }),
      },
    );
  }

  function handleToggleActive() {
    if (!user) return;
    toggleMutation.mutate(user.id, {
      onSuccess: (updated) =>
        toast({
          title: updated.isActive ? 'Login activated' : 'Login deactivated',
          variant: 'success',
        }),
      onError: (error) =>
        toast({ title: "Login wasn't changed", description: error.message, variant: 'error' }),
    });
  }

  return (
    <div className="flex flex-col gap-4">
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <div className="flex flex-col gap-1">
          <span className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
            Username
          </span>
          <span className="font-mono text-sm text-foreground">{user.username}</span>
        </div>
        <div className="flex flex-col gap-1">
          <span className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
            Status
          </span>
          <span>
            <StatusBadge isActive={user.isActive} />
          </span>
        </div>
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="linkedLoginRole">Role</Label>
          {canEdit ? (
            <div className="flex gap-2">
              <Select
                value={roleId}
                onValueChange={(value) => setPendingRoleId(value === user.roleId ? null : value)}
              >
                <SelectTrigger id="linkedLoginRole" aria-label="Login role">
                  <SelectValue placeholder={user.roleName} />
                </SelectTrigger>
                <SelectContent>
                  {roles?.items.map((role) => (
                    <SelectItem key={role.id} value={role.id}>
                      {role.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              {pendingRoleId && (
                <Button
                  type="button"
                  size="sm"
                  className="h-10 shrink-0"
                  onClick={handleUpdateRole}
                  disabled={updateMutation.isPending}
                >
                  {updateMutation.isPending ? 'Updating…' : 'Update role'}
                </Button>
              )}
            </div>
          ) : (
            <span className="text-sm text-foreground">{user.roleName}</span>
          )}
        </div>
        <div className="flex flex-col gap-1">
          <span className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
            Last login
          </span>
          <span className="text-sm text-foreground">
            {user.lastLoginAt ? new Date(user.lastLoginAt).toLocaleString('en-IN') : 'Never'}
          </span>
        </div>
      </div>

      <div className="flex flex-wrap items-center gap-2">
        {canEdit && (
          <>
            <Button
              type="button"
              variant="outline"
              size="sm"
              className="gap-1.5"
              onClick={() => setIsSettingPassword(true)}
            >
              <KeyRound className="h-4 w-4" />
              Reset password
            </Button>
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={handleToggleActive}
              disabled={toggleMutation.isPending}
            >
              {user.isActive ? 'Deactivate login' : 'Activate login'}
            </Button>
          </>
        )}
        <Button asChild variant="ghost" size="sm" className="gap-1.5">
          <Link to={`/users/${user.id}`}>
            <ExternalLink className="h-4 w-4" />
            Open in Users
          </Link>
        </Button>
      </div>

      {canEdit && (
        <p className="text-xs text-muted-foreground">
          Changes to the login save straight away, separately from the rest of this page.
        </p>
      )}

      {/* This card sits inside the Employee/Consultant <form>, and React bubbles a portalled
          dialog's submit up the component tree — stop it here so saving a password never
          also submits the outer form. */}
      {isSettingPassword && (
        <div onSubmit={(event) => event.stopPropagation()}>
          <SetPasswordDialog
            user={user}
            isSubmitting={setPasswordMutation.isPending}
            apiError={
              setPasswordMutation.error instanceof ApiError ? setPasswordMutation.error : null
            }
            onSubmit={(password) =>
              setPasswordMutation.mutate(
                { id: user.id, request: { password } },
                {
                  onSuccess: () => {
                    setIsSettingPassword(false);
                    toast({
                      title: 'Password reset',
                      description: 'They must change it the next time they log in.',
                      variant: 'success',
                    });
                  },
                },
              )
            }
            onCancel={() => {
              setIsSettingPassword(false);
              setPasswordMutation.reset();
            }}
          />
        </div>
      )}
    </div>
  );
}
