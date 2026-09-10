import { Loader2, Pencil, Repeat } from 'lucide-react';
import { Link, useParams } from 'react-router-dom';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { PageBanner } from '@/components/PageBanner';
import { StaffName } from '@/components/StaffName';
import { useSwapRequestQuery } from '../../features/shiftSwapRequests';

const statusVariant = { Pending: 'warning', Approved: 'success', Rejected: 'destructive', Cancelled: 'secondary' } as const;

export default function ShiftSwapRequestViewPage() {
  const { id } = useParams<{ id: string }>();
  const { data: request, isPending, isError } = useSwapRequestQuery(id);

  if (isPending) {
    return (
      <div className="flex flex-1 items-center justify-center gap-2 p-6 text-sm text-muted-foreground">
        <Loader2 className="h-4 w-4 animate-spin" />
        Loading shift swap request…
      </div>
    );
  }

  if (isError || !request) {
    return (
      <div className="p-6">
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          Shift swap request not found.
        </p>
      </div>
    );
  }

  return (
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={Repeat}
        title={
          <>
            <StaffName staffId={request.requestedByStaffId} /> ↔ <StaffName staffId={request.requestedToStaffId} />
          </>
        }
        titleExtra={<Badge variant={statusVariant[request.status]}>{request.status}</Badge>}
        subtitle={`Requested ${new Date(request.requestedDate).toLocaleString('en-IN')}`}
        backTo="/admin/hr/shift-swap-requests"
        backLabel="Back to shift swap requests"
        rightActions={
          <Button
            asChild
            variant="outline"
            className="gap-1.5 border-page-banner-foreground/30 bg-page-banner-foreground/10 text-page-banner-foreground hover:bg-page-banner-foreground/20"
          >
            <Link to={`/admin/hr/shift-swap-requests/${request.id}/edit`}>
              <Pencil className="h-4 w-4" />
              Edit
            </Link>
          </Button>
        }
      />

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
        <Card>
          <CardContent className="grid grid-cols-1 gap-4 py-6 sm:grid-cols-2">
            <div>
              <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Current shift assignment</p>
              <p className="mt-1 font-mono text-xs text-foreground">{request.currentShiftAssignmentId}</p>
            </div>
            <div>
              <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Requested shift assignment</p>
              <p className="mt-1 font-mono text-xs text-foreground">{request.requestedShiftAssignmentId}</p>
            </div>
            <div>
              <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Approved by</p>
              <p className="mt-1 text-sm text-foreground">{request.approvedBy ? <StaffName staffId={request.approvedBy} /> : '—'}</p>
            </div>
            <div>
              <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Approved date</p>
              <p className="mt-1 text-sm text-foreground">
                {request.approvedDate ? new Date(request.approvedDate).toLocaleString('en-IN') : '—'}
              </p>
            </div>
            <div className="sm:col-span-2">
              <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Remarks</p>
              <p className="mt-1 text-sm text-foreground">{request.remarks || '—'}</p>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
