import { Loader2, Users } from 'lucide-react';
import { useMemo, useState } from 'react';
import { arrivalSourceLabel } from '@/features/patients/arrivalSourceLabel';
import { PatientBreakdownChart } from '@/features/patients/components/PatientBreakdownChart';
import { RegistrationsTrendChart } from '@/features/patients/components/RegistrationsTrendChart';
import { encounterTypeShortLabel } from '@/features/patients/encounterTypeLabel';
import { usePatientsForReportQuery } from '@/features/patients/hooks/usePatientsForReportQuery';
import { usePatientVisitsForReportQuery } from '@/features/patients/hooks/usePatientVisitsForReportQuery';
import { maritalStatusLabel } from '@/features/patients/maritalStatusLabel';
import {
  getAllergyPrevalence,
  getPatientsByArrivalSource,
  getPatientsByBloodGroup,
  getPatientsByGender,
  getPatientsByMaritalStatus,
  getRegistrationsOverTime,
  getVisitsByConsultant,
  getVisitsByDepartment,
  getVisitsByType,
  type ReportDateRange,
} from '@/features/patients/patientReport';
import { resolveRecordLabel } from '@/features/masters';
import { useMasterOptionsQuery } from '@/features/masters';
import { Card, CardContent } from '@/components/ui/card';
import { PageBanner } from '@/components/PageBanner';
import { ReportDateRangeFilter } from '@/features/reports';
import type { MaritalStatus, ModeOfArrivalSource, VisitType } from '@hms/shared';

function toDateInputValue(date: Date): string {
  return date.toISOString().slice(0, 10);
}

function defaultRange(): ReportDateRange {
  const today = new Date();
  const from = new Date(today);
  from.setDate(from.getDate() - 90);
  return { from: toDateInputValue(from), to: toDateInputValue(today) };
}

/**
 * Patient Reports — the "Reports" nav item's first real content (was a bare PlaceholderPage).
 * Scoped to Patients for this pass; other domains (Lab/Radiology volume, HR, etc.) are a
 * separate future slice, same one-slice-at-a-time discipline as the rest of this initiative.
 *
 * Two independently-fetched data sets, both scoped to the same date range server-side: patients
 * push `from`/`to` into the existing patients list endpoint (added for this feature — confirmed
 * live, against a tenant with 7,000+ patients, that fetching *everyone* on every report load
 * was the actual rate-limit/connection failure, not just a lack of request concurrency — see
 * patientReportData.ts's own comment), and visits push `from`/`to` into a new
 * PatientVisitsController.GetAll endpoint (there was previously no cross-patient visits query
 * at all, only one-patient-at-a-time).
 */
export default function PatientAnalyticsChartsPage() {
  const [range, setRange] = useState<ReportDateRange>(defaultRange);

  const { data: patientData, isPending: isPatientsPending } = usePatientsForReportQuery(range.from, range.to);
  const { data: visits, isPending: isVisitsPending } = usePatientVisitsForReportQuery(range.from, range.to);
  const patients = patientData?.patients;

  // Primes the department/consultant reference cache so the Department/Consultant charts'
  // resolveRecordLabel calls resolve real names on first render — same reasoning
  // ProfitReportPage's own priming hooks give.
  const { data: departmentOptions } = useMasterOptionsQuery('department');
  const { data: consultantOptions } = useMasterOptionsQuery('consultant');

  const registrationsOverTime = useMemo(() => getRegistrationsOverTime(patients ?? []), [patients]);
  const byGender = useMemo(() => getPatientsByGender(patients ?? []), [patients]);
  const byMaritalStatus = useMemo(() => getPatientsByMaritalStatus(patients ?? []), [patients]);
  const byBloodGroup = useMemo(() => getPatientsByBloodGroup(patients ?? []), [patients]);
  const byArrivalSource = useMemo(() => getPatientsByArrivalSource(patients ?? []), [patients]);
  const allergyPrevalence = useMemo(() => getAllergyPrevalence(patients ?? []), [patients]);
  const byVisitType = useMemo(() => getVisitsByType(visits ?? []), [visits]);
  const byDepartment = useMemo(() => getVisitsByDepartment(visits ?? []), [visits, departmentOptions]);
  const byConsultant = useMemo(() => getVisitsByConsultant(visits ?? []), [visits, consultantOptions]);

  const isPending = isPatientsPending || isVisitsPending;

  return (
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={Users}
        title="Patient Reports"
        subtitle="Demographics, arrival source, allergies, and visit activity across every patient in the selected period."
      />

      <div className="flex flex-1 flex-col gap-4 p-6 lg:p-8">
        <ReportDateRangeFilter range={range} onChange={setRange} />

        {patientData?.truncated && (
          <p className="rounded-md bg-warning/10 px-3 py-2 text-sm text-warning">
            More patients were registered in this period than this report scans at once — the patient charts below are based on a
            sample of the {patientData.patients.length} most recent, not a full count. Narrow the date range for exact figures.
          </p>
        )}

        {isPending && (
          <div className="flex items-center justify-center gap-2 py-10 text-sm text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" />
            Loading patient and visit data…
          </div>
        )}

        {!isPending && (
          <>
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
              <Card>
                <CardContent className="flex flex-col gap-1 py-4">
                  <span className="text-xs text-muted-foreground">Patients Registered</span>
                  <span className="text-lg font-semibold text-foreground">{patients?.length ?? 0}</span>
                </CardContent>
              </Card>
              <Card>
                <CardContent className="flex flex-col gap-1 py-4">
                  <span className="text-xs text-muted-foreground">Total Visits</span>
                  <span className="text-lg font-semibold text-foreground">{visits?.length ?? 0}</span>
                </CardContent>
              </Card>
              <Card>
                <CardContent className="flex flex-col gap-1 py-4">
                  <span className="text-xs text-muted-foreground">Patients With a Recorded Allergy</span>
                  <span className="text-lg font-semibold text-foreground">{(patients ?? []).filter((p) => p.allergies.length > 0).length}</span>
                </CardContent>
              </Card>
            </div>

            <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
              <RegistrationsTrendChart data={registrationsOverTime} />
              <PatientBreakdownChart title="Patients by Gender" rows={byGender} formatLabel={(label) => (label === 'NA' ? 'N/A' : label)} />
              <PatientBreakdownChart
                title="Patients by Marital Status"
                rows={byMaritalStatus}
                formatLabel={(label) => maritalStatusLabel(label as MaritalStatus)}
              />
              <PatientBreakdownChart
                title="Patients by Arrival Source"
                description="The closest field to a referral source — a category, not a specific referring doctor"
                rows={byArrivalSource}
                formatLabel={(label) => arrivalSourceLabel(label as ModeOfArrivalSource)}
              />
              <PatientBreakdownChart title="Patients by Blood Group" rows={byBloodGroup} />
              <PatientBreakdownChart title="Allergy Type Prevalence" rows={allergyPrevalence} />
              <PatientBreakdownChart
                title="Visits by Type"
                rows={byVisitType}
                formatLabel={(label) => encounterTypeShortLabel(label as VisitType)}
              />
              <PatientBreakdownChart
                title="Visits by Department"
                description="Top 10 — one count per consultation line, a visit with multiple consultants counts once per department named"
                rows={byDepartment}
                formatLabel={(id) => resolveRecordLabel('department', id)}
              />
              <PatientBreakdownChart
                title="Visits by Consultant"
                description="Top 10"
                rows={byConsultant}
                formatLabel={(id) => resolveRecordLabel('consultant', id)}
              />
            </div>
          </>
        )}
      </div>
    </div>
  );
}
