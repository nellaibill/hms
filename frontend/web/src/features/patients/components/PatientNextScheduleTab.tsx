import type { OpdConsultationDetail, PatientVisit } from '@hms/shared';
import { CalendarClock, ExternalLink, Loader2 } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Badge } from '@/components/ui/badge';
import { useOpdConsultationsByPatientQuery } from '@/features/opdConsultation';
import { useAuth } from '../../auth/AuthContext';
import { usePatientVisitsQuery } from '../hooks/usePatientVisitsQuery';

/** Today as "YYYY-MM-DD" in the browser's own (hospital) time zone — the same shape as a
 * consultation's reviewDate, so the two compare as plain strings. */
function todayIso(): string {
  const now = new Date();
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`;
}

/** A "YYYY-MM-DD" date read as a local calendar day — new Date("YYYY-MM-DD") alone is UTC midnight,
 * which shows as the previous day west of UTC. */
function formatIsoDate(value: string): string {
  const [year, month, day] = value.split('-').map(Number);
  return new Date(year, month - 1, day).toLocaleDateString('en-IN');
}

/** "21/10/2026 (OP-2026-000006)" — the visit the review was scheduled on, with its OP/IP No, or
 * what that number is still waiting for (ADR-103/ADR-104). */
function scheduledOnLabel(visit: PatientVisit | undefined, consultation: OpdConsultationDetail): string {
  const date = new Date(visit?.createdAt ?? consultation.header.appointmentTime).toLocaleDateString('en-IN');
  const number = visit?.visitNumber ?? (visit?.visitNumberPending ? (visit.visitType === 'OP' ? 'Fee pending' : 'Advance pending') : null);
  return number ? `${date} (${number})` : date;
}

/**
 * Next Schedule tab — the patient's upcoming visits: every review date a doctor set on an OPD
 * consultation that is today or later, soonest first, with the department and consultant of that
 * consultation and the visit it was scheduled on. The review date is the only structured "next
 * visit" the app records today, so that's what this lists.
 */
export function PatientNextScheduleTab({ patientId }: { patientId: string }) {
  const { hasPermission } = useAuth();
  const canViewClinical = hasPermission('clinical-care.view');
  // Same queries (and cache entries) as the Medical Information tab.
  const { data: consultations, isPending, isError } = useOpdConsultationsByPatientQuery(patientId, canViewClinical);
  const { data: visits } = usePatientVisitsQuery(patientId);

  if (!canViewClinical) {
    return (
      <p className="rounded-lg border border-dashed border-border py-10 text-center text-sm text-muted-foreground">
        You don't have permission to view clinical consultation records.
      </p>
    );
  }

  if (isPending) {
    return (
      <div className="flex items-center justify-center gap-2 py-10 text-sm text-muted-foreground">
        <Loader2 className="h-4 w-4 animate-spin" />
        Loading the next schedule…
      </div>
    );
  }

  if (isError) {
    return (
      <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
        Couldn't load consultation records — please try again.
      </p>
    );
  }

  const today = todayIso();
  const upcoming = (consultations ?? [])
    .filter((consultation): consultation is OpdConsultationDetail & { note: { reviewDate: string } } =>
      Boolean(consultation.note.reviewDate && consultation.note.reviewDate >= today),
    )
    .sort((a, b) => a.note.reviewDate.localeCompare(b.note.reviewDate));

  if (upcoming.length === 0) {
    return (
      <div className="flex flex-col items-center gap-2 rounded-lg border border-dashed border-border py-10 text-center text-sm text-muted-foreground">
        <CalendarClock className="h-6 w-6" />
        No upcoming visits scheduled. A review date set in an OPD consultation shows here.
      </div>
    );
  }

  return (
    <div className="overflow-x-auto rounded-lg border border-border">
      <table className="w-full text-sm">
        <thead className="bg-sidebar-active text-left text-xs font-medium uppercase tracking-wide text-sidebar-active-foreground">
          <tr>
            <th className="px-3 py-2">Scheduled Date</th>
            <th className="px-3 py-2">Department</th>
            <th className="px-3 py-2">Consultant</th>
            <th className="px-3 py-2">Scheduled On</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {upcoming.map((consultation) => {
            const { header, note } = consultation;
            return (
              <tr key={header.consultationId} className="hover:bg-muted/30">
                <td className="whitespace-nowrap px-3 py-2 font-medium text-foreground">
                  {formatIsoDate(note.reviewDate)}
                  {note.reviewDate === today && (
                    <Badge variant="warning" className="ml-2 text-[10px]">
                      Today
                    </Badge>
                  )}
                </td>
                <td className="px-3 py-2 text-foreground">{header.departmentName}</td>
                <td className="px-3 py-2 text-foreground">{header.consultantName}</td>
                <td className="whitespace-nowrap px-3 py-2 text-muted-foreground">
                  <Link
                    to={`/clinical/opd/consultations/${header.consultationId}`}
                    className="inline-flex items-center gap-1 hover:text-primary hover:underline"
                    title="Open the consultation this review was scheduled in"
                  >
                    {scheduledOnLabel(
                      visits?.find((visit) => visit.visitId === header.visitId),
                      consultation,
                    )}
                    <ExternalLink className="h-3 w-3" />
                  </Link>
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}
