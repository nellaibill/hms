import { Clock, Loader2, Pencil } from 'lucide-react';
import { Link, useParams } from 'react-router-dom';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { PageBanner } from '@/components/PageBanner';
import { useShiftQuery } from '../../features/shifts';

export default function ShiftViewPage() {
  const { id } = useParams<{ id: string }>();
  const { data: shift, isPending, isError } = useShiftQuery(id);

  if (isPending) {
    return (
      <div className="flex flex-1 items-center justify-center gap-2 p-6 text-sm text-muted-foreground">
        <Loader2 className="h-4 w-4 animate-spin" />
        Loading shift…
      </div>
    );
  }

  if (isError || !shift) {
    return (
      <div className="p-6">
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          Shift not found.
        </p>
      </div>
    );
  }

  return (
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={Clock}
        title={shift.name}
        titleExtra={shift.isNightShift && <Badge variant="secondary">Night</Badge>}
        subtitle={<span className="font-mono">{shift.code}</span>}
        backTo="/admin/hr/shifts"
        backLabel="Back to shifts"
        rightActions={
          <Button
            asChild
            variant="outline"
            className="gap-1.5 border-page-banner-foreground/30 bg-page-banner-foreground/10 text-page-banner-foreground hover:bg-page-banner-foreground/20"
          >
            <Link to={`/admin/hr/shifts/${shift.id}/edit`}>
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
              <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Timing</p>
              <p className="mt-1 font-mono text-sm text-foreground">
                {shift.startTime.slice(0, 5)} – {shift.endTime.slice(0, 5)}
              </p>
            </div>
            <div>
              <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Status</p>
              <p className="mt-1">
                <Badge variant={shift.isActive ? 'success' : 'secondary'}>{shift.isActive ? 'Active' : 'Inactive'}</Badge>
              </p>
            </div>
            <div>
              <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Break</p>
              <p className="mt-1 text-sm text-foreground">{shift.breakMinutes} minutes</p>
            </div>
            <div>
              <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Grace period</p>
              <p className="mt-1 text-sm text-foreground">{shift.graceMinutes} minutes</p>
            </div>
            <div>
              <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Created</p>
              <p className="mt-1 text-sm text-foreground">{new Date(shift.createdAt).toLocaleString('en-IN')}</p>
            </div>
            {shift.updatedAt && (
              <div>
                <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Last updated</p>
                <p className="mt-1 text-sm text-foreground">{new Date(shift.updatedAt).toLocaleString('en-IN')}</p>
              </div>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
