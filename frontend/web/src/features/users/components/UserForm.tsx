import { ApiError, userProfileSchema, type UserProfileFormValues } from '@hms/shared';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect } from 'react';
import { useForm } from 'react-hook-form';

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
    <form onSubmit={handleSubmit(onSubmit)} noValidate>
      {generalError && <p className="form-error-banner">{generalError}</p>}

      <div className="form-field">
        <label htmlFor="firstName">First name</label>
        <input id="firstName" autoComplete="given-name" {...register('firstName')} />
        {errors.firstName && <p className="form-error">{errors.firstName.message}</p>}
      </div>

      <div className="form-field">
        <label htmlFor="lastName">Last name</label>
        <input id="lastName" autoComplete="family-name" {...register('lastName')} />
        {errors.lastName && <p className="form-error">{errors.lastName.message}</p>}
      </div>

      <div className="form-field">
        <label htmlFor="email">Email</label>
        <input id="email" type="email" autoComplete="email" {...register('email')} />
        {errors.email && <p className="form-error">{errors.email.message}</p>}
      </div>

      <div className="form-field">
        <label htmlFor="phoneNumber">Phone number</label>
        <input id="phoneNumber" autoComplete="tel" {...register('phoneNumber')} />
        {errors.phoneNumber && <p className="form-error">{errors.phoneNumber.message}</p>}
      </div>

      <button type="submit" disabled={isSubmitting}>
        {isSubmitting ? 'Saving…' : submitLabel}
      </button>
    </form>
  );
}
