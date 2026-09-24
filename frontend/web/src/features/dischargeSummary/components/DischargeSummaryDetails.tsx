import type { DischargeSummary, StaffDirectoryEntry } from '@hms/shared';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';

function Field({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div>
      <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">{label}</p>
      <p className="mt-1 whitespace-pre-wrap text-sm text-foreground">{value || '—'}</p>
    </div>
  );
}

const foodInstructionLabels: Record<string, string> = {
  BeforeFood: 'Before food',
  AfterFood: 'After food',
};

function staffName(id: string | null | undefined, staff: StaffDirectoryEntry[]): string {
  if (!id) return '—';
  const entry = staff.find((s) => s.id === id);
  return entry ? `${entry.firstName} ${entry.lastName}` : '—';
}

interface DischargeSummaryDetailsProps {
  summary: DischargeSummary;
  staff: StaffDirectoryEntry[];
}

/**
 * Read-only rendering of a discharge summary's full content — used for the Finalized view
 * page (no more PUT calls once finalized) and doubles as the on-screen layout the PDF export
 * mirrors. Section order matches the approved plan / DischargeSummaryForm's edit layout.
 */
export function DischargeSummaryDetails({ summary, staff }: DischargeSummaryDetailsProps) {
  return (
    <div className="flex flex-col gap-6">
      <Card>
        <CardHeader>
          <CardTitle className="text-base">Diagnosis &amp; Clinical Summary</CardTitle>
        </CardHeader>
        <CardContent className="grid grid-cols-1 gap-4">
          <Field label="Final diagnosis" value={summary.finalDiagnosis} />
          <Field label="Chief complaints" value={summary.chiefComplaints} />
          <Field label="History of presenting illness" value={summary.historyOfPresentingIllness} />
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <Field label="Past medical history" value={summary.pastMedicalHistory} />
            <Field label="Past surgical history" value={summary.pastSurgicalHistory} />
            <Field label="Family history" value={summary.familyHistory} />
            <Field label="Personal history" value={summary.personalHistory} />
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Examination</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          <Field label="General examination" value={summary.generalExamination} />
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <Field label="CVS findings" value={summary.cvsFindings} />
            <Field label="RS findings" value={summary.rsFindings} />
            <Field label="P/A findings" value={summary.paFindings} />
            <Field label="CNS findings" value={summary.cnsFindings} />
          </div>
          <Field label="Local examination" value={summary.localExamination} />
          <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-6">
            <Field label="Gait" value={summary.gait} />
            <Field label="Height (cm)" value={summary.heightCm} />
            <Field label="Weight (kg)" value={summary.weightKg} />
            <Field label="Pulse rate" value={summary.pulseRate} />
            <Field label="Respiratory rate" value={summary.respiratoryRate} />
            <Field label="Temperature (°F)" value={summary.temperatureF} />
            <Field label="SpO₂ (%)" value={summary.spO2Percent} />
            <Field label="Blood pressure" value={summary.bloodPressure} />
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Course in Hospital</CardTitle>
        </CardHeader>
        <CardContent>
          <Field label="Course in hospital" value={summary.courseInHospital} />
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Procedures / OT</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <Field label="Procedure name" value={summary.procedureName} />
            <Field label="Procedure date/time" value={summary.procedureDateTime ? new Date(summary.procedureDateTime).toLocaleString('en-IN') : null} />
            <Field label="Primary surgeon" value={summary.primarySurgeon} />
            <Field label="Assistant surgeon(s)" value={summary.assistantSurgeons} />
            <Field label="Anaesthetist" value={summary.anaesthetist} />
            <Field label="Anaesthesia" value={summary.anaesthesia} />
            <Field label="Surgical position" value={summary.surgicalPosition} />
          </div>
          <Field label="Intra-operative findings" value={summary.intraOperativeFindings} />
          <Field label="Operative notes" value={summary.operativeNotes} />
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Discharge Medications</CardTitle>
        </CardHeader>
        <CardContent>
          {summary.medications.length === 0 ? (
            <p className="text-sm text-muted-foreground">No medications recorded.</p>
          ) : (
            <div className="overflow-x-auto rounded-md border border-border">
              <table className="w-full min-w-[720px] text-sm">
                <thead className="bg-sidebar-active text-xs uppercase text-sidebar-active-foreground">
                  <tr>
                    <th className="px-2 py-2 text-left">Drug</th>
                    <th className="px-2 py-2 text-left">Dose</th>
                    <th className="px-2 py-2 text-left">Route</th>
                    <th className="px-2 py-2 text-left">M</th>
                    <th className="px-2 py-2 text-left">N</th>
                    <th className="px-2 py-2 text-left">E</th>
                    <th className="px-2 py-2 text-left">N</th>
                    <th className="px-2 py-2 text-left">Days</th>
                    <th className="px-2 py-2 text-left">Food</th>
                  </tr>
                </thead>
                <tbody>
                  {[...summary.medications]
                    .sort((a, b) => a.sortOrder - b.sortOrder)
                    .map((medication) => (
                      <tr key={medication.id} className="border-t border-border">
                        <td className="px-2 py-1.5">{medication.drugName}</td>
                        <td className="px-2 py-1.5">{medication.dose}</td>
                        <td className="px-2 py-1.5">{medication.route}</td>
                        <td className="px-2 py-1.5">{medication.morningQty}</td>
                        <td className="px-2 py-1.5">{medication.noonQty}</td>
                        <td className="px-2 py-1.5">{medication.eveningQty}</td>
                        <td className="px-2 py-1.5">{medication.nightQty}</td>
                        <td className="px-2 py-1.5">{medication.durationDays}</td>
                        <td className="px-2 py-1.5">{foodInstructionLabels[medication.foodInstruction] ?? medication.foodInstruction}</td>
                      </tr>
                    ))}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Discharge Advice</CardTitle>
        </CardHeader>
        <CardContent className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <Field label="Diet" value={summary.diet} />
          <Field label="Wound care" value={summary.woundCare} />
          <Field label="Activity" value={summary.activity} />
          <Field label="Physiotherapy" value={summary.physiotherapy} />
          <Field label="Review instructions" value={summary.reviewInstructions} />
          <Field label="Emergency instructions" value={summary.emergencyInstructions} />
          <Field label="Condition at discharge" value={summary.conditionAtDischarge} />
        </CardContent>
      </Card>

      <Card>
        <CardHeader className="flex flex-row items-center justify-between">
          <CardTitle className="text-base">Finalization</CardTitle>
          {summary.status === 'Finalized' && <Badge variant="success">Finalized</Badge>}
        </CardHeader>
        <CardContent className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <Field label="Prepared by" value={staffName(summary.preparedByUserId, staff)} />
          <Field label="Checked by" value={staffName(summary.checkedByUserId, staff)} />
          <Field label="Consultant approved by" value={staffName(summary.consultantApprovedByUserId, staff)} />
          <Field label="Finalized at" value={summary.finalizedAt ? new Date(summary.finalizedAt).toLocaleString('en-IN') : null} />
        </CardContent>
      </Card>
    </div>
  );
}
