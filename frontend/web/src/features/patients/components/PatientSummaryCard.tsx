import type { Gender, Patient } from '@hms/shared';
import {
  CalendarPlus,
  Droplet,
  FileUp,
  Mars,
  MoreHorizontal,
  Pencil,
  Printer,
  Transgender,
  Trash2,
  UserRound,
  Venus,
  VenusAndMars,
} from 'lucide-react';
import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { StaffName } from '@/components/StaffName';
import { useAuth } from '../../auth/AuthContext';
import { bloodGroupLabel } from '../bloodGroupLabel';
import { useDeletePatientMutation } from '../hooks/usePatientMutations';
import { usePatientDocumentUrl } from '../hooks/usePatientDocumentUrl';
import { usePatientVisitsQuery } from '../hooks/usePatientVisitsQuery';
import { DeletePatientDialog } from './DeletePatientDialog';

interface PatientSummaryCardProps {
  patient: Patient;
  /** Jumps the tab strip below to Documents — see PatientViewPage, which owns the controlled tab state. */
  onAddDocument: () => void;
}

function MetaItem({ children }: { children: React.ReactNode }) {
  return <span className="inline-flex items-center gap-1 text-sm text-foreground">{children}</span>;
}

/** Was previously a single hardcoded VenusAndMars (⚥) icon for every patient regardless of
 * gender — this maps each real Gender value to its own symbol, falling back to the combined
 * ⚥ glyph only for 'NA' (not known/not recorded), where no single symbol would be accurate. */
function GenderIcon({ gender, className }: { gender: Gender; className?: string }) {
  switch (gender) {
    case 'Male':
      return <Mars className={className} />;
    case 'Female':
      return <Venus className={className} />;
    case 'Transgender':
      return <Transgender className={className} />;
    case 'NA':
      return <VenusAndMars className={className} />;
  }
}

/** Compact, full-width identity + quick-glance bar for the patient view page — replaces the old
 * left-rail card. Shows only fields that exist on the Patient record (plus Last Visit, sourced
 * from the real visits list); there's deliberately no Patient Type / Primary Doctor / Next
 * Appointment / Status row here since none of those are tracked in the schema yet. */
export function PatientSummaryCard({ patient, onAddDocument }: PatientSummaryCardProps) {
  const photoUrl = usePatientDocumentUrl(patient.id, 'Other');
  const { data: visits } = usePatientVisitsQuery(patient.id);
  const { hasPermission } = useAuth();
  const navigate = useNavigate();
  const deleteMutation = useDeletePatientMutation();
  const [confirmingDelete, setConfirmingDelete] = useState(false);

  const lastVisit = visits?.[0];

  function handleConfirmDelete() {
    deleteMutation.mutate(patient.id, {
      onSuccess: () => navigate('/patients/registration'),
    });
  }

  return (
    // Soft primary-tinted gradient + left accent bar — gives the identity header some warmth
    // without competing with the section cards below it (which stay plain bg-card).
    <div className="flex flex-col gap-3 rounded-lg border border-border border-l-4 border-l-primary bg-gradient-to-r from-primary/[0.06] via-card to-card p-3 shadow-soft-md lg:flex-row lg:items-center lg:justify-between print:border-l-border print:bg-none print:shadow-none">
      <div className="flex items-center gap-3">
        {photoUrl ? (
          <img
            src={photoUrl}
            alt={`${patient.firstName} ${patient.lastName}`}
            className="h-14 w-14 shrink-0 rounded-full border-2 border-primary/30 object-cover"
          />
        ) : (
          <span className="flex h-14 w-14 shrink-0 items-center justify-center rounded-full border-2 border-primary/30 bg-primary/10 text-primary">
            <UserRound className="h-6 w-6" />
          </span>
        )}

        <div className="flex flex-col gap-1">
          <div className="flex flex-wrap items-center gap-2">
            <h1 className="text-lg font-semibold leading-none text-foreground">
              {patient.title} {patient.firstName} {patient.lastName}
            </h1>
            <Badge variant="secondary" className="font-mono text-[11px]">
              {patient.uhid}
            </Badge>
          </div>

          <div className="flex flex-wrap items-center gap-x-3 gap-y-1 text-sm text-muted-foreground">
            <MetaItem>{patient.age} Yrs</MetaItem>
            <span className="text-border">·</span>
            <MetaItem>
              <GenderIcon gender={patient.gender} className="h-3.5 w-3.5" />
              {patient.gender}
            </MetaItem>
            <span className="text-border">·</span>
            <MetaItem>
              <Droplet className="h-3.5 w-3.5 text-destructive" />
              {bloodGroupLabel(patient.bloodGroup)}
            </MetaItem>
            <span className="text-border">·</span>
            <MetaItem>{patient.primaryPhone}</MetaItem>
          </div>

          <div className="flex flex-wrap items-center gap-x-3 gap-y-1 text-xs text-muted-foreground">
            <span>Registered {new Date(patient.createdAt).toLocaleDateString('en-IN')}</span>
            <span className="text-border">·</span>
            <span>
              Last visit{' '}
              {lastVisit ? `${new Date(lastVisit.createdAt).toLocaleDateString('en-IN')} (${lastVisit.visitType})` : '—'}
            </span>
            {patient.updatedAt && (
              <>
                <span className="text-border">·</span>
                <span>
                  Last updated {new Date(patient.updatedAt).toLocaleDateString('en-IN')}
                  {patient.updatedBy && (
                    <>
                      {' '}
                      by <StaffName staffId={patient.updatedBy} />
                    </>
                  )}
                </span>
              </>
            )}
          </div>
        </div>
      </div>

      {/* Add Visit sits in its own centered row under the main action row, rather than
          alongside it — this page's main actions (Edit/Add Document/Print/…) are all about
          the patient record itself, where Add Visit starts a whole separate flow (a new
          registration/encounter) that deserves its own visual weight, not to compete for
          space in an already-full row. */}
      <div className="flex shrink-0 flex-col items-center gap-2 print:hidden">
        <div className="flex flex-wrap items-center gap-2">
          {hasPermission('patient-management.edit') && (
            <Button asChild size="sm" className="gap-1.5">
              <Link to={`/patients/registration/${patient.id}/edit`}>
                <Pencil className="h-4 w-4" />
                Edit Patient
              </Link>
            </Button>
          )}
          <Button variant="outline" size="sm" className="gap-1.5" onClick={onAddDocument}>
            <FileUp className="h-4 w-4" />
            Add Document
          </Button>
          <Button variant="outline" size="sm" className="gap-1.5" onClick={() => window.print()}>
            <Printer className="h-4 w-4" />
            Print
          </Button>
          {hasPermission('patient-management.delete') && (
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="outline" size="icon" aria-label="More actions">
                  <MoreHorizontal className="h-4 w-4" />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                <DropdownMenuItem className="text-destructive focus:text-destructive" onClick={() => setConfirmingDelete(true)}>
                  <Trash2 className="h-4 w-4" />
                  Delete patient
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          )}
        </div>
        {/* Primary color + default (not sm) size — same treatment as Edit Patient above, since
            this is just as consequential an action and was getting lost visually as a small
            outline button. */}
        {hasPermission('patient-management.edit') && (
          <Button asChild className="gap-1.5">
            <Link to={`/patients/registration/${patient.id}/visits/new`}>
              <CalendarPlus className="h-4 w-4" />
              Add Visit
            </Link>
          </Button>
        )}
      </div>

      {confirmingDelete && (
        <DeletePatientDialog
          patient={patient}
          isDeleting={deleteMutation.isPending}
          onConfirm={handleConfirmDelete}
          onCancel={() => setConfirmingDelete(false)}
        />
      )}
    </div>
  );
}
