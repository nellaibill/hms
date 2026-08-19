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
import { ChevronLeft, ChevronRight, MapPin, Plus, Stethoscope, User, X } from 'lucide-react';
import { useState } from 'react';
import { Controller, useFieldArray, useForm, type FieldErrors } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { bloodGroupLabel } from '../bloodGroupLabel';
import { calculateDetailedAge } from '../detailedAge';
import { humanize } from '../humanize';
import { Field, FormSection } from './FormSection';
import { PatientDocumentUpload } from './PatientDocumentUpload';

interface PatientEditFormProps {
  patientId: string;
  defaultValues: PatientEditUiFormValues;
  isSubmitting: boolean;
  apiError: ApiError | null;
  onSubmit: (values: PatientEditUiFormValues) => void;
  onCancel: () => void;
}

const TAB_ORDER = ['patient-info', 'contact-info', 'medical-info'] as const;
type TabId = (typeof TAB_ORDER)[number];

// Which top-level form fields live on each tab — used to jump to the first tab with an
// error on a failed submit, and to flag tabs with a red dot so an error on a tab the user
// isn't currently viewing doesn't silently block submission with no visible cause.
const TAB_ERROR_FIELDS: Record<TabId, (keyof PatientEditUiFormValues)[]> = {
  'patient-info': ['title', 'firstName', 'lastName', 'dateOfBirth', 'gender', 'bloodGroup'],
  'contact-info': [
    'addressLine1',
    'addressLine2',
    'addressLine3',
    'district',
    'state',
    'pincode',
    'primaryPhone',
    'additionalPhones',
    'email',
    'profession',
    'emergencyContactRelationship',
    'emergencyContactName',
    'emergencyContactPhone',
  ],
  'medical-info': ['hasKnownAllergy', 'allergyCategory', 'allergySpecify', 'allergySeverity'],
};

function tabWithFirstError(errors: FieldErrors<PatientEditUiFormValues>): TabId | null {
  return TAB_ORDER.find((tab) => TAB_ERROR_FIELDS[tab].some((field) => Boolean(errors[field]))) ?? null;
}

function isTabId(value: string): value is TabId {
  return (TAB_ORDER as readonly string[]).includes(value);
}

/** Updates a patient's demographic/master-data fields only — Registration Details and Billing are intentionally not editable here (see docs/DecisionLog.md ADR-008). */
export function PatientEditForm({ patientId, defaultValues, isSubmitting, apiError, onSubmit, onCancel }: PatientEditFormProps) {
  const {
    register,
    control,
    handleSubmit,
    watch,
    trigger,
    formState: { errors },
  } = useForm<PatientEditUiFormValues>({
    resolver: zodResolver(patientEditUiSchema),
    defaultValues,
    mode: 'onChange',
  });

  const additionalPhones = useFieldArray({ control, name: 'additionalPhones' });
  const hasKnownAllergy = watch('hasKnownAllergy');

  const [activeTab, setActiveTab] = useState<TabId>('patient-info');
  // Tabs the user has actually tried to leave (via Next) or a final submit attempt — a tab
  // the user hasn't reached yet shouldn't show an error dot just because its untouched
  // required fields are technically invalid. Mirrors PatientRegistrationForm's gating so
  // the two forms behave identically.
  const [attemptedTabs, setAttemptedTabs] = useState<ReadonlySet<TabId>>(new Set());
  const activeTabIndex = TAB_ORDER.indexOf(activeTab);
  const isFirstTab = activeTabIndex === 0;
  const isLastTab = activeTabIndex === TAB_ORDER.length - 1;
  const goToPreviousTab = () => setActiveTab(TAB_ORDER[activeTabIndex - 1]);

  // Validates every tab from the current one up to (but not including) the target before
  // landing on it — covers both the Next button and clicking a tab header directly.
  // Moving backward to an already-visited tab is always allowed with no validation.
  const goToTab = async (target: TabId) => {
    const targetIndex = TAB_ORDER.indexOf(target);
    if (targetIndex <= activeTabIndex) {
      setActiveTab(target);
      return;
    }
    for (let i = activeTabIndex; i < targetIndex; i++) {
      const tab = TAB_ORDER[i];
      const isTabValid = await trigger(TAB_ERROR_FIELDS[tab]);
      setAttemptedTabs((prev) => new Set(prev).add(tab));
      if (!isTabValid) {
        setActiveTab(tab);
        return;
      }
    }
    setActiveTab(target);
  };
  const goToNextTab = () => goToTab(TAB_ORDER[activeTabIndex + 1]);

  const onInvalid = (invalidFields: FieldErrors<PatientEditUiFormValues>) => {
    setAttemptedTabs(new Set(TAB_ORDER));
    const firstErroredTab = tabWithFirstError(invalidFields);
    if (firstErroredTab) setActiveTab(firstErroredTab);
  };

  const dateOfBirth = watch('dateOfBirth');
  const detailedAge = dateOfBirth ? calculateDetailedAge(dateOfBirth) : null;

  const generalError = apiError?.message ?? null;
  const serverValidationMessages = apiError?.validationErrors?.map((issue) => issue.message) ?? [];

  // Every tab but the last has no type="submit" button (Cancel/Previous/Next are all
  // type="button") — with no default button, pressing Enter in a plain text input falls
  // back to the browser's native implicit form submission (a full page reload/navigation,
  // bypassing React entirely) instead of doing nothing or advancing the wizard. Blocking
  // Enter on <input> elements avoids that silent reload; it doesn't affect Select/Radix
  // dropdowns (which aren't <input>s and handle their own Enter key) or the real submit
  // button on the last tab (a click, not a keydown-Enter-on-input).
  const blockEnterKeySubmit = (event: React.KeyboardEvent<HTMLFormElement>) => {
    if (event.key === 'Enter' && event.target instanceof HTMLInputElement) {
      event.preventDefault();
    }
  };

  return (
    <div className="flex w-full max-w-6xl flex-col gap-5">
      <form
        onSubmit={handleSubmit(onSubmit, onInvalid)}
        onKeyDown={blockEnterKeySubmit}
        noValidate
        className="flex flex-1 flex-col gap-4"
      >
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

      <Tabs value={activeTab} onValueChange={(value) => isTabId(value) && void goToTab(value)}>
        <TabsList>
          <TabsTrigger value="patient-info" hasError={attemptedTabs.has('patient-info') && TAB_ERROR_FIELDS['patient-info'].some((f) => Boolean(errors[f]))}>
            <User className="h-4 w-4" />
            Patient Information
          </TabsTrigger>
          <TabsTrigger value="contact-info" hasError={attemptedTabs.has('contact-info') && TAB_ERROR_FIELDS['contact-info'].some((f) => Boolean(errors[f]))}>
            <MapPin className="h-4 w-4" />
            Contact Information
          </TabsTrigger>
          <TabsTrigger value="medical-info" hasError={attemptedTabs.has('medical-info') && TAB_ERROR_FIELDS['medical-info'].some((f) => Boolean(errors[f]))}>
            <Stethoscope className="h-4 w-4" />
            Medical Information
          </TabsTrigger>
        </TabsList>

        <TabsContent value="patient-info" className="pt-4">
        <FormSection id="demographics" title="Patient Identification & Demographics">
          <div className="flex flex-wrap gap-3">
            <Field label="Title" htmlFor="title" error={errors.title?.message} className="flex w-full flex-col gap-1 sm:w-28">
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
            <Field label="Date of birth" htmlFor="dateOfBirth" error={errors.dateOfBirth?.message} className="flex w-full flex-col gap-1 sm:w-48">
              <Input id="dateOfBirth" type="date" {...register('dateOfBirth')} />
              {detailedAge && <p className="text-xs text-muted-foreground">Age: {detailedAge}</p>}
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
            <Field
              label="Blood group"
              htmlFor="bloodGroup"
              error={errors.bloodGroup?.message}
              className="flex w-full flex-col gap-1 sm:w-32"
            >
              <Controller
                name="bloodGroup"
                control={control}
                render={({ field }) => (
                  <Select value={field.value} onValueChange={field.onChange}>
                    <SelectTrigger id="bloodGroup" aria-label="Blood group">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {BLOOD_GROUPS.map((b) => (
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
        </TabsContent>

        <TabsContent value="contact-info" className="pt-4">
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
        </TabsContent>

        <TabsContent value="medical-info" className="pt-4">
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

        <FormSection id="document-upload" title="Document Upload" description="Upload or replace the patient's photo and ID proof.">
          <PatientDocumentUpload patientId={patientId} bare />
        </FormSection>
        </TabsContent>
      </Tabs>

        <div className="sticky bottom-0 z-10 -mx-4 flex items-center justify-between gap-3 border-t border-border bg-background/95 px-4 py-3 backdrop-blur supports-[backdrop-filter]:bg-background/80">
          <Button type="button" variant="outline" onClick={onCancel}>
            Cancel
          </Button>
          <div className="flex gap-3">
            {!isFirstTab && (
              <Button type="button" variant="outline" onClick={goToPreviousTab}>
                <ChevronLeft className="h-4 w-4" />
                Previous
              </Button>
            )}
            {!isLastTab && (
              <Button type="button" onClick={goToNextTab}>
                Next
                <ChevronRight className="h-4 w-4" />
              </Button>
            )}
            {isLastTab && (
              <Button type="submit" disabled={isSubmitting}>
                {isSubmitting ? 'Saving…' : 'Save Changes'}
              </Button>
            )}
          </div>
        </div>
      </form>
    </div>
  );
}
