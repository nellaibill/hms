import { ArrowLeft, CalendarClock, Loader2, Pencil } from 'lucide-react';
import { Link, useParams } from 'react-router-dom';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { StaffName } from '@/components/StaffName';
import { useStaffAvailabilityRecordQuery } from '../../features/staffAvailability';

export default function StaffAvailabilityViewPage() {
  const { id } = useParams<{ id: string }>();
  const { data: record, isPending, isError } = useStaffAvailabilityRecordQuery(id);

  if (isPending) {
    return (
      <div className="flex flex-1 items-center justify-center gap-2 p-6 text-sm text-muted-foreground">
        <Loader2 className="h-4 w-4 animate-spin" />
        Loading availability record…
      </div>
    );
  }

  if (isError || !record) {
    return (
      <div className="p-6">
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          Availability record not found.
        </p>
      </div>
    );
  }

  return (
    <div className="flex flex-1 flex-col">
      <div className="px-6 pt-4 lg:px-8">
        <Link to="/admin/hr/staff-availability" className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
          <ArrowLeft className="h-4 w-4" />
          Back to staff availability
        </Link>
      </div>

      <div className="relative mt-3 flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        <div className="absolute right-6 top-1/2 -translate-y-1/2">
          <Button
            asChild
            variant="outline"
            className="gap-1.5 border-page-banner-foreground/30 bg-page-banner-foreground/10 text-page-banner-foreground hover:bg-page-banner-foreground/20"
          >
            <Link to={`/admin/hr/staff-availability/${record.id}/edit`}>
              <Pencil className="h-4 w-4" />
              Edit
            </Link>
          </Button>
        </div>
        <div className="flex items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
            <CalendarClock className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight">
            <StaffName staffId={record.staffId} />
          </h1>
        </div>
        <p className="text-sm text-page-banner-foreground/85">
          {record.startDate} to {record.endDate}
        </p>
      </div>

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
        <Card>
          <CardContent className="grid grid-cols-1 gap-4 py-6 sm:grid-cols-2">
            <div>
              <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Status</p>
              <p className="mt-1">
                <Badge variant={record.availabilityStatus === 'Available' ? 'success' : 'secondary'}>{record.availabilityStatus}</Badge>
              </p>
            </div>
            <div>
              <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Reason</p>
              <p className="mt-1 text-sm text-foreground">{record.reason || '—'}</p>
            </div>
            <div>
              <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Created</p>
              <p className="mt-1 text-sm text-foreground">{new Date(record.createdAt).toLocaleString('en-IN')}</p>
            </div>
            {record.updatedAt && (
              <div>
                <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Last updated</p>
                <p className="mt-1 text-sm text-foreground">{new Date(record.updatedAt).toLocaleString('en-IN')}</p>
              </div>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
