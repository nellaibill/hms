import { ApiError, userProfileSchema, type UserProfileFormValues } from '@hms/shared';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';

interface UserFormProps {
  defaultValues?: Partial<UserProfileFormValues>;
  onSubmit: (values: UserProfileFormValues) => void;
  isSubmitting: boolean;
  submitLabel: string;
  apiError: ApiError | null;
}

export function UserForm({ defaultValues, onSubmit, isSubmitting, submitLabel, apiError }: UserFormProps) {
  const {
    register,
    handleSubmit,
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
      const fieldName = (issue.field.charAt(0).toLowerCase() + issue.field.slice(1)) as keyof UserProfileFormValues;
      setError(fieldName, { type: 'server', message: issue.message });
    }
  }, [apiError, setError]);

  const generalError = apiError && !apiError.validationErrors ? apiError.message : null;

  return (
    <form onSubmit={handleSubmit(onSubmit)} noValidate className="flex max-w-lg flex-col gap-4">
      {generalError && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {generalError}
        </p>
      )}

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="username">Username</Label>
        <Input id="username" autoComplete="username" {...register('username')} />
        {errors.username && <p className="text-sm text-destructive">{errors.username.message}</p>}
      </div>

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

      <Button type="submit" disabled={isSubmitting} className="mt-2 self-start">
        {isSubmitting ? 'Saving…' : submitLabel}
      </Button>
    </form>
  );
}
