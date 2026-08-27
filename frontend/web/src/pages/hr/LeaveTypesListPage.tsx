import type { LeaveTypeResponse } from '@hms/shared';
import { CalendarOff, Loader2, Plus } from 'lucide-react';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { Pagination } from '@/components/Pagination';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { useAuth } from '@/features/auth/AuthContext';
import { LeaveTypeFormDialog, LeaveTypeTable, useDeleteLeaveTypeMutation, useLeaveTypesQuery } from '../../features/leaveTypes';

export default function LeaveTypesListPage() {
  const [page, setPage] = useState(1);
  const [formDialog, setFormDialog] = useState<{ mode: 'create' | 'edit'; leaveType?: LeaveTypeResponse } | null>(null);
  const [leaveTypePendingDelete, setLeaveTypePendingDelete] = useState<LeaveTypeResponse | null>(null);

  const { hasPermission } = useAuth();
  const { data, isPending, isError, error } = useLeaveTypesQuery({ page, pageSize: 20, sort: 'name' });
  const deleteMutation = useDeleteLeaveTypeMutation();

  function handleConfirmDelete() {
    if (!leaveTypePendingDelete) return;
    deleteMutation.mutate(leaveTypePendingDelete.id, { onSuccess: () => setLeaveTypePendingDelete(null) });
  }

  return (
    <div className="flex flex-1 flex-col">
      <div className="px-6 pt-4 lg:px-8">
        <Link to="/admin/hr" className="text-sm text-muted-foreground hover:text-foreground">
          &larr; Back to HR
        </Link>
      </div>

      <div className="mt-3 flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        <div className="flex items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
            <CalendarOff className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight">Leave Types</h1>
        </div>
        <p className="max-w-2xl text-sm text-page-banner-foreground/85">Leave type reference data — max days/year and paid/unpaid status.</p>
      </div>

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
        <div className="flex justify-end">
          {hasPermission('workforce-admin.create') && (
            <Button className="gap-1.5" onClick={() => setFormDialog({ mode: 'create' })}>
              <Plus className="h-4 w-4" />
              New Leave Type
            </Button>
          )}
        </div>

        {isPending && (
          <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" />
            Loading leave types…
          </div>
        )}

        {isError && (
          <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
            {error instanceof Error ? error.message : 'Failed to load leave types.'}
          </p>
        )}

        {!isPending && !isError && data && data.items.length === 0 && (
          <Card className="border-dashed">
            <CardContent className="flex flex-col items-center gap-2 py-16 text-center">
              <p className="text-sm font-medium text-foreground">No leave types found</p>
              <p className="text-sm text-muted-foreground">Create the first leave type to get started.</p>
            </CardContent>
          </Card>
        )}

        {!isPending && !isError && data && data.items.length > 0 && (
          <div className="flex flex-col gap-3">
            <LeaveTypeTable
              leaveTypes={data.items}
              onEditRequested={(leaveType) => setFormDialog({ mode: 'edit', leaveType })}
              onDeleteRequested={setLeaveTypePendingDelete}
            />
            <Pagination meta={data.meta} onPageChange={setPage} />
          </div>
        )}

        {formDialog && (
          <LeaveTypeFormDialog mode={formDialog.mode} leaveType={formDialog.leaveType} onClose={() => setFormDialog(null)} />
        )}

        {leaveTypePendingDelete && (
          <Dialog open onOpenChange={(open) => !open && setLeaveTypePendingDelete(null)}>
            <DialogContent role="alertdialog" aria-labelledby="delete-leave-type-title">
              <DialogHeader>
                <DialogTitle id="delete-leave-type-title">Delete leave type?</DialogTitle>
                <DialogDescription>
                  This will remove <strong className="text-foreground">{leaveTypePendingDelete.name}</strong> ({leaveTypePendingDelete.code})
                  from active lists. The record is retained (soft delete).
                </DialogDescription>
              </DialogHeader>
              <DialogFooter>
                <Button variant="outline" onClick={() => setLeaveTypePendingDelete(null)} disabled={deleteMutation.isPending}>
                  Cancel
                </Button>
                <Button variant="destructive" onClick={handleConfirmDelete} disabled={deleteMutation.isPending}>
                  {deleteMutation.isPending ? 'Deleting…' : 'Delete'}
                </Button>
              </DialogFooter>
            </DialogContent>
          </Dialog>
        )}
      </div>
    </div>
  );
}
