import { ApiError, transferBedSchema, type Bed, type TransferBedFormValues } from '@hms/shared';
import { zodResolver } from '@hookform/resolvers/zod';
import { useQuery } from '@tanstack/react-query';
import { useEffect, useState } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Label } from '@/components/ui/label';
import { BedSelect } from '@/components/BedSelect';
import { WardSelect } from '@/components/WardSelect';
import { bedsApi } from '@/services/apiClient';

interface TransferBedDialogProps {
  currentWardName: string;
  currentBedNumber: string;
  currentBedId: string;
  isSubmitting: boolean;
  apiError: ApiError | null;
  onSubmit: (values: TransferBedFormValues) => void;
  onCancel: () => void;
}

export function TransferBedDialog({
  currentWardName,
  currentBedNumber,
  currentBedId,
  isSubmitting,
  apiError,
  onSubmit,
  onCancel,
}: TransferBedDialogProps) {
  const {
    control,
    register,
    handleSubmit,
    setError,
    setValue,
    watch,
    formState: { errors },
  } = useForm<TransferBedFormValues>({
    resolver: zodResolver(transferBedSchema),
    defaultValues: { newWardId: '', newBedId: '', transferReason: '' },
  });

  const newWardId = watch('newWardId');
  const newBedId = watch('newBedId');
  const [availableBeds, setAvailableBeds] = useState<Bed[]>([]);
  const targetBed = availableBeds.find((bed) => bed.id === newBedId);
  const { data: currentBed } = useQuery({
    queryKey: ['ipd', 'beds', currentBedId],
    queryFn: () => bedsApi.getBedById(currentBedId),
  });

  useEffect(() => {
    if (!apiError?.validationErrors) {
      return;
    }
    for (const issue of apiError.validationErrors) {
      const fieldName = (issue.field.charAt(0).toLowerCase() + issue.field.slice(1)) as keyof TransferBedFormValues;
      setError(fieldName, { type: 'server', message: issue.message });
    }
  }, [apiError, setError]);

  const generalError = apiError && !apiError.validationErrors ? apiError.message : null;

  return (
    <Dialog open onOpenChange={(open) => !open && onCancel()}>
      <DialogContent aria-labelledby="transfer-bed-title">
        <form onSubmit={handleSubmit(onSubmit)} noValidate className="flex flex-col gap-4">
          <DialogHeader>
            <DialogTitle id="transfer-bed-title">Transfer bed</DialogTitle>
            <DialogDescription>
              Currently in <strong className="text-foreground">{currentWardName}</strong>, bed{' '}
              <strong className="text-foreground">{currentBedNumber}</strong>.
            </DialogDescription>
          </DialogHeader>

          {generalError && (
            <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
              {generalError}
            </p>
          )}

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="newWardId">New ward</Label>
            <Controller
              control={control}
              name="newWardId"
              render={({ field }) => (
                <WardSelect
                  id="newWardId"
                  value={field.value}
                  onValueChange={(value) => {
                    field.onChange(value);
                    setValue('newBedId', '');
                  }}
                />
              )}
            />
            {errors.newWardId && <p className="text-sm text-destructive">{errors.newWardId.message}</p>}
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="newBedId">New bed</Label>
            <Controller
              control={control}
              name="newBedId"
              render={({ field }) => (
                <BedSelect id="newBedId" value={field.value} onValueChange={field.onChange} wardId={newWardId} onBedsLoaded={setAvailableBeds} />
              )}
            />
            {errors.newBedId && <p className="text-sm text-destructive">{errors.newBedId.message}</p>}
          </div>

          {(currentBed || targetBed) && (
            <div className="flex flex-col gap-1 rounded-md border border-border bg-muted/30 px-3 py-2.5 text-sm">
              <p>
                Current bed <span className="font-semibold text-foreground">{currentBedNumber}</span>:{' '}
                <span className="font-semibold text-foreground">₹{(currentBed?.dailyCharge ?? 0).toLocaleString('en-IN')}/day</span>
              </p>
              {targetBed && (
                <p>
                  Target bed <span className="font-semibold text-foreground">{targetBed.bedNumber}</span>:{' '}
                  <span className="font-semibold text-foreground">₹{targetBed.dailyCharge.toLocaleString('en-IN')}/day</span>
                </p>
              )}
              <p className="text-xs text-muted-foreground">Billing for the transfer date will use the highest room tariff occupied that day.</p>
            </div>
          )}

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="transferReason">Transfer reason (optional)</Label>
            <textarea
              id="transferReason"
              rows={2}
              {...register('transferReason')}
              className="flex w-full rounded-md border border-input bg-background px-3 py-2 text-sm shadow-sm placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
              placeholder="e.g. Ward change per doctor's advice"
            />
            {errors.transferReason && <p className="text-sm text-destructive">{errors.transferReason.message}</p>}
          </div>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={onCancel} disabled={isSubmitting}>
              Cancel
            </Button>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? 'Transferring…' : 'Transfer Bed'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
