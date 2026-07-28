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
import { ClipboardList, MapPin, Plus, Stethoscope, User, X } from 'lucide-react';
import { Controller, useFieldArray, useForm } from 'react-hook-form';
import { useNavigate } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Field, FormSection } from './FormSection';
import { bloodGroupLabel } from '../bloodGroupLabel';
import { calculateDetailedAge } from '../detailedAge';
import { humanize } from '../humanize';

interface PatientRegistrationFormProps {
  isSubmitting: boolean;
  apiError: ApiError | null;
  onSubmit: (values: PatientRegistrationUiFormValues) => void;
}

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
export function PatientRegistrationForm({ isSubmitting, apiError, onSubmit }: PatientRegistrationFormProps) {
  const navigate = useNavigate();
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

  // Server-side validation errors can't be mapped 1:1 to this form's field paths — the
  // submitted request is bridged/composed into the backend's narrower DTO shape by the
  // caller (see toRequest() in PatientRegistrationCreatePage), so a server field name
  // like "Registration.Department" doesn't correspond to a single form control here.
  // Surfaced as a general list instead of per-field errors.
  const generalError = apiError?.message ?? null;
  const serverValidationMessages = apiError?.validationErrors?.map((issue) => issue.message) ?? [];

  return (
    <div className="mx-auto flex w-full max-w-5xl flex-col gap-4">
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

      <Tabs defaultValue="patient-info">
        <TabsList>
          <TabsTrigger value="patient-info">
            <User className="h-4 w-4" />
            Patient Information
          </TabsTrigger>
          <TabsTrigger value="contact-info">
            <MapPin className="h-4 w-4" />
            Contact Information
          </TabsTrigger>
          <TabsTrigger value="medical-info">
            <Stethoscope className="h-4 w-4" />
            Medical Information
          </TabsTrigger>
          <TabsTrigger value="registration-details">
            <ClipboardList className="h-4 w-4" />
            Registration Details
          </TabsTrigger>
        </TabsList>

        <TabsContent value="patient-info" className="pt-4">
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

        <FormSection id="mode-of-arrival" title="Mode of Arrival" description="How the patient found or was referred to the hospital.">
          <div className="flex flex-wrap gap-3">
            <Field label="Source" htmlFor="arrivalCategory" className="flex w-full flex-col gap-1 sm:w-56">
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
              <Field label="Appointment type" htmlFor="appointmentType" className="flex min-w-[160px] flex-1 flex-col gap-1">
                <Input id="appointmentType" {...register('registration.appointmentType')} />
              </Field>
            )}
            <Field
              label="Department"
              htmlFor="department"
              error={errors.registration?.department?.message}
              className="flex min-w-[160px] flex-1 flex-col gap-1"
            >
              <Input id="department" {...register('registration.department')} />
            </Field>
            <Field
              label="Consultant"
              htmlFor="consultant"
              error={errors.registration?.consultant?.message}
              className="flex min-w-[160px] flex-1 flex-col gap-1"
            >
              <Input id="consultant" {...register('registration.consultant')} />
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
      </Tabs>

        <div className="sticky bottom-0 z-10 -mx-4 flex justify-end gap-3 border-t border-border bg-background/95 px-4 py-3 backdrop-blur supports-[backdrop-filter]:bg-background/80">
          <Button type="button" variant="outline" onClick={() => navigate('/patients/registration')}>
            Cancel
          </Button>
          <Button type="submit" disabled={isSubmitting}>
            {isSubmitting ? 'Registering…' : 'Register Patient'}
          </Button>
        </div>
      </form>
    </div>
  );
}
