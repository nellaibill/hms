import type { EmployeeResponse } from '@hms/shared';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';

interface DeleteEmployeeDialogProps {
  employee: EmployeeResponse;
  isDeleting: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}

export function DeleteEmployeeDialog({ employee, isDeleting, onConfirm, onCancel }: DeleteEmployeeDialogProps) {
  return (
    <Dialog open onOpenChange={(open) => !open && onCancel()}>
      <DialogContent role="alertdialog" aria-labelledby="delete-employee-title">
        <DialogHeader>
          <DialogTitle id="delete-employee-title">Delete employee?</DialogTitle>
          <DialogDescription>
            This will remove{' '}
            <strong className="text-foreground">
              {employee.firstName} {employee.lastName}
            </strong>{' '}
            ({employee.employeeCode}) from active lists. The record is retained (soft delete).
          </DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <Button variant="outline" onClick={onCancel} disabled={isDeleting}>
            Cancel
          </Button>
          <Button variant="destructive" onClick={onConfirm} disabled={isDeleting}>
            {isDeleting ? 'Deleting…' : 'Delete'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
