import { ApiError, createHospitalSchema, type CreateHospitalFormValues } from '@hms/shared';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect, useState } from 'react';
import { useForm, type FieldErrors, type UseFormRegister } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { PasswordInput } from '@/components/ui/password-input';
import { featureLabel, OPTIONAL_FEATURE_KEYS } from '../featureCatalog';

interface HospitalFormProps {
  onSubmit: (values: CreateHospitalFormValues) => void;
  isSubmitting: boolean;
  submitLabel: string;
  apiError: ApiError | null;
}

interface TextFieldProps {
  name: keyof CreateHospitalFormValues;
  label: string;
  register: UseFormRegister<CreateHospitalFormValues>;
  errors: FieldErrors<CreateHospitalFormValues>;
  type?: 'text' | 'email' | 'password';
}

function TextField({ name, label, register, errors, type = 'text' }: TextFieldProps) {
  const error = errors[name];
  const inputId = `hospital-field-${name}`;
  return (
    <div className="flex min-w-[200px] flex-1 flex-col gap-1">
      <Label htmlFor={inputId}>
        {label}
        <span className="text-destructive"> *</span>
      </Label>
      {type === 'password' ? (
        <PasswordInput id={inputId} {...register(name)} />
      ) : (
        <Input id={inputId} type={type} {...register(name)} />
      )}
      {error && <p className="text-sm text-destructive">{String(error.message)}</p>}
    </div>
  );
}

export function HospitalForm({ onSubmit, isSubmitting, submitLabel, apiError }: HospitalFormProps) {
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<CreateHospitalFormValues>({
    resolver: zodResolver(createHospitalSchema),
    defaultValues: {
      hospitalName: '',
      hospitalCode: '',
      mobileNumber: '',
      address: '',
      city: '',
      state: '',
      pincode: '',
      superAdminUsername: '',
      superAdminFirstName: '',
      superAdminLastName: '',
      superAdminEmail: '',
      superAdminPhoneNumber: '',
      superAdminPassword: '',
      superAdminConfirmPassword: '',
      enabledFeatureKeys: [],
    },
  });

  // Mandatory features (identity/masters/patients/documents/branding) are always provisioned
  // server-side regardless of this selection — only the optional ones are choosable here.
  const [enabledFeatureKeys, setEnabledFeatureKeys] = useState<Set<string>>(new Set());

  function toggleFeature(key: string) {
    setEnabledFeatureKeys((current) => {
      const next = new Set(current);
      if (next.has(key)) {
        next.delete(key);
      } else {
        next.add(key);
      }
      return next;
    });
  }

  function handleFormSubmit(values: CreateHospitalFormValues) {
    onSubmit({ ...values, enabledFeatureKeys: Array.from(enabledFeatureKeys) });
  }

  // Server-side validation/business-rule failures (e.g. duplicate hospital code) are mapped
  // onto the same field-level display client validation uses — mirrors ProductForm.tsx.
  useEffect(() => {
    if (!apiError?.validationErrors) {
      return;
    }
    for (const issue of apiError.validationErrors) {
      const fieldName = (issue.field.charAt(0).toLowerCase() + issue.field.slice(1)) as keyof CreateHospitalFormValues;
      setError(fieldName, { type: 'server', message: issue.message });
    }
  }, [apiError, setError]);

  const generalError = apiError && !apiError.validationErrors ? apiError.message : null;

  return (
    <form onSubmit={handleSubmit(handleFormSubmit)} noValidate className="mx-auto flex w-full max-w-4xl flex-col gap-5">
      {generalError && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {generalError}
        </p>
      )}

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Hospital Information</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-wrap gap-4">
          <TextField name="hospitalName" label="Hospital Name" register={register} errors={errors} />
          <TextField name="hospitalCode" label="Hospital Code" register={register} errors={errors} />
          <TextField name="mobileNumber" label="Mobile Number" register={register} errors={errors} />
          <TextField name="address" label="Address" register={register} errors={errors} />
          <TextField name="city" label="City" register={register} errors={errors} />
          <TextField name="state" label="State" register={register} errors={errors} />
          <TextField name="pincode" label="Pincode" register={register} errors={errors} />
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Super Administrator</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-wrap gap-4">
          <TextField name="superAdminUsername" label="Username" register={register} errors={errors} />
          <TextField name="superAdminFirstName" label="First Name" register={register} errors={errors} />
          <TextField name="superAdminLastName" label="Last Name" register={register} errors={errors} />
          <TextField name="superAdminEmail" label="Email" type="email" register={register} errors={errors} />
          <TextField name="superAdminPhoneNumber" label="Phone Number" register={register} errors={errors} />
          <TextField name="superAdminPassword" label="Password" type="password" register={register} errors={errors} />
          <TextField
            name="superAdminConfirmPassword"
            label="Retype Password"
            type="password"
            register={register}
            errors={errors}
          />
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Modules</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-col gap-2">
          <p className="text-sm text-muted-foreground">
            Every hospital gets Identity, Master Data, Patients, Documents, and Branding. Choose any additional
            modules this hospital should have — more can be enabled later from the Platform Portal.
          </p>
          <div className="grid grid-cols-2 gap-2 pt-1">
            {OPTIONAL_FEATURE_KEYS.map((key) => (
              <label key={key} className="flex items-center gap-2 text-sm">
                <input
                  type="checkbox"
                  checked={enabledFeatureKeys.has(key)}
                  onChange={() => toggleFeature(key)}
                  className="h-3.5 w-3.5 rounded border-input text-primary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                />
                {featureLabel(key)}
              </label>
            ))}
          </div>
        </CardContent>
      </Card>

      <div className="sticky bottom-0 z-10 -mx-4 flex justify-end gap-3 border-t border-border bg-background/95 px-4 py-3 backdrop-blur supports-[backdrop-filter]:bg-background/80">
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Provisioning…' : submitLabel}
        </Button>
      </div>
    </form>
  );
}
