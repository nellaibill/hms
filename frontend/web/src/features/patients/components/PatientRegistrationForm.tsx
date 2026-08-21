import {
  ADMISSION_TYPES,
  ALLERGY_CATEGORIES,
  ALLERGY_SEVERITIES,
  ApiError,
  ARRIVAL_SOURCE_CATEGORIES,
  BLOOD_GROUPS,
  ENCOUNTER_TYPES,
  OFFLINE_AD_CHANNELS,
  ONLINE_AD_CHANNELS,
  PATIENT_GENDERS,
  PATIENT_RELATIVE_REFERRAL_SOURCES,
  patientRegistrationUiSchema,
  PHONE_RELATIONS,
  REFERRAL_COLUMN_CATEGORIES,
  RELATIONSHIPS,
  TITLES,
  type PatientRegistrationUiFormValues,
} from '@hms/shared';
import { zodResolver } from '@hookform/resolvers/zod';
import { ChevronLeft, ChevronRight, ClipboardList, MapPin, Plus, Receipt, Stethoscope, User, X } from 'lucide-react';
import { useEffect, useRef, useState } from 'react';
import { Controller, useController, useFieldArray, useForm, type Control, type FieldErrors } from 'react-hook-form';
import { useNavigate } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { AppointmentTypeSelect } from '@/components/AppointmentTypeSelect';
import { ConsultantSelect } from '@/components/ConsultantSelect';
import { DepartmentSelect } from '@/components/DepartmentSelect';
import { BillingStep, defaultBillingFormValues, type BillingFormValues, type BillingStepHandle } from '@/features/billing';
import { DocumentUploadStaging, emptyStagedDocuments, type StagedDocuments } from './DocumentUploadStaging';
import { Field, FormSection } from './FormSection';
import { bloodGroupLabel } from '../bloodGroupLabel';
import { calculateDetailedAge } from '../detailedAge';
import { humanize } from '../humanize';
import { loadRegistrationDraft, saveRegistrationDraft } from '../registrationDraft';

interface PatientRegistrationFormProps {
  isSubmitting: boolean;
  apiError: ApiError | null;
  onSubmit: (values: PatientRegistrationUiFormValues, documents: StagedDocuments, billing: BillingFormValues) => void;
}

const TAB_ORDER = ['patient-info', 'contact-info', 'medical-info', 'registration-details', 'billing'] as const;
type TabId = (typeof TAB_ORDER)[number];

// Which top-level form fields live on each of the four *patient* tabs — used to jump to the
// first tab with an error on a failed submit, and to flag tabs with a red dot so an error on
// a tab the user isn't currently viewing doesn't silently block submission with no visible
// cause. Billing lives in its own useForm (see BillingStep) and is validated/flagged
// separately — see billingStepRef and billingTabHasError below.
const TAB_ERROR_FIELDS: Record<Exclude<TabId, 'billing'>, (keyof PatientRegistrationUiFormValues)[]> = {
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
  'medical-info': ['hasKnownAllergy', 'allergyCategory', 'allergySpecify', 'allergySeverity', 'arrivalSource'],
  'registration-details': ['registration'],
};

const PATIENT_TAB_ORDER = TAB_ORDER.filter((tab): tab is Exclude<TabId, 'billing'> => tab !== 'billing');

function tabWithFirstError(errors: FieldErrors<PatientRegistrationUiFormValues>): TabId | null {
  return PATIENT_TAB_ORDER.find((tab) => TAB_ERROR_FIELDS[tab].some((field) => Boolean(errors[field]))) ?? null;
}

function isTabId(value: string): value is TabId {
  return (TAB_ORDER as readonly string[]).includes(value);
}

/** How long to wait after the user stops typing before writing the draft to localStorage — avoids a write on every keystroke. */
const DRAFT_SAVE_DEBOUNCE_MS = 400;

const defaultValues: PatientRegistrationUiFormValues = {
  title: 'Mr',
  firstName: '',
  lastName: '',
  dateOfBirth: '',
  gender: 'Male',
  bloodGroup: 'Unknown',
  addressLine1: '',
  addressLine2: '',
  addressLine3: '',
  district: '',
  state: '',
  pincode: '',
  primaryPhone: { number: '', relation: 'Self' },
  additionalPhones: [],
  email: '',
  profession: '',
  emergencyContactRelationship: 'Father',
  emergencyContactName: '',
  emergencyContactPhone: '',
  hasKnownAllergy: false,
  allergyCategory: '',
  allergySpecify: '',
  allergySeverity: '',
  arrivalSource: { category: 'DoctorReferral' },
  registration: {
    encounterType: 'OP',
    departmentId: '',
    consultantId: '',
    additionalConsultants: [],
    appointmentTypeId: '',
    admissionType: '',
    category: '',
  },
};

/**
 * New Patient Registration — matches LH Software.docx's Reception & Registration table
 * and the standalone Patient Mode of Arrival Form field-for-field. Grouped into four top
 * tabs (Patient / Contact / Medical / Registration Details) rather than one long scrolling
 * page (not the full spec's hard-gated/autosaving wizard — see docs/DecisionLog.md).
 *
 * UI-only per the current phase: the submitted request is bridged to the *existing*
 * backend Contracts by the caller's toRequest() (see PatientRegistrationCreatePage) —
 * some fields collected here (Transgender/NA gender, the 2nd additional phone's relation,
 * OP appointment type, structured referral/arrival-source) don't have a backend field to
 * persist into yet and are composed/defaulted/dropped there until backend Phase 2.
 */
function isPatientTabId(value: TabId): value is Exclude<TabId, 'billing'> {
  return value !== 'billing';
}

export function PatientRegistrationForm({ isSubmitting, apiError, onSubmit }: PatientRegistrationFormProps) {
  const navigate = useNavigate();

  // Loaded once per mount — a fresh visit to "New Patient Registration" picks up any draft
  // left behind by a refresh or accidental navigation away, so nothing already entered is lost.
  const [initialDraft] = useState(() => loadRegistrationDraft());

  const {
    register,
    control,
    handleSubmit,
    watch,
    trigger,
    getValues,
    setValue,
    formState: { errors },
  } = useForm<PatientRegistrationUiFormValues>({
    resolver: zodResolver(patientRegistrationUiSchema),
    defaultValues: initialDraft?.values ?? defaultValues,
    mode: 'onChange',
  });

  const additionalPhones = useFieldArray({ control, name: 'additionalPhones' });
  // UI-only demo affordance — see additionalConsultantSchema's own doc comment for why this
  // never reaches CreatePatientRequest.
  const additionalConsultants = useFieldArray({ control, name: 'registration.additionalConsultants' });
  const [documents, setDocuments] = useState<StagedDocuments>(emptyStagedDocuments);

  // Billing (step 5) owns its own useForm — see BillingStep — so it's driven through this
  // imperative handle (validate/getValues) rather than being a field on this form's schema.
  const billingStepRef = useRef<BillingStepHandle>(null);
  const [billingTabHasError, setBillingTabHasError] = useState(false);
  // Shown only right after a submit attempt gets blocked by invalid Billing sections — a
  // silent tab-jump doesn't tell a first-time user *why* they landed back on Billing.
  // Cleared as soon as the user starts fixing it (see handleBillingChange).
  const [billingBlockedSubmit, setBillingBlockedSubmit] = useState(false);
  // Set only if goToTab's validation step throws (e.g. malformed data left over from an
  // old draft) — without this, that failure left Next looking like it silently did nothing:
  // no tab change, no error, nothing (see goToTab below).
  const [navigationError, setNavigationError] = useState<string | null>(null);

  const [activeTab, setActiveTab] = useState<TabId>(
    initialDraft && isTabId(initialDraft.activeTab) ? initialDraft.activeTab : 'patient-info',
  );
  // Previous/Next (goToPreviousTab/goToTab below) set activeTab directly on this component's
  // own state, bypassing <Tabs>'s internal setValue — which is the only place that resets
  // scroll position (see tabs.tsx's own comment on that same bug for a direct tab-header
  // click). Without this, clicking Next while scrolled down a long tab (e.g. Medical
  // Information's Allergy/Mode of Arrival/Document Upload) leaves the page scrolled to that
  // same pixel offset on the new, often-shorter tab — landing past its fields entirely and
  // looking exactly like Next silently did nothing (confirmed live: the tab really does
  // change underneath, just off-screen). Runs for every activeTab change (including
  // tab-header clicks, where <Tabs> already does this too) since either source is just as
  // capable of leaving the page scrolled past the new tab's content.
  //
  // 'instant', not 'smooth': confirmed live that a 'smooth' scrollIntoView call here
  // frequently produces zero net scroll movement — the animation appears to get cancelled by
  // a layout shift immediately following the tab swap (a large block of the DOM gets replaced
  // by TabsContent's conditional render right as this runs). 'instant' isn't interruptible.
  const formTopRef = useRef<HTMLDivElement>(null);
  useEffect(() => {
    formTopRef.current?.scrollIntoView({ behavior: 'instant', block: 'start' });
  }, [activeTab]);
  // Tabs the user has actually tried to leave (via Next) or a final submit attempt — a tab
  // the user hasn't reached yet shouldn't show an error dot just because its untouched
  // required fields are technically invalid.
  const [attemptedTabs, setAttemptedTabs] = useState<ReadonlySet<TabId>>(new Set());
  const activeTabIndex = TAB_ORDER.indexOf(activeTab);
  const isFirstTab = activeTabIndex === 0;
  const isLastTab = activeTabIndex === TAB_ORDER.length - 1;
  const goToPreviousTab = () => setActiveTab(TAB_ORDER[activeTabIndex - 1]);

  // Autosave the draft — a debounced write on every field change (from either the patient
  // form or the separate billing form), plus an immediate write whenever the active tab
  // changes (so refreshing right after clicking Next doesn't lose the step position even
  // before the next keystroke on the new tab). Both forms write into the same draftRef so a
  // single JSON blob always reflects the latest of both.
  const draftRef = useRef({
    values: initialDraft?.values ?? defaultValues,
    billing: initialDraft?.billing ?? defaultBillingFormValues,
  });
  const activeTabRef = useRef(activeTab);
  activeTabRef.current = activeTab;
  const saveTimeoutRef = useRef<ReturnType<typeof setTimeout>>();
  const scheduleDraftSave = () => {
    clearTimeout(saveTimeoutRef.current);
    saveTimeoutRef.current = setTimeout(() => {
      saveRegistrationDraft(draftRef.current.values, draftRef.current.billing, activeTabRef.current);
    }, DRAFT_SAVE_DEBOUNCE_MS);
  };
  useEffect(() => {
    const subscription = watch((values) => {
      draftRef.current.values = values as PatientRegistrationUiFormValues;
      scheduleDraftSave();
    });
    return () => {
      clearTimeout(saveTimeoutRef.current);
      subscription.unsubscribe();
    };
  }, [watch]);
  const handleBillingChange = (values: BillingFormValues) => {
    draftRef.current.billing = values;
    scheduleDraftSave();
    // The user is actively fixing Billing — the "you were sent back here" banner has done its job.
    setBillingBlockedSubmit(false);
  };
  useEffect(() => {
    saveRegistrationDraft(getValues(), draftRef.current.billing, activeTab);
    // eslint-disable-next-line react-hooks/exhaustive-deps -- only re-save immediately on a tab change, not on every getValues identity change.
  }, [activeTab]);

  // Validates every *patient* tab from the current one up to (but not including) the target
  // before landing on it — covers both the Next button and clicking a tab header directly,
  // so jumping straight to "Registration Details" doesn't skip validating the tabs in
  // between. Moving backward to an already-visited tab is always allowed with no validation.
  // Billing has no fields on this form, so it's simply skipped here — its own validation
  // gate runs separately, on final submit (see onValidSubmit below).
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
        if (!isPatientTabId(tab)) continue;
        const isTabValid = await trigger(TAB_ERROR_FIELDS[tab]);
        setAttemptedTabs((prev) => new Set(prev).add(tab));
        if (!isTabValid) {
          setActiveTab(tab);
          return;
        }
      }
      setActiveTab(target);
    } catch {
      // Without this, a thrown validation error left Next looking like it did nothing —
      // no tab change, no message, nothing — since goToNextTab's onClick doesn't await or
      // catch this promise. Surfacing something is better than the silent failure.
      setNavigationError('Something went wrong checking this step. Please try again — if it keeps happening, refresh the page.');
    }
  };
  const goToNextTab = () => goToTab(TAB_ORDER[activeTabIndex + 1]);

  const onInvalid = (invalidFields: FieldErrors<PatientRegistrationUiFormValues>) => {
    // A submit attempt validates the whole form, so every tab is now "attempted" regardless
    // of whether the user ever visited it via Next.
    setAttemptedTabs(new Set(TAB_ORDER));
    const firstErroredTab = tabWithFirstError(invalidFields);
    if (firstErroredTab) setActiveTab(firstErroredTab);
  };

  // Runs once the *patient* form itself is valid — still needs to gate on Billing (its own
  // form) before actually registering, since Billing can't proceed with invalid expanded
  // cards either. On a billing error, jump there and let its own error dots take over.
  const onValidSubmit = async (values: PatientRegistrationUiFormValues) => {
    const billingValid = await billingStepRef.current?.validate();
    if (!billingValid) {
      setActiveTab('billing');
      setBillingBlockedSubmit(true);
      return;
    }
    setBillingBlockedSubmit(false);
    onSubmit(values, documents, billingStepRef.current!.getValues());
  };

  const dateOfBirth = watch('dateOfBirth');
  const detailedAge = dateOfBirth ? calculateDetailedAge(dateOfBirth) : null;

  const hasKnownAllergy = watch('hasKnownAllergy');
  const arrivalCategory = watch('arrivalSource.category');
  const patientRelativeSource = watch('arrivalSource.patientRelativeReferral.source');
  const onlineChannel = watch('arrivalSource.onlineAd.channel');
  const offlineChannel = watch('arrivalSource.offlineAd.channel');
  const encounterType = watch('registration.encounterType');
  const isIpOrEmergency = encounterType === 'IP' || encounterType === 'Emergency';
  const isDayCare = encounterType === 'DayCare';
  const showReferralColumn = isIpOrEmergency || isDayCare;
  const registrationDepartmentId = watch('registration.departmentId');

  // A consultant picked under the previous department is meaningless once the department
  // changes — same reasoning as DispenseCartForm resetting Batch when Product changes.
  function handleDepartmentChange(newDepartmentId: string, onChange: (value: string) => void) {
    onChange(newDepartmentId);
    setValue('registration.consultantId', '');
  }

  // Server-side validation errors can't be mapped 1:1 to this form's field paths — the
  // submitted request is bridged/composed into the backend's narrower DTO shape by the
  // caller (see toRequest() in PatientRegistrationCreatePage), so a server field name
  // like "Registration.Department" doesn't correspond to a single form control here.
  // Surfaced as a general list instead of per-field errors.
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

  return (
    <div ref={formTopRef} className="flex w-full max-w-6xl scroll-mt-20 flex-col gap-5">
      <form
        onSubmit={handleSubmit(onValidSubmit, onInvalid)}
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
          <TabsTrigger
            value="registration-details"
            hasError={attemptedTabs.has('registration-details') && TAB_ERROR_FIELDS['registration-details'].some((f) => Boolean(errors[f]))}
          >
            <ClipboardList className="h-4 w-4" />
            Registration Details
          </TabsTrigger>
          <TabsTrigger value="billing" hasError={billingTabHasError}>
            <Receipt className="h-4 w-4" />
            Billing
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
              <Field
                label="Specify"
                htmlFor="allergySpecify"
                error={errors.allergySpecify?.message}
                className="flex min-w-[200px] flex-1 flex-col gap-1"
              >
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

        <FormSection id="mode-of-arrival" title="Mode of Arrival" description="How the patient found or was referred to the hospital.">
          <div className="flex flex-wrap gap-3">
            <Field
              label="Source"
              htmlFor="arrivalCategory"
              error={errors.arrivalSource?.category?.message}
              className="flex w-full flex-col gap-1 sm:w-56"
            >
              <Controller
                name="arrivalSource.category"
                control={control}
                render={({ field }) => (
                  <Select value={field.value} onValueChange={field.onChange}>
                    <SelectTrigger id="arrivalCategory" aria-label="Mode of arrival source">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {ARRIVAL_SOURCE_CATEGORIES.map((c) => (
                        <SelectItem key={c} value={c}>
                          {humanize(c)}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                )}
              />
            </Field>

            {arrivalCategory === 'DoctorReferral' && (
              <>
                <Field
                  label="Doctor name"
                  htmlFor="doctorReferralName"
                  error={errors.arrivalSource?.doctorReferral?.doctorName?.message}
                  className="flex min-w-[160px] flex-1 flex-col gap-1"
                >
                  <Input id="doctorReferralName" {...register('arrivalSource.doctorReferral.doctorName')} />
                </Field>
                <Field label="Department" htmlFor="doctorReferralDepartment" className="flex min-w-[160px] flex-1 flex-col gap-1">
                  <Input id="doctorReferralDepartment" {...register('arrivalSource.doctorReferral.department')} />
                </Field>
                <Field label="Hospital" htmlFor="doctorReferralHospital" className="flex min-w-[160px] flex-1 flex-col gap-1">
                  <Input id="doctorReferralHospital" {...register('arrivalSource.doctorReferral.hospital')} />
                </Field>
              </>
            )}

            {arrivalCategory === 'PatientOrRelativeReferral' && (
              <>
                <Field
                  label="Source"
                  htmlFor="patientRelativeSource"
                  error={errors.arrivalSource?.patientRelativeReferral?.source?.message}
                  className="flex w-full flex-col gap-1 sm:w-48"
                >
                  <Controller
                    name="arrivalSource.patientRelativeReferral.source"
                    control={control}
                    render={({ field }) => (
                      <Select value={field.value} onValueChange={field.onChange}>
                        <SelectTrigger id="patientRelativeSource" aria-label="Patient or relative referral source">
                          <SelectValue placeholder="Select" />
                        </SelectTrigger>
                        <SelectContent>
                          {PATIENT_RELATIVE_REFERRAL_SOURCES.map((s) => (
                            <SelectItem key={s} value={s}>
                              {humanize(s)}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    )}
                  />
                </Field>
                {patientRelativeSource === 'Other' && (
                  <Field
                    label="Please specify"
                    htmlFor="patientRelativeDetails"
                    error={errors.arrivalSource?.patientRelativeReferral?.details?.message}
                    className="flex min-w-[200px] flex-1 flex-col gap-1 sm:max-w-md"
                  >
                    <Input id="patientRelativeDetails" {...register('arrivalSource.patientRelativeReferral.details')} />
                  </Field>
                )}
              </>
            )}

            {arrivalCategory === 'OnlineAdvertisement' && (
              <>
                <Field
                  label="Channel"
                  htmlFor="onlineChannel"
                  error={errors.arrivalSource?.onlineAd?.channel?.message}
                  className="flex w-full flex-col gap-1 sm:w-48"
                >
                  <Controller
                    name="arrivalSource.onlineAd.channel"
                    control={control}
                    render={({ field }) => (
                      <Select value={field.value} onValueChange={field.onChange}>
                        <SelectTrigger id="onlineChannel" aria-label="Online advertisement channel">
                          <SelectValue placeholder="Select" />
                        </SelectTrigger>
                        <SelectContent>
                          {ONLINE_AD_CHANNELS.map((c) => (
                            <SelectItem key={c} value={c}>
                              {humanize(c)}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    )}
                  />
                </Field>
                {onlineChannel === 'Other' && (
                  <Field
                    label="Please specify"
                    htmlFor="onlineDetails"
                    error={errors.arrivalSource?.onlineAd?.details?.message}
                    className="flex min-w-[200px] flex-1 flex-col gap-1 sm:max-w-md"
                  >
                    <Input id="onlineDetails" {...register('arrivalSource.onlineAd.details')} />
                  </Field>
                )}
              </>
            )}

            {arrivalCategory === 'OfflineAdvertisement' && (
              <>
                <Field
                  label="Channel"
                  htmlFor="offlineChannel"
                  error={errors.arrivalSource?.offlineAd?.channel?.message}
                  className="flex w-full flex-col gap-1 sm:w-56"
                >
                  <Controller
                    name="arrivalSource.offlineAd.channel"
                    control={control}
                    render={({ field }) => (
                      <Select value={field.value} onValueChange={field.onChange}>
                        <SelectTrigger id="offlineChannel" aria-label="Offline advertisement channel">
                          <SelectValue placeholder="Select" />
                        </SelectTrigger>
                        <SelectContent>
                          {OFFLINE_AD_CHANNELS.map((c) => (
                            <SelectItem key={c} value={c}>
                              {humanize(c)}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    )}
                  />
                </Field>
                {offlineChannel === 'Other' && (
                  <Field
                    label="Please specify"
                    htmlFor="offlineDetails"
                    error={errors.arrivalSource?.offlineAd?.details?.message}
                    className="flex min-w-[200px] flex-1 flex-col gap-1 sm:max-w-md"
                  >
                    <Input id="offlineDetails" {...register('arrivalSource.offlineAd.details')} />
                  </Field>
                )}
              </>
            )}
          </div>
        </FormSection>

        <FormSection id="document-upload" title="Document Upload" description="Optional — the patient photo and ID proof are uploaded once the record is saved.">
          <DocumentUploadStaging value={documents} onChange={setDocuments} />
        </FormSection>
        </TabsContent>

        <TabsContent value="registration-details" className="pt-4">
        <FormSection id="registration-details" title="Registration / Encounter Details">
          <div className="flex flex-wrap gap-3">
            <Field label="Encounter type" htmlFor="encounterType" className="flex w-full flex-col gap-1 sm:w-56">
              <Controller
                name="registration.encounterType"
                control={control}
                render={({ field }) => (
                  <Select value={field.value} onValueChange={field.onChange}>
                    <SelectTrigger id="encounterType" aria-label="Encounter type">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {ENCOUNTER_TYPES.map((e) => (
                        <SelectItem key={e} value={e}>
                          {e === 'DayCare' ? 'Day-care / Observation' : e}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                )}
              />
            </Field>
            {encounterType === 'OP' && (
              <Field
                label="Appointment type"
                htmlFor="appointmentType"
                error={errors.registration?.appointmentTypeId?.message}
                className="flex min-w-[160px] flex-1 flex-col gap-1"
              >
                <Controller
                  name="registration.appointmentTypeId"
                  control={control}
                  render={({ field }) => (
                    <AppointmentTypeSelect id="appointmentType" value={field.value ?? ''} onValueChange={field.onChange} />
                  )}
                />
              </Field>
            )}
            <Field
              label="Department"
              htmlFor="department"
              error={errors.registration?.departmentId?.message}
              className="flex min-w-[160px] flex-1 flex-col gap-1"
            >
              <Controller
                name="registration.departmentId"
                control={control}
                render={({ field }) => (
                  <DepartmentSelect
                    id="department"
                    value={field.value}
                    onValueChange={(value) => handleDepartmentChange(value, field.onChange)}
                  />
                )}
              />
            </Field>
            <Field
              label="Consultant"
              htmlFor="consultant"
              error={errors.registration?.consultantId?.message}
              className="flex min-w-[160px] flex-1 flex-col gap-1"
            >
              <Controller
                name="registration.consultantId"
                control={control}
                render={({ field }) => (
                  <ConsultantSelect
                    id="consultant"
                    value={field.value}
                    onValueChange={field.onChange}
                    departmentId={registrationDepartmentId}
                  />
                )}
              />
            </Field>
            {/* Progressive disclosure: Admission (IP/Emergency) / Observation (Day-care) type only applies beyond OP. */}
            {showReferralColumn && (
              <Field
                label={isDayCare ? 'Observation type' : 'Admission type'}
                htmlFor="admissionType"
                error={errors.registration?.admissionType?.message}
                className="flex w-full flex-col gap-1 sm:w-40"
              >
                <Controller
                  name="registration.admissionType"
                  control={control}
                  render={({ field }) => (
                    <Select value={field.value || undefined} onValueChange={field.onChange}>
                      <SelectTrigger id="admissionType" aria-label={isDayCare ? 'Observation type' : 'Admission type'}>
                        <SelectValue placeholder="Select" />
                      </SelectTrigger>
                      <SelectContent>
                        {ADMISSION_TYPES.map((a) => (
                          <SelectItem key={a} value={a}>
                            {a}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  )}
                />
              </Field>
            )}
            {showReferralColumn && (
              <Field label="Category" htmlFor="category" className="flex w-full flex-col gap-1 sm:w-56">
                <Input id="category" {...register('registration.category')} />
              </Field>
            )}
          </div>

          {additionalConsultants.fields.map((field, index) => (
            <AdditionalConsultantRow
              key={field.id}
              control={control}
              index={index}
              onRemove={() => additionalConsultants.remove(index)}
            />
          ))}

          {additionalConsultants.fields.length < 3 && (
            <Button
              type="button"
              variant="outline"
              size="sm"
              className="w-fit gap-1.5"
              onClick={() => additionalConsultants.append({ departmentId: '', consultantId: '' })}
            >
              <Plus className="h-4 w-4" />
              Add another Consultant
            </Button>
          )}

          {showReferralColumn && (
            <div className="flex flex-wrap gap-3 rounded-md border border-dashed border-border p-3">
              <Field label="Referral category" htmlFor="referralCategory" className="flex w-full flex-col gap-1 sm:w-36">
                <Controller
                  name="registration.referral.category"
                  control={control}
                  render={({ field }) => (
                    <Select value={field.value} onValueChange={field.onChange}>
                      <SelectTrigger id="referralCategory" aria-label="Referral category">
                        <SelectValue placeholder="Select" />
                      </SelectTrigger>
                      <SelectContent>
                        {REFERRAL_COLUMN_CATEGORIES.map((c) => (
                          <SelectItem key={c} value={c}>
                            {c}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  )}
                />
              </Field>
              <Field label="Details" htmlFor="referralDetails" className="flex min-w-[160px] flex-1 flex-col gap-1 sm:max-w-md">
                <Input id="referralDetails" {...register('registration.referral.details')} />
              </Field>
              <Field
                label="Contact number"
                htmlFor="referralContactNumber"
                className="flex w-full flex-col gap-1 sm:w-48"
              >
                <Input id="referralContactNumber" {...register('registration.referral.contactNumber')} />
              </Field>
            </div>
          )}
        </FormSection>
        </TabsContent>

        <TabsContent value="billing" className="pt-4">
          {billingBlockedSubmit && (
            <div role="alert" className="mb-4 rounded-md bg-warning/15 px-3 py-2 text-sm text-foreground">
              Complete the highlighted billing sections below before registering the patient.
            </div>
          )}
          <BillingStep
            defaultValues={initialDraft?.billing}
            onChange={handleBillingChange}
            onErrorStateChange={setBillingTabHasError}
            ref={billingStepRef}
          />
        </TabsContent>
      </Tabs>

        <div className="sticky bottom-0 z-10 -mx-4 flex items-center justify-between gap-3 border-t border-border bg-background/95 px-4 py-3 backdrop-blur supports-[backdrop-filter]:bg-background/80">
          <Button type="button" variant="outline" onClick={() => navigate('/patients/registration')}>
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
                {isSubmitting ? 'Registering…' : 'Register Patient'}
              </Button>
            )}
          </div>
        </div>
      </form>
    </div>
  );
}

interface AdditionalConsultantRowProps {
  control: Control<PatientRegistrationUiFormValues>;
  index: number;
  onRemove: () => void;
}

/** One "Add another Consultant" row — its own Department + Consultant pair, independent of
 * the primary one above and of every other additional row. Uses useController (the hook
 * form of Controller) rather than two <Controller>s so handleRowDepartmentChange can reach
 * both fields' onChange directly, the same "clear the child when the parent changes"
 * behavior the primary Department/Consultant pair and Ward/Bed already have elsewhere in
 * this app — just without a wrapping component to hang a handler function off of here. */
function AdditionalConsultantRow({ control, index, onRemove }: AdditionalConsultantRowProps) {
  const departmentField = useController({ control, name: `registration.additionalConsultants.${index}.departmentId` as const });
  const consultantField = useController({ control, name: `registration.additionalConsultants.${index}.consultantId` as const });

  function handleRowDepartmentChange(value: string) {
    departmentField.field.onChange(value);
    consultantField.field.onChange('');
  }

  return (
    <div className="flex flex-wrap items-end gap-3 rounded-md border border-dashed border-border p-3">
      <Field
        label={`Department ${index + 2}`}
        htmlFor={`additional-department-${index}`}
        className="flex min-w-[160px] flex-1 flex-col gap-1"
      >
        <DepartmentSelect
          id={`additional-department-${index}`}
          value={departmentField.field.value ?? ''}
          onValueChange={handleRowDepartmentChange}
        />
      </Field>
      <Field
        label={`Consultant ${index + 2}`}
        htmlFor={`additional-consultant-${index}`}
        className="flex min-w-[160px] flex-1 flex-col gap-1"
      >
        <ConsultantSelect
          id={`additional-consultant-${index}`}
          value={consultantField.field.value ?? ''}
          onValueChange={consultantField.field.onChange}
          departmentId={departmentField.field.value || undefined}
        />
      </Field>
      <Button type="button" variant="ghost" size="icon" aria-label={`Remove consultant ${index + 2}`} onClick={onRemove}>
        <X className="h-4 w-4" />
      </Button>
    </div>
  );
}
