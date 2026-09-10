import { CalendarClock, Loader2, Pencil } from 'lucide-react';
import { Link, useParams } from 'react-router-dom';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { PageBanner } from '@/components/PageBanner';
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
      <PageBanner
        icon={CalendarClock}
        title={<StaffName staffId={record.staffId} />}
        subtitle={`${record.startDate} to ${record.endDate}`}
        backTo="/admin/hr/staff-availability"
        backLabel="Back to staff availability"
        rightActions={
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
        }
      />

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
