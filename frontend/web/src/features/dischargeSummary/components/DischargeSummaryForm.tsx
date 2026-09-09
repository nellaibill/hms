import { ApiError, updateDischargeSummarySchema, type DischargeSummary, type DischargeSummaryFormValues } from '@hms/shared';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect } from 'react';
import { useForm, type UseFormRegisterReturn } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { toFormValues } from '../mapDischargeSummaryForm';
import { DischargeMedicationsTable } from './DischargeMedicationsTable';

const textareaClassName =
  'flex w-full rounded-md border border-input bg-background px-3 py-2 text-sm shadow-sm placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2';

function TextField({ id, label, registration, error, placeholder }: { id: string; label: string; registration: UseFormRegisterReturn; error?: string; placeholder?: string }) {
  return (
    <div className="flex flex-col gap-1.5">
      <Label htmlFor={id}>{label}</Label>
      <Input id={id} placeholder={placeholder} {...registration} />
      {error && <p className="text-sm text-destructive">{error}</p>}
    </div>
  );
}

function NumberField({
  id,
  label,
  registration,
  error,
  step,
}: {
  id: string;
  label: string;
  registration: UseFormRegisterReturn;
  error?: string;
  step?: string;
}) {
  return (
    <div className="flex flex-col gap-1.5">
      <Label htmlFor={id}>{label}</Label>
      <Input id={id} type="number" step={step ?? '0.1'} {...registration} />
      {error && <p className="text-sm text-destructive">{error}</p>}
    </div>
  );
}

function TextAreaField({
  id,
  label,
  registration,
  error,
  rows = 3,
  placeholder,
}: {
  id: string;
  label: string;
  registration: UseFormRegisterReturn;
  error?: string;
  rows?: number;
  placeholder?: string;
}) {
  return (
    <div className="flex flex-col gap-1.5">
      <Label htmlFor={id}>{label}</Label>
      <textarea id={id} rows={rows} placeholder={placeholder} {...registration} className={textareaClassName} />
      {error && <p className="text-sm text-destructive">{error}</p>}
    </div>
  );
}

interface DischargeSummaryFormProps {
  summary: DischargeSummary;
  isSubmitting: boolean;
  apiError: ApiError | null;
  onSave: (values: DischargeSummaryFormValues) => void;
}

/**
 * The Draft-editable body of the discharge summary — Diagnosis & Clinical Summary /
 * Examination / Course in Hospital / Procedures-OT (manual fields, no OT module yet) /
 * Discharge Medications / Discharge Advice, matching the approved plan's section layout.
 * Admission Details (read-only, live from Patients/IPD) and Finalization (sign-off pickers)
 * are deliberately NOT part of this form — they render around it on the edit page, since
 * neither is submitted via PUT.
 */
export function DischargeSummaryForm({ summary, isSubmitting, apiError, onSave }: DischargeSummaryFormProps) {
  const {
    control,
    register,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<DischargeSummaryFormValues>({
    resolver: zodResolver(updateDischargeSummarySchema),
    defaultValues: toFormValues(summary),
  });

  // Server-side validation failures (docs/ApiStandards.md §5) are mapped onto the same
  // field-level display client validation uses, per docs/FrontendArchitecture.md §9.
  useEffect(() => {
    if (!apiError?.validationErrors) {
      return;
    }
    for (const issue of apiError.validationErrors) {
      const fieldName = (issue.field.charAt(0).toLowerCase() + issue.field.slice(1)) as keyof DischargeSummaryFormValues;
      setError(fieldName, { type: 'server', message: issue.message });
    }
  }, [apiError, setError]);

  const generalError = apiError && !apiError.validationErrors ? apiError.message : null;

  return (
    <form onSubmit={handleSubmit(onSave)} noValidate className="flex flex-col gap-6">
      {generalError && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {generalError}
        </p>
      )}

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Diagnosis &amp; Clinical Summary</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          <TextAreaField id="finalDiagnosis" label="Final diagnosis" registration={register('finalDiagnosis')} error={errors.finalDiagnosis?.message} rows={2} />
          <TextAreaField id="chiefComplaints" label="Chief complaints" registration={register('chiefComplaints')} error={errors.chiefComplaints?.message} rows={2} />
          <TextAreaField
            id="historyOfPresentingIllness"
            label="History of presenting illness"
            registration={register('historyOfPresentingIllness')}
            error={errors.historyOfPresentingIllness?.message}
            rows={4}
          />
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <TextAreaField id="pastMedicalHistory" label="Past medical history" registration={register('pastMedicalHistory')} error={errors.pastMedicalHistory?.message} rows={2} />
            <TextAreaField id="pastSurgicalHistory" label="Past surgical history" registration={register('pastSurgicalHistory')} error={errors.pastSurgicalHistory?.message} rows={2} />
            <TextAreaField id="familyHistory" label="Family history" registration={register('familyHistory')} error={errors.familyHistory?.message} rows={2} />
            <TextAreaField id="personalHistory" label="Personal history" registration={register('personalHistory')} error={errors.personalHistory?.message} rows={2} />
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Examination</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          <TextAreaField id="generalExamination" label="General examination" registration={register('generalExamination')} error={errors.generalExamination?.message} rows={2} />
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <TextAreaField id="cvsFindings" label="CVS findings" registration={register('cvsFindings')} error={errors.cvsFindings?.message} rows={2} />
            <TextAreaField id="rsFindings" label="RS findings" registration={register('rsFindings')} error={errors.rsFindings?.message} rows={2} />
            <TextAreaField id="paFindings" label="P/A findings" registration={register('paFindings')} error={errors.paFindings?.message} rows={2} />
            <TextAreaField id="cnsFindings" label="CNS findings" registration={register('cnsFindings')} error={errors.cnsFindings?.message} rows={2} />
          </div>
          <TextAreaField id="localExamination" label="Local examination" registration={register('localExamination')} error={errors.localExamination?.message} rows={2} />
          <TextField id="gait" label="Gait" registration={register('gait')} error={errors.gait?.message} />

          <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-6">
            <NumberField id="heightCm" label="Height (cm)" registration={register('heightCm')} error={errors.heightCm?.message} />
            <NumberField id="weightKg" label="Weight (kg)" registration={register('weightKg')} error={errors.weightKg?.message} />
            <NumberField id="pulseRate" label="Pulse rate" registration={register('pulseRate')} error={errors.pulseRate?.message} step="1" />
            <NumberField id="respiratoryRate" label="Respiratory rate" registration={register('respiratoryRate')} error={errors.respiratoryRate?.message} step="1" />
            <NumberField id="temperatureF" label="Temperature (°F)" registration={register('temperatureF')} error={errors.temperatureF?.message} />
            <NumberField id="spO2Percent" label="SpO₂ (%)" registration={register('spO2Percent')} error={errors.spO2Percent?.message} step="1" />
          </div>
          <TextField id="bloodPressure" label="Blood pressure" registration={register('bloodPressure')} error={errors.bloodPressure?.message} placeholder="e.g. 130/80" />
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Course in Hospital</CardTitle>
        </CardHeader>
        <CardContent>
          <TextAreaField
            id="courseInHospital"
            label="Course in hospital"
            registration={register('courseInHospital')}
            error={errors.courseInHospital?.message}
            rows={6}
            placeholder="Narrative summary of the patient's hospital stay…"
          />
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Procedures / OT</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <TextField id="procedureName" label="Procedure name" registration={register('procedureName')} error={errors.procedureName?.message} />
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="procedureDateTime">Procedure date/time</Label>
              <Input id="procedureDateTime" type="datetime-local" {...register('procedureDateTime')} />
              {errors.procedureDateTime && <p className="text-sm text-destructive">{errors.procedureDateTime.message}</p>}
            </div>
            <TextField id="primarySurgeon" label="Primary surgeon" registration={register('primarySurgeon')} error={errors.primarySurgeon?.message} />
            <TextField id="assistantSurgeons" label="Assistant surgeon(s)" registration={register('assistantSurgeons')} error={errors.assistantSurgeons?.message} />
            <TextField id="anaesthetist" label="Anaesthetist" registration={register('anaesthetist')} error={errors.anaesthetist?.message} />
            <TextField id="anaesthesia" label="Anaesthesia" registration={register('anaesthesia')} error={errors.anaesthesia?.message} />
            <TextField id="surgicalPosition" label="Surgical position" registration={register('surgicalPosition')} error={errors.surgicalPosition?.message} />
          </div>
          <TextAreaField
            id="intraOperativeFindings"
            label="Intra-operative findings"
            registration={register('intraOperativeFindings')}
            error={errors.intraOperativeFindings?.message}
            rows={3}
          />
          <TextAreaField id="operativeNotes" label="Operative notes" registration={register('operativeNotes')} error={errors.operativeNotes?.message} rows={3} />
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Discharge Medications</CardTitle>
        </CardHeader>
        <CardContent>
          <DischargeMedicationsTable control={control} register={register} errors={errors} />
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Discharge Advice</CardTitle>
        </CardHeader>
        <CardContent className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <TextAreaField id="diet" label="Diet" registration={register('diet')} error={errors.diet?.message} rows={2} />
          <TextAreaField id="woundCare" label="Wound care" registration={register('woundCare')} error={errors.woundCare?.message} rows={2} />
          <TextAreaField id="activity" label="Activity" registration={register('activity')} error={errors.activity?.message} rows={2} />
          <TextAreaField id="physiotherapy" label="Physiotherapy" registration={register('physiotherapy')} error={errors.physiotherapy?.message} rows={2} />
          <TextAreaField id="reviewInstructions" label="Review instructions" registration={register('reviewInstructions')} error={errors.reviewInstructions?.message} rows={2} />
          <TextAreaField
            id="emergencyInstructions"
            label="Emergency instructions"
            registration={register('emergencyInstructions')}
            error={errors.emergencyInstructions?.message}
            rows={2}
          />
          <TextAreaField
            id="conditionAtDischarge"
            label="Condition at discharge"
            registration={register('conditionAtDischarge')}
            error={errors.conditionAtDischarge?.message}
            rows={2}
          />
        </CardContent>
      </Card>

      <div className="flex gap-3">
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Saving…' : 'Save'}
        </Button>
      </div>
    </form>
  );
}
