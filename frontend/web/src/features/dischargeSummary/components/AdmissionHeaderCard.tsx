import type { Admission, Patient } from '@hms/shared';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';

function Field({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div>
      <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">{label}</p>
      <p className="mt-1 text-sm text-foreground">{value}</p>
    </div>
  );
}

interface AdmissionHeaderCardProps {
  patient: Patient;
  admission: Admission;
}

/**
 * Read-only "Admission Details" header for the discharge summary edit/view pages — every
 * field here is pulled live from the Patients/IPD APIs rather than duplicated onto
 * DischargeSummary, per the approved plan's "no data duplication" principle (patient/
 * admission display fields are never copied into the discharge summary record).
 */
export function AdmissionHeaderCard({ patient, admission }: AdmissionHeaderCardProps) {
  const allergySummary =
    patient.allergies.length > 0 ? patient.allergies.map((allergy) => allergy.specify || allergy.allergyType).join(', ') : 'None recorded';
  const addressSummary = [patient.address.addressLine1, patient.address.addressLine2, patient.address.addressLine3].filter(Boolean).join(', ');

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base">Admission Details</CardTitle>
      </CardHeader>
      <CardContent className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <Field label="Patient name" value={`${patient.firstName} ${patient.lastName}`} />
        <Field label="UHID" value={patient.uhid} />
        <Field label="Age / Gender" value={`${patient.age} / ${patient.gender}`} />
        <Field label="Address" value={addressSummary || '—'} />
        <Field label="Allergies" value={allergySummary} />
        <Field label="Admission number" value={admission.admissionNumber} />
        <Field label="Ward / Bed" value={`${admission.wardName} / ${admission.bedNumber}`} />
        <Field label="Admission date" value={new Date(admission.admissionDateTime).toLocaleString('en-IN')} />
        <Field label="Consultant" value={admission.consultantName} />
        <Field
          label="Discharge date"
          value={admission.dischargeDateTime ? new Date(admission.dischargeDateTime).toLocaleString('en-IN') : '—'}
        />
      </CardContent>
    </Card>
  );
}
