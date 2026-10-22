import { Loader2 } from 'lucide-react';
import { Link } from 'react-router-dom';
import { LoadingOverlay } from '@/components/LoadingOverlay';
import { PatientNameLink } from '@/components/PatientNameLink';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent } from '@/components/ui/card';
import { useOpdScheduledVisitsQuery } from '../hooks/useOpdScheduledVisitsQuery';
import { formatScheduledDate, todayIsoDate, type OpdFilterValues } from '../types';
import { OpdListHeading } from './OpdListHeading';

interface OpdScheduledVisitsTableProps {
  filters: OpdFilterValues;
}

/**
 * OPD Scheduled Visits tab — every patient's upcoming visits, from today on, soonest first: the
 * review date a doctor set on an OPD consultation, with the patient, the department and
 * consultant of that consultation, and the doctor's follow-up notes. Department/Consultant/
 * Search filter it; the From/To dates don't apply (it always starts today). A Consultant sees
 * only their own patients — the backend forces that.
 */
export function OpdScheduledVisitsTable({ filters }: OpdScheduledVisitsTableProps) {
  const today = todayIsoDate();
  const { data, isPending, isPlaceholderData, isError, error } = useOpdScheduledVisitsQuery({
    from: today,
    departmentId: filters.departmentId,
    consultantId: filters.consultantId,
    search: filters.search || undefined,
  });

  return (
    <div className="flex flex-col gap-3">
      <OpdListHeading title="Scheduled Visits" from={today} to="">
        {data &&
          (isPlaceholderData ? (
            <Badge variant="secondary" className="gap-1.5">
              <Loader2 className="h-3 w-3 animate-spin" />
              Updating…
            </Badge>
          ) : (
            <Badge variant="secondary">Total: {data.length} Scheduled</Badge>
          ))}
      </OpdListHeading>

      {isPending && (
        <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" />
          Loading scheduled visits…
        </div>
      )}

      {isError && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {error instanceof Error ? error.message : 'Failed to load scheduled visits.'}
        </p>
      )}

      <LoadingOverlay active={isPlaceholderData} label="Loading scheduled visits…" className="flex flex-col gap-3">
        {!isPending && !isError && data && data.length === 0 && (
          <Card className="border-dashed">
            <CardContent className="flex flex-col items-center gap-2 py-16 text-center">
              <p className="text-sm font-medium text-foreground">No scheduled visits</p>
              <p className="text-sm text-muted-foreground">A review date set in an OPD consultation shows up here.</p>
            </CardContent>
          </Card>
        )}

        {!isPending && !isError && data && data.length > 0 && (
          <div className="overflow-x-auto rounded-lg border border-border">
            <table className="w-full text-sm">
              <thead className="bg-sidebar-active text-left text-xs font-medium uppercase tracking-wide text-sidebar-active-foreground">
                <tr>
                  <th className="px-4 py-2.5">#</th>
                  <th className="px-4 py-2.5">Date</th>
                  <th className="px-4 py-2.5">Patient Name</th>
                  <th className="px-4 py-2.5">Age</th>
                  <th className="px-4 py-2.5">Gender</th>
                  <th className="px-4 py-2.5">UHID</th>
                  <th className="px-4 py-2.5">Department</th>
                  <th className="px-4 py-2.5">Consultant</th>
                  <th className="px-4 py-2.5">Notes</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border">
                {data.map((row, index) => (
                  <tr key={row.consultationId} className="hover:bg-muted/30">
                    <td className="px-4 py-3 text-muted-foreground">{index + 1}</td>
                    <td className="whitespace-nowrap px-4 py-3 font-medium text-foreground">
                      <Link to={`/clinical/opd/consultations/${row.consultationId}`} className="hover:text-primary hover:underline" title="Open the consultation this visit was scheduled in">
                        {formatScheduledDate(row.scheduledDate)}
                      </Link>
                      {row.scheduledDate === today && (
                        <Badge variant="warning" className="ml-2 text-[10px]">
                          Today
                        </Badge>
                      )}
                    </td>
                    <td className="whitespace-nowrap px-4 py-3">
                      <PatientNameLink patientId={row.patientId}>{row.patientName}</PatientNameLink>
                    </td>
                    <td className="whitespace-nowrap px-4 py-3 text-foreground">{row.age} Years</td>
                    <td className="whitespace-nowrap px-4 py-3 text-foreground">{row.gender}</td>
                    <td className="whitespace-nowrap px-4 py-3 font-mono text-xs text-muted-foreground">{row.uhid}</td>
                    <td className="whitespace-nowrap px-4 py-3 text-foreground">{row.departmentName}</td>
                    <td className="whitespace-nowrap px-4 py-3 text-foreground">{row.consultantName}</td>
                    <td className="min-w-[12rem] px-4 py-3 text-muted-foreground">
                      <span className="whitespace-pre-wrap">{row.notes || '—'}</span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </LoadingOverlay>
    </div>
  );
}
