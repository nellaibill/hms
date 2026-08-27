import { ApiError, createEmployeeSchema, EMPLOYEE_GENDERS, EMPLOYEE_TYPES, EMPLOYMENT_STATUSES, type EmployeeFormValues } from '@hms/shared';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { DepartmentSelect } from '@/components/DepartmentSelect';
import { DesignationSelect } from '@/components/DesignationSelect';
import { EmployeeSelect } from '@/components/EmployeeSelect';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Switch } from '@/components/ui/switch';

interface EmployeeFormProps {
  mode: 'create' | 'edit';
  /** The employee's own id, when editing — excluded from the Reporting Manager options
   * (an employee can't be their own manager). */
  employeeId?: string;
  defaultValues?: Partial<EmployeeFormValues>;
  onSubmit: (values: EmployeeFormValues) => void;
  isSubmitting: boolean;
  submitLabel: string;
  apiError: ApiError | null;
}

export function EmployeeForm({ mode, employeeId, defaultValues, onSubmit, isSubmitting, submitLabel, apiError }: EmployeeFormProps) {
  const {
    register,
    control,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<EmployeeFormValues>({
    resolver: zodResolver(createEmployeeSchema),
    defaultValues: {
      employeeCode: '',
      firstName: '',
      lastName: '',
      gender: 'Male',
      dateOfBirth: '',
      phone: '',
      email: '',
      address: '',
      emergencyContactName: '',
      emergencyContactPhone: '',
      departmentId: '',
      designationId: '',
      employeeType: 'Permanent',
      joiningDate: '',
      employmentStatus: 'Active',
      reportingManagerId: '',
      profilePhotoUrl: '',
      userId: '',
      isActive: true,
      ...defaultValues,
    },
  });

  // Server-side validation failures (docs/ApiStandards.md §5) are mapped onto the same
  // field-level display client validation uses, per docs/FrontendArchitecture.md §9.
  useEffect(() => {
    if (!apiError?.validationErrors) {
      return;
    }

    for (const issue of apiError.validationErrors) {
      const fieldName = (issue.field.charAt(0).toLowerCase() + issue.field.slice(1)) as keyof EmployeeFormValues;
      setError(fieldName, { type: 'server', message: issue.message });
    }
  }, [apiError, setError]);

  const generalError = apiError && !apiError.validationErrors ? apiError.message : null;

  return (
    <form onSubmit={handleSubmit(onSubmit)} noValidate className="flex max-w-3xl flex-col gap-8">
      {generalError && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {generalError}
        </p>
      )}

      <section className="flex flex-col gap-4">
        <h2 className="text-sm font-semibold uppercase tracking-wide text-muted-foreground">Identity &amp; Personal</h2>
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="employeeCode">Employee Code</Label>
            <Input id="employeeCode" disabled={mode === 'edit'} {...register('employeeCode')} />
            {mode === 'edit' && <p className="text-xs text-muted-foreground">Code can't be changed after an employee is created.</p>}
            {errors.employeeCode && <p className="text-sm text-destructive">{errors.employeeCode.message}</p>}
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="firstName">First Name</Label>
              <Input id="firstName" {...register('firstName')} />
              {errors.firstName && <p className="text-sm text-destructive">{errors.firstName.message}</p>}
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="lastName">Last Name</Label>
              <Input id="lastName" {...register('lastName')} />
              {errors.lastName && <p className="text-sm text-destructive">{errors.lastName.message}</p>}
            </div>
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="gender">Gender</Label>
            <Controller
              control={control}
              name="gender"
              render={({ field }) => (
                <Select value={field.value} onValueChange={field.onChange}>
                  <SelectTrigger id="gender" aria-label="Gender">
                    <SelectValue placeholder="Select gender" />
                  </SelectTrigger>
                  <SelectContent>
                    {EMPLOYEE_GENDERS.map((gender) => (
                      <SelectItem key={gender} value={gender}>
                        {gender}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              )}
            />
            {errors.gender && <p className="text-sm text-destructive">{errors.gender.message}</p>}
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="dateOfBirth">Date of Birth</Label>
            <Input id="dateOfBirth" type="date" {...register('dateOfBirth')} />
            {errors.dateOfBirth && <p className="text-sm text-destructive">{errors.dateOfBirth.message}</p>}
          </div>
        </div>
      </section>

      <section className="flex flex-col gap-4">
        <h2 className="text-sm font-semibold uppercase tracking-wide text-muted-foreground">Contact</h2>
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="phone">Phone</Label>
            <Input id="phone" {...register('phone')} />
            {errors.phone && <p className="text-sm text-destructive">{errors.phone.message}</p>}
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="email">Email</Label>
            <Input id="email" type="email" {...register('email')} />
            {errors.email && <p className="text-sm text-destructive">{errors.email.message}</p>}
          </div>
          <div className="flex flex-col gap-1.5 sm:col-span-2">
            <Label htmlFor="address">Address</Label>
            <textarea
              id="address"
              rows={2}
              className="flex w-full resize-none rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground shadow-sm transition-colors placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-background disabled:cursor-not-allowed disabled:opacity-50"
              {...register('address')}
            />
            {errors.address && <p className="text-sm text-destructive">{errors.address.message}</p>}
          </div>
        </div>
      </section>

      <section className="flex flex-col gap-4">
        <h2 className="text-sm font-semibold uppercase tracking-wide text-muted-foreground">Emergency Contact</h2>
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="emergencyContactName">Contact Name</Label>
            <Input id="emergencyContactName" {...register('emergencyContactName')} />
            {errors.emergencyContactName && <p className="text-sm text-destructive">{errors.emergencyContactName.message}</p>}
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="emergencyContactPhone">Contact Phone</Label>
            <Input id="emergencyContactPhone" {...register('emergencyContactPhone')} />
            {errors.emergencyContactPhone && <p className="text-sm text-destructive">{errors.emergencyContactPhone.message}</p>}
          </div>
        </div>
      </section>

      <section className="flex flex-col gap-4">
        <h2 className="text-sm font-semibold uppercase tracking-wide text-muted-foreground">Employment</h2>
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="departmentId">Department</Label>
            <Controller
              control={control}
              name="departmentId"
              render={({ field }) => <DepartmentSelect id="departmentId" value={field.value} onValueChange={field.onChange} />}
            />
            {errors.departmentId && <p className="text-sm text-destructive">{errors.departmentId.message}</p>}
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="designationId">Designation</Label>
            <Controller
              control={control}
              name="designationId"
              render={({ field }) => <DesignationSelect id="designationId" value={field.value} onValueChange={field.onChange} />}
            />
            {errors.designationId && <p className="text-sm text-destructive">{errors.designationId.message}</p>}
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="employeeType">Employee Type</Label>
            <Controller
              control={control}
              name="employeeType"
              render={({ field }) => (
                <Select value={field.value} onValueChange={field.onChange}>
                  <SelectTrigger id="employeeType" aria-label="Employee type">
                    <SelectValue placeholder="Select type" />
                  </SelectTrigger>
                  <SelectContent>
                    {EMPLOYEE_TYPES.map((type) => (
                      <SelectItem key={type} value={type}>
                        {type}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              )}
            />
            {errors.employeeType && <p className="text-sm text-destructive">{errors.employeeType.message}</p>}
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="joiningDate">Joining Date</Label>
            <Input id="joiningDate" type="date" {...register('joiningDate')} />
            {errors.joiningDate && <p className="text-sm text-destructive">{errors.joiningDate.message}</p>}
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="employmentStatus">Employment Status</Label>
            <Controller
              control={control}
              name="employmentStatus"
              render={({ field }) => (
                <Select value={field.value} onValueChange={field.onChange}>
                  <SelectTrigger id="employmentStatus" aria-label="Employment status">
                    <SelectValue placeholder="Select status" />
                  </SelectTrigger>
                  <SelectContent>
                    {EMPLOYMENT_STATUSES.map((status) => (
                      <SelectItem key={status} value={status}>
                        {status}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              )}
            />
            {errors.employmentStatus && <p className="text-sm text-destructive">{errors.employmentStatus.message}</p>}
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="reportingManagerId">Reporting Manager (optional)</Label>
            <Controller
              control={control}
              name="reportingManagerId"
              render={({ field }) => (
                <EmployeeSelect
                  id="reportingManagerId"
                  value={field.value ?? ''}
                  onValueChange={field.onChange}
                  excludeId={employeeId}
                  includeNoneOption
                  ariaLabel="Reporting manager"
                />
              )}
            />
            {errors.reportingManagerId && <p className="text-sm text-destructive">{errors.reportingManagerId.message}</p>}
          </div>
        </div>

        <div className="flex items-center justify-between rounded-md border border-border px-3 py-2.5">
          <Label htmlFor="isActive" className="cursor-pointer">Active</Label>
          <Controller
            control={control}
            name="isActive"
            render={({ field }) => <Switch id="isActive" checked={field.value} onCheckedChange={field.onChange} />}
          />
        </div>
      </section>

      <section className="flex flex-col gap-4">
        <h2 className="text-sm font-semibold uppercase tracking-wide text-muted-foreground">Advanced (optional)</h2>
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="profilePhotoUrl">Profile Photo URL</Label>
            <Input id="profilePhotoUrl" {...register('profilePhotoUrl')} />
            {errors.profilePhotoUrl && <p className="text-sm text-destructive">{errors.profilePhotoUrl.message}</p>}
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="userId">Linked User Account Id</Label>
            <Input id="userId" placeholder="Login user account id, if any" {...register('userId')} />
            {errors.userId && <p className="text-sm text-destructive">{errors.userId.message}</p>}
          </div>
        </div>
      </section>

      <Button type="submit" disabled={isSubmitting} className="self-start">
        {isSubmitting ? 'Saving…' : submitLabel}
      </Button>
    </form>
  );
}
