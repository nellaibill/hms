import { ApiError, userProfileSchema, type User, type UserProfileFormValues } from '@hms/shared';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect, useState } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { Settings, ShieldCheck, Trash2, UserRound } from 'lucide-react';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader } from '@/components/ui/card';
import { CardHeading } from './CardHeading';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Switch } from '@/components/ui/switch';
import { ConsultantProfileCard } from './ConsultantProfileCard';
import { StatusBadge } from './StatusBadge';
import { env } from '@/config/env';
import { useRolesForSelect } from '../hooks/useRolesForSelect';
import { useActivateUserMutation, useDeactivateUserMutation } from '../hooks/useUserMutations';
import { isConsultantRoleName } from '@/lib/isConsultantRoleName';

export interface UserEditFormSubmitValues extends UserProfileFormValues {
  consultantId: string | null;
}

interface UserEditFormProps {
  user: User;
  onSubmit: (values: UserEditFormSubmitValues) => void;
  isSubmitting: boolean;
  apiError: ApiError | null;
  onUploadPhoto: () => void;
  onDelete: () => void;
  onCancel: () => void;
}

function initialsOf(firstName: string, lastName: string) {
  return `${firstName[0] ?? ''}${lastName[0] ?? ''}`.toUpperCase();
}

export function UserEditForm({ user, onSubmit, isSubmitting, apiError, onUploadPhoto, onDelete, onCancel }: UserEditFormProps) {
  const { data: roles } = useRolesForSelect();
  const activateMutation = useActivateUserMutation();
  const deactivateMutation = useDeactivateUserMutation();

  const [linkToConsultant, setLinkToConsultant] = useState(Boolean(user.consultantId));
  const [consultantId, setConsultantId] = useState(user.consultantId ?? '');
  const [consultantError, setConsultantError] = useState<string | null>(null);

  const {
    register,
    control,
    handleSubmit,
    watch,
    setError,
    formState: { errors },
  } = useForm<UserProfileFormValues>({
    resolver: zodResolver(userProfileSchema),
    defaultValues: {
      username: user.username,
      firstName: user.firstName,
      lastName: user.lastName,
      email: user.email,
      phoneNumber: user.phoneNumber ?? '',
      roleId: user.roleId,
    },
  });

  // Server-side validation failures (docs/ApiStandards.md §5) are mapped onto the same
  // field-level display client validation uses, per docs/FrontendArchitecture.md §9.
  useEffect(() => {
    if (!apiError?.validationErrors) {
      return;
    }

    for (const issue of apiError.validationErrors) {
      const fieldName = (issue.field.charAt(0).toLowerCase() + issue.field.slice(1)) as keyof UserProfileFormValues;
      setError(fieldName, { type: 'server', message: issue.message });
    }
  }, [apiError, setError]);

  const generalError = apiError && !apiError.validationErrors ? apiError.message : null;

  const selectedRoleId = watch('roleId');
  const selectedRole = roles?.items.find((role) => role.id === selectedRoleId);
  const consultantMappingRequired = isConsultantRoleName(selectedRole?.name);

  function handleToggleConsultant(checked: boolean) {
    setLinkToConsultant(checked);
    setConsultantError(null);
    if (!checked) {
      setConsultantId('');
    }
  }

  function handleFormSubmit(values: UserProfileFormValues) {
    if (linkToConsultant && !consultantId) {
      setConsultantError('Select a consultant, or turn the link off.');
      return;
    }
    if (consultantMappingRequired && !linkToConsultant) {
      setConsultantError('This role requires a linked consultant before you can save.');
      return;
    }

    setConsultantError(null);
    onSubmit({ ...values, consultantId: linkToConsultant ? consultantId : null });
  }

  function handleActiveToggle(checked: boolean) {
    if (checked) {
      activateMutation.mutate(user.id);
    } else {
      deactivateMutation.mutate(user.id);
    }
  }

  return (
    <form onSubmit={handleSubmit(handleFormSubmit)} noValidate className="flex flex-col gap-6">
      {generalError && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {generalError}
        </p>
      )}

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
        {/* Left column */}
        <Card>
          <CardHeader>
            <CardHeading icon={UserRound} title="Basic Information" description="Personal and contact details for this user." />
          </CardHeader>
          <CardContent className="flex flex-col gap-4">
            <div className="flex items-center gap-4">
              <Avatar className="h-16 w-16">
                <AvatarImage src={user.profilePhotoUrl ? `${env.apiBaseUrl}/${user.profilePhotoUrl}` : undefined} alt="" />
                <AvatarFallback className="text-base">{initialsOf(user.firstName, user.lastName)}</AvatarFallback>
              </Avatar>
              <div className="flex flex-col gap-1">
                <Button type="button" variant="outline" size="sm" onClick={onUploadPhoto}>
                  Change Photo
                </Button>
                <p className="text-xs text-muted-foreground">JPG, PNG up to 2MB</p>
              </div>
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="username">Username</Label>
              <Input id="username" autoComplete="username" {...register('username')} />
              {errors.username && <p className="text-sm text-destructive">{errors.username.message}</p>}
            </div>

            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="firstName">First name</Label>
                <Input id="firstName" autoComplete="given-name" {...register('firstName')} />
                {errors.firstName && <p className="text-sm text-destructive">{errors.firstName.message}</p>}
              </div>
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="lastName">Last name</Label>
                <Input id="lastName" autoComplete="family-name" {...register('lastName')} />
                {errors.lastName && <p className="text-sm text-destructive">{errors.lastName.message}</p>}
              </div>
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="email">Email</Label>
              <Input id="email" type="email" autoComplete="email" {...register('email')} />
              {errors.email && <p className="text-sm text-destructive">{errors.email.message}</p>}
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="phoneNumber">Phone number</Label>
              <Input id="phoneNumber" autoComplete="tel" {...register('phoneNumber')} />
              {errors.phoneNumber && <p className="text-sm text-destructive">{errors.phoneNumber.message}</p>}
            </div>
          </CardContent>
        </Card>

        {/* Right column */}
        <div className="flex flex-col gap-6">
          <Card>
            <CardHeader>
              <CardHeading icon={ShieldCheck} title="Role & Access" description="Assign a role to this user." />
            </CardHeader>
            <CardContent className="flex flex-col gap-3">
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="roleId">Primary Role</Label>
                <Controller
                  control={control}
                  name="roleId"
                  render={({ field }) => (
                    <Select value={field.value} onValueChange={field.onChange}>
                      <SelectTrigger id="roleId" aria-label="Role">
                        <SelectValue placeholder="Select a role" />
                      </SelectTrigger>
                      <SelectContent>
                        {roles?.items.map((role) => (
                          <SelectItem key={role.id} value={role.id}>
                            {role.name}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  )}
                />
                {errors.roleId && <p className="text-sm text-destructive">{errors.roleId.message}</p>}
              </div>
              <p className="rounded-md bg-muted/50 px-3 py-2 text-xs text-muted-foreground">
                Roles control what this user can do in the system. A user can have an administrative role (e.g., Super Admin, Admin) and also
                be linked to a consultant.
              </p>
            </CardContent>
          </Card>

          <ConsultantProfileCard
            linkToConsultant={linkToConsultant}
            consultantId={consultantId}
            onToggle={handleToggleConsultant}
            onChangeConsultant={(value) => {
              setConsultantId(value);
              setConsultantError(null);
            }}
            error={consultantError}
          />

          <Card>
            <CardHeader>
              <CardHeading icon={Settings} title="Account Settings" />
            </CardHeader>
            <CardContent className="flex flex-col gap-4">
              <div className="flex items-center justify-between">
                <div className="flex items-start gap-3">
                  <Switch
                    id="isActive"
                    checked={user.isActive}
                    onCheckedChange={handleActiveToggle}
                    disabled={activateMutation.isPending || deactivateMutation.isPending}
                    aria-label="Active"
                  />
                  <div className="flex flex-col gap-0.5">
                    <Label htmlFor="isActive" className="cursor-pointer">
                      Active
                    </Label>
                    <p className="text-xs text-muted-foreground">Inactive users cannot log in to the system.</p>
                  </div>
                </div>
                <StatusBadge isActive={user.isActive} />
              </div>

              <div className="grid grid-cols-1 gap-3 border-t border-border pt-3 text-xs text-muted-foreground sm:grid-cols-2">
                <div className="flex flex-col gap-0.5">
                  <span className="font-medium text-foreground">Created On</span>
                  <span>{new Date(user.createdAt).toLocaleString('en-IN')}</span>
                </div>
                <div className="flex flex-col gap-0.5">
                  <span className="font-medium text-foreground">Last Login</span>
                  <span>{user.lastLoginAt ? new Date(user.lastLoginAt).toLocaleString('en-IN') : 'Never'}</span>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>

      <div className="flex items-center justify-between border-t border-border pt-4">
        <Button type="button" variant="destructive" className="gap-1.5" onClick={onDelete}>
          <Trash2 className="h-4 w-4" />
          Delete User
        </Button>
        <div className="flex items-center gap-2">
          <Button type="button" variant="outline" onClick={onCancel} disabled={isSubmitting}>
            Cancel
          </Button>
          <Button type="submit" disabled={isSubmitting}>
            {isSubmitting ? 'Saving…' : 'Save Changes'}
          </Button>
        </div>
      </div>
    </form>
  );
}
