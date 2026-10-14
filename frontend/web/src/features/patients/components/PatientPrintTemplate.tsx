import type { Patient } from '@hms/shared';
import { DistrictName } from '@/components/DistrictName';
import { StateName } from '@/components/StateName';
import { branding } from '@/config/branding';
import { useBrandLogo } from '@/features/branding/brandLogo';
import { PrintPageStyle } from '@/features/branding/components/PrintPageStyle';
import { useBrandingQuery } from '@/features/branding/hooks/useBrandingQuery';
import { useAuth } from '../../auth/AuthContext';
import { modeOfArrivalChannelLabel } from '../arrivalChannelLabel';
import { bloodGroupLabel } from '../bloodGroupLabel';
import { humanize } from '../humanize';
import { maritalStatusLabel } from '../maritalStatusLabel';
import { maskIdNumber } from '../maskIdNumber';
import { usePatientDocumentUrl } from '../hooks/usePatientDocumentUrl';
import { usePatientVisitsQuery } from '../hooks/usePatientVisitsQuery';

function formatDate(value?: string | null): string {
  if (!value) return '—';
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? '—' : date.toLocaleDateString('en-IN');
}

function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="mt-5 break-inside-avoid">
      <h2 className="border-b border-black pb-1 text-sm font-bold uppercase tracking-wide">{title}</h2>
      <div className="mt-2">{children}</div>
    </div>
  );
}

function FieldRow({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div className="flex gap-2 py-0.5 text-sm">
      <span className="w-36 shrink-0 font-semibold text-gray-700">{label}</span>
      <span className="min-w-0 break-words">{value}</span>
    </div>
  );
}

/**
 * The printed patient sheet behind the patient page's Print button. Same hidden-div /
 * `.print-target` pattern as OpdConsultationPrintTemplate.tsx: index.css's print rule hides
 * everything else on the page once printing starts. The page had no print target before, so
 * Print produced blank sheets. Covers what the Overview tab shows: identity, contact, address,
 * emergency contacts, allergies and registration details. The ID proof number stays masked.
 * When printing, media queries measure the printable width of the page, so `sm:grid-cols-2`
 * gives two field columns on A4/Letter and one on A5 (about 470px wide), where two columns
 * would break phone numbers mid-number.
 */
export function PatientPrintTemplate({ patient }: { patient: Patient }) {
  const { data: brandingConfig } = useBrandingQuery();
  const { user } = useAuth();
  const hospitalName = brandingConfig?.hospitalName ?? branding.hospitalName;
  const address = brandingConfig?.address;
  const phoneNumber = brandingConfig?.phoneNumber;
  // The configured Print logo, else the Primary logo, else the bundled default.
  const printLogo = useBrandLogo('print');
  const photoUrl = usePatientDocumentUrl(patient.id, 'Other');
  const { data: visits } = usePatientVisitsQuery(patient.id);
  const lastVisit = visits?.[0];
  const fullName = [patient.title, patient.firstName, patient.lastName].filter(Boolean).join(' ');

  return (
    <div className="print-target hidden bg-white p-10 text-black print:block" style={{ fontFamily: 'Georgia, "Times New Roman", serif' }}>
      <PrintPageStyle document="patient" />

      <div className="flex flex-col items-center gap-1 border-b-2 border-black pb-4 text-center">
        <img src={printLogo.url} alt={hospitalName} className="max-w-full" style={{ width: printLogo.display.width, height: printLogo.display.height, objectFit: printLogo.display.fit }} />
        <span className="text-2xl font-bold tracking-tight">{hospitalName}</span>
        {address && <span className="text-xs text-gray-600">{address}</span>}
        {phoneNumber && <span className="text-xs text-gray-600">Phone: {phoneNumber}</span>}
        <span className="mt-1 text-sm font-semibold uppercase tracking-widest">Patient Registration</span>
      </div>

      <div className="mt-6 flex items-start justify-between gap-6">
        <div className="flex flex-col gap-0.5">
          <span className="text-xs font-semibold uppercase tracking-wide text-gray-500">Patient</span>
          <span className="text-base font-semibold">{fullName}</span>
          <span className="text-sm text-gray-700">
            UHID: {patient.uhid} · {patient.age} Years / {patient.gender}
          </span>
          <span className="text-sm text-gray-700">Phone: {patient.primaryPhone}</span>
        </div>
        <div className="flex shrink-0 items-start gap-4">
          <div className="flex flex-col items-end gap-1 text-right text-sm">
            <span className="whitespace-nowrap">
              <span className="text-gray-600">Registered</span> {formatDate(patient.createdAt)}
            </span>
            <span className="whitespace-nowrap">
              <span className="text-gray-600">Last visit</span>{' '}
              {lastVisit ? `${formatDate(lastVisit.createdAt)} (${lastVisit.visitType})` : '—'}
            </span>
          </div>
          {photoUrl && <img src={photoUrl} alt={fullName} className="h-24 w-20 border border-gray-400 object-cover" />}
        </div>
      </div>

      <Section title="Personal & Contact">
        <div className="grid grid-cols-1 gap-x-6 sm:grid-cols-2">
          <FieldRow label="Date of birth" value={formatDate(patient.dateOfBirth)} />
          <FieldRow label="Blood group" value={bloodGroupLabel(patient.bloodGroup)} />
          <FieldRow label="Marital status" value={maritalStatusLabel(patient.maritalStatus)} />
          <FieldRow label="Profession" value={patient.profession || '—'} />
          <FieldRow label="Primary phone" value={patient.primaryPhone} />
          <FieldRow label="Secondary phone" value={patient.secondaryPhone || '—'} />
          <FieldRow label="Email" value={patient.email || '—'} />
        </div>
      </Section>

      <Section title="Address">
        <p className="text-sm">{[patient.address.addressLine1, patient.address.addressLine2, patient.address.addressLine3].filter(Boolean).join(', ')}</p>
        <p className="text-sm">
          <DistrictName stateId={patient.address.stateId} districtId={patient.address.districtId} />, <StateName stateId={patient.address.stateId} /> —{' '}
          {patient.address.pincode}
        </p>
      </Section>

      <Section title="Emergency Contacts">
        {patient.emergencyContacts.length === 0 ? (
          <p className="text-sm text-gray-600">No emergency contact recorded.</p>
        ) : (
          <table className="w-full border-collapse text-sm">
            <thead>
              <tr className="border-b-2 border-black">
                <th className="py-1 pr-2 text-left font-semibold">Name</th>
                <th className="py-1 pr-2 text-left font-semibold">Relationship</th>
                <th className="py-1 text-left font-semibold">Contact number</th>
              </tr>
            </thead>
            <tbody>
              {patient.emergencyContacts.map((contact) => (
                <tr key={contact.id} className="border-b border-gray-300">
                  <td className="py-1 pr-2">{contact.name}</td>
                  <td className="py-1 pr-2">{humanize(contact.relationship)}</td>
                  <td className="py-1">{contact.phone}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </Section>

      <Section title="Allergies">
        {patient.allergies.length === 0 ? (
          <p className="text-sm text-gray-600">No allergies have been recorded for this patient.</p>
        ) : (
          <table className="w-full border-collapse text-sm">
            <thead>
              <tr className="border-b-2 border-black">
                <th className="py-1 pr-2 text-left font-semibold">Allergen</th>
                <th className="py-1 pr-2 text-left font-semibold">Type</th>
                <th className="py-1 text-left font-semibold">Severity</th>
              </tr>
            </thead>
            <tbody>
              {patient.allergies.map((allergy) => (
                <tr key={allergy.id} className="border-b border-gray-300">
                  <td className="py-1 pr-2 font-medium">{allergy.specify || allergy.allergyType}</td>
                  <td className="py-1 pr-2">{allergy.allergyType}</td>
                  <td className={allergy.severity === 'Severe' ? 'py-1 font-bold' : 'py-1'}>{allergy.severity}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </Section>

      <Section title="Registration Details">
        <div className="grid grid-cols-1 gap-x-6 sm:grid-cols-2">
          <FieldRow label="Registration source" value={humanize(patient.modeOfArrivalSource)} />
          {patient.modeOfArrivalChannel && <FieldRow label="Arrival channel" value={modeOfArrivalChannelLabel(patient.modeOfArrivalChannel)} />}
          {patient.modeOfArrivalSpecify && <FieldRow label="Arrival details" value={patient.modeOfArrivalSpecify} />}
          <FieldRow label="ID proof type" value={patient.idProofType ?? '—'} />
          <FieldRow label="ID proof number" value={patient.idProofNumber ? maskIdNumber(patient.idProofNumber) : '—'} />
        </div>
      </Section>

      <div className="mt-10 flex items-end justify-between border-t border-gray-300 pt-3 text-xs text-gray-600">
        <span>Printed by {user?.name ?? 'Front Desk'}</span>
        <span>Printed {new Date().toLocaleString('en-IN')}</span>
      </div>
    </div>
  );
}
