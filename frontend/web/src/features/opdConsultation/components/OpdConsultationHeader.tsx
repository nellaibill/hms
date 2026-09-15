import type { OpdConsultationHeader as OpdConsultationHeaderDto, OpdConsultationStatus } from '@hms/shared';
import { Building2, CalendarClock, Phone, Stethoscope, UserRound } from 'lucide-react';
import { OpdStatusBadge } from '@/features/opd/components/OpdStatusBadge';

interface OpdConsultationHeaderProps {
  header: OpdConsultationHeaderDto;
}

function formatAppointmentDateTime(iso: string): string {
  return new Date(iso).toLocaleString('en-GB', { day: '2-digit', month: 'short', year: 'numeric', hour: 'numeric', minute: '2-digit', hour12: true });
}

/** Read-only patient/appointment/consultant/department strip at the top of the OPD Consultation
 * form — every field here comes from Patients' own OPD queue data (via the backend's
 * IOpdQueryService seam), never editable here. Modeled on PatientSummaryCard's identity-bar
 * layout, compacted since this form already has a lot of vertical content below it. */
export function OpdConsultationHeader({ header }: OpdConsultationHeaderProps) {
  return (
    <div className="flex flex-col gap-3 rounded-lg border border-border border-l-4 border-l-primary bg-gradient-to-r from-primary/[0.06] via-card to-card p-4 shadow-soft-md lg:flex-row lg:items-center lg:justify-between">
      <div className="flex items-center gap-3">
        <span className="flex h-12 w-12 shrink-0 items-center justify-center rounded-full border-2 border-primary/30 bg-primary/10 text-primary">
          <UserRound className="h-6 w-6" />
        </span>
        <div className="flex flex-col gap-1">
          <div className="flex flex-wrap items-center gap-2">
            <h1 className="text-base font-semibold leading-none text-foreground">{header.patientName}</h1>
            <span className="font-mono text-xs text-muted-foreground">{header.uhid}</span>
          </div>
          <div className="flex flex-wrap items-center gap-x-3 gap-y-1 text-sm text-muted-foreground">
            <span>
              {header.age} Years / {header.gender}
            </span>
            <span className="inline-flex items-center gap-1">
              <Phone className="h-3.5 w-3.5" />
              {header.phoneNumber}
            </span>
          </div>
        </div>
      </div>

      <div className="flex flex-wrap items-center gap-x-6 gap-y-2">
        <div className="flex flex-col gap-0.5">
          <span className="flex items-center gap-1 text-xs text-muted-foreground">
            <CalendarClock className="h-3.5 w-3.5" />
            Appointment
          </span>
          <span className="text-sm font-medium text-foreground">{formatAppointmentDateTime(header.appointmentTime)}</span>
        </div>
        <div className="flex flex-col gap-0.5">
          <span className="flex items-center gap-1 text-xs text-muted-foreground">
            <Stethoscope className="h-3.5 w-3.5" />
            Consultant
          </span>
          <span className="text-sm font-medium text-foreground">{header.consultantName}</span>
        </div>
        <div className="flex flex-col gap-0.5">
          <span className="flex items-center gap-1 text-xs text-muted-foreground">
            <Building2 className="h-3.5 w-3.5" />
            Department
          </span>
          <span className="text-sm font-medium text-foreground">{header.departmentName}</span>
        </div>
        <OpdStatusBadge status={header.consultationStatus as OpdConsultationStatus} />
      </div>
    </div>
  );
}
