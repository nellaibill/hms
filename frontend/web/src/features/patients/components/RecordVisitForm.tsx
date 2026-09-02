import { ENCOUNTER_TYPES_UI, recordVisitUiSchema, type ApiError, type RecordVisitUiFormValues } from '@hms/shared';
import { zodResolver } from '@hookform/resolvers/zod';
import { Plus, X } from 'lucide-react';
import { useEffect } from 'react';
import { Controller, useController, useFieldArray, useForm, type Control } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { AppointmentTypeSelect } from '@/components/AppointmentTypeSelect';
import { ConsultantSelect } from '@/components/ConsultantSelect';
import { ConsultationTypeSelect } from '@/components/ConsultationTypeSelect';
import { DepartmentSelect } from '@/components/DepartmentSelect';
import { Field, FormSection } from './FormSection';
import { encounterTypeLabel, encounterTypeShortLabel } from '../encounterTypeLabel';

interface RecordVisitFormProps {
  isSubmitting: boolean;
  apiError: ApiError | null;
  onSubmit: (values: RecordVisitUiFormValues) => void;
  onCancel: () => void;
  /** Mirrors the form's own dirty state up to the parent page, which owns the actual
   * unsaved-changes navigation guard (useUnsavedChangesGuard). */
  onDirtyChange?: (isDirty: boolean) => void;
}

const defaultValues: RecordVisitUiFormValues = {
  encounterType: 'OP',
  departmentId: '',
  consultantId: '',
  additionalConsultants: [],
  appointmentTypeId: '',
  consultationTypeId: '',
};

/** The standalone "Add Visit" form for an existing patient — same fields, same
 * ConsultantSelect/DepartmentSelect/etc. components, and the same recordVisitUiSchema
 * validation as the Registration Details tab of the New Patient Registration wizard
 * (PatientRegistrationForm.tsx), just as its own small form rather than one tab of a larger
 * one. Kept as a separate component (some duplicated JSX) rather than sharing the wizard's
 * tab component directly — that component is typed to the wizard's own
 * Control<PatientRegistrationUiFormValues> and `registration.*` field paths, which don't
 * apply here. */
export function RecordVisitForm({ isSubmitting, apiError, onSubmit, onCancel, onDirtyChange }: RecordVisitFormProps) {
  const {
    control,
    handleSubmit,
    watch,
    setValue,
    formState: { errors, isDirty },
  } = useForm<RecordVisitUiFormValues>({
    resolver: zodResolver(recordVisitUiSchema),
    defaultValues,
  });

  useEffect(() => {
    onDirtyChange?.(isDirty);
  }, [isDirty, onDirtyChange]);

  const encounterType = watch('encounterType');
  const departmentId = watch('departmentId');
  const additionalConsultants = useFieldArray({ control, name: 'additionalConsultants' });

  // A consultant picked under the previous department is meaningless once the department
  // changes — same reasoning as the wizard's own handleDepartmentChange.
  function handleDepartmentChange(newDepartmentId: string, onChange: (value: string) => void) {
    onChange(newDepartmentId);
    setValue('consultantId', '');
  }

  const generalError = apiError?.message ?? null;
  const serverValidationMessages = apiError?.validationErrors?.map((issue) => issue.message) ?? [];

  return (
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

      <FormSection id="visit-details" title="Registration / Encounter Details">
        <div className="flex flex-wrap gap-3">
          <Field label="Encounter type" htmlFor="encounterType" className="flex w-full flex-col gap-1 sm:w-56">
            <Controller
              name="encounterType"
              control={control}
              render={({ field }) => (
                <Select value={field.value} onValueChange={field.onChange}>
                  <SelectTrigger id="encounterType" aria-label="Encounter type">
                    <SelectValue>{encounterTypeShortLabel(field.value)}</SelectValue>
                  </SelectTrigger>
                  <SelectContent>
                    {ENCOUNTER_TYPES_UI.map((e) => (
                      <SelectItem key={e} value={e}>
                        {encounterTypeLabel(e)}
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
              error={errors.appointmentTypeId?.message}
              className="flex min-w-[160px] flex-1 flex-col gap-1"
            >
              <Controller
                name="appointmentTypeId"
                control={control}
                render={({ field }) => (
                  <AppointmentTypeSelect id="appointmentType" value={field.value ?? ''} onValueChange={field.onChange} />
                )}
              />
            </Field>
          )}
          <Field label="Department" htmlFor="department" error={errors.departmentId?.message} className="flex min-w-[160px] flex-1 flex-col gap-1">
            <Controller
              name="departmentId"
              control={control}
              render={({ field }) => (
                <DepartmentSelect id="department" value={field.value} onValueChange={(value) => handleDepartmentChange(value, field.onChange)} />
              )}
            />
          </Field>
          <Field label="Consultant" htmlFor="consultant" error={errors.consultantId?.message} className="flex min-w-[160px] flex-1 flex-col gap-1">
            <Controller
              name="consultantId"
              control={control}
              render={({ field }) => (
                <ConsultantSelect id="consultant" value={field.value} onValueChange={field.onChange} departmentId={departmentId} />
              )}
            />
            {additionalConsultants.fields.length < 3 && (
              <Button
                type="button"
                variant="link"
                size="sm"
                className="h-auto w-fit gap-1 px-0 py-0 text-xs"
                onClick={() => additionalConsultants.append({ departmentId: '', consultantId: '', consultationTypeId: '' })}
              >
                <Plus className="h-3 w-3" />
                Add another Consultant
              </Button>
            )}
          </Field>
          <Field label="Consultation Type" htmlFor="consultationType" className="flex min-w-[220px] flex-1 flex-col gap-1">
            <Controller
              name="consultationTypeId"
              control={control}
              render={({ field }) => <ConsultationTypeSelect id="consultationType" value={field.value ?? ''} onValueChange={field.onChange} />}
            />
          </Field>
        </div>

        {additionalConsultants.fields.map((field, index) => (
          <AdditionalConsultantRow key={field.id} control={control} index={index} onRemove={() => additionalConsultants.remove(index)} />
        ))}
      </FormSection>

      <div className="flex items-center justify-between gap-3">
        <Button type="button" variant="outline" onClick={onCancel}>
          Cancel
        </Button>
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Saving…' : 'Add Visit'}
        </Button>
      </div>
    </form>
  );
}

interface AdditionalConsultantRowProps {
  control: Control<RecordVisitUiFormValues>;
  index: number;
  onRemove: () => void;
}

/** One "Add another Consultant" row — mirrors PatientRegistrationForm's AdditionalConsultantRow exactly, just against this form's own field paths (no `registration.` prefix). */
function AdditionalConsultantRow({ control, index, onRemove }: AdditionalConsultantRowProps) {
  const departmentField = useController({ control, name: `additionalConsultants.${index}.departmentId` as const });
  const consultantField = useController({ control, name: `additionalConsultants.${index}.consultantId` as const });
  const consultationTypeField = useController({ control, name: `additionalConsultants.${index}.consultationTypeId` as const });

  function handleRowDepartmentChange(value: string) {
    departmentField.field.onChange(value);
    consultantField.field.onChange('');
  }

  return (
    <div className="flex flex-wrap items-end gap-3 rounded-md border border-dashed border-border p-3">
      <Field label={`Department ${index + 2}`} htmlFor={`additional-department-${index}`} className="flex min-w-[160px] flex-1 flex-col gap-1">
        <DepartmentSelect id={`additional-department-${index}`} value={departmentField.field.value ?? ''} onValueChange={handleRowDepartmentChange} />
      </Field>
      <Field label={`Consultant ${index + 2}`} htmlFor={`additional-consultant-${index}`} className="flex min-w-[160px] flex-1 flex-col gap-1">
        <ConsultantSelect
          id={`additional-consultant-${index}`}
          value={consultantField.field.value ?? ''}
          onValueChange={consultantField.field.onChange}
          departmentId={departmentField.field.value || undefined}
        />
      </Field>
      <Field
        label={`Consultation Type ${index + 2}`}
        htmlFor={`additional-consultation-type-${index}`}
        className="flex min-w-[220px] flex-1 flex-col gap-1"
      >
        <ConsultationTypeSelect
          id={`additional-consultation-type-${index}`}
          value={consultationTypeField.field.value ?? ''}
          onValueChange={consultationTypeField.field.onChange}
        />
      </Field>
      <Button type="button" variant="ghost" size="icon" aria-label={`Remove consultant ${index + 2}`} onClick={onRemove}>
        <X className="h-4 w-4" />
      </Button>
    </div>
  );
}
