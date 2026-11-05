import type { PatientVisit } from '@hms/shared';
import { AlertTriangle } from 'lucide-react';
import { Fragment } from 'react';
import { Link } from 'react-router-dom';
import { ConsultantName } from '@/components/ConsultantName';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent } from '@/components/ui/card';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { encounterTypeShortLabel } from '@/features/patients/encounterTypeLabel';
import { visitDay, type VisitBillingStatus } from '../billingVisit';

const STATUS_LABELS: Record<VisitBillingStatus, string> = {
  NotBilled: 'Not billed',
  Pending: 'Payment pending',
  OnHold: 'On hold',
  Paid: 'Paid',
};

const STATUS_VARIANTS: Record<VisitBillingStatus, 'secondary' | 'warning' | 'success'> = {
  NotBilled: 'secondary',
  Pending: 'warning',
  OnHold: 'warning',
  Paid: 'success',
};

function formatVisitTime(visit: PatientVisit): string {
  return new Date(visit.createdAt).toLocaleString('en-IN', { day: '2-digit', month: 'short', year: 'numeric', hour: 'numeric', minute: '2-digit' });
}

/** "OP · OP-2026-000007 · 28 Oct 2026, 5:35 pm · Dr. L. Madhavi" — what a visit is, at a glance. */
function VisitDescription({ visit }: { visit: PatientVisit }) {
  return (
    <>
      {encounterTypeShortLabel(visit.visitType)}
      {visit.visitNumber ? ` · ${visit.visitNumber}` : ''} · {formatVisitTime(visit)}
      {visit.consultations.map((consultation) => (
        <Fragment key={`${consultation.departmentId}-${consultation.consultantId}`}>
          {' · '}
          <ConsultantName consultantId={consultation.consultantId} />
        </Fragment>
      ))}
    </>
  );
}

interface BillingVisitCardProps {
  patientId: string;
  /** Newest first. */
  visits: PatientVisit[];
  /** The visit being billed — undefined while a choice is needed, or when there's no visit. */
  visit: PatientVisit | undefined;
  needsChoice: boolean;
  statusOf: (visitId: string) => VisitBillingStatus;
  /** Today's local date (YYYY-MM-DD), to flag a visit from an earlier day. */
  today: string;
  onChange: (visitId: string) => void;
}

/**
 * OPD Billing Entry's "Billing visit" (ADR-117): which of the patient's visits this bill is saved
 * against — and so which visit gets the OP No and reads Paid on the OPD list. Before this the page
 * silently billed the newest visit, and one doctor's fee landed on another doctor's visit. Shown
 * above the bill; with more than one visit, a dropdown changes it.
 */
export function BillingVisitCard({ patientId, visits, visit, needsChoice, statusOf, today, onChange }: BillingVisitCardProps) {
  const addVisitLink = (
    <Link to={`/patients/registration/${patientId}/visits/new`} className="font-medium text-primary hover:underline">
      Add Visit
    </Link>
  );
  const unpaidToday = visits.filter((candidate) => visitDay(candidate) === today && statusOf(candidate.visitId) !== 'Paid').length;

  return (
    <Card>
      <CardContent className="flex flex-col gap-3 py-4">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div className="flex min-w-0 flex-col gap-0.5">
            <span className="text-xs text-muted-foreground">Billing visit</span>
            {visit ? (
              <span className="flex flex-wrap items-center gap-2 text-sm font-semibold text-foreground">
                <span>
                  <VisitDescription visit={visit} />
                </span>
                <Badge variant={STATUS_VARIANTS[statusOf(visit.visitId)]}>{STATUS_LABELS[statusOf(visit.visitId)]}</Badge>
              </span>
            ) : needsChoice ? (
              <span className="text-sm font-semibold text-foreground">Choose the visit you're billing</span>
            ) : (
              <span className="text-sm font-semibold text-foreground">No visit recorded</span>
            )}
          </div>
          {visits.length > 1 && (
            <Select value={visit?.visitId ?? ''} onValueChange={onChange}>
              <SelectTrigger className="w-full sm:w-96" aria-label="Visit to bill">
                <SelectValue placeholder="Choose a visit" />
              </SelectTrigger>
              <SelectContent>
                {visits.map((candidate) => (
                  <SelectItem key={candidate.visitId} value={candidate.visitId}>
                    <VisitDescription visit={candidate} /> — {STATUS_LABELS[statusOf(candidate.visitId)]}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          )}
        </div>

        {needsChoice && (
          <p className="flex items-start gap-2 rounded-md border border-warning/40 bg-warning/10 px-3 py-2 text-sm text-foreground">
            <AlertTriangle className="mt-0.5 h-4 w-4 shrink-0 text-warning" />
            <span>This patient has {unpaidToday} visits today that aren't paid yet. Choose the one you're billing.</span>
          </p>
        )}
        {visit && visitDay(visit) !== today && (
          <p className="flex items-start gap-2 rounded-md border border-warning/40 bg-warning/10 px-3 py-2 text-sm text-foreground">
            <AlertTriangle className="mt-0.5 h-4 w-4 shrink-0 text-warning" />
            <span>
              This visit is from {new Date(visit.createdAt).toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' })}. If the patient
              has come for a new visit today, {addVisitLink} first.
            </span>
          </p>
        )}
        {!visit && !needsChoice && (
          <p className="text-sm text-muted-foreground">
            This bill won't be linked to a visit, so no OP No is issued for it. To bill a visit, {addVisitLink} first.
          </p>
        )}
      </CardContent>
    </Card>
  );
}
