import type { Patient } from '@hms/shared';
import { Badge } from '@/components/ui/badge';
import { describeBillingItem, formatCurrency, getBillingForPatient, type BillingItem } from '@/features/billing';
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

function BillingLineItem({ item }: { item: BillingItem }) {
  const { serviceLabel, consultantName } = describeBillingItem(item);
  return (
    <div className="flex items-start justify-between gap-3 py-2.5 text-sm">
      <div className="min-w-0">
        <p className="font-medium text-foreground">
          {item.billingType} — {serviceLabel}
        </p>
        <p className="truncate text-xs text-muted-foreground">
          {consultantName}
          {item.discount > 0 ? ` · Discount ${formatCurrency(item.discount)}` : ''}
        </p>
      </div>
      <div className="flex shrink-0 flex-col items-end gap-1">
        <span className="font-medium tabular-nums text-foreground">{formatCurrency(item.total)}</span>
        <Badge variant={item.paymentStatus === 'Paid' ? 'success' : 'warning'} className="text-[10px]">
          {item.paymentStatus}
        </Badge>
      </div>
    </div>
  );
}

/** Billing is its own bounded context (see features/billing) with its own mock store — this reads it read-only for display, the same way it reads DocumentUpload's storage elsewhere. A patient can have zero billing records (every category is optional at registration) — that's shown explicitly rather than hiding the section, so "no charges were entered" reads as a fact, not a missing feature. */
function BillingSection({ patientId }: { patientId: string }) {
  const billings = getBillingForPatient(patientId);

  return (
    <div className="flex flex-col gap-1">
      <h2 className="text-sm font-semibold text-foreground">Billing</h2>
      <div className="rounded-lg border border-border p-4 sm:p-5">
        {billings.length === 0 ? (
          <p className="text-sm text-muted-foreground">No billing recorded for this patient yet.</p>
        ) : (
          <div className="flex flex-col gap-4">
            {billings.map((billing) => (
              <div key={billing.id} className="flex flex-col divide-y divide-border">
                {billing.items.map((item) => (
                  <BillingLineItem key={item.id} item={item} />
                ))}
                <div className="flex flex-wrap items-center justify-between gap-2 pt-2.5 text-sm">
                  <span className="text-muted-foreground">
                    Gross {formatCurrency(billing.grossAmount)}
                    {billing.totalDiscount > 0 ? ` · Discount −${formatCurrency(billing.totalDiscount)}` : ''}
                  </span>
                  <span className="text-base font-semibold text-primary">Net {formatCurrency(billing.netAmount)}</span>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
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

      <BillingSection patientId={patient.id} />
    </div>
  );
}
