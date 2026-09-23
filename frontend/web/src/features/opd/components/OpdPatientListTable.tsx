import type { OpdConsultationStatus, OpdPatientListItem } from '@hms/shared';
import { Eye, Loader2, Stethoscope } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { LoadingOverlay } from '@/components/LoadingOverlay';
import { Pagination } from '@/components/Pagination';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent } from '@/components/ui/card';
import { useStartConsultationMutation } from '../hooks/useStartConsultationMutation';
import { useOpdPatientListQuery } from '../hooks/useOpdPatientListQuery';
import { useVisitPaymentStatusesQuery } from '../hooks/useVisitPaymentStatusesQuery';
import { OpdPaymentStatusBadge } from './OpdPaymentStatusBadge';
import { OpdStatusBadge } from './OpdStatusBadge';
import { toRangeEnd, toRangeStart, type OpdFilterValues } from '../types';

// A consultation still Waiting or already CheckedIn hasn't started yet — "Consult" starts it.
const CONSULTABLE_STATUSES: OpdConsultationStatus[] = ['Waiting', 'CheckedIn'];

// InConsultation/Completed rows have a real OpdConsultationNote — "View" opens that same
// clinical form (read-only once Completed, per OpdConsultationForm's own fieldset) so a
// consultation can be reopened to check, edit (while still InConsultation), print, or
// download it. Cancelled/NoShow rows never got that far, so they still go to the patient's
// registration/demographics page instead — there's no clinical note to show for those.
const CONSULTATION_VIEWABLE_STATUSES: OpdConsultationStatus[] = ['InConsultation', 'Completed'];

const PAGE_SIZE = 10;

// Includes the date, not just the time — the From/To filter above can span more than one day,
// so time alone can't tell two rows on different days apart.
function formatAppointmentDateTime(iso: string): string {
  return new Date(iso).toLocaleString('en-GB', {
    day: '2-digit',
    month: 'short',
    hour: 'numeric',
    minute: '2-digit',
    hour12: true,
  });
}

function formatHeadingDate(dateIso: string): string {
  if (!dateIso) {
    return '';
  }
  return new Date(dateIso).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' });
}

interface OpdPatientListTableProps {
  filters: OpdFilterValues;
  page: number;
  onPageChange: (page: number) => void;
}

/** OPD Patient List tab — the module's primary view: every OPD consultation queued for the
 * selected date range, with a "Consult"/"View" action per row depending on where the
 * consultation is in its Waiting -> CheckedIn -> InConsultation -> Completed lifecycle. */
export function OpdPatientListTable({ filters, page, onPageChange }: OpdPatientListTableProps) {
  const navigate = useNavigate();
  const startConsultation = useStartConsultationMutation();

  const { data, isPending, isPlaceholderData, isError, error } = useOpdPatientListQuery({
    page,
    pageSize: PAGE_SIZE,
    from: toRangeStart(filters.from),
    to: toRangeEnd(filters.to),
    departmentId: filters.departmentId,
    consultantId: filters.consultantId,
    status: filters.status as OpdConsultationStatus | undefined,
    search: filters.search || undefined,
  });

  function handleConsult(row: OpdPatientListItem) {
    startConsultation.mutate(row.consultationId, {
      onSuccess: () => navigate(`/clinical/opd/consultations/${row.consultationId}`),
    });
  }

  const { getStatus: getPaymentStatus } = useVisitPaymentStatusesQuery((data?.items ?? []).map((row) => row.patientId));

  return (
    <div className="flex flex-col gap-3">
      <Card>
        <CardContent className="flex flex-wrap items-center justify-between gap-3 py-4">
          <h2 className="text-sm font-semibold text-foreground">
            OPD Patients {formatHeadingDate(filters.to || filters.from) && `(${formatHeadingDate(filters.to || filters.from)})`}
          </h2>
          {data &&
            (isPlaceholderData ? (
              <Badge variant="secondary" className="gap-1.5">
                <Loader2 className="h-3 w-3 animate-spin" />
                Updating…
              </Badge>
            ) : (
              <Badge variant="secondary">Total: {data.meta.totalCount} Patients</Badge>
            ))}
        </CardContent>
      </Card>

      {isPending && (
        <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" />
          Loading OPD patients…
        </div>
      )}

      {isError && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {error instanceof Error ? error.message : 'Failed to load OPD patients.'}
        </p>
      )}

      <LoadingOverlay active={isPlaceholderData} label="Loading OPD patients…" className="flex flex-col gap-3">
        {!isPending && !isError && data && data.items.length === 0 && (
          <Card className="border-dashed">
            <CardContent className="flex flex-col items-center gap-2 py-16 text-center">
              <p className="text-sm font-medium text-foreground">No OPD patients found</p>
              <p className="text-sm text-muted-foreground">Try a different date range or filter.</p>
            </CardContent>
          </Card>
        )}

        {!isPending && !isError && data && data.items.length > 0 && (
          <>
            <div className="overflow-x-auto rounded-lg border border-border">
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead className="bg-muted/50 text-left text-xs font-medium uppercase tracking-wide text-muted-foreground">
                    <tr>
                      <th className="px-4 py-2.5">#</th>
                      <th className="px-4 py-2.5">Patient Name</th>
                      <th className="px-4 py-2.5">Age/Gender</th>
                      <th className="px-4 py-2.5">UHID</th>
                      <th className="px-4 py-2.5">Phone Number</th>
                      <th className="px-4 py-2.5">Consultant</th>
                      <th className="px-4 py-2.5">Department</th>
                      <th className="px-4 py-2.5">Appointment Date &amp; Time</th>
                      <th className="px-4 py-2.5">Status</th>
                      <th className="px-4 py-2.5">Payment Status</th>
                      <th className="px-4 py-2.5">Action</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-border">
                    {data.items.map((row, index) => (
                      <tr key={row.consultationId} className="hover:bg-muted/30">
                        <td className="px-4 py-3 text-muted-foreground">{(page - 1) * PAGE_SIZE + index + 1}</td>
                        <td className="whitespace-nowrap px-4 py-3 font-medium text-foreground">{row.patientName}</td>
                        <td className="whitespace-nowrap px-4 py-3 text-foreground">
                          {row.age} Years / {row.gender[0]}
                        </td>
                        <td className="whitespace-nowrap px-4 py-3 font-mono text-xs text-muted-foreground">{row.uhid}</td>
                        <td className="whitespace-nowrap px-4 py-3 text-foreground">{row.phoneNumber}</td>
                        <td className="whitespace-nowrap px-4 py-3 text-foreground">{row.consultantName}</td>
                        <td className="whitespace-nowrap px-4 py-3 text-foreground">{row.departmentName}</td>
                        <td className="whitespace-nowrap px-4 py-3 text-foreground">{formatAppointmentDateTime(row.appointmentTime)}</td>
                        <td className="whitespace-nowrap px-4 py-3">
                          <OpdStatusBadge status={row.status} />
                        </td>
                        <td className="whitespace-nowrap px-4 py-3">
                          <OpdPaymentStatusBadge status={getPaymentStatus(row.patientId, row.visitId)} />
                        </td>
                        <td className="whitespace-nowrap px-4 py-3">
                          {CONSULTABLE_STATUSES.includes(row.status) ? (
                            <Button size="sm" className="gap-1.5" disabled={startConsultation.isPending} onClick={() => handleConsult(row)}>
                              <Stethoscope className="h-3.5 w-3.5" />
                              Consult
                            </Button>
                          ) : CONSULTATION_VIEWABLE_STATUSES.includes(row.status) ? (
                            <Button
                              size="sm"
                              variant="outline"
                              className="gap-1.5"
                              onClick={() => navigate(`/clinical/opd/consultations/${row.consultationId}`)}
                            >
                              <Eye className="h-3.5 w-3.5" />
                              View
                            </Button>
                          ) : (
                            <Button size="sm" variant="outline" className="gap-1.5" onClick={() => navigate(`/patients/registration/${row.patientId}`)}>
                              <Eye className="h-3.5 w-3.5" />
                              View
                            </Button>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
            <Pagination meta={data.meta} onPageChange={onPageChange} />
          </>
        )}
      </LoadingOverlay>
    </div>
  );
}
