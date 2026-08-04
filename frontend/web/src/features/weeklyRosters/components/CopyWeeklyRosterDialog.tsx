import { ApiError, copyWeeklyRosterSchema, type CopyWeeklyRosterFormValues, type WeeklyRoster } from '@hms/shared';
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';

interface CopyWeeklyRosterDialogProps {
  roster: WeeklyRoster;
  isSubmitting: boolean;
  apiError: ApiError | null;
  onSubmit: (values: CopyWeeklyRosterFormValues) => void;
  onCancel: () => void;
}

export function CopyWeeklyRosterDialog({ roster, isSubmitting, apiError, onSubmit, onCancel }: CopyWeeklyRosterDialogProps) {
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<CopyWeeklyRosterFormValues>({
    resolver: zodResolver(copyWeeklyRosterSchema),
    defaultValues: { targetWeekStartDate: '' },
  });

  const generalError = apiError && !apiError.validationErrors ? apiError.message : null;

  return (
    <Dialog open onOpenChange={(open) => !open && onCancel()}>
      <DialogContent aria-labelledby="copy-weekly-roster-title">
        <form onSubmit={handleSubmit(onSubmit)} noValidate>
          <DialogHeader>
            <DialogTitle id="copy-weekly-roster-title">Copy roster to another week</DialogTitle>
            <DialogDescription>
              Duplicates this roster's department onto a new, unpublished roster for the week you choose. Shift assignments
              aren't copied.
            </DialogDescription>
          </DialogHeader>

          <div className="flex flex-col gap-4 py-2">
            {generalError && (
              <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
                {generalError}
              </p>
            )}

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="targetWeekStartDate">Target week start date</Label>
              <Input id="targetWeekStartDate" type="date" {...register('targetWeekStartDate')} />
              {errors.targetWeekStartDate && <p className="text-sm text-destructive">{errors.targetWeekStartDate.message}</p>}
            </div>

            <p className="text-xs text-muted-foreground">Copying from week of {roster.weekStartDate}.</p>
          </div>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={onCancel} disabled={isSubmitting}>
              Cancel
            </Button>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? 'Copying…' : 'Copy Roster'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
