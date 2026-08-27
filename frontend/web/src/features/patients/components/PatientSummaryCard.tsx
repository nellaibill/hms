import type { Patient } from '@hms/shared';
import { Droplet, Pencil, Phone, User, UserRound, VenusAndMars, type LucideIcon } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { bloodGroupLabel } from '../bloodGroupLabel';
import { usePatientDocumentUrl } from '../hooks/usePatientDocumentUrl';

interface PatientSummaryCardProps {
  patient: Patient;
}

function Stat({ icon: Icon, label, value }: { icon: LucideIcon; label: string; value: string }) {
  return (
    <div className="flex items-center gap-2.5 py-2">
      <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-md bg-accent text-muted-foreground">
        <Icon className="h-4 w-4" />
      </span>
      <div className="flex min-w-0 flex-col">
        <span className="text-[11px] uppercase tracking-wide text-muted-foreground">{label}</span>
        <span className="truncate text-sm font-medium text-foreground">{value}</span>
      </div>
    </div>
  );
}

/** Compact left-rail identity card — photo, name, UHID, and the handful of facts a
 * receptionist or doctor needs at a glance without opening the Profile tab. Replaces the
 * old full-width banner + Document Upload side panel (see PatientViewPage). */
export function PatientSummaryCard({ patient }: PatientSummaryCardProps) {
  const photoUrl = usePatientDocumentUrl(patient.id, 'Other');

  return (
    <div className="flex flex-col gap-4 rounded-lg border border-border bg-card p-5 shadow-soft-md">
      <div className="flex flex-col items-center gap-3 text-center">
        {photoUrl ? (
          <img
            src={photoUrl}
            alt={`${patient.firstName} ${patient.lastName}`}
            className="h-24 w-24 rounded-full border border-border object-cover"
          />
        ) : (
          <span className="flex h-24 w-24 items-center justify-center rounded-full border border-border bg-accent text-muted-foreground">
            <UserRound className="h-10 w-10" />
          </span>
        )}
        <div className="flex flex-col gap-1">
          <h1 className="text-base font-semibold text-foreground">
            {patient.title} {patient.firstName} {patient.lastName}
          </h1>
          <Badge variant="secondary" className="mx-auto font-mono text-[11px]">
            {patient.uhid}
          </Badge>
        </div>
      </div>

      <div className="flex flex-col divide-y divide-border border-y border-border">
        <Stat icon={User} label="Age" value={`${patient.age} yrs`} />
        <Stat icon={VenusAndMars} label="Gender" value={patient.gender} />
        <Stat icon={Droplet} label="Blood group" value={patient.bloodGroup ? bloodGroupLabel(patient.bloodGroup) : '—'} />
        <Stat icon={Phone} label="Mobile" value={patient.primaryPhone} />
      </div>

      <Button asChild variant="outline" className="gap-1.5">
        <Link to={`/patients/registration/${patient.id}/edit`}>
          <Pencil className="h-4 w-4" />
          Edit patient
        </Link>
      </Button>
    </div>
  );
}
