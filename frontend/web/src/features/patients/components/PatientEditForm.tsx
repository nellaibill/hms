import {
  ALLERGY_CATEGORIES,
  ALLERGY_SEVERITIES,
  ApiError,
  BLOOD_GROUPS,
  patientEditUiSchema,
  PATIENT_GENDERS,
  PHONE_RELATIONS,
  RELATIONSHIPS,
  TITLES,
  type PatientEditUiFormValues,
} from '@hms/shared';
import { zodResolver } from '@hookform/resolvers/zod';
import { Plus, X } from 'lucide-react';
import { Controller, useFieldArray, useForm } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { bloodGroupLabel } from '../bloodGroupLabel';
import { humanize } from '../humanize';
import { Field, FormSection } from './FormSection';
import { SectionNav, type SectionNavItem } from './SectionNav';

interface PatientEditFormProps {
  defaultValues: PatientEditUiFormValues;
  isSubmitting: boolean;
  apiError: ApiError | null;
  onSubmit: (values: PatientEditUiFormValues) => void;
  onCancel: () => void;
}

const sections: SectionNavItem[] = [
  { id: 'demographics', label: 'Demographics' },
  { id: 'address', label: 'Address' },
  { id: 'contact', label: 'Contact Details' },
  { id: 'emergency-contact', label: 'Emergency Contact' },
  { id: 'allergy', label: 'Allergy Details' },
];

/** Updates a patient's demographic/master-data fields only — the encounter is not editable here (see docs/DecisionLog.md's MVP-scope ADR). */
export function PatientEditForm({ defaultValues, isSubmitting, apiError, onSubmit, onCancel }: PatientEditFormProps) {
  const {
    register,
    control,
    handleSubmit,
    watch,
    formState: { errors },
  } = useForm<PatientEditUiFormValues>({
    resolver: zodResolver(patientEditUiSchema),
    defaultValues,
  });

  const additionalPhones = useFieldArray({ control, name: 'additionalPhones' });
  const hasKnownAllergy = watch('hasKnownAllergy');

  const generalError = apiError?.message ?? null;
  const serverValidationMessages = apiError?.validationErrors?.map((issue) => issue.message) ?? [];

  return (
    <div className="mx-auto flex w-full max-w-[1920px] gap-4">
      <SectionNav sections={sections} className="hidden w-44 shrink-0 lg:flex" />
      <div aria-hidden="true" className="hidden w-px shrink-0 bg-border lg:block" />

      <form onSubmit={handleSubmit(onSubmit)} noValidate className="flex flex-1 flex-col gap-4">
        {(generalError || serverValidationMessages.length > 0) && (
          <div role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
            {generalError && <p>{generalError}</p>}
            {serverValidationMessages.length > 0 && (
              <ul className="list-inside list-disc">
                {serverValidationMessages.map((message) => (
                  <li key={message}>{message}</li>
                ))}
              </ul>
            )}
          </div>
        )}

        <FormSection id="demographics" title="Patient Identification & Demographics">
          <div className="flex flex-wrap gap-3">
            <Field label="Title" htmlFor="title" className="flex w-full flex-col gap-1 sm:w-28">
              <Controller
                name="title"
                control={control}
                render={({ field }) => (
                  <Select value={field.value} onValueChange={field.onChange}>
                    <SelectTrigger id="title" aria-label="Title">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {TITLES.map((t) => (
                        <SelectItem key={t} value={t}>
                          {t}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                )}
              />
            </Field>
            <Field
              label="First name"
              htmlFor="firstName"
              error={errors.firstName?.message}
              className="flex min-w-[160px] flex-1 flex-col gap-1"
            >
              <Input id="firstName" {...register('firstName')} />
            </Field>
            <Field
              label="Last name"
              htmlFor="lastName"
              error={errors.lastName?.message}
              className="flex min-w-[160px] flex-1 flex-col gap-1"
            >
              <Input id="lastName" {...register('lastName')} />
            </Field>
            <Field label="Date of birth" htmlFor="dateOfBirth" error={errors.dateOfBirth?.message} className="flex w-full flex-col gap-1 sm:w-44">
              <Input id="dateOfBirth" type="date" {...register('dateOfBirth')} />
            </Field>
          </div>

          <div className="flex flex-wrap gap-3">
            <Field label="Gender" htmlFor="gender" className="flex w-full flex-col gap-1 sm:w-36">
              <Controller
                name="gender"
                control={control}
                render={({ field }) => (
                  <Select value={field.value} onValueChange={field.onChange}>
                    <SelectTrigger id="gender" aria-label="Gender">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {PATIENT_GENDERS.map((g) => (
                        <SelectItem key={g} value={g}>
                          {g}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                )}
              />
            </Field>
            <Field label="Blood group" htmlFor="bloodGroup" className="flex w-full flex-col gap-1 sm:w-32">
              <Controller
                name="bloodGroup"
                control={control}
                render={({ field }) => (
                  <Select value={field.value || undefined} onValueChange={field.onChange}>
                    <SelectTrigger id="bloodGroup" aria-label="Blood group">
                      <SelectValue placeholder="Unknown" />
                    </SelectTrigger>
                    <SelectContent>
                      {BLOOD_GROUPS.filter((b) => b !== 'Unknown').map((b) => (
                        <SelectItem key={b} value={b}>
                          {bloodGroupLabel(b)}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                )}
              />
            </Field>
          </div>
        </FormSection>

        <FormSection id="address" title="Address">
          <div className="flex flex-wrap gap-3">
            <Field
              label="Address line 1 (door no. & building name)"
              htmlFor="addressLine1"
              error={errors.addressLine1?.message}
              className="flex min-w-[260px] flex-1 flex-col gap-1"
            >
              <Input id="addressLine1" {...register('addressLine1')} />
            </Field>
            <Field label="Address line 2 (street)" htmlFor="addressLine2" className="flex min-w-[220px] flex-1 flex-col gap-1">
              <Input id="addressLine2" {...register('addressLine2')} />
            </Field>
          </div>
          <div className="flex flex-wrap gap-3">
            <Field label="Address line 3 (city)" htmlFor="addressLine3" className="flex min-w-[160px] flex-1 flex-col gap-1">
              <Input id="addressLine3" {...register('addressLine3')} />
            </Field>
            <Field
              label="District"
              htmlFor="district"
              error={errors.district?.message}
              className="flex min-w-[160px] flex-1 flex-col gap-1"
            >
              <Input id="district" {...register('district')} />
            </Field>
            <Field label="State" htmlFor="state" error={errors.state?.message} className="flex min-w-[160px] flex-1 flex-col gap-1">
              <Input id="state" {...register('state')} />
            </Field>
            <Field label="Pincode" htmlFor="pincode" error={errors.pincode?.message} className="flex w-full flex-col gap-1 sm:w-32">
              <Input id="pincode" inputMode="numeric" {...register('pincode')} />
            </Field>
          </div>
        </FormSection>

        <FormSection id="contact" title="Contact Details" description="Primary phone required. Add up to two additional numbers.">
          <div className="flex flex-wrap gap-3">
            <Field
              label="Primary phone"
              htmlFor="primaryPhoneNumber"
              error={errors.primaryPhone?.number?.message}
              className="flex min-w-[160px] flex-1 flex-col gap-1"
            >
              <Input id="primaryPhoneNumber" {...register('primaryPhone.number')} />
            </Field>
            <Field label="Relation" htmlFor="primaryPhoneRelation" className="flex w-full flex-col gap-1 sm:w-44">
              <Controller
                name="primaryPhone.relation"
                control={control}
                render={({ field }) => (
                  <Select value={field.value} onValueChange={field.onChange}>
                    <SelectTrigger id="primaryPhoneRelation" aria-label="Primary phone relation">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {PHONE_RELATIONS.map((r) => (
                        <SelectItem key={r} value={r}>
                          {humanize(r)}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                )}
              />
            </Field>
            <Field label="Email" htmlFor="email" error={errors.email?.message} className="flex min-w-[180px] flex-1 flex-col gap-1">
              <Input id="email" type="email" {...register('email')} />
            </Field>
            <Field label="Profession" htmlFor="profession" className="flex min-w-[160px] flex-1 flex-col gap-1">
              <Input id="profession" {...register('profession')} />
            </Field>
          </div>

          {additionalPhones.fields.map((field, index) => (
            <div key={field.id} className="flex flex-wrap items-end gap-3">
              <Field
                label={`Additional phone ${index + 1}`}
                htmlFor={`additionalPhones.${index}.number`}
                className="flex min-w-[160px] flex-1 flex-col gap-1"
              >
                <Input id={`additionalPhones.${index}.number`} {...register(`additionalPhones.${index}.number` as const)} />
              </Field>
              <Field label="Relation" htmlFor={`additionalPhones.${index}.relation`} className="flex w-full flex-col gap-1 sm:w-44">
                <Controller
                  name={`additionalPhones.${index}.relation` as const}
                  control={control}
                  render={({ field: relationField }) => (
                    <Select value={relationField.value} onValueChange={relationField.onChange}>
                      <SelectTrigger id={`additionalPhones.${index}.relation`} aria-label={`Additional phone ${index + 1} relation`}>
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        {PHONE_RELATIONS.map((r) => (
                          <SelectItem key={r} value={r}>
                            {humanize(r)}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  )}
                />
              </Field>
              <Button type="button" variant="ghost" size="icon" aria-label="Remove phone number" onClick={() => additionalPhones.remove(index)}>
                <X className="h-4 w-4" />
              </Button>
            </div>
          ))}

          {additionalPhones.fields.length < 2 && (
            <Button
              type="button"
              variant="outline"
              size="sm"
              className="w-fit gap-1.5"
              onClick={() => additionalPhones.append({ number: '', relation: 'Self' })}
            >
              <Plus className="h-4 w-4" />
              Add another number
            </Button>
          )}
        </FormSection>

        <FormSection id="emergency-contact" title="Emergency Contact">
          <div className="flex flex-wrap gap-3">
            <Field label="Relationship" htmlFor="emergencyContactRelationship" className="flex w-full flex-col gap-1 sm:w-44">
              <Controller
                name="emergencyContactRelationship"
                control={control}
                render={({ field }) => (
                  <Select value={field.value} onValueChange={field.onChange}>
                    <SelectTrigger id="emergencyContactRelationship" aria-label="Emergency contact relationship">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {RELATIONSHIPS.map((r) => (
                        <SelectItem key={r} value={r}>
                          {humanize(r)}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                )}
              />
            </Field>
            <Field
              label="Name"
              htmlFor="emergencyContactName"
              error={errors.emergencyContactName?.message}
              className="flex min-w-[180px] flex-1 flex-col gap-1"
            >
              <Input id="emergencyContactName" {...register('emergencyContactName')} />
            </Field>
            <Field
              label="Phone"
              htmlFor="emergencyContactPhone"
              error={errors.emergencyContactPhone?.message}
              className="flex min-w-[160px] flex-1 flex-col gap-1"
            >
              <Input id="emergencyContactPhone" {...register('emergencyContactPhone')} />
            </Field>
          </div>
        </FormSection>

        <FormSection id="allergy" title="Allergy Details">
          <div className="flex items-center gap-2">
            <input id="hasKnownAllergy" type="checkbox" className="h-4 w-4 rounded border-input" {...register('hasKnownAllergy')} />
            <label htmlFor="hasKnownAllergy" className="cursor-pointer text-sm font-normal text-foreground">
              Patient has a known allergy
            </label>
          </div>
          {hasKnownAllergy && (
            <div className="flex flex-wrap gap-3">
              <Field label="Type" htmlFor="allergyCategory" error={errors.allergyCategory?.message} className="flex w-full flex-col gap-1 sm:w-40">
                <Controller
                  name="allergyCategory"
                  control={control}
                  render={({ field }) => (
                    <Select value={field.value || undefined} onValueChange={field.onChange}>
                      <SelectTrigger id="allergyCategory" aria-label="Allergy type">
                        <SelectValue placeholder="Select type" />
                      </SelectTrigger>
                      <SelectContent>
                        {ALLERGY_CATEGORIES.map((c) => (
                          <SelectItem key={c} value={c}>
                            {c}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  )}
                />
              </Field>
              <Field label="Specify" htmlFor="allergySpecify" className="flex min-w-[200px] flex-1 flex-col gap-1">
                <Input id="allergySpecify" placeholder="e.g. Penicillin, Peanuts…" {...register('allergySpecify')} />
              </Field>
              <Field
                label="Severity"
                htmlFor="allergySeverity"
                error={errors.allergySeverity?.message}
                className="flex w-full flex-col gap-1 sm:w-60"
              >
                <Controller
                  name="allergySeverity"
                  control={control}
                  render={({ field }) => (
                    <Select value={field.value || undefined} onValueChange={field.onChange}>
                      <SelectTrigger id="allergySeverity" aria-label="Allergy severity">
                        <SelectValue placeholder="Select severity" />
                      </SelectTrigger>
                      <SelectContent>
                        {ALLERGY_SEVERITIES.map((s) => (
                          <SelectItem key={s} value={s}>
                            {s === 'Severe' ? 'Severe / Life-Threatening' : s}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  )}
                />
              </Field>
            </div>
          )}
        </FormSection>

        <div className="sticky bottom-0 z-10 -mx-4 flex justify-end gap-3 border-t border-border bg-background/95 px-4 py-3 backdrop-blur supports-[backdrop-filter]:bg-background/80">
          <Button type="button" variant="outline" onClick={onCancel}>
            Cancel
          </Button>
          <Button type="submit" disabled={isSubmitting}>
            {isSubmitting ? 'Saving…' : 'Save Changes'}
          </Button>
        </div>
      </form>
    </div>
  );
}
