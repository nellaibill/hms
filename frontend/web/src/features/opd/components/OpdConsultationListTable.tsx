import { Loader2, Users } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { formatConsultantAvailability } from '@/features/masters';
import { useOpdConsultationSummaryQuery } from '../hooks/useOpdConsultationSummaryQuery';
import { toRangeEnd, toRangeStart, type OpdFilterValues } from '../types';

interface OpdConsultationListTableProps {
  filters: OpdFilterValues;
  /** Switches the page to the Patient List tab, scoped to this row's consultant — the
   * "View Patients" action. */
  onViewPatients: (consultantId: string) => void;
}

/** OPD Consultation List tab — one row per consultant with any OPD activity in the selected
 * date range, aggregated by HMS.Modules.Patients.Contracts.OpdConsultationSummaryItem. Not
 * paginated (the backend returns a plain list); this endpoint only filters on from/to/
 * departmentId/consultantId — the shared filter bar's Status/Search fields don't apply here. */
export function OpdConsultationListTable({ filters, onViewPatients }: OpdConsultationListTableProps) {
  const { data, isPending, isError, error } = useOpdConsultationSummaryQuery({
    from: toRangeStart(filters.from),
    to: toRangeEnd(filters.to),
    departmentId: filters.departmentId,
    consultantId: filters.consultantId,
  });

  if (isPending) {
    return (
      <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
        <Loader2 className="h-4 w-4 animate-spin" />
        Loading consultation summary…
      </div>
    );
  }

  if (isError) {
    return (
      <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
        {error instanceof Error ? error.message : 'Failed to load consultation summary.'}
      </p>
    );
  }

  if (!data || data.length === 0) {
    return (
      <Card className="border-dashed">
        <CardContent className="flex flex-col items-center gap-2 py-16 text-center">
          <p className="text-sm font-medium text-foreground">No consultation activity found</p>
          <p className="text-sm text-muted-foreground">Try a different date range or department.</p>
        </CardContent>
      </Card>
    );
  }

  return (
    <div className="overflow-x-auto rounded-lg border border-border">
      <div className="overflow-x-auto">
        <table className="w-full text-sm">
          <thead className="bg-muted/50 text-left text-xs font-medium uppercase tracking-wide text-muted-foreground">
            <tr>
              <th className="px-4 py-2.5">Consultant</th>
              <th className="px-4 py-2.5">Department</th>
              <th className="px-4 py-2.5">Availability</th>
              <th className="px-4 py-2.5">Total Patients</th>
              <th className="px-4 py-2.5">Waiting</th>
              <th className="px-4 py-2.5">In Consultation</th>
              <th className="px-4 py-2.5">Completed</th>
              <th className="px-4 py-2.5">Action</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border">
            {data.map((row) => (
              <tr key={row.consultantId} className="hover:bg-muted/30">
                <td className="whitespace-nowrap px-4 py-3 font-medium text-foreground">{row.consultantName}</td>
                <td className="whitespace-nowrap px-4 py-3 text-foreground">{row.departmentName}</td>
                <td className="whitespace-nowrap px-4 py-3 text-muted-foreground">
                  {formatConsultantAvailability({
                    availableDays: row.availableDays ?? [],
                    visitStartTime: row.visitStartTime,
                    visitEndTime: row.visitEndTime,
                    visitStartTime2: row.visitStartTime2,
                    visitEndTime2: row.visitEndTime2,
                  }) ?? '—'}
                </td>
                <td className="whitespace-nowrap px-4 py-3 text-foreground">{row.totalPatients}</td>
                <td className="whitespace-nowrap px-4 py-3 text-foreground">{row.waiting}</td>
                <td className="whitespace-nowrap px-4 py-3 text-foreground">{row.inConsultation}</td>
                <td className="whitespace-nowrap px-4 py-3 text-foreground">{row.completed}</td>
                <td className="whitespace-nowrap px-4 py-3">
                  <Button size="sm" variant="outline" className="gap-1.5" onClick={() => onViewPatients(row.consultantId)}>
                    <Users className="h-3.5 w-3.5" />
                    View Patients
                  </Button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
