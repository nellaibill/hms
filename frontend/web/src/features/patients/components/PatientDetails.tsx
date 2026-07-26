import type { Patient } from '@hms/shared';
import { env } from '../../../config/env';
import { bloodGroupLabel } from '../bloodGroupLabel';

interface PatientDetailsProps {
  patient: Patient;
}

function Field({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div className="flex flex-col gap-0.5 py-2.5">
      <dt className="text-xs font-medium uppercase tracking-wide text-muted-foreground">{label}</dt>
      <dd className="text-sm text-foreground">{value}</dd>
    </div>
  );
}

function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="flex flex-col gap-1">
      <h2 className="text-sm font-semibold text-foreground">{title}</h2>
      <dl className="grid grid-cols-1 divide-y divide-border rounded-lg border border-border px-4 sm:grid-cols-2 sm:divide-y-0 sm:divide-x sm:px-0 sm:[&>*]:px-5 xl:grid-cols-3">
        {children}
      </dl>
    </div>
  );
}

export function PatientDetails({ patient }: PatientDetailsProps) {
  const registration = patient.currentRegistration;

  return (
    <div className="flex flex-col gap-6">
      {patient.photoPath && (
        <img
          src={`${env.apiBaseUrl}/${patient.photoPath}`}
          alt={`${patient.firstName} ${patient.lastName}`}
          className="h-32 w-32 rounded-lg border border-border object-cover"
        />
      )}

      <Section title="Demographics">
        <Field label="UHID" value={<span className="font-mono">{patient.uhid}</span>} />
        <Field label="Name" value={`${patient.title} ${patient.firstName} ${patient.lastName}`} />
        <Field label="Date of birth" value={new Date(patient.dateOfBirth).toLocaleDateString('en-IN')} />
        <Field label="Age" value={patient.age} />
        <Field label="Gender" value={patient.gender} />
        <Field label="Blood group" value={patient.bloodGroup ? bloodGroupLabel(patient.bloodGroup) : '—'} />
      </Section>

      <Section title="Address">
        <Field
          label="Address"
          value={[patient.addressLine1, patient.addressLine2, patient.addressLine3].filter(Boolean).join(', ')}
        />
        <Field label="District / State" value={`${patient.district}, ${patient.state}`} />
        <Field label="Pincode" value={patient.pincode} />
      </Section>

      <Section title="Contact">
        <Field label="Primary phone" value={patient.primaryPhone} />
        <Field label="Alternate phone" value={patient.alternatePhone || '—'} />
        <Field label="Email" value={patient.email || '—'} />
        <Field label="Profession" value={patient.profession || '—'} />
      </Section>

      <Section title="Emergency Contact">
        <Field label="Name" value={patient.emergencyContactName} />
        <Field label="Relationship" value={patient.emergencyContactRelationship} />
        <Field label="Phone" value={patient.emergencyContactPhone} />
      </Section>

      <Section title="Allergy Details">
        <Field label="Known allergy" value={patient.hasKnownAllergy ? 'Yes' : 'No'} />
        {patient.hasKnownAllergy && (
          <>
            <Field label="Type" value={patient.allergyType || '—'} />
            <Field label="Severity" value={patient.allergySeverity || '—'} />
          </>
        )}
      </Section>

      {registration && (
        <Section title="Current Registration">
          <Field label="Registration number" value={<span className="font-mono">{registration.registrationNumber}</span>} />
          <Field label="Encounter type" value={registration.encounterType} />
          <Field label="Mode of arrival" value={registration.modeOfArrival} />
          <Field label="Department" value={registration.department} />
          <Field label="Consultant" value={registration.consultant} />
          {registration.admissionType && <Field label="Admission type" value={registration.admissionType} />}
          {registration.referralSource && <Field label="Referral source" value={registration.referralSource} />}
          {registration.category && <Field label="Category" value={registration.category} />}
        </Section>
      )}
    </div>
  );
}
