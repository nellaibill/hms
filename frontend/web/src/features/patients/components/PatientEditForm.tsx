import {
  type Allergy,
  ALLERGY_CATEGORIES,
  ALLERGY_SEVERITIES,
  type AllergySeverity,
  type AllergyType,
  ApiError,
  BLOOD_GROUPS,
  ID_PROOF_TYPES,
  MARITAL_STATUSES,
  patientEditUiSchema,
  PATIENT_GENDERS,
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
import { DistrictSelect } from '@/components/DistrictSelect';
import { StateSelect } from '@/components/StateSelect';
import { bloodGroupLabel } from '../bloodGroupLabel';
import { calculateDetailedAge, dateOfBirthInputBounds } from '../detailedAge';
import { tabErrorMessages } from '../formErrorSummary';
import { humanize } from '../humanize';
import { maritalStatusLabel } from '../maritalStatusLabel';
import { useAddPatientAllergyMutation, useRemovePatientAllergyMutation } from '../hooks/usePatientMutations';
import { titleLabel } from '../titleLabel';
import { Field, FormSection } from './FormSection';
import { PatientDocumentUpload } from './PatientDocumentUpload';
import { TabErrorSummary } from './TabErrorSummary';

interface PatientEditFormProps {
  patientId: string;
  /** The patient's current allergy list — lives outside the RHF form entirely; add/remove hit
   * the backend's real per-row endpoints immediately (see the Allergy Details section below),
   * so this always reflects the latest saved state via the query that refetches after each
   * mutation, not local form state. */
  allergies: Allergy[];
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
  'patient-info': ['title', 'firstName', 'lastName', 'dateOfBirth', 'gender', 'bloodGroup', 'maritalStatus'],
  'contact-info': [
    'addressLine1',
    'addressLine2',
    'addressLine3',
    'district',
    'state',
    'pincode',
    'primaryPhone',
    'secondaryPhone',
    'email',
    'profession',
    'emergencyContactRelationship',
    'emergencyContactName',
    'emergencyContactPhone',
    'additionalEmergencyContacts',
  ],
  'medical-info': ['idProofType', 'idProofNumber'],
};

function tabWithFirstError(errors: FieldErrors<PatientEditUiFormValues>): TabId | null {
  return TAB_ORDER.find((tab) => TAB_ERROR_FIELDS[tab].some((field) => Boolean(errors[field]))) ?? null;
}

function isTabId(value: string): value is TabId {
  return (TAB_ORDER as readonly string[]).includes(value);
}

/** Updates a patient's demographic/master-data fields only — Registration Details and Billing are intentionally not editable here (see docs/DecisionLog.md ADR-008). */
export function PatientEditForm({ patientId, allergies, defaultValues, isSubmitting, apiError, onSubmit, onCancel }: PatientEditFormProps) {
  const {
    register,
    control,
    handleSubmit,
    watch,
    trigger,
    setValue,
    formState: { errors },
  } = useForm<PatientEditUiFormValues>({
    resolver: zodResolver(patientEditUiSchema),
    defaultValues,
    // Deliberately not `mode: 'onChange'` — see PatientRegistrationForm's identical fix for
    // why: it races an overlapping validation pass against goToTab's own trigger() call.
  });

  const idProofType = watch('idProofType');
  const state = watch('state');

  // Allergy add/remove happen immediately against the real backend endpoints — see this
  // component's own doc comment on the `allergies` prop for why they're not part of the RHF
  // form. `newAllergy`/`showAddAllergyRow` are the local draft state for the one row being
  // added; they're unrelated to react-hook-form entirely.
  const addAllergyMutation = useAddPatientAllergyMutation();
  const removeAllergyMutation = useRemovePatientAllergyMutation();
  const [showAddAllergyRow, setShowAddAllergyRow] = useState(false);
  const [newAllergy, setNewAllergy] = useState<{ allergyType: AllergyType | ''; specify: string; severity: AllergySeverity | '' }>({
    allergyType: '',
    specify: '',
    severity: '',
  });
  const [addAllergyError, setAddAllergyError] = useState<string | null>(null);

  function handleAddAllergy() {
    if (!newAllergy.allergyType || !newAllergy.severity) {
      setAddAllergyError('Type and severity are required.');
      return;
    }
    setAddAllergyError(null);
    addAllergyMutation.mutate(
      { id: patientId, request: { allergyType: newAllergy.allergyType, specify: newAllergy.specify.trim() || undefined, severity: newAllergy.severity } },
      {
        onSuccess: () => {
          setNewAllergy({ allergyType: '', specify: '', severity: '' });
          setShowAddAllergyRow(false);
        },
      },
    );
  }

  function handleRemoveAllergy(allergyId: string) {
    removeAllergyMutation.mutate({ id: patientId, allergyId });
  }

  // A district picked under the previous state is meaningless once the state changes —
  // mirrors PatientRegistrationForm's identical handleStateChange.
  function handleStateChange(newState: string, onChange: (value: string) => void) {
    onChange(newState);
    setValue('district', '');
  }
  // The first Emergency Contact is its own always-present, always-required set of fields —
  // this is only for the extras added via "Add Emergency Contact". See
  // PatientRegistrationForm's identical additionalEmergencyContacts.
  const additionalEmergencyContacts = useFieldArray({ control, name: 'additionalEmergencyContacts' });

  const [activeTab, setActiveTab] = useState<TabId>('patient-info');
  // Set only if goToTab's validation step throws — without this, that failure left Next
  // looking like it silently did nothing: no tab change, no error, nothing (see goToTab below).
  const [navigationError, setNavigationError] = useState<string | null>(null);
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
    setNavigationError(null);
    const targetIndex = TAB_ORDER.indexOf(target);
    if (targetIndex <= activeTabIndex) {
      setActiveTab(target);
      return;
    }
    try {
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
    } catch {
      // Without this, a thrown validation error left Next looking like it did nothing — no
      // tab change, no message — since goToNextTab's onClick doesn't await or catch this.
      setNavigationError('Something went wrong checking this step. Please try again — if it keeps happening, refresh the page.');
    }
  };
  const goToNextTab = () => goToTab(TAB_ORDER[activeTabIndex + 1]);

  // Every validation message for one tab's fields, gated the same way its red-dot indicator
  // is (attemptedTabs) — mirrors PatientRegistrationForm's identical tabMessages.
  const tabMessages = (tab: TabId): string[] => (attemptedTabs.has(tab) ? tabErrorMessages(errors, TAB_ERROR_FIELDS[tab]) : []);

  const onInvalid = (invalidFields: FieldErrors<PatientEditUiFormValues>) => {
    setAttemptedTabs(new Set(TAB_ORDER));
    const firstErroredTab = tabWithFirstError(invalidFields);
    if (firstErroredTab) setActiveTab(firstErroredTab);
  };

  const dateOfBirth = watch('dateOfBirth');
  const detailedAge = dateOfBirth ? calculateDetailedAge(dateOfBirth) : null;

  const generalError = navigationError ?? apiError?.message ?? null;
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

  // Widened to match PatientRegistrationForm.tsx — same reasoning, see its own comment.
  return (
    <div className="flex w-full max-w-[100rem] flex-col gap-5">
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
        <TabErrorSummary messages={tabMessages('patient-info')} />
        <FormSection id="demographics" title="Patient Identification & Demographics">
          <div className="flex flex-wrap gap-3">
            <Field label="Title" htmlFor="title" className="flex w-full flex-col gap-1 sm:w-28">
              <Controller
                name="title"
                control={control}
                render={({ field }) => (
                  <Select value={field.value} onValueChange={field.onChange}>
                    <SelectTrigger id="title" aria-label="Title">
                      {/* Descriptive age/gender guidance shows only in the open dropdown list
                          (see titleLabel) — once selected, the trigger displays just the
                          title itself, not the guidance text, and that's also what's saved. */}
                      <SelectValue>{field.value}</SelectValue>
                    </SelectTrigger>
                    <SelectContent>
                      {TITLES.map((t) => (
                        <SelectItem key={t} value={t}>
                          {titleLabel(t)}
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
              className="flex min-w-[160px] flex-1 flex-col gap-1"
            >
              <Input id="firstName" {...register('firstName')} />
            </Field>
            <Field
              label="Last name"
              htmlFor="lastName"
              className="flex min-w-[160px] flex-1 flex-col gap-1"
            >
              <Input id="lastName" {...register('lastName')} />
            </Field>
            <Field label="Date of birth" htmlFor="dateOfBirth" className="flex w-full flex-col gap-1 sm:w-48">
              <Input id="dateOfBirth" type="date" {...dateOfBirthInputBounds()} {...register('dateOfBirth')} />
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
            <Field label="Marital status" htmlFor="maritalStatus" className="flex w-full flex-col gap-1 sm:w-40">
              <Controller
                name="maritalStatus"
                control={control}
                render={({ field }) => (
                  <Select value={field.value} onValueChange={field.onChange}>
                    <SelectTrigger id="maritalStatus" aria-label="Marital status">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {MARITAL_STATUSES.map((m) => (
                        <SelectItem key={m} value={m}>
                          {maritalStatusLabel(m)}
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
        <TabErrorSummary messages={tabMessages('contact-info')} />
        <FormSection id="address" title="Address">
          <div className="flex flex-wrap gap-3">
            <Field
              label="Address line 1 (door no. & building name)"
              htmlFor="addressLine1"
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
            <Field label="State" htmlFor="state" className="flex min-w-[160px] flex-1 flex-col gap-1">
              <Controller
                name="state"
                control={control}
                render={({ field }) => (
                  <StateSelect id="state" value={field.value} onValueChange={(value) => handleStateChange(value, field.onChange)} />
                )}
              />
            </Field>
            <Field
              label="District"
              htmlFor="district"
              className="flex min-w-[160px] flex-1 flex-col gap-1"
            >
              <Controller
                name="district"
                control={control}
                render={({ field }) => <DistrictSelect id="district" value={field.value} onValueChange={field.onChange} stateId={state} />}
              />
            </Field>
            <Field label="Pincode" htmlFor="pincode" className="flex w-full flex-col gap-1 sm:w-32">
              <Input id="pincode" inputMode="numeric" {...register('pincode')} />
            </Field>
          </div>
        </FormSection>

        <FormSection id="contact" title="Contact Details" description="Primary phone required.">
          <div className="flex flex-wrap gap-3">
            <Field
              label="Primary phone"
              htmlFor="primaryPhoneNumber"
              className="flex min-w-[160px] flex-1 flex-col gap-1"
            >
              <Input id="primaryPhoneNumber" {...register('primaryPhone.number')} />
            </Field>
            <Field
              label="Secondary phone (optional)"
              htmlFor="secondaryPhone"
              className="flex min-w-[160px] flex-1 flex-col gap-1"
            >
              <Input id="secondaryPhone" {...register('secondaryPhone')} />
            </Field>
            <Field label="Email" htmlFor="email" className="flex min-w-[180px] flex-1 flex-col gap-1">
              <Input id="email" type="email" {...register('email')} />
            </Field>
            <Field label="Profession" htmlFor="profession" className="flex min-w-[160px] flex-1 flex-col gap-1">
              <Input id="profession" {...register('profession')} />
            </Field>
          </div>
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
              className="flex min-w-[180px] flex-1 flex-col gap-1"
            >
              <Input id="emergencyContactName" {...register('emergencyContactName')} />
            </Field>
            <Field
              label="Phone"
              htmlFor="emergencyContactPhone"
              className="flex min-w-[160px] flex-1 flex-col gap-1"
            >
              <Input id="emergencyContactPhone" {...register('emergencyContactPhone')} />
            </Field>
          </div>

          {additionalEmergencyContacts.fields.map((field, index) => (
            <div key={field.id} className="flex flex-wrap items-end gap-3">
              <Field
                label="Relationship"
                htmlFor={`additionalEmergencyContacts.${index}.relationship`}
                className="flex w-full flex-col gap-1 sm:w-44"
              >
                <Controller
                  name={`additionalEmergencyContacts.${index}.relationship` as const}
                  control={control}
                  render={({ field: relationshipField }) => (
                    <Select value={relationshipField.value} onValueChange={relationshipField.onChange}>
                      <SelectTrigger id={`additionalEmergencyContacts.${index}.relationship`} aria-label={`Emergency contact ${index + 2} relationship`}>
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
                htmlFor={`additionalEmergencyContacts.${index}.name`}
                className="flex min-w-[180px] flex-1 flex-col gap-1"
              >
                <Input id={`additionalEmergencyContacts.${index}.name`} {...register(`additionalEmergencyContacts.${index}.name` as const)} />
              </Field>
              <Field
                label="Phone"
                htmlFor={`additionalEmergencyContacts.${index}.phone`}
                className="flex min-w-[160px] flex-1 flex-col gap-1"
              >
                <Input id={`additionalEmergencyContacts.${index}.phone`} {...register(`additionalEmergencyContacts.${index}.phone` as const)} />
              </Field>
              <Button
                type="button"
                variant="ghost"
                size="icon"
                aria-label={`Remove emergency contact ${index + 2}`}
                onClick={() => additionalEmergencyContacts.remove(index)}
              >
                <X className="h-4 w-4" />
              </Button>
            </div>
          ))}

          {additionalEmergencyContacts.fields.length < 2 && (
            <Button
              type="button"
              variant="outline"
              size="sm"
              className="w-fit gap-1.5"
              onClick={() => additionalEmergencyContacts.append({ relationship: 'Father', name: '', phone: '' })}
            >
              <Plus className="h-4 w-4" />
              Add Emergency Contact
            </Button>
          )}
        </FormSection>
        </TabsContent>

        <TabsContent value="medical-info" className="pt-4">
        <TabErrorSummary messages={tabMessages('medical-info')} />
        <FormSection id="allergy" title="Allergy Details">
          {allergies.length === 0 && !showAddAllergyRow && <p className="text-sm text-muted-foreground">No known allergies recorded.</p>}

          {allergies.map((allergy) => (
            <div key={allergy.id} className="flex flex-wrap items-center gap-3 rounded-md border border-border p-3">
              <span className="min-w-[100px] text-sm font-medium text-foreground">{allergy.allergyType}</span>
              <span className="flex-1 text-sm text-muted-foreground">{allergy.specify || '—'}</span>
              <span className="text-sm text-foreground">{allergy.severity === 'Severe' ? 'Severe / Life-Threatening' : allergy.severity}</span>
              <Button
                type="button"
                variant="ghost"
                size="icon"
                aria-label={`Remove ${allergy.allergyType} allergy`}
                disabled={removeAllergyMutation.isPending}
                onClick={() => handleRemoveAllergy(allergy.id)}
              >
                <X className="h-4 w-4" />
              </Button>
            </div>
          ))}

          {addAllergyError && (
            <p role="alert" className="text-sm text-destructive">
              {addAllergyError}
            </p>
          )}
          {addAllergyMutation.isError && (
            <p role="alert" className="text-sm text-destructive">
              Failed to add allergy — please try again.
            </p>
          )}
          {removeAllergyMutation.isError && (
            <p role="alert" className="text-sm text-destructive">
              Failed to remove allergy — please try again.
            </p>
          )}

          {showAddAllergyRow ? (
            <div className="flex flex-wrap items-end gap-3">
              <Field label="Type" htmlFor="new-allergy-type" className="flex w-full flex-col gap-1 sm:w-40">
                <Select
                  value={newAllergy.allergyType || undefined}
                  onValueChange={(value) => setNewAllergy((prev) => ({ ...prev, allergyType: value as AllergyType }))}
                >
                  <SelectTrigger id="new-allergy-type" aria-label="New allergy type">
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
              </Field>
              <Field label="Specify" htmlFor="new-allergy-specify" className="flex min-w-[200px] flex-1 flex-col gap-1">
                <Input
                  id="new-allergy-specify"
                  placeholder="e.g. Penicillin, Peanuts…"
                  value={newAllergy.specify}
                  onChange={(event) => setNewAllergy((prev) => ({ ...prev, specify: event.target.value }))}
                />
              </Field>
              <Field label="Severity" htmlFor="new-allergy-severity" className="flex w-full flex-col gap-1 sm:w-60">
                <Select
                  value={newAllergy.severity || undefined}
                  onValueChange={(value) => setNewAllergy((prev) => ({ ...prev, severity: value as AllergySeverity }))}
                >
                  <SelectTrigger id="new-allergy-severity" aria-label="New allergy severity">
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
              </Field>
              <Button type="button" onClick={handleAddAllergy} disabled={addAllergyMutation.isPending}>
                {addAllergyMutation.isPending ? 'Adding…' : 'Add'}
              </Button>
              <Button
                type="button"
                variant="ghost"
                onClick={() => {
                  setShowAddAllergyRow(false);
                  setAddAllergyError(null);
                  setNewAllergy({ allergyType: '', specify: '', severity: '' });
                }}
              >
                Cancel
              </Button>
            </div>
          ) : (
            <Button type="button" variant="outline" size="sm" className="w-fit gap-1.5" onClick={() => setShowAddAllergyRow(true)}>
              <Plus className="h-4 w-4" />
              Add another Allergy
            </Button>
          )}
        </FormSection>

        <FormSection id="id-proof" title="ID Proof">
          <div className="flex flex-wrap gap-3">
            <Field label="ID proof type" htmlFor="idProofType" className="flex w-full flex-col gap-1 sm:w-48">
              <Controller
                name="idProofType"
                control={control}
                render={({ field }) => (
                  <Select value={field.value} onValueChange={field.onChange}>
                    <SelectTrigger id="idProofType" aria-label="ID proof type">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {ID_PROOF_TYPES.map((type) => (
                        <SelectItem key={type} value={type}>
                          {type}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                )}
              />
            </Field>
            <Field label={`${idProofType} number`} htmlFor="idProofNumber" className="flex min-w-[200px] flex-1 flex-col gap-1">
              <Input id="idProofNumber" {...register('idProofNumber')} />
            </Field>
          </div>
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
