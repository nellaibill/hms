import { ApiError, createHospitalSchema, type CreateHospitalFormValues } from '@hms/shared';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect, useState } from 'react';
import { Controller, useForm, type Control, type FieldErrors, type UseFormRegister } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { PasswordInput } from '@/components/ui/password-input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { featureLabel, OPTIONAL_FEATURE_KEYS } from '../featureCatalog';
import { CITIES_BY_STATE, INDIAN_STATES_AND_UTS, type IndianStateOrUt } from '../indiaStatesAndCities';

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
  type?: 'text' | 'email' | 'password' | 'number';
  /** Omits the red required-asterisk and swaps in this help text instead — used for fields
   * that have a sensible default rather than needing the user to type something in. */
  helpText?: string;
}

function TextField({ name, label, register, errors, type = 'text', helpText }: TextFieldProps) {
  const error = errors[name];
  const inputId = `hospital-field-${name}`;
  return (
    <div className="flex min-w-[200px] flex-1 flex-col gap-1">
      <Label htmlFor={inputId}>
        {label}
        {helpText ? null : <span className="text-destructive"> *</span>}
      </Label>
      {type === 'password' ? (
        <PasswordInput id={inputId} {...register(name)} />
      ) : (
        <Input id={inputId} type={type} {...register(name)} />
      )}
      {helpText && !error && <p className="text-sm text-muted-foreground">{helpText}</p>}
      {error && <p className="text-sm text-destructive">{String(error.message)}</p>}
    </div>
  );
}

interface StateFieldProps {
  control: Control<CreateHospitalFormValues>;
  errors: FieldErrors<CreateHospitalFormValues>;
  onStateChange: (newState: string) => void;
}

function StateField({ control, errors, onStateChange }: StateFieldProps) {
  return (
    <div className="flex min-w-[200px] flex-1 flex-col gap-1">
      <Label htmlFor="hospital-field-state">
        State
        <span className="text-destructive"> *</span>
      </Label>
      <Controller
        name="state"
        control={control}
        render={({ field }) => (
          <Select
            value={field.value}
            onValueChange={(value) => {
              field.onChange(value);
              onStateChange(value);
            }}
          >
            <SelectTrigger id="hospital-field-state" aria-label="State">
              <SelectValue placeholder="Select state…" />
            </SelectTrigger>
            <SelectContent>
              {INDIAN_STATES_AND_UTS.map((state) => (
                <SelectItem key={state} value={state}>
                  {state}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        )}
      />
      {errors.state && <p className="text-sm text-destructive">{String(errors.state.message)}</p>}
    </div>
  );
}

interface CityFieldProps {
  register: UseFormRegister<CreateHospitalFormValues>;
  errors: FieldErrors<CreateHospitalFormValues>;
  state: string;
}

/** A free-text input, not a locked picklist — an exhaustive list of every Indian city isn't
 * realistic to maintain, and a hospital's actual city missing from a strict dropdown would
 * block onboarding entirely. The `<datalist>` still gives a native dropdown-style suggestion
 * list, scoped to the selected state, without preventing a custom entry. */
function CityField({ register, errors, state }: CityFieldProps) {
  const suggestions = (INDIAN_STATES_AND_UTS as readonly string[]).includes(state)
    ? CITIES_BY_STATE[state as IndianStateOrUt]
    : [];

  return (
    <div className="flex min-w-[200px] flex-1 flex-col gap-1">
      <Label htmlFor="hospital-field-city">
        City
        <span className="text-destructive"> *</span>
      </Label>
      <Input id="hospital-field-city" list="hospital-city-suggestions" {...register('city')} />
      <datalist id="hospital-city-suggestions">
        {suggestions.map((city) => (
          <option key={city} value={city} />
        ))}
      </datalist>
      {errors.city && <p className="text-sm text-destructive">{String(errors.city.message)}</p>}
    </div>
  );
}

export function HospitalForm({ onSubmit, isSubmitting, submitLabel, apiError }: HospitalFormProps) {
  const {
    register,
    control,
    handleSubmit,
    setError,
    setValue,
    watch,
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
      importedPatientCapacity: 40000,
    },
  });

  const selectedState = watch('state');

  // A city picked under the previous state is meaningless once the state changes — same
  // reasoning as PatientRegistrationForm's handleDepartmentChange/handleStateChange.
  function handleStateChange() {
    setValue('city', '');
  }

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
          <StateField control={control} errors={errors} onStateChange={handleStateChange} />
          <CityField register={register} errors={errors} state={selectedState} />
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
          <CardTitle className="text-base">Patient Numbering</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-wrap gap-4">
          <TextField
            name="importedPatientCapacity"
            label="Imported Patient Capacity"
            type="number"
            register={register}
            errors={errors}
            helpText="UHIDs 1 through this number are reserved for bulk-imported/legacy patients; new registrations start right after it. Cannot be changed once the hospital is created."
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
