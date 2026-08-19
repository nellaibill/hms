import { useState } from 'react';
import { ApiError, type TenantListItemResponse } from '@hms/shared';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { useDeleteHospitalMutation } from '../hooks/useDeleteHospitalMutation';
import { useDeletePreviewQuery } from '../hooks/useDeletePreviewQuery';

interface DeleteHospitalDialogProps {
  hospital: TenantListItemResponse;
  onClose: () => void;
}

export function DeleteHospitalDialog({ hospital, onClose }: DeleteHospitalDialogProps) {
  const previewQuery = useDeletePreviewQuery(hospital.id);
  const deleteMutation = useDeleteHospitalMutation();
  const [confirmText, setConfirmText] = useState('');

  const isConfirmed = confirmText.trim().toLowerCase() === hospital.hospitalCode.toLowerCase();

  const handleDelete = () => {
    deleteMutation.mutate(
      { id: hospital.id, confirmHospitalCode: confirmText.trim() },
      { onSuccess: onClose },
    );
  };

  const apiError = deleteMutation.error instanceof ApiError ? deleteMutation.error.message : null;

  return (
    <Dialog open onOpenChange={(open) => !open && onClose()}>
      <DialogContent aria-labelledby="delete-hospital-title">
        <DialogHeader>
          <DialogTitle id="delete-hospital-title">Delete hospital</DialogTitle>
          <DialogDescription>
            This blocks every user at <strong className="text-foreground">{hospital.hospitalName}</strong> from
            signing in and hides it from the active hospital list. Its data is not deleted — the hospital's own
            database stays fully intact, and this can be reversed later from the Deleted Hospitals list.
          </DialogDescription>
        </DialogHeader>

        {previewQuery.isPending && <p className="text-sm text-muted-foreground">Loading…</p>}

        {previewQuery.data && (
          <div className="flex flex-col gap-4">
            <dl className="grid grid-cols-2 gap-x-4 gap-y-1 text-sm">
              <dt className="text-muted-foreground">Hospital code</dt>
              <dd className="font-mono">{previewQuery.data.hospitalCode}</dd>
              <dt className="text-muted-foreground">Current status</dt>
              <dd>{previewQuery.data.status}</dd>
              <dt className="text-muted-foreground">Registered</dt>
              <dd>{new Date(previewQuery.data.createdAt).toLocaleDateString('en-IN')}</dd>
            </dl>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="confirm-hospital-code">
                Type <span className="font-mono">{hospital.hospitalCode}</span> to confirm
              </Label>
              <Input
                id="confirm-hospital-code"
                value={confirmText}
                onChange={(event) => setConfirmText(event.target.value)}
                autoComplete="off"
              />
            </div>

            {apiError && (
              <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
                {apiError}
              </p>
            )}
          </div>
        )}

        <DialogFooter>
          <Button variant="outline" onClick={onClose} disabled={deleteMutation.isPending}>
            Cancel
          </Button>
          <Button
            variant="destructive"
            onClick={handleDelete}
            disabled={!isConfirmed || deleteMutation.isPending}
          >
            {deleteMutation.isPending ? 'Deleting…' : 'Delete hospital'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
