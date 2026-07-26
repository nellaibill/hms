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
}

const sections: SectionNavItem[] = [
  { id: 'demographics', label: 'Demographics' },
  { id: 'address', label: 'Address' },
  { id: 'contact', label: 'Contact Details' },
  { id: 'emergency-contact', label: 'Emergency Contact' },
  { id: 'allergy', label: 'Allergy Details' },
];

/** Updates a patient's demographic/master-data fields only — the encounter is not editable here (see docs/DecisionLog.md's MVP-scope ADR). */
export function PatientEditForm({ defaultValues, isSubmitting, apiError, onSubmit }: PatientEditFormProps) {
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
    <div className="flex max-w-5xl gap-8">
      <SectionNav sections={sections} className="hidden w-52 shrink-0 lg:flex" />

      <form onSubmit={handleSubmit(onSubmit)} noValidate className="flex flex-1 flex-col gap-6">
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
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
            <Field label="Title" htmlFor="title">
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
            <Field label="First name" htmlFor="firstName" error={errors.firstName?.message} className="flex flex-col gap-1.5 sm:col-span-2">
              <Input id="firstName" {...register('firstName')} />
            </Field>
          </div>

          <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
            <Field label="Last name" htmlFor="lastName" error={errors.lastName?.message} className="flex flex-col gap-1.5 sm:col-span-2">
              <Input id="lastName" {...register('lastName')} />
            </Field>
            <Field label="Date of birth" htmlFor="dateOfBirth" error={errors.dateOfBirth?.message}>
              <Input id="dateOfBirth" type="date" {...register('dateOfBirth')} />
            </Field>
          </div>

          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <Field label="Gender" htmlFor="gender">
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
            <Field label="Blood group" htmlFor="bloodGroup">
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
          <Field label="Address line 1 (door no. & building name)" htmlFor="addressLine1" error={errors.addressLine1?.message}>
            <Input id="addressLine1" {...register('addressLine1')} />
          </Field>
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <Field label="Address line 2 (street)" htmlFor="addressLine2">
              <Input id="addressLine2" {...register('addressLine2')} />
            </Field>
            <Field label="Address line 3 (city)" htmlFor="addressLine3">
              <Input id="addressLine3" {...register('addressLine3')} />
            </Field>
          </div>
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
            <Field label="District" htmlFor="district" error={errors.district?.message}>
              <Input id="district" {...register('district')} />
            </Field>
            <Field label="State" htmlFor="state" error={errors.state?.message}>
              <Input id="state" {...register('state')} />
            </Field>
            <Field label="Pincode" htmlFor="pincode" error={errors.pincode?.message}>
              <Input id="pincode" inputMode="numeric" {...register('pincode')} />
            </Field>
          </div>
        </FormSection>

        <FormSection
          id="contact"
          title="Contact Details"
          description="Primary number is required; up to two additional numbers can be added, each with its own relation to the patient."
        >
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <Field label="Primary phone" htmlFor="primaryPhoneNumber" error={errors.primaryPhone?.number?.message}>
              <Input id="primaryPhoneNumber" {...register('primaryPhone.number')} />
            </Field>
            <Field label="Relation" htmlFor="primaryPhoneRelation">
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
          </div>

          {additionalPhones.fields.map((field, index) => (
            <div key={field.id} className="grid grid-cols-1 gap-4 sm:grid-cols-[1fr_1fr_auto] sm:items-end">
              <Field label={`Additional phone ${index + 1}`} htmlFor={`additionalPhones.${index}.number`}>
                <Input id={`additionalPhones.${index}.number`} {...register(`additionalPhones.${index}.number` as const)} />
              </Field>
              <Field label="Relation" htmlFor={`additionalPhones.${index}.relation`}>
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

          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <Field label="Email" htmlFor="email" error={errors.email?.message}>
              <Input id="email" type="email" {...register('email')} />
            </Field>
            <Field label="Profession" htmlFor="profession">
              <Input id="profession" {...register('profession')} />
            </Field>
          </div>
        </FormSection>

        <FormSection id="emergency-contact" title="Emergency Contact">
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
            <Field label="Relationship" htmlFor="emergencyContactRelationship">
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
            <Field label="Name" htmlFor="emergencyContactName" error={errors.emergencyContactName?.message}>
              <Input id="emergencyContactName" {...register('emergencyContactName')} />
            </Field>
            <Field label="Phone" htmlFor="emergencyContactPhone" error={errors.emergencyContactPhone?.message}>
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
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
              <Field label="Type" htmlFor="allergyCategory" error={errors.allergyCategory?.message}>
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
              <Field label="Specify" htmlFor="allergySpecify" className="flex flex-col gap-1.5 sm:col-span-2">
                <Input id="allergySpecify" placeholder="e.g. Penicillin, Peanuts…" {...register('allergySpecify')} />
              </Field>
              <Field
                label="Severity"
                htmlFor="allergySeverity"
                error={errors.allergySeverity?.message}
                className="flex flex-col gap-1.5 sm:col-span-3 sm:w-1/3"
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

        <Button type="submit" disabled={isSubmitting} className="self-start">
          {isSubmitting ? 'Saving…' : 'Save Changes'}
        </Button>
      </form>
    </div>
  );
}
