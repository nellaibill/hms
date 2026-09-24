import type { OpdConsultationDetail, PatientVisit } from '@hms/shared';
import { ExternalLink, HeartPulse, Loader2 } from 'lucide-react';
import type { ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { Badge } from '@/components/ui/badge';
import { resolveRecordLabel, useMasterOptionsQuery } from '@/features/masters';
import { formatDiagnosisLabel, formatPrescriptionDetails } from '@/features/opdConsultation/consultationLabels';
import { useOpdConsultationsByPatientQuery } from '@/features/opdConsultation';
import { useAuth } from '../../auth/AuthContext';
import { usePatientVisitsQuery } from '../hooks/usePatientVisitsQuery';

function formatDateTime(value?: string | null): string {
  return value ? new Date(value).toLocaleString('en-IN', { dateStyle: 'medium', timeStyle: 'short' }) : '—';
}

/** One labelled block — rendered only when there's something recorded, so the card shows what
 * the consultation actually captured rather than a wall of empty "—" rows. */
function Block({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="flex flex-col gap-0.5">
      <span className="text-xs font-semibold uppercase tracking-wide text-primary">{label}</span>
      <div className="text-sm text-foreground">{children}</div>
    </div>
  );
}

function Text({ value }: { value?: string | null }) {
  return <p className="whitespace-pre-wrap">{value}</p>;
}

/** The consultation type isn't on the consultation note/header — it lives on the Patients
 * module's own visit record, matched here by visit + consultant + department. */
function consultationTypeIdFor(visits: PatientVisit[] | undefined, detail: OpdConsultationDetail): string | null | undefined {
  const visit = visits?.find((v) => v.visitId === detail.header.visitId);
  return visit?.consultations.find((c) => c.consultantId === detail.header.consultantId && c.departmentId === detail.header.departmentId)
    ?.consultationTypeId;
}

function ConsultationCard({ detail, consultationTypeId }: { detail: OpdConsultationDetail; consultationTypeId?: string | null }) {
  const { header, note } = detail;
  const vitals = [
    note.heightCm != null && `Height ${note.heightCm} cm`,
    note.weightKg != null && `Weight ${note.weightKg} kg`,
    note.pulseRate != null && `PR ${note.pulseRate} bpm`,
    note.bloodPressure && `BP ${note.bloodPressure} mmHg`,
    note.temperatureF != null && `Temp ${note.temperatureF} °F`,
    note.spO2Percent != null && `SpO2 ${note.spO2Percent}%`,
  ].filter(Boolean) as string[];
  const hasFollowUp = Boolean(note.reviewDate || note.followUpInstructions || note.emergencyReviewInstructions);
  const hasReferral = Boolean(note.referralDepartmentId || note.referralConsultantId || note.referralReason);
  const hasAnyDetail =
    vitals.length > 0 ||
    Boolean(note.presentingComplaints || note.clinicalHistory || note.examinationFindings || note.planOfManagement) ||
    note.diagnoses.length > 0 ||
    note.investigations.length > 0 ||
    (note.prescriptions ?? []).length > 0 ||
    hasFollowUp ||
    hasReferral;

  return (
    <div className="flex flex-col overflow-hidden rounded-lg border border-foreground/15 bg-card">
      <div className="flex flex-wrap items-center justify-between gap-2 bg-sidebar-accent px-3 py-2 text-sidebar-foreground">
        <div className="flex flex-wrap items-center gap-x-3 gap-y-1 text-sm">
          <span className="font-semibold">{formatDateTime(header.appointmentTime)}</span>
          <span>{header.consultantName}</span>
          <span className="text-sidebar-foreground/75">{header.departmentName}</span>
          {consultationTypeId && (
            <Badge variant="outline" className="text-[10px]">
              {resolveRecordLabel('consultationType', consultationTypeId)}
            </Badge>
          )}
        </div>
        <div className="flex items-center gap-2">
          <Badge variant={note.status === 'Completed' ? 'success' : 'warning'} className="text-[10px]">
            {note.status}
          </Badge>
          <Link
            to={`/clinical/opd/consultations/${header.consultationId}`}
            className="inline-flex items-center gap-1 text-xs font-medium text-primary hover:underline"
          >
            Open <ExternalLink className="h-3 w-3" />
          </Link>
        </div>
      </div>

      <div className="grid grid-cols-1 gap-3 p-3 md:grid-cols-2">
        {!hasAnyDetail && <p className="text-sm text-muted-foreground">No clinical details recorded for this consultation yet.</p>}
        {vitals.length > 0 && (
          <Block label="Vitals">
            <p>{vitals.join(' · ')}</p>
          </Block>
        )}
        {note.presentingComplaints && (
          <Block label="Chief complaints">
            <Text value={note.presentingComplaints} />
          </Block>
        )}
        {note.clinicalHistory && (
          <Block label="History">
            <Text value={note.clinicalHistory} />
          </Block>
        )}
        {note.examinationFindings && (
          <Block label="Clinical notes (examination findings)">
            <Text value={note.examinationFindings} />
          </Block>
        )}
        {note.diagnoses.length > 0 && (
          <Block label="Diagnosis">
            <ul className="flex flex-col gap-0.5">
              {note.diagnoses.map((d) => (
                <li key={d.id ?? d.diagnosisId}>
                  {formatDiagnosisLabel(d)} <span className="text-xs text-muted-foreground">({d.type})</span>
                </li>
              ))}
            </ul>
          </Block>
        )}
        {note.investigations.length > 0 && (
          <Block label="Investigations">
            <ul className="flex flex-col gap-0.5">
              {note.investigations.map((i, index) => (
                <li key={i.id ?? index}>
                  {i.name}{' '}
                  <span className="text-xs text-muted-foreground">
                    ({i.department} · {i.priority})
                  </span>
                </li>
              ))}
            </ul>
          </Block>
        )}
        {(note.prescriptions ?? []).length > 0 && (
          <Block label="Prescription">
            <ul className="flex flex-col gap-0.5">
              {note.prescriptions.map((p, index) => (
                <li key={p.id ?? index}>
                  <span className="font-medium">{p.drugName}</span>
                  {formatPrescriptionDetails(p) && <span className="text-muted-foreground"> — {formatPrescriptionDetails(p)}</span>}
                  {p.instructions && <span className="text-xs text-muted-foreground"> ({p.instructions})</span>}
                </li>
              ))}
            </ul>
          </Block>
        )}
        {note.planOfManagement && (
          <Block label="Plan of management / medications">
            <Text value={note.planOfManagement} />
          </Block>
        )}
        {hasFollowUp && (
          <Block label="Follow-up">
            {note.reviewDate && <p>Review on {new Date(note.reviewDate).toLocaleDateString('en-IN')}</p>}
            {note.followUpInstructions && <Text value={note.followUpInstructions} />}
            {note.emergencyReviewInstructions && (
              <p className="whitespace-pre-wrap text-muted-foreground">Emergency: {note.emergencyReviewInstructions}</p>
            )}
          </Block>
        )}
        {hasReferral && (
          <Block label="Referral">
            <p>
              {[
                note.referralDepartmentId && resolveRecordLabel('department', note.referralDepartmentId),
                note.referralConsultantId && resolveRecordLabel('consultant', note.referralConsultantId),
              ]
                .filter(Boolean)
                .join(' — ')}
            </p>
            {note.referralReason && <Text value={note.referralReason} />}
          </Block>
        )}
      </div>
    </div>
  );
}

/** Medical Information tab — the patient's OPD consultation history, read straight from the
 * OpdConsultation module's own notes (the same records the consultation form saves). Nothing
 * here is new clinical data: every field shown is one the consultation form already captures,
 * and a field is only shown when it was actually recorded. */
export function PatientMedicalInformationTab({ patientId }: { patientId: string }) {
  const { hasPermission } = useAuth();
  const canViewClinical = hasPermission('clinical-care.view');
  const { data: consultations, isPending, isError } = useOpdConsultationsByPatientQuery(patientId, canViewClinical);
  const { data: visits } = usePatientVisitsQuery(patientId);
  // Primes the reference cache resolveRecordLabel reads for diagnosis/referral/type names.
  useMasterOptionsQuery('diagnosis');
  useMasterOptionsQuery('department');
  useMasterOptionsQuery('consultant');
  useMasterOptionsQuery('consultationType');

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
        Loading medical information…
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

  if (!consultations || consultations.length === 0) {
    return (
      <div className="flex flex-col items-center gap-2 rounded-lg border border-dashed border-border py-10 text-center text-sm text-muted-foreground">
        <HeartPulse className="h-6 w-6" />
        No consultation records for this patient yet. Allergies are shown on the Overview tab.
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-2.5">
      {consultations.map((detail) => (
        <ConsultationCard key={detail.header.consultationId} detail={detail} consultationTypeId={consultationTypeIdFor(visits, detail)} />
      ))}
    </div>
  );
}
