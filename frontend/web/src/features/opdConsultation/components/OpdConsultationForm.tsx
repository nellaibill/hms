import {
  completeOpdConsultationSchema,
  saveOpdConsultationSchema,
  type ApiError,
  type OpdConsultationFormValues,
  type OpdConsultationNote,
  type SaveOpdConsultationRequest,
} from '@hms/shared';
import { zodResolver } from '@hookform/resolvers/zod';
import {
  Activity,
  AlertTriangle,
  CalendarClock,
  ClipboardList,
  Download,
  FlaskConical,
  ListChecks,
  Plus,
  Printer,
  RotateCcw,
  Save,
  Send,
  Sparkles,
  Stethoscope,
  Trash2,
} from 'lucide-react';
import { useEffect, useState } from 'react';
import { Controller, useFieldArray, useForm, type Control, type FieldErrors, type UseFormRegister } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { SearchableSelect } from '@/components/ui/searchable-select';
import { DepartmentSelect } from '@/components/DepartmentSelect';
import { ConsultantSelect } from '@/components/ConsultantSelect';
import { useAuth } from '@/features/auth/AuthContext';
import { getDisplayLabel, getMasterConfig, resolveRecordLabel, useMasterOptionsQuery } from '@/features/masters';
import { AiNoteDictationPanel } from './AiNoteDictationPanel';
import { textareaClassName } from './textareaClassName';

function SectionCard({ icon: Icon, title, children }: { icon: React.ElementType; title: string; children: React.ReactNode }) {
  return (
    <Card>
      <CardHeader className="flex flex-row items-center gap-2 space-y-0 rounded-t-xl bg-muted/50 py-3">
        <span className="flex h-7 w-7 shrink-0 items-center justify-center rounded-md bg-primary/10 text-primary">
          <Icon className="h-4 w-4" />
        </span>
        <CardTitle className="text-base">{title}</CardTitle>
      </CardHeader>
      <CardContent className="flex flex-col gap-4 pt-4">{children}</CardContent>
    </Card>
  );
}

function TextField({
  id,
  label,
  registration,
  error,
  required,
  type = 'text',
  step,
  placeholder,
}: {
  id: string;
  label: string;
  registration: ReturnType<UseFormRegister<OpdConsultationFormValues>>;
  error?: string;
  required?: boolean;
  type?: string;
  step?: string;
  placeholder?: string;
}) {
  return (
    <div className="flex flex-1 flex-col gap-1.5" style={{ minWidth: 160 }}>
      <Label htmlFor={id}>
        {label}
        {required && <span className="text-destructive"> *</span>}
      </Label>
      <Input id={id} type={type} step={step} placeholder={placeholder} {...registration} />
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
  required,
}: {
  id: string;
  label: string;
  registration: ReturnType<UseFormRegister<OpdConsultationFormValues>>;
  error?: string;
  rows?: number;
  placeholder?: string;
  required?: boolean;
}) {
  return (
    <div className="flex flex-col gap-1.5">
      <Label htmlFor={id}>
        {label}
        {required && <span className="text-destructive"> *</span>}
      </Label>
      <textarea id={id} rows={rows} placeholder={placeholder} className={textareaClassName} {...registration} />
      {error && <p className="text-sm text-destructive">{error}</p>}
    </div>
  );
}

function DiagnosisSection({
  control,
  errors,
}: {
  control: Control<OpdConsultationFormValues>;
  errors: FieldErrors<OpdConsultationFormValues>;
}) {
  const { fields, append, remove } = useFieldArray({ control, name: 'diagnoses' });
  const [selectedDiagnosisId, setSelectedDiagnosisId] = useState('');
  const { data: diagnosisOptions } = useMasterOptionsQuery('diagnosis');
  const diagnosisConfig = getMasterConfig('diagnosis');

  const availableOptions = (diagnosisOptions ?? [])
    .filter((option) => option.isActive && !fields.some((line) => line.diagnosisId === option.id))
    .map((option) => ({ value: option.id, label: diagnosisConfig ? getDisplayLabel(diagnosisConfig, option) : option.id }));

  function handleAdd() {
    if (!selectedDiagnosisId) return;
    append({ diagnosisId: selectedDiagnosisId, type: fields.length === 0 ? 'Primary' : 'Secondary' });
    setSelectedDiagnosisId('');
  }

  const listError = typeof errors.diagnoses?.message === 'string' ? errors.diagnoses.message : undefined;

  return (
    <div className="flex flex-col gap-3">
      <div className="flex flex-wrap items-end gap-2">
        <div className="min-w-[260px] flex-1">
          <Label className="mb-1.5 block">Search diagnosis (name or ICD code)…</Label>
          <SearchableSelect
            id="opd-consultation-diagnosis-search"
            value={selectedDiagnosisId}
            onValueChange={setSelectedDiagnosisId}
            options={availableOptions}
            placeholder="Search diagnosis…"
            searchPlaceholder="Search by name or ICD code…"
          />
        </div>
        <Button type="button" onClick={handleAdd} disabled={!selectedDiagnosisId} className="gap-1.5">
          <Plus className="h-4 w-4" />
          Add
        </Button>
      </div>
      {listError && <p className="text-sm text-destructive">{listError}</p>}

      {fields.length > 0 && (
        <div className="overflow-x-auto rounded-md border border-border">
          <table className="w-full text-sm">
            <thead className="bg-muted/50 text-left text-xs uppercase text-muted-foreground">
              <tr>
                <th className="px-3 py-2">#</th>
                <th className="px-3 py-2">Diagnosis</th>
                <th className="w-40 px-3 py-2">Type</th>
                <th className="w-12 px-3 py-2" />
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {fields.map((field, index) => (
                <tr key={field.id}>
                  <td className="px-3 py-2 text-muted-foreground">{index + 1}</td>
                  <td className="px-3 py-2 text-foreground">{resolveRecordLabel('diagnosis', field.diagnosisId)}</td>
                  <td className="px-3 py-2">
                    <Controller
                      control={control}
                      name={`diagnoses.${index}.type`}
                      render={({ field: typeField }) => (
                        <Select value={typeField.value} onValueChange={typeField.onChange}>
                          <SelectTrigger aria-label="Diagnosis type">
                            <SelectValue />
                          </SelectTrigger>
                          <SelectContent>
                            <SelectItem value="Primary">Primary</SelectItem>
                            <SelectItem value="Secondary">Secondary</SelectItem>
                          </SelectContent>
                        </Select>
                      )}
                    />
                  </td>
                  <td className="px-3 py-2 text-right">
                    <Button type="button" variant="ghost" size="icon" aria-label="Remove diagnosis" onClick={() => remove(index)}>
                      <Trash2 className="h-4 w-4 text-destructive" />
                    </Button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

const INVESTIGATION_DEPARTMENTS = ['Laboratory', 'Radiology'] as const;
const INVESTIGATION_PRIORITIES = ['Routine', 'Urgent', 'Stat'] as const;

function InvestigationsSection({ control, register }: { control: Control<OpdConsultationFormValues>; register: UseFormRegister<OpdConsultationFormValues> }) {
  const { fields, append, remove } = useFieldArray({ control, name: 'investigations' });
  const [draftName, setDraftName] = useState('');
  const [draftDepartment, setDraftDepartment] = useState<(typeof INVESTIGATION_DEPARTMENTS)[number]>('Laboratory');
  const [draftPriority, setDraftPriority] = useState<(typeof INVESTIGATION_PRIORITIES)[number]>('Routine');

  function handleAdd() {
    if (!draftName.trim()) return;
    append({ name: draftName.trim(), department: draftDepartment, priority: draftPriority });
    setDraftName('');
    setDraftDepartment('Laboratory');
    setDraftPriority('Routine');
  }

  return (
    <div className="flex flex-col gap-3">
      <div className="flex flex-wrap items-end gap-2">
        <div className="min-w-[220px] flex-1">
          <Label htmlFor="opd-investigation-name" className="mb-1.5 block">
            Investigation
          </Label>
          <Input
            id="opd-investigation-name"
            placeholder="e.g. CBC, X-Ray Knee (Right)"
            value={draftName}
            onChange={(e) => setDraftName(e.target.value)}
          />
        </div>
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="opd-investigation-department">Department</Label>
          <Select value={draftDepartment} onValueChange={(value) => setDraftDepartment(value as typeof draftDepartment)}>
            <SelectTrigger id="opd-investigation-department" className="w-40">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {INVESTIGATION_DEPARTMENTS.map((department) => (
                <SelectItem key={department} value={department}>
                  {department}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="opd-investigation-priority">Priority</Label>
          <Select value={draftPriority} onValueChange={(value) => setDraftPriority(value as typeof draftPriority)}>
            <SelectTrigger id="opd-investigation-priority" className="w-36">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {INVESTIGATION_PRIORITIES.map((priority) => (
                <SelectItem key={priority} value={priority}>
                  {priority}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        <Button type="button" onClick={handleAdd} disabled={!draftName.trim()} className="gap-1.5">
          <Plus className="h-4 w-4" />
          Add Investigation
        </Button>
      </div>

      {fields.length > 0 && (
        <div className="overflow-x-auto rounded-md border border-border">
          <table className="w-full text-sm">
            <thead className="bg-muted/50 text-left text-xs uppercase text-muted-foreground">
              <tr>
                <th className="px-3 py-2">#</th>
                <th className="px-3 py-2">Investigation</th>
                <th className="px-3 py-2">Department</th>
                <th className="px-3 py-2">Priority</th>
                <th className="w-12 px-3 py-2" />
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {fields.map((field, index) => (
                <tr key={field.id}>
                  <td className="px-3 py-2 text-muted-foreground">{index + 1}</td>
                  <td className="px-3 py-2 text-foreground">
                    <input type="hidden" {...register(`investigations.${index}.name`)} />
                    {field.name}
                  </td>
                  <td className="px-3 py-2 text-muted-foreground">{field.department}</td>
                  <td className="px-3 py-2">
                    <span className="rounded-full bg-muted px-2 py-0.5 text-xs font-medium text-muted-foreground">{field.priority}</span>
                  </td>
                  <td className="px-3 py-2 text-right">
                    <Button type="button" variant="ghost" size="icon" aria-label="Remove investigation" onClick={() => remove(index)}>
                      <Trash2 className="h-4 w-4 text-destructive" />
                    </Button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

/** The form keeps unset optional single-value fields (reviewDate, referral pickers) as ''
 * (react-hook-form/Select inputs need a defined string, not undefined) but the backend's
 * SaveOpdConsultationRequest declares them Guid?/DateOnly? — an empty string fails JSON
 * deserialization before the controller runs and comes back as a 400. Normalize '' to null here,
 * at the one seam between the form's own values and the wire request. */
function toSaveRequest(values: OpdConsultationFormValues): SaveOpdConsultationRequest {
  return {
    ...values,
    reviewDate: values.reviewDate || null,
    referralDepartmentId: values.referralDepartmentId || null,
    referralConsultantId: values.referralConsultantId || null,
  };
}

function toFormValues(note: OpdConsultationNote): OpdConsultationFormValues {
  return {
    heightCm: note.heightCm ?? undefined,
    weightKg: note.weightKg ?? undefined,
    pulseRate: note.pulseRate ?? undefined,
    bloodPressure: note.bloodPressure ?? '',
    temperatureF: note.temperatureF ?? undefined,
    spO2Percent: note.spO2Percent ?? undefined,
    presentingComplaints: note.presentingComplaints ?? '',
    clinicalHistory: note.clinicalHistory ?? '',
    examinationFindings: note.examinationFindings ?? '',
    diagnoses: note.diagnoses.map((d) => ({ diagnosisId: d.diagnosisId, type: d.type })),
    investigations: note.investigations.map((i) => ({ name: i.name, department: i.department, priority: i.priority })),
    planOfManagement: note.planOfManagement ?? '',
    reviewDate: note.reviewDate ?? '',
    followUpInstructions: note.followUpInstructions ?? '',
    emergencyReviewInstructions: note.emergencyReviewInstructions ?? '',
    referralDepartmentId: note.referralDepartmentId ?? '',
    referralConsultantId: note.referralConsultantId ?? '',
    referralReason: note.referralReason ?? '',
  };
}

interface OpdConsultationFormProps {
  note: OpdConsultationNote;
  isSavingDraft: boolean;
  isCompleting: boolean;
  isReopening: boolean;
  canReopen: boolean;
  apiError: ApiError | null;
  onSaveDraft: (values: SaveOpdConsultationRequest) => void;
  onComplete: (values: SaveOpdConsultationRequest) => void;
  onReopen: () => void;
  onPrint: () => void;
  onDownloadPdf: () => void;
}

/**
 * The editable body of the OPD Consultation clinical note — Vitals / Clinical Assessment /
 * Diagnosis / Investigations / Plan of Management / Follow-up-Review / Emergency Review (SOS) /
 * Referral, matching the attached mockup's functional sections but with this app's own plain
 * muted-header Card convention (MasterForm's fieldGroup style) rather than the mockup's
 * per-section colors — see the approved plan for why. Read-only once the note's own Status is
 * Completed (OpdConsultationPage passes readOnly down via disabling this same form's controls).
 *
 * Save Draft uses this form's own (lenient) resolver — nothing is required. Complete
 * Consultation re-validates the current values against the stricter completeOpdConsultationSchema
 * on click (not via the form's resolver, since react-hook-form only supports one resolver per
 * form instance) and only calls onComplete once that passes.
 */
export function OpdConsultationForm({
  note,
  isSavingDraft,
  isCompleting,
  isReopening,
  canReopen,
  apiError,
  onSaveDraft,
  onComplete,
  onReopen,
  onPrint,
  onDownloadPdf,
}: OpdConsultationFormProps) {
  const {
    control,
    register,
    handleSubmit,
    getValues,
    setValue,
    setError,
    clearErrors,
    watch,
    formState: { errors },
  } = useForm<OpdConsultationFormValues>({
    resolver: zodResolver(saveOpdConsultationSchema),
    defaultValues: toFormValues(note),
  });

  const { hasFeature } = useAuth();
  const readOnly = note.status === 'Completed';
  const heightCm = watch('heightCm');
  const weightKg = watch('weightKg');
  const bmi = heightCm && weightKg && Number(heightCm) > 0 ? (Number(weightKg) / (Number(heightCm) / 100) ** 2).toFixed(1) : null;

  useEffect(() => {
    if (!apiError?.validationErrors) return;
    for (const issue of apiError.validationErrors) {
      const fieldName = (issue.field.charAt(0).toLowerCase() + issue.field.slice(1)) as keyof OpdConsultationFormValues;
      setError(fieldName, { type: 'server', message: issue.message });
    }
  }, [apiError, setError]);

  const generalError = apiError && !apiError.validationErrors ? apiError.message : null;

  function handleCompleteClick() {
    clearErrors();
    const values = getValues();
    const result = completeOpdConsultationSchema.safeParse(values);
    if (!result.success) {
      for (const issue of result.error.issues) {
        setError(issue.path.join('.') as keyof OpdConsultationFormValues, { type: 'manual', message: issue.message });
      }
      return;
    }
    onComplete(toSaveRequest(result.data));
  }

  return (
    <form onSubmit={handleSubmit((values) => onSaveDraft(toSaveRequest(values)))} noValidate className="flex flex-col gap-6">
      {generalError && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {generalError}
        </p>
      )}

      <fieldset disabled={readOnly} className="grid grid-cols-1 gap-6 lg:grid-cols-2 lg:items-start">
        <div className="flex flex-col gap-6">
          {hasFeature('opd-ambient-notes') && (
            <SectionCard icon={Sparkles} title="Ambient Note (AI)">
              <AiNoteDictationPanel consultationId={note.consultationId} setValue={setValue} disabled={readOnly} />
            </SectionCard>
          )}

          <SectionCard icon={Activity} title="Vitals">
            <div className="flex flex-wrap gap-4">
              <TextField id="heightCm" label="Height (cm)" registration={register('heightCm')} error={errors.heightCm?.message} required type="number" step="0.1" />
              <TextField id="weightKg" label="Weight (kg)" registration={register('weightKg')} error={errors.weightKg?.message} required type="number" step="0.1" />
              <div className="flex flex-1 flex-col gap-1.5" style={{ minWidth: 100 }}>
                <Label>BMI</Label>
                <div className="flex h-10 items-center rounded-md border border-dashed border-input bg-muted px-3 text-sm text-muted-foreground">{bmi ?? '—'}</div>
              </div>
              <TextField id="pulseRate" label="PR (bpm)" registration={register('pulseRate')} error={errors.pulseRate?.message} type="number" step="1" />
              <TextField id="bloodPressure" label="BP (mmHg)" registration={register('bloodPressure')} error={errors.bloodPressure?.message} placeholder="e.g. 120/80" />
              <TextField id="temperatureF" label="Temperature (°F)" registration={register('temperatureF')} error={errors.temperatureF?.message} type="number" step="0.1" />
              <TextField id="spO2Percent" label="SpO2 (%)" registration={register('spO2Percent')} error={errors.spO2Percent?.message} type="number" step="1" />
            </div>
          </SectionCard>

          <SectionCard icon={ClipboardList} title="Clinical Assessment">
            <TextAreaField
              id="presentingComplaints"
              label="Presenting Complaints"
              registration={register('presentingComplaints')}
              error={errors.presentingComplaints?.message}
              required
              rows={2}
            />
            <TextAreaField id="clinicalHistory" label="Clinical History" registration={register('clinicalHistory')} error={errors.clinicalHistory?.message} rows={2} />
            <TextAreaField
              id="examinationFindings"
              label="Examination Findings"
              registration={register('examinationFindings')}
              error={errors.examinationFindings?.message}
              rows={3}
            />
          </SectionCard>

          <SectionCard icon={Stethoscope} title="Diagnosis">
            <DiagnosisSection control={control} errors={errors} />
          </SectionCard>
        </div>

        <div className="flex flex-col gap-6">
          <SectionCard icon={FlaskConical} title="Investigations">
            <InvestigationsSection control={control} register={register} />
          </SectionCard>

          <SectionCard icon={ListChecks} title="Plan of Management">
            <TextAreaField
              id="planOfManagement"
              label="Plan of Management"
              registration={register('planOfManagement')}
              error={errors.planOfManagement?.message}
              rows={4}
              placeholder="e.g. Tablet Paracetamol 500 mg SOS for pain, Knee support advised, Physiotherapy…"
            />
          </SectionCard>

          <SectionCard icon={CalendarClock} title="Follow-up / Review">
            <div className="flex flex-wrap gap-4">
              <div className="flex flex-1 flex-col gap-1.5" style={{ minWidth: 160 }}>
                <Label htmlFor="reviewDate">Review Date</Label>
                <Input id="reviewDate" type="date" {...register('reviewDate')} />
              </div>
              <div className="flex flex-[2] flex-col gap-1.5" style={{ minWidth: 240 }}>
                <Label htmlFor="followUpInstructions">Follow-up Instructions</Label>
                <textarea id="followUpInstructions" rows={2} className={textareaClassName} {...register('followUpInstructions')} />
              </div>
            </div>
          </SectionCard>

          <SectionCard icon={AlertTriangle} title="Emergency Review (SOS)">
            <TextAreaField
              id="emergencyReviewInstructions"
              label="Emergency Review Instructions"
              registration={register('emergencyReviewInstructions')}
              error={errors.emergencyReviewInstructions?.message}
              rows={2}
              placeholder="e.g. In case of increased pain, swelling or fever, visit emergency department."
            />
          </SectionCard>

          <SectionCard icon={Send} title="Referral (if any)">
            <div className="flex flex-wrap gap-4">
              <div className="flex min-w-[200px] flex-1 flex-col gap-1.5">
                <Label htmlFor="referralDepartmentId">Referral Department</Label>
                <Controller
                  control={control}
                  name="referralDepartmentId"
                  render={({ field }) => <DepartmentSelect id="referralDepartmentId" value={field.value ?? ''} onValueChange={field.onChange} />}
                />
              </div>
              <div className="flex min-w-[200px] flex-1 flex-col gap-1.5">
                <Label htmlFor="referralConsultantId">Referral Doctor</Label>
                <Controller
                  control={control}
                  name="referralConsultantId"
                  render={({ field }) => (
                    <ConsultantSelect id="referralConsultantId" value={field.value ?? ''} onValueChange={field.onChange} departmentId={watch('referralDepartmentId') || undefined} />
                  )}
                />
              </div>
              <div className="flex min-w-[240px] flex-[2] flex-col gap-1.5">
                <Label htmlFor="referralReason">Reason</Label>
                <textarea id="referralReason" rows={2} placeholder="Enter referral reason…" className={textareaClassName} {...register('referralReason')} />
              </div>
            </div>
          </SectionCard>
        </div>
      </fieldset>

      <div className="sticky bottom-0 z-10 -mx-4 flex flex-wrap justify-end gap-3 border-t border-border bg-background/95 px-4 py-3 backdrop-blur supports-[backdrop-filter]:bg-background/80">
        {!readOnly && (
          <>
            <Button type="submit" variant="outline" disabled={isSavingDraft} className="gap-1.5">
              <Save className="h-4 w-4" />
              {isSavingDraft ? 'Saving…' : 'Save Draft'}
            </Button>
            <Button type="button" disabled={isCompleting} className="gap-1.5" onClick={handleCompleteClick}>
              <Stethoscope className="h-4 w-4" />
              {isCompleting ? (
                'Completing…'
              ) : (
                <>
                  Complete<span className="hidden sm:inline"> Consultation</span>
                </>
              )}
            </Button>
          </>
        )}
        {readOnly && canReopen && (
          <Button type="button" variant="outline" disabled={isReopening} className="gap-1.5" onClick={onReopen}>
            <RotateCcw className="h-4 w-4" />
            {isReopening ? 'Reopening…' : 'Reopen'}
          </Button>
        )}
        {/* Icon-only below sm (label kept for screen readers/tooltip) so the whole bar fits on
            one row on a phone instead of wrapping into a two-row bar over the form. */}
        <Button type="button" variant="outline" className="gap-1.5" onClick={onPrint} aria-label="Print" title="Print">
          <Printer className="h-4 w-4" />
          <span className="hidden sm:inline">Print</span>
        </Button>
        <Button type="button" variant="outline" className="gap-1.5" onClick={onDownloadPdf} aria-label="Download PDF" title="Download PDF">
          <Download className="h-4 w-4" />
          <span className="hidden sm:inline">Download PDF</span>
        </Button>
      </div>
    </form>
  );
}
