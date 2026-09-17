import { ApiError, userProfileSchema, type User, type UserProfileFormValues } from '@hms/shared';
import { zodResolver } from '@hookform/resolvers/zod';
import { useQuery } from '@tanstack/react-query';
import { useEffect, useState } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { Trash2 } from 'lucide-react';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { DepartmentName } from '@/components/DepartmentName';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { SearchableSelect } from '@/components/ui/searchable-select';
import { Switch } from '@/components/ui/switch';
import { StatusBadge } from './StatusBadge';
import { env } from '@/config/env';
import { consultantsApi } from '@/services/apiClient';
import { useRolesForSelect } from '../hooks/useRolesForSelect';
import { useActivateUserMutation, useDeactivateUserMutation } from '../hooks/useUserMutations';

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

// A role name containing "consultant" (e.g. "Doctor / Consultant") is treated as a
// clinical role that needs a consultant mapping — freeform Role names have no dedicated
// "is clinical" flag today, so this mirrors the substring check that's the closest
// available signal, same spirit as LoginTypes.cs's own name-based role matching.
function isConsultantRoleName(name: string | undefined) {
  return Boolean(name?.toLowerCase().includes('consultant'));
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

  // Deliberately not scoped to a department (unlike ConsultantSelect elsewhere) — the
  // consultant mapping here is independent of any clinical workflow, so any active
  // consultant in the hospital should be pickable.
  const { data: consultantsData } = useQuery({
    queryKey: ['consultants', 'select-list', 'all-departments'],
    queryFn: () => consultantsApi.getConsultants({ pageSize: 100, isActive: true }),
    enabled: linkToConsultant,
  });

  const selectedConsultant = consultantsData?.items.find((c) => c.id === consultantId);
  const consultantOptions = (consultantsData?.items ?? []).map((c) => ({
    value: c.id,
    label: c.specialization ? `${c.name} — ${c.specialization}` : c.name,
  }));

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
            <CardTitle>Basic Information</CardTitle>
            <CardDescription>Personal and contact details for this user.</CardDescription>
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
              <CardTitle>Role &amp; Access</CardTitle>
              <CardDescription>Assign a role to this user.</CardDescription>
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

          <Card>
            <CardHeader>
              <CardTitle>Consultant Profile <span className="font-normal text-muted-foreground">(Optional)</span></CardTitle>
              <CardDescription>Link this user to a consultant/doctor. This can be set for any role.</CardDescription>
            </CardHeader>
            <CardContent className="flex flex-col gap-3">
              <div className="flex items-start gap-3">
                <Switch
                  id="linkConsultant"
                  checked={linkToConsultant}
                  onCheckedChange={handleToggleConsultant}
                  aria-label="Link this user to a consultant"
                />
                <div className="flex flex-col gap-0.5">
                  <Label htmlFor="linkConsultant" className="cursor-pointer">
                    Link this user to a consultant
                  </Label>
                  <p className="text-xs text-muted-foreground">Enable this if the user is a doctor/consultant in the hospital.</p>
                </div>
              </div>

              {linkToConsultant && (
                <div className="flex flex-col gap-1.5">
                  <Label htmlFor="consultantId">Consultant</Label>
                  <SearchableSelect
                    id="consultantId"
                    value={consultantId}
                    onValueChange={(value) => {
                      setConsultantId(value);
                      setConsultantError(null);
                    }}
                    options={consultantOptions}
                    placeholder="Select consultant…"
                    searchPlaceholder="Search by name…"
                    ariaLabel="Consultant"
                  />

                  {selectedConsultant && (
                    <div className="mt-1 flex flex-col gap-1 rounded-md bg-muted/50 px-3 py-2 text-xs text-muted-foreground">
                      <span>
                        <strong className="text-foreground">Specialization:</strong> {selectedConsultant.specialization ?? '—'}
                      </span>
                      <span>
                        <strong className="text-foreground">Department:</strong>{' '}
                        {selectedConsultant.departmentId ? <DepartmentName departmentId={selectedConsultant.departmentId} /> : '—'}
                      </span>
                    </div>
                  )}
                </div>
              )}

              {consultantError && <p className="text-sm text-destructive">{consultantError}</p>}
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Account Settings</CardTitle>
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
