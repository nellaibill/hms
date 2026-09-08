import { ApiError, createProgressNoteSchema, type ProgressNoteFormValues } from '@hms/shared';
import { zodResolver } from '@hookform/resolvers/zod';
import { Loader2, Plus } from 'lucide-react';
import { useForm } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { usePostProgressNoteMutation, useProgressNotesQuery } from '../hooks/useProgressNotes';

interface ProgressNotesPanelProps {
  admissionId: string;
}

const defaultValues: ProgressNoteFormValues = {
  noteDateTime: '',
  clinicalCondition: '',
  progress: '',
  diagnosis: '',
  assessment: '',
  plan: '',
  instructions: '',
};

export function ProgressNotesPanel({ admissionId }: ProgressNotesPanelProps) {
  const { data: notes, isPending, isError } = useProgressNotesQuery(admissionId);
  const mutation = usePostProgressNoteMutation(admissionId);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<ProgressNoteFormValues>({
    resolver: zodResolver(createProgressNoteSchema),
    defaultValues,
  });

  function onSubmit(values: ProgressNoteFormValues) {
    mutation.mutate(
      {
        noteDateTime: new Date(values.noteDateTime).toISOString(),
        clinicalCondition: values.clinicalCondition || null,
        progress: values.progress || null,
        diagnosis: values.diagnosis || null,
        assessment: values.assessment || null,
        plan: values.plan || null,
        instructions: values.instructions || null,
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
          Loading progress notes…
        </div>
      )}

      {isError && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          Failed to load progress notes.
        </p>
      )}

      {!isPending && !isError && (
        <div className="flex flex-col gap-3">
          {(notes ?? []).length === 0 && <p className="py-4 text-center text-sm text-muted-foreground">No progress notes recorded yet.</p>}
          {notes?.map((note) => (
            <div key={note.id} className="rounded-lg border border-border p-3">
              <p className="mb-2 text-xs font-medium uppercase tracking-wide text-muted-foreground">
                {new Date(note.noteDateTime).toLocaleString('en-IN')}
              </p>
              <dl className="grid grid-cols-1 gap-x-4 gap-y-1 text-sm sm:grid-cols-2">
                {note.clinicalCondition && (
                  <div>
                    <dt className="text-xs text-muted-foreground">Condition</dt>
                    <dd className="text-foreground">{note.clinicalCondition}</dd>
                  </div>
                )}
                {note.progress && (
                  <div>
                    <dt className="text-xs text-muted-foreground">Progress</dt>
                    <dd className="text-foreground">{note.progress}</dd>
                  </div>
                )}
                {note.diagnosis && (
                  <div>
                    <dt className="text-xs text-muted-foreground">Diagnosis</dt>
                    <dd className="text-foreground">{note.diagnosis}</dd>
                  </div>
                )}
                {note.assessment && (
                  <div>
                    <dt className="text-xs text-muted-foreground">Assessment</dt>
                    <dd className="text-foreground">{note.assessment}</dd>
                  </div>
                )}
                {note.plan && (
                  <div>
                    <dt className="text-xs text-muted-foreground">Plan</dt>
                    <dd className="text-foreground">{note.plan}</dd>
                  </div>
                )}
                {note.instructions && (
                  <div>
                    <dt className="text-xs text-muted-foreground">Instructions</dt>
                    <dd className="text-foreground">{note.instructions}</dd>
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

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="noteDateTime">Note date/time</Label>
          <Input id="noteDateTime" type="datetime-local" className="w-64" {...register('noteDateTime')} />
          {errors.noteDateTime && <p className="text-xs text-destructive">{errors.noteDateTime.message}</p>}
        </div>

        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="clinicalCondition">Clinical condition</Label>
            <Input id="clinicalCondition" {...register('clinicalCondition')} />
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="progress">Progress</Label>
            <Input id="progress" {...register('progress')} />
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="diagnosis">Diagnosis</Label>
            <Input id="diagnosis" {...register('diagnosis')} />
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="assessment">Assessment</Label>
            <Input id="assessment" {...register('assessment')} />
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="plan">Plan</Label>
            <Input id="plan" {...register('plan')} />
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="instructions">Instructions</Label>
            <Input id="instructions" {...register('instructions')} />
          </div>
        </div>

        <Button type="submit" disabled={mutation.isPending} className="w-fit gap-1.5">
          <Plus className="h-4 w-4" />
          {mutation.isPending ? 'Saving…' : 'Add Progress Note'}
        </Button>
      </form>
    </div>
  );
}
