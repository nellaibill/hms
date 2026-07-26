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
import { Plus, X } from 'lucide-react';
import { Controller, useFieldArray, useForm } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Field, FormSection } from './FormSection';
import { bloodGroupLabel } from '../bloodGroupLabel';
import { humanize } from '../humanize';
import { SectionNav, type SectionNavItem } from './SectionNav';

interface PatientRegistrationFormProps {
  isSubmitting: boolean;
  apiError: ApiError | null;
  onSubmit: (values: PatientRegistrationUiFormValues) => void;
}

const sections: SectionNavItem[] = [
  { id: 'demographics', label: 'Demographics' },
  { id: 'address', label: 'Address' },
  { id: 'contact', label: 'Contact Details' },
  { id: 'emergency-contact', label: 'Emergency Contact' },
  { id: 'allergy', label: 'Allergy Details' },
  { id: 'mode-of-arrival', label: 'Mode of Arrival' },
  { id: 'registration-details', label: 'Registration Details' },
];

const defaultValues: PatientRegistrationUiFormValues = {
  title: 'Mr',
  firstName: '',
  lastName: '',
  dateOfBirth: '',
  gender: 'Male',
  bloodGroup: '',
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
    department: '',
    consultant: '',
    appointmentType: '',
    admissionType: '',
    category: '',
  },
};

/**
 * New Patient Registration — matches LH Software.docx's Reception & Registration table
 * and the standalone Patient Mode of Arrival Form field-for-field. Still one scrolling
 * form (not the full spec's hard-gated/autosaving wizard — see docs/DecisionLog.md), with
 * a lightweight SectionNav for quick jumping between the seven sections below.
 *
 * UI-only per the current phase: the submitted request is bridged to the *existing*
 * backend Contracts by the caller's toRequest() (see PatientRegistrationCreatePage) —
 * some fields collected here (Transgender/NA gender, the 2nd additional phone's relation,
 * OP appointment type, structured referral/arrival-source) don't have a backend field to
 * persist into yet and are composed/defaulted/dropped there until backend Phase 2.
 */
export function PatientRegistrationForm({ isSubmitting, apiError, onSubmit }: PatientRegistrationFormProps) {
  const {
    register,
    control,
    handleSubmit,
    watch,
    formState: { errors },
  } = useForm<PatientRegistrationUiFormValues>({
    resolver: zodResolver(patientRegistrationUiSchema),
    defaultValues,
  });

  const additionalPhones = useFieldArray({ control, name: 'additionalPhones' });

  const hasKnownAllergy = watch('hasKnownAllergy');
  const arrivalCategory = watch('arrivalSource.category');
  const patientRelativeSource = watch('arrivalSource.patientRelativeReferral.source');
  const onlineChannel = watch('arrivalSource.onlineAd.channel');
  const offlineChannel = watch('arrivalSource.offlineAd.channel');
  const encounterType = watch('registration.encounterType');
  const isIpOrEmergency = encounterType === 'IP' || encounterType === 'Emergency';
  const isDayCare = encounterType === 'DayCare';
  const showReferralColumn = isIpOrEmergency || isDayCare;

  // Server-side validation errors can't be mapped 1:1 to this form's field paths — the
  // submitted request is bridged/composed into the backend's narrower DTO shape by the
  // caller (see toRequest() in PatientRegistrationCreatePage), so a server field name
  // like "Registration.Department" doesn't correspond to a single form control here.
  // Surfaced as a general list instead of per-field errors.
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
              <Field label="Severity" htmlFor="allergySeverity" error={errors.allergySeverity?.message} className="flex flex-col gap-1.5 sm:col-span-3 sm:w-1/3">
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

        <FormSection
          id="mode-of-arrival"
          title="Mode of Arrival"
          description="How the patient found or was referred to the hospital (per the Patient Mode of Arrival Form)."
        >
          <Field label="Source" htmlFor="arrivalCategory" className="flex flex-col gap-1.5 sm:w-1/2">
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
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
              <Field
                label="Doctor name"
                htmlFor="doctorReferralName"
                error={errors.arrivalSource?.doctorReferral?.doctorName?.message}
              >
                <Input id="doctorReferralName" {...register('arrivalSource.doctorReferral.doctorName')} />
              </Field>
              <Field label="Department" htmlFor="doctorReferralDepartment">
                <Input id="doctorReferralDepartment" {...register('arrivalSource.doctorReferral.department')} />
              </Field>
              <Field label="Hospital" htmlFor="doctorReferralHospital">
                <Input id="doctorReferralHospital" {...register('arrivalSource.doctorReferral.hospital')} />
              </Field>
            </div>
          )}

          {arrivalCategory === 'PatientOrRelativeReferral' && (
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <Field
                label="Source"
                htmlFor="patientRelativeSource"
                error={errors.arrivalSource?.patientRelativeReferral?.source?.message}
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
                >
                  <Input id="patientRelativeDetails" {...register('arrivalSource.patientRelativeReferral.details')} />
                </Field>
              )}
            </div>
          )}

          {arrivalCategory === 'OnlineAdvertisement' && (
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <Field label="Channel" htmlFor="onlineChannel" error={errors.arrivalSource?.onlineAd?.channel?.message}>
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
                <Field label="Please specify" htmlFor="onlineDetails" error={errors.arrivalSource?.onlineAd?.details?.message}>
                  <Input id="onlineDetails" {...register('arrivalSource.onlineAd.details')} />
                </Field>
              )}
            </div>
          )}

          {arrivalCategory === 'OfflineAdvertisement' && (
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <Field label="Channel" htmlFor="offlineChannel" error={errors.arrivalSource?.offlineAd?.channel?.message}>
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
                <Field label="Please specify" htmlFor="offlineDetails" error={errors.arrivalSource?.offlineAd?.details?.message}>
                  <Input id="offlineDetails" {...register('arrivalSource.offlineAd.details')} />
                </Field>
              )}
            </div>
          )}
        </FormSection>

        <FormSection id="registration-details" title="Registration / Encounter Details">
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <Field label="Encounter type" htmlFor="encounterType">
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
              <Field label="Appointment type" htmlFor="appointmentType">
                <Input id="appointmentType" {...register('registration.appointmentType')} />
              </Field>
            )}
          </div>

          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <Field label="Department" htmlFor="department" error={errors.registration?.department?.message}>
              <Input id="department" {...register('registration.department')} />
            </Field>
            <Field label="Consultant" htmlFor="consultant" error={errors.registration?.consultant?.message}>
              <Input id="consultant" {...register('registration.consultant')} />
            </Field>
          </div>

          {/* Progressive disclosure: Admission (IP/Emergency) / Observation (Day-care) type only applies beyond OP. */}
          {showReferralColumn && (
            <div className="flex flex-col gap-1.5 sm:w-1/2">
              <Field
                label={isDayCare ? 'Observation type' : 'Admission type'}
                htmlFor="admissionType"
                error={errors.registration?.admissionType?.message}
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
            </div>
          )}

          {showReferralColumn && (
            <div className="grid grid-cols-1 gap-4 rounded-md border border-dashed border-border p-4 sm:grid-cols-3">
              <Field label="Referral category" htmlFor="referralCategory" className="flex flex-col gap-1.5 sm:col-span-1">
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
              <Field label="Details" htmlFor="referralDetails">
                <Input id="referralDetails" {...register('registration.referral.details')} />
              </Field>
              <Field label="Contact number" htmlFor="referralContactNumber">
                <Input id="referralContactNumber" {...register('registration.referral.contactNumber')} />
              </Field>
            </div>
          )}

          {showReferralColumn && (
            <Field label="Category" htmlFor="category" className="flex flex-col gap-1.5 sm:w-1/2">
              <Input id="category" {...register('registration.category')} />
            </Field>
          )}
        </FormSection>

        <Button type="submit" disabled={isSubmitting} className="self-start">
          {isSubmitting ? 'Registering…' : 'Register Patient'}
        </Button>
      </form>
    </div>
  );
}
