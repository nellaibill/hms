import { ApiError, type AssignBedRequest, type Bed } from '@hms/shared';
import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Label } from '@/components/ui/label';
import { BedSelect } from '@/components/BedSelect';
import { WardSelect } from '@/components/WardSelect';

interface AssignBedDialogProps {
  patientName: string;
  isSubmitting: boolean;
  apiError: ApiError | null;
  onSubmit: (request: AssignBedRequest) => void;
  onCancel: () => void;
}

/** Assigns a ward/bed to a Requested admission (raised from OPD), moving it to Admitted —
 * mirrors TransferBedDialog's exact ward/bed-picker layout, minus the "current bed" summary
 * a Requested admission doesn't have yet. */
export function AssignBedDialog({ patientName, isSubmitting, apiError, onSubmit, onCancel }: AssignBedDialogProps) {
  const [wardId, setWardId] = useState('');
  const [bedId, setBedId] = useState('');
  const [availableBeds, setAvailableBeds] = useState<Bed[]>([]);
  const selectedBed = availableBeds.find((bed) => bed.id === bedId);

  const generalError = apiError && !apiError.validationErrors ? apiError.message : null;

  function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    if (!wardId || !bedId) {
      return;
    }
    onSubmit({ wardId, bedId });
  }

  return (
    <Dialog open onOpenChange={(open) => !open && onCancel()}>
      <DialogContent aria-labelledby="assign-bed-title">
        <form onSubmit={handleSubmit} className="flex flex-col gap-4">
          <DialogHeader>
            <DialogTitle id="assign-bed-title">Assign bed</DialogTitle>
            <DialogDescription>
              Assign a ward and bed to <strong className="text-foreground">{patientName}</strong>'s requested admission.
            </DialogDescription>
          </DialogHeader>

          {generalError && (
            <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
              {generalError}
            </p>
          )}

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="assignWardId">Ward</Label>
            <WardSelect
              id="assignWardId"
              value={wardId}
              onValueChange={(value) => {
                setWardId(value);
                setBedId('');
              }}
            />
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="assignBedId">Bed</Label>
            <BedSelect id="assignBedId" value={bedId} onValueChange={setBedId} wardId={wardId} onBedsLoaded={setAvailableBeds} />
          </div>

          {selectedBed && (
            <div className="rounded-md border border-border bg-muted/30 px-3 py-2.5 text-sm">
              Bed <span className="font-semibold text-foreground">{selectedBed.bedNumber}</span>:{' '}
              <span className="font-semibold text-foreground">₹{selectedBed.dailyCharge.toLocaleString('en-IN')}/day</span>
            </div>
          )}

          <DialogFooter>
            <Button type="button" variant="outline" onClick={onCancel} disabled={isSubmitting}>
              Cancel
            </Button>
            <Button type="submit" disabled={isSubmitting || !wardId || !bedId}>
              {isSubmitting ? 'Assigning…' : 'Assign Bed'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
