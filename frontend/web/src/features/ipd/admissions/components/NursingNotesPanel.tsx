import { ApiError, createNursingNoteSchema, NURSING_SHIFTS, type NursingNoteFormValues } from '@hms/shared';
import { zodResolver } from '@hookform/resolvers/zod';
import { Loader2, Plus } from 'lucide-react';
import { Controller, useForm } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { useNursingNotesQuery, usePostNursingNoteMutation } from '../hooks/useNursingNotes';

interface NursingNotesPanelProps {
  admissionId: string;
}

const defaultValues: NursingNoteFormValues = {
  noteDateTime: '',
  shift: 'Morning',
  observation: '',
  intervention: '',
  patientResponse: '',
  remarks: '',
};

export function NursingNotesPanel({ admissionId }: NursingNotesPanelProps) {
  const { data: notes, isPending, isError } = useNursingNotesQuery(admissionId);
  const mutation = usePostNursingNoteMutation(admissionId);

  const {
    control,
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<NursingNoteFormValues>({
    resolver: zodResolver(createNursingNoteSchema),
    defaultValues,
  });

  function onSubmit(values: NursingNoteFormValues) {
    mutation.mutate(
      {
        noteDateTime: new Date(values.noteDateTime).toISOString(),
        shift: values.shift,
        observation: values.observation || null,
        intervention: values.intervention || null,
        patientResponse: values.patientResponse || null,
        remarks: values.remarks || null,
      },
      { onSuccess: () => reset(defaultValues) },
    );
  }

  const apiError = mutation.error instanceof ApiError ? mutation.error : null;

  return (
    <div className="flex flex-col gap-4">
      {isPending && (
        <div className="flex items-center gap-2 py-6 text-sm text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" />
          Loading nursing notes…
        </div>
      )}

      {isError && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          Failed to load nursing notes.
        </p>
      )}

      {!isPending && !isError && (
        <div className="flex flex-col gap-3">
          {(notes ?? []).length === 0 && <p className="py-4 text-center text-sm text-muted-foreground">No nursing notes recorded yet.</p>}
          {notes?.map((note) => (
            <div key={note.id} className="rounded-lg border border-border p-3">
              <p className="mb-2 text-xs font-medium uppercase tracking-wide text-muted-foreground">
                {new Date(note.noteDateTime).toLocaleString('en-IN')} · {note.shift} Shift
              </p>
              <dl className="grid grid-cols-1 gap-x-4 gap-y-1 text-sm sm:grid-cols-2">
                {note.observation && (
                  <div>
                    <dt className="text-xs text-muted-foreground">Observation</dt>
                    <dd className="text-foreground">{note.observation}</dd>
                  </div>
                )}
                {note.intervention && (
                  <div>
                    <dt className="text-xs text-muted-foreground">Intervention</dt>
                    <dd className="text-foreground">{note.intervention}</dd>
                  </div>
                )}
                {note.patientResponse && (
                  <div>
                    <dt className="text-xs text-muted-foreground">Patient response</dt>
                    <dd className="text-foreground">{note.patientResponse}</dd>
                  </div>
                )}
                {note.remarks && (
                  <div>
                    <dt className="text-xs text-muted-foreground">Remarks</dt>
                    <dd className="text-foreground">{note.remarks}</dd>
                  </div>
                )}
              </dl>
            </div>
          ))}
        </div>
      )}

      <form onSubmit={handleSubmit(onSubmit)} noValidate className="flex flex-col gap-3 rounded-lg border border-dashed border-border p-4">
        {apiError && !apiError.validationErrors && (
          <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
            {apiError.message}
          </p>
        )}

        <div className="flex flex-wrap items-end gap-3">
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="noteDateTime">Note date/time</Label>
            <Input id="noteDateTime" type="datetime-local" className="w-64" {...register('noteDateTime')} />
            {errors.noteDateTime && <p className="text-xs text-destructive">{errors.noteDateTime.message}</p>}
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="shift">Shift</Label>
            <Controller
              control={control}
              name="shift"
              render={({ field }) => (
                <Select value={field.value} onValueChange={field.onChange}>
                  <SelectTrigger id="shift" className="w-40" aria-label="Shift">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {NURSING_SHIFTS.map((shift) => (
                      <SelectItem key={shift} value={shift}>
                        {shift}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              )}
            />
          </div>
        </div>

        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="observation">Observation</Label>
            <Input id="observation" {...register('observation')} />
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="intervention">Intervention</Label>
            <Input id="intervention" {...register('intervention')} />
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="patientResponse">Patient response</Label>
            <Input id="patientResponse" {...register('patientResponse')} />
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="remarks">Remarks</Label>
            <Input id="remarks" {...register('remarks')} />
          </div>
        </div>

        <Button type="submit" disabled={mutation.isPending} className="w-fit gap-1.5">
          <Plus className="h-4 w-4" />
          {mutation.isPending ? 'Saving…' : 'Add Nursing Note'}
        </Button>
      </form>
    </div>
  );
}
