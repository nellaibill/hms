import { Loader2 } from 'lucide-react';
import { Link } from 'react-router-dom';
import { LoadingOverlay } from '@/components/LoadingOverlay';
import { PatientNameLink } from '@/components/PatientNameLink';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent } from '@/components/ui/card';
import { useOpdMarkedPatientsQuery } from '../hooks/useOpdMarkedPatientsQuery';
import { formatMarkedAt, type OpdFilterValues } from '../types';
import { OpdListHeading } from './OpdListHeading';

interface OpdMarkedPatientsTableProps {
  filters: OpdFilterValues;
}

/**
 * OPD Marked Patients tab — every consultation whose patient a doctor marked (the green "Mark
 * Patient" button on the consultation page), newest mark first, with the patient, the
 * department and consultant of that consultation, and the reason given. Department/Consultant/
 * Search filter it; the From/To dates and Status don't apply. A Consultant sees only their own
 * patients — the backend forces that.
 */
export function OpdMarkedPatientsTable({ filters }: OpdMarkedPatientsTableProps) {
  const { data, isPending, isPlaceholderData, isError, error } = useOpdMarkedPatientsQuery({
    departmentId: filters.departmentId,
    consultantId: filters.consultantId,
    search: filters.search || undefined,
  });

  return (
    <div className="flex flex-col gap-3">
      <OpdListHeading title="Marked Patients" from="" to="">
        {data &&
          (isPlaceholderData ? (
            <Badge variant="secondary" className="gap-1.5">
              <Loader2 className="h-3 w-3 animate-spin" />
              Updating…
            </Badge>
          ) : (
            <Badge variant="secondary">Total: {data.length} Marked</Badge>
          ))}
      </OpdListHeading>

      {isPending && (
        <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" />
          Loading marked patients…
        </div>
      )}

      {isError && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {error instanceof Error ? error.message : 'Failed to load marked patients.'}
        </p>
      )}

      <LoadingOverlay active={isPlaceholderData} label="Loading marked patients…" className="flex flex-col gap-3">
        {!isPending && !isError && data && data.length === 0 && (
          <Card className="border-dashed">
            <CardContent className="flex flex-col items-center gap-2 py-16 text-center">
              <p className="text-sm font-medium text-foreground">No marked patients</p>
              <p className="text-sm text-muted-foreground">A patient marked from an OPD consultation shows up here.</p>
            </CardContent>
          </Card>
        )}

        {!isPending && !isError && data && data.length > 0 && (
          <div className="overflow-x-auto rounded-lg border border-border">
            <table className="w-full text-sm">
              <thead className="bg-sidebar-active text-left text-xs font-medium uppercase tracking-wide text-sidebar-active-foreground">
                <tr>
                  <th className="px-4 py-2.5">#</th>
                  <th className="px-4 py-2.5">Marked On</th>
                  <th className="px-4 py-2.5">Patient Name</th>
                  <th className="px-4 py-2.5">Age</th>
                  <th className="px-4 py-2.5">Gender</th>
                  <th className="px-4 py-2.5">UHID</th>
                  <th className="px-4 py-2.5">Department</th>
                  <th className="px-4 py-2.5">Marked By</th>
                  <th className="px-4 py-2.5">Reason</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border">
                {data.map((row, index) => (
                  <tr key={row.consultationId} className="hover:bg-muted/30">
                    <td className="px-4 py-3 text-muted-foreground">{index + 1}</td>
                    <td className="whitespace-nowrap px-4 py-3 font-medium text-foreground">
                      <Link to={`/clinical/opd/consultations/${row.consultationId}`} className="hover:text-primary hover:underline" title="Open the consultation the patient was marked on">
                        {formatMarkedAt(row.markedAt)}
                      </Link>
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
                      <span className="whitespace-pre-wrap">{row.reason || '—'}</span>
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
