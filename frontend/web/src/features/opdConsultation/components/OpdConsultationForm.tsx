import {
  completeOpdConsultationSchema,
  saveOpdConsultationSchema,
  type ApiError,
  type OpdConsultationFormValues,
  type OpdConsultationNote,
  type OpdDiagnosisOption,
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
  Pill,
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
import { resolveRecordLabel } from '@/features/masters';
import { useDebouncedValue } from '@/hooks/useDebouncedValue';
import { useConsultationDiagnosesQuery, useCreateConsultationDiagnosisMutation, useInvestigationServicesQuery } from '../hooks/useConsultationCatalogQueries';
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
  const [search, setSearch] = useState('');
  const debouncedSearch = useDebouncedValue(search.trim(), 250);
  // Served by the OPD consultation API under clinical-care (not the admin-only Masters
  // endpoints), searching name OR ICD code server-side — regression report OPD-03.
  const { data: diagnosisOptions, isFetching } = useConsultationDiagnosesQuery(debouncedSearch);
  const createDiagnosis = useCreateConsultationDiagnosisMutation();
  const [createError, setCreateError] = useState<string | null>(null);

  const optionLabel = (option: OpdDiagnosisOption) => (option.icdCode ? `${option.name} (${option.icdCode})` : option.name);
  const availableOptions = (diagnosisOptions ?? [])
    .filter((option) => !fields.some((line) => line.diagnosisId === option.id))
    .map((option) => ({ value: option.id, label: optionLabel(option), keywords: option.icdCode ?? undefined }));

  function appendDiagnosis(option: OpdDiagnosisOption) {
    if (fields.some((line) => line.diagnosisId === option.id)) return;
    append({ diagnosisId: option.id, type: fields.length === 0 ? 'Primary' : 'Secondary', diagnosisName: option.name, icdCode: option.icdCode ?? null });
  }

  function handleAdd() {
    const option = diagnosisOptions?.find((o) => o.id === selectedDiagnosisId);
    if (!option) return;
    appendDiagnosis(option);
    setSelectedDiagnosisId('');
  }

  // Nothing matched — add it to the catalog and straight onto this consultation.
  function handleCreate(name: string) {
    setCreateError(null);
    createDiagnosis.mutate(
      { name },
      {
        onSuccess: (created) => appendDiagnosis(created),
        onError: () => setCreateError(`Couldn't add "${name}" — please try again.`),
      },
    );
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
            onSearchChange={setSearch}
            isLoading={isFetching}
            onCreate={handleCreate}
            createLabel={(q) => `Add "${q}" as a new diagnosis`}
          />
        </div>
        <Button type="button" onClick={handleAdd} disabled={!selectedDiagnosisId} className="gap-1.5">
          <Plus className="h-4 w-4" />
          Add
        </Button>
      </div>
      {createDiagnosis.isPending && <p className="text-sm text-muted-foreground">Adding diagnosis…</p>}
      {createError && <p className="text-sm text-destructive">{createError}</p>}
      {listError && <p className="text-sm text-destructive">{listError}</p>}

      {fields.length > 0 && (
        <div className="overflow-x-auto rounded-md border border-border">
          <table className="w-full text-sm">
            <thead className="bg-sidebar-active text-left text-xs uppercase text-sidebar-active-foreground">
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
                  <td className="px-3 py-2 text-foreground">{diagnosisLineLabel(field)}</td>
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

/** Name (ICD) from the line itself (the server resolves it on every response), falling back to
 * the Masters reference cache for anything older. */
function diagnosisLineLabel(line: { diagnosisId: string; diagnosisName?: string | null; icdCode?: string | null }) {
  if (line.diagnosisName) return line.icdCode ? `${line.diagnosisName} (${line.icdCode})` : line.diagnosisName;
  return resolveRecordLabel('diagnosis', line.diagnosisId);
}

const INVESTIGATION_DEPARTMENTS = ['Laboratory', 'Radiology'] as const;
const INVESTIGATION_PRIORITIES = ['Routine', 'Urgent', 'Stat'] as const;

function InvestigationsSection({ control, register }: { control: Control<OpdConsultationFormValues>; register: UseFormRegister<OpdConsultationFormValues> }) {
  const { fields, append, remove } = useFieldArray({ control, name: 'investigations' });
  const [draftDepartment, setDraftDepartment] = useState<(typeof INVESTIGATION_DEPARTMENTS)[number]>('Laboratory');
  const [draftPriority, setDraftPriority] = useState<(typeof INVESTIGATION_PRIORITIES)[number]>('Routine');
  const [selectedServiceId, setSelectedServiceId] = useState('');
  const [freeTextName, setFreeTextName] = useState('');
  // OPD-01: picking from the Laboratory/Radiology catalog links the line to a real service, so
  // OPD Billing Entry pre-adds it and billing creates the lab order. A test that isn't in the
  // catalog can still be written as free text (documentation only).
  const { data: services, isLoading } = useInvestigationServicesQuery(draftDepartment);

  const serviceOptions = (services ?? [])
    .filter((service) => !fields.some((line) => line.serviceId === service.id))
    .map((service) => ({ value: service.id, label: service.name, keywords: service.code }));

  function reset() {
    setSelectedServiceId('');
    setFreeTextName('');
    setDraftPriority('Routine');
  }

  function handleAdd() {
    const service = services?.find((s) => s.id === selectedServiceId);
    if (service) {
      append({ name: service.name, department: draftDepartment, priority: draftPriority, serviceId: service.id });
    } else if (freeTextName.trim()) {
      append({ name: freeTextName.trim(), department: draftDepartment, priority: draftPriority, serviceId: null });
    } else {
      return;
    }
    reset();
  }

  return (
    <div className="flex flex-col gap-3">
      <div className="flex flex-wrap items-end gap-2">
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="opd-investigation-department">Department</Label>
          <Select
            value={draftDepartment}
            onValueChange={(value) => {
              setDraftDepartment(value as typeof draftDepartment);
              setSelectedServiceId('');
            }}
          >
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
        <div className="min-w-[220px] flex-1">
          <Label htmlFor="opd-investigation-service" className="mb-1.5 block">
            Investigation
          </Label>
          <SearchableSelect
            id="opd-investigation-service"
            value={selectedServiceId}
            onValueChange={(value) => {
              setSelectedServiceId(value);
              setFreeTextName('');
            }}
            options={serviceOptions}
            placeholder={isLoading ? 'Loading tests…' : `Search ${draftDepartment.toLowerCase()} tests…`}
            searchPlaceholder="Search by test name or code…"
            isLoading={isLoading}
            onCreate={(q) => {
              setSelectedServiceId('');
              setFreeTextName(q);
            }}
            createLabel={(q) => `Use "${q}" (not in catalog — won't be billed automatically)`}
          />
          {freeTextName && (
            <p className="mt-1 text-xs text-muted-foreground">
              Free text: <span className="font-medium text-foreground">{freeTextName}</span> — billing staff will need to add this test by hand.
            </p>
          )}
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
        <Button type="button" onClick={handleAdd} disabled={!selectedServiceId && !freeTextName.trim()} className="gap-1.5">
          <Plus className="h-4 w-4" />
          Add Investigation
        </Button>
      </div>

      {fields.length > 0 && (
        <div className="overflow-x-auto rounded-md border border-border">
          <table className="w-full text-sm">
            <thead className="bg-sidebar-active text-left text-xs uppercase text-sidebar-active-foreground">
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
                    {!field.serviceId && <span className="ml-2 text-xs text-muted-foreground">(free text — not auto-billed)</span>}
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

const EMPTY_PRESCRIPTION = { drugName: '', dose: '', route: '', frequency: '', durationDays: '', instructions: '' };

/** OPD-02: structured prescription — drug, dose, route, frequency, duration, instructions. Only
 * the drug name is required, so a doctor can write it as briefly as on paper. */
function PrescriptionsSection({ control, errors }: { control: Control<OpdConsultationFormValues>; errors: FieldErrors<OpdConsultationFormValues> }) {
  const { fields, append, remove } = useFieldArray({ control, name: 'prescriptions' });
  const [draft, setDraft] = useState(EMPTY_PRESCRIPTION);
  const [draftError, setDraftError] = useState<string | null>(null);

  function update(key: keyof typeof EMPTY_PRESCRIPTION, value: string) {
    setDraft((prev) => ({ ...prev, [key]: value }));
  }

  function handleAdd() {
    if (!draft.drugName.trim()) {
      setDraftError('Enter the drug name.');
      return;
    }
    const duration = draft.durationDays.trim() ? Number(draft.durationDays) : undefined;
    if (duration !== undefined && (!Number.isInteger(duration) || duration < 1 || duration > 365)) {
      setDraftError('Duration must be a whole number of days between 1 and 365.');
      return;
    }
    setDraftError(null);
    append({
      drugName: draft.drugName.trim(),
      dose: draft.dose.trim(),
      route: draft.route.trim(),
      frequency: draft.frequency.trim(),
      durationDays: duration,
      instructions: draft.instructions.trim(),
    });
    setDraft(EMPTY_PRESCRIPTION);
  }

  const rowErrors = Array.isArray(errors.prescriptions) ? errors.prescriptions : [];

  return (
    <div className="flex flex-col gap-3">
      <div className="grid grid-cols-1 gap-2 sm:grid-cols-6 sm:items-end">
        <div className="flex flex-col gap-1.5 sm:col-span-2">
          <Label htmlFor="opd-rx-drug">Drug</Label>
          <Input id="opd-rx-drug" placeholder="e.g. Amoxicillin 500 mg" value={draft.drugName} onChange={(e) => update('drugName', e.target.value)} />
        </div>
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="opd-rx-dose">Dose</Label>
          <Input id="opd-rx-dose" placeholder="1 tab" value={draft.dose} onChange={(e) => update('dose', e.target.value)} />
        </div>
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="opd-rx-route">Route</Label>
          <Input id="opd-rx-route" placeholder="Oral" value={draft.route} onChange={(e) => update('route', e.target.value)} />
        </div>
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="opd-rx-frequency">Frequency</Label>
          <Input id="opd-rx-frequency" placeholder="1-0-1" value={draft.frequency} onChange={(e) => update('frequency', e.target.value)} />
        </div>
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="opd-rx-duration">Days</Label>
          <Input id="opd-rx-duration" type="number" min={1} max={365} step={1} placeholder="5" value={draft.durationDays} onChange={(e) => update('durationDays', e.target.value)} />
        </div>
        <div className="flex flex-col gap-1.5 sm:col-span-5">
          <Label htmlFor="opd-rx-instructions">Instructions</Label>
          <Input id="opd-rx-instructions" placeholder="e.g. After food" value={draft.instructions} onChange={(e) => update('instructions', e.target.value)} />
        </div>
        <Button type="button" onClick={handleAdd} className="gap-1.5">
          <Plus className="h-4 w-4" />
          Add Medicine
        </Button>
      </div>
      {draftError && <p className="text-sm text-destructive">{draftError}</p>}

      {fields.length > 0 && (
        <div className="overflow-x-auto rounded-md border border-border">
          <table className="w-full text-sm">
            <thead className="bg-sidebar-active text-left text-xs uppercase text-sidebar-active-foreground">
              <tr>
                <th className="px-3 py-2">#</th>
                <th className="px-3 py-2">Drug</th>
                <th className="px-3 py-2">Dose</th>
                <th className="px-3 py-2">Route</th>
                <th className="px-3 py-2">Frequency</th>
                <th className="px-3 py-2">Days</th>
                <th className="px-3 py-2">Instructions</th>
                <th className="w-12 px-3 py-2" />
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {fields.map((field, index) => (
                <tr key={field.id}>
                  <td className="px-3 py-2 text-muted-foreground">{index + 1}</td>
                  <td className="px-3 py-2 font-medium text-foreground">
                    {field.drugName}
                    {rowErrors[index]?.drugName?.message && <p className="text-xs text-destructive">{rowErrors[index]?.drugName?.message}</p>}
                  </td>
                  <td className="px-3 py-2 text-muted-foreground">{field.dose || '—'}</td>
                  <td className="px-3 py-2 text-muted-foreground">{field.route || '—'}</td>
                  <td className="px-3 py-2 text-muted-foreground">{field.frequency || '—'}</td>
                  <td className="px-3 py-2 text-muted-foreground">{field.durationDays ?? '—'}</td>
                  <td className="px-3 py-2 text-muted-foreground">{field.instructions || '—'}</td>
                  <td className="px-3 py-2 text-right">
                    <Button type="button" variant="ghost" size="icon" aria-label="Remove medicine" onClick={() => remove(index)}>
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
    investigations: values.investigations.map((i) => ({ ...i, serviceId: i.serviceId || null })),
    prescriptions: values.prescriptions.map((p) => ({
      drugName: p.drugName,
      dose: p.dose || null,
      route: p.route || null,
      frequency: p.frequency || null,
      durationDays: p.durationDays ?? null,
      instructions: p.instructions || null,
    })),
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
    diagnoses: note.diagnoses.map((d) => ({ diagnosisId: d.diagnosisId, type: d.type, diagnosisName: d.diagnosisName ?? null, icdCode: d.icdCode ?? null })),
    investigations: note.investigations.map((i) => ({ name: i.name, department: i.department, priority: i.priority, serviceId: i.serviceId ?? null })),
    prescriptions: (note.prescriptions ?? []).map((p) => ({
      drugName: p.drugName,
      dose: p.dose ?? '',
      route: p.route ?? '',
      frequency: p.frequency ?? '',
      durationDays: p.durationDays ?? undefined,
      instructions: p.instructions ?? '',
    })),
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
  // Only for values the form would accept (height 0–300 cm, weight 0–500 kg, both above 0) — an
  // out-of-range or negative entry already shows its own field error, and a BMI derived from it
  // (e.g. -0.1) is just noise next to that error.
  const heightValue = Number(heightCm);
  const weightValue = Number(weightKg);
  const bmi =
    heightValue > 0 && heightValue <= 300 && weightValue > 0 && weightValue <= 500
      ? (weightValue / (heightValue / 100) ** 2).toFixed(1)
      : null;

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

          <SectionCard icon={Pill} title="Prescription">
            <PrescriptionsSection control={control} errors={errors} />
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
        {/* Icon-only below lg (label kept for screen readers/tooltip) so the whole bar fits on
            one row instead of wrapping into a two-row bar over the form — on a phone, and also
            on laptop/split-screen widths where the pinned sidebar leaves the form ~530px. */}
        <Button type="button" variant="outline" className="gap-1.5" onClick={onPrint} aria-label="Print" title="Print">
          <Printer className="h-4 w-4" />
          <span className="hidden lg:inline">Print</span>
        </Button>
        <Button type="button" variant="outline" className="gap-1.5" onClick={onDownloadPdf} aria-label="Download PDF" title="Download PDF">
          <Download className="h-4 w-4" />
          <span className="hidden lg:inline">Download PDF</span>
        </Button>
      </div>
    </form>
  );
}
