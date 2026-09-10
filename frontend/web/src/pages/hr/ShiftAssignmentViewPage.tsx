import { CalendarCheck2, Loader2, Pencil } from 'lucide-react';
import { Link, useParams } from 'react-router-dom';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { PageBanner } from '@/components/PageBanner';
import { StaffName } from '@/components/StaffName';
import { ShiftName, useShiftAssignmentQuery } from '../../features/shiftAssignments';

const statusVariant = { Scheduled: 'success', Completed: 'secondary', Cancelled: 'destructive' } as const;

export default function ShiftAssignmentViewPage() {
  const { id } = useParams<{ id: string }>();
  const { data: assignment, isPending, isError } = useShiftAssignmentQuery(id);

  if (isPending) {
    return (
      <div className="flex flex-1 items-center justify-center gap-2 p-6 text-sm text-muted-foreground">
        <Loader2 className="h-4 w-4 animate-spin" />
        Loading shift assignment…
      </div>
    );
  }

  if (isError || !assignment) {
    return (
      <div className="p-6">
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          Shift assignment not found.
        </p>
      </div>
    );
  }

  return (
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={CalendarCheck2}
        title={<StaffName staffId={assignment.staffId} />}
        titleExtra={<Badge variant={statusVariant[assignment.status]}>{assignment.status}</Badge>}
        subtitle={assignment.rosterDate}
        backTo="/admin/hr/shift-assignments"
        backLabel="Back to shift assignments"
        rightActions={
          <Button
            asChild
            variant="outline"
            className="gap-1.5 border-page-banner-foreground/30 bg-page-banner-foreground/10 text-page-banner-foreground hover:bg-page-banner-foreground/20"
          >
            <Link to={`/admin/hr/shift-assignments/${assignment.id}/edit`}>
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
              <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Shift</p>
              <p className="mt-1 text-sm text-foreground">
                <ShiftName shiftId={assignment.shiftId} />
              </p>
            </div>
            <div>
              <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Department ID</p>
              <p className="mt-1 font-mono text-sm text-foreground">{assignment.departmentId}</p>
            </div>
            <div>
              <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Remarks</p>
              <p className="mt-1 text-sm text-foreground">{assignment.remarks || '—'}</p>
            </div>
            <div>
              <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Created</p>
              <p className="mt-1 text-sm text-foreground">{new Date(assignment.createdAt).toLocaleString('en-IN')}</p>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
