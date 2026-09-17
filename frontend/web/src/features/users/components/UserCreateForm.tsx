import { ApiError, userProfileSchema, type UserProfileFormValues } from '@hms/shared';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect, useState } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { Info, Settings, ShieldCheck, UserRound } from 'lucide-react';
import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader } from '@/components/ui/card';
import { CardHeading } from './CardHeading';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { ConsultantProfileCard } from './ConsultantProfileCard';
import { useRolesForSelect } from '../hooks/useRolesForSelect';
import { isConsultantRoleName } from '../utils/isConsultantRoleName';

export interface UserCreateFormSubmitValues extends UserProfileFormValues {
  consultantId: string | null;
}

interface UserCreateFormProps {
  onSubmit: (values: UserCreateFormSubmitValues) => void;
  isSubmitting: boolean;
  apiError: ApiError | null;
  onCancel: () => void;
}

function initialsOf(firstName: string, lastName: string) {
  return `${firstName[0] ?? ''}${lastName[0] ?? ''}`.toUpperCase() || '?';
}

/** Same card layout as UserEditForm (Basic Information / Role & Access / Consultant
 * Profile / Account Settings), minus the fields that only make sense once a user exists:
 * no photo upload (UsersController.UploadProfilePhoto is a later, separate step — never
 * part of creation), no Active toggle or Created/Last Login (a new account is always
 * active from User.Create, and hasn't logged in yet), no Delete. Kept as a distinct
 * component from UserEditForm rather than one form with an isEdit flag, since the two
 * diverge on what's editable, not just on styling. */
export function UserCreateForm({ onSubmit, isSubmitting, apiError, onCancel }: UserCreateFormProps) {
  const { data: roles } = useRolesForSelect();

  const [linkToConsultant, setLinkToConsultant] = useState(false);
  const [consultantId, setConsultantId] = useState('');
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
      username: '',
      firstName: '',
      lastName: '',
      email: '',
      phoneNumber: '',
      roleId: '',
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

  const firstName = watch('firstName');
  const lastName = watch('lastName');
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
                <AvatarFallback className="text-base">{initialsOf(firstName, lastName)}</AvatarFallback>
              </Avatar>
              <div className="flex flex-col gap-1">
                <Button type="button" variant="outline" size="sm" disabled title="Available after creating this account">
                  Change Photo
                </Button>
                <p className="text-xs text-muted-foreground">Add a photo after creating this account</p>
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
            <CardContent>
              <p className="flex items-start gap-2 rounded-md bg-muted/50 px-3 py-2 text-xs text-muted-foreground">
                <Info className="mt-0.5 h-3.5 w-3.5 shrink-0" />
                New accounts are active by default. A password and profile photo can be set from this user's details page after it's created.
              </p>
            </CardContent>
          </Card>
        </div>
      </div>

      <div className="flex items-center justify-end gap-2 border-t border-border pt-4">
        <Button type="button" variant="outline" onClick={onCancel} disabled={isSubmitting}>
          Cancel
        </Button>
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Creating…' : 'Create User'}
        </Button>
      </div>
    </form>
  );
}
