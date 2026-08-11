import type { Patient } from '@hms/shared';
import { CalendarClock, ClipboardList, FileText, HeartPulse, Loader2, type LucideIcon } from 'lucide-react';
import { Badge } from '@/components/ui/badge';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { AppointmentTypeName } from '@/components/AppointmentTypeName';
import { ConsultantName } from '@/components/ConsultantName';
import { DepartmentName } from '@/components/DepartmentName';
import { describeBillingItem, formatCurrency, getBillingForPatient, type BillingItem } from '@/features/billing';
import { PatientDocumentUpload } from './PatientDocumentUpload';
import { usePatientRegistrationsQuery } from '../hooks/usePatientRegistrationsQuery';
import { env } from '../../../config/env';
import { bloodGroupLabel } from '../bloodGroupLabel';

interface PatientDetailsProps {
  patient: Patient;
}

function Field({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div className="flex flex-col gap-0.5 py-1.5">
      <dt className="text-xs font-medium uppercase tracking-wide text-muted-foreground">{label}</dt>
      <dd className="text-sm text-foreground">{value}</dd>
    </div>
  );
}

/** A compact bordered white card — the "clean card" building block every Profile-tab
 * section is made of, laid out two-up on wide screens (see PatientProfileTab). */
function InfoCard({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="flex flex-col gap-1 rounded-lg border border-border bg-card p-4">
      <h2 className="text-sm font-semibold text-foreground">{title}</h2>
      <dl className="grid grid-cols-1 gap-x-4 divide-y divide-border sm:grid-cols-2 sm:divide-y-0">{children}</dl>
    </div>
  );
}

function EmptyState({ icon: Icon, message }: { icon: LucideIcon; message: string }) {
  return (
    <div className="flex flex-col items-center gap-2 rounded-lg border border-dashed border-border py-10 text-center text-sm text-muted-foreground">
      <Icon className="h-6 w-6" />
      {message}
    </div>
  );
}

function PatientProfileTab({ patient }: { patient: Patient }) {
  const registration = patient.currentRegistration;

  return (
    <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
      <InfoCard title="Demographics">
        <Field label="UHID" value={<span className="font-mono">{patient.uhid}</span>} />
        <Field label="Name" value={`${patient.title} ${patient.firstName} ${patient.lastName}`} />
        <Field label="Date of birth" value={new Date(patient.dateOfBirth).toLocaleDateString('en-IN')} />
        <Field label="Age" value={patient.age} />
        <Field label="Gender" value={patient.gender} />
        <Field label="Blood group" value={patient.bloodGroup ? bloodGroupLabel(patient.bloodGroup) : '—'} />
      </InfoCard>

      <InfoCard title="Address">
        <Field
          label="Address"
          value={[patient.addressLine1, patient.addressLine2, patient.addressLine3].filter(Boolean).join(', ')}
        />
        <Field label="District / State" value={`${patient.district}, ${patient.state}`} />
        <Field label="Pincode" value={patient.pincode} />
      </InfoCard>

      <InfoCard title="Contact">
        <Field label="Primary phone" value={patient.primaryPhone} />
        <Field label="Alternate phone" value={patient.alternatePhone || '—'} />
        <Field label="Email" value={patient.email || '—'} />
        <Field label="Profession" value={patient.profession || '—'} />
      </InfoCard>

      <InfoCard title="Emergency Contact">
        <Field label="Name" value={patient.emergencyContactName} />
        <Field label="Relationship" value={patient.emergencyContactRelationship} />
        <Field label="Phone" value={patient.emergencyContactPhone} />
      </InfoCard>

      <InfoCard title="Allergy Details">
        <Field label="Known allergy" value={patient.hasKnownAllergy ? 'Yes' : 'No'} />
        {patient.hasKnownAllergy && (
          <>
            <Field label="Type" value={patient.allergyType || '—'} />
            <Field label="Severity" value={patient.allergySeverity || '—'} />
          </>
        )}
      </InfoCard>

      {registration && (
        <InfoCard title="Current Registration">
          <Field label="Registration number" value={<span className="font-mono">{registration.registrationNumber}</span>} />
          <Field label="Encounter type" value={registration.encounterType} />
          <Field label="Mode of arrival" value={registration.modeOfArrival} />
          <Field label="Department" value={<DepartmentName departmentId={registration.departmentId} />} />
          <Field label="Consultant" value={<ConsultantName consultantId={registration.consultantId} />} />
          {registration.appointmentTypeId && (
            <Field label="Appointment type" value={<AppointmentTypeName appointmentTypeId={registration.appointmentTypeId} />} />
          )}
          {registration.admissionType && <Field label="Admission type" value={registration.admissionType} />}
          {registration.referralSource && <Field label="Referral source" value={registration.referralSource} />}
          {registration.category && <Field label="Category" value={registration.category} />}
        </InfoCard>
      )}
    </div>
  );
}

/** Every encounter/visit this patient has ever had — the registration history endpoint
 * (POST/GET .../registrations) existed on the backend but had no frontend consumer until
 * now; a returning patient's Create-only first registration was previously the only one
 * ever visible. */
function PatientVisitsTab({ patientId }: { patientId: string }) {
  const { data: registrations, isPending } = usePatientRegistrationsQuery(patientId);

  if (isPending) {
    return (
      <div className="flex items-center justify-center gap-2 rounded-lg border border-border py-10 text-sm text-muted-foreground">
        <Loader2 className="h-4 w-4 animate-spin" />
        Loading visits…
      </div>
    );
  }

  if (!registrations || registrations.length === 0) {
    return <EmptyState icon={CalendarClock} message="No visits have been recorded for this patient yet." />;
  }

  return (
    <div className="overflow-x-auto rounded-lg border border-border">
      <table className="w-full text-sm">
        <thead className="bg-muted/50 text-left text-xs font-medium uppercase tracking-wide text-muted-foreground">
          <tr>
            <th className="whitespace-nowrap px-4 py-2.5">Registration #</th>
            <th className="whitespace-nowrap px-4 py-2.5">Date</th>
            <th className="whitespace-nowrap px-4 py-2.5">Encounter</th>
            <th className="whitespace-nowrap px-4 py-2.5">Department</th>
            <th className="whitespace-nowrap px-4 py-2.5">Consultant</th>
            <th className="whitespace-nowrap px-4 py-2.5">Mode of arrival</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {registrations.map((registration) => (
            <tr key={registration.id} className="hover:bg-muted/30">
              <td className="whitespace-nowrap px-4 py-3 font-mono text-xs text-muted-foreground">{registration.registrationNumber}</td>
              <td className="whitespace-nowrap px-4 py-3 text-muted-foreground">{new Date(registration.createdAt).toLocaleDateString('en-IN')}</td>
              <td className="whitespace-nowrap px-4 py-3">
                <Badge variant="outline">{registration.encounterType}</Badge>
              </td>
              <td className="whitespace-nowrap px-4 py-3 text-foreground">
                <DepartmentName departmentId={registration.departmentId} />
              </td>
              <td className="whitespace-nowrap px-4 py-3 text-foreground">
                <ConsultantName consultantId={registration.consultantId} />
              </td>
              <td className="whitespace-nowrap px-4 py-3 text-muted-foreground">{registration.modeOfArrival}</td>
            </tr>
          ))}
        </tbody>
      </table>
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
function PatientBillingTab({ patientId }: { patientId: string }) {
  const billings = getBillingForPatient(patientId);

  if (billings.length === 0) {
    return <EmptyState icon={FileText} message="No billing recorded for this patient yet." />;
  }

  return (
    <div className="rounded-lg border border-border p-4 sm:p-5">
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
    </div>
  );
}

function PatientDocumentsTab({ patient }: { patient: Patient }) {
  return (
    <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
      <div className="flex flex-col gap-3 rounded-lg border border-border bg-card p-4">
        <h2 className="text-sm font-semibold text-foreground">On file</h2>
        <div className="flex flex-col gap-1">
          <span className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Patient photo</span>
          {patient.photoPath ? (
            <img
              src={`${env.apiBaseUrl}/${patient.photoPath}`}
              alt={`${patient.firstName} ${patient.lastName}`}
              className="mt-1 h-28 w-28 rounded-md border border-border object-cover"
            />
          ) : (
            <span className="text-sm text-muted-foreground">Not uploaded.</span>
          )}
        </div>
        <div className="flex flex-col gap-1">
          <span className="text-xs font-medium uppercase tracking-wide text-muted-foreground">ID proof</span>
          {patient.idProofPath ? (
            <a
              href={`${env.apiBaseUrl}/${patient.idProofPath}`}
              target="_blank"
              rel="noreferrer"
              className="text-sm text-primary hover:underline"
            >
              {patient.idProofType} on file — view
            </a>
          ) : (
            <span className="text-sm text-muted-foreground">Not uploaded.</span>
          )}
        </div>
      </div>

      <div className="rounded-lg border border-border bg-card p-4">
        <h2 className="mb-3 text-sm font-semibold text-foreground">Upload new</h2>
        <PatientDocumentUpload patientId={patient.id} bare />
      </div>
    </div>
  );
}

export function PatientDetails({ patient }: PatientDetailsProps) {
  return (
    <Tabs defaultValue="profile">
      <TabsList>
        <TabsTrigger value="profile">Profile</TabsTrigger>
        <TabsTrigger value="visits">Visits</TabsTrigger>
        <TabsTrigger value="billing">Billing</TabsTrigger>
        <TabsTrigger value="documents">Documents</TabsTrigger>
        <TabsTrigger value="timeline">Timeline</TabsTrigger>
        <TabsTrigger value="medical-history">Medical History</TabsTrigger>
      </TabsList>

      <TabsContent value="profile" className="pt-4">
        <PatientProfileTab patient={patient} />
      </TabsContent>

      <TabsContent value="visits" className="pt-4">
        <PatientVisitsTab patientId={patient.id} />
      </TabsContent>

      <TabsContent value="billing" className="pt-4">
        <PatientBillingTab patientId={patient.id} />
      </TabsContent>

      <TabsContent value="documents" className="pt-4">
        <PatientDocumentsTab patient={patient} />
      </TabsContent>

      <TabsContent value="timeline" className="pt-4">
        <EmptyState icon={ClipboardList} message="No timeline activity has been recorded for this patient yet." />
      </TabsContent>

      <TabsContent value="medical-history" className="pt-4">
        <EmptyState
          icon={HeartPulse}
          message="Detailed medical history (diagnoses, medications, past procedures) isn't tracked in this system yet — see the Allergy Details card on the Profile tab for what is captured today."
        />
      </TabsContent>
    </Tabs>
  );
}
