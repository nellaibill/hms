import type { OpdConsultationHeader, OpdConsultationNote } from '@hms/shared';
import defaultLogoUrl from '@/assets/logo.png';
import { branding } from '@/config/branding';
import { useBrandingQuery } from '@/features/branding/hooks/useBrandingQuery';
import { resolveRecordLabel } from '@/features/masters';
import { formatDiagnosisLabel, formatPrescriptionDetails } from '../consultationLabels';

interface OpdConsultationPrintTemplateProps {
  header: OpdConsultationHeader;
  note: OpdConsultationNote;
}

function formatDateTime(value?: string | null): string {
  if (!value) return '—';
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? '—' : date.toLocaleString('en-IN');
}

function formatDate(value?: string | null): string {
  if (!value) return '—';
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? '—' : date.toLocaleDateString('en-IN');
}

function formatNumber(value?: number | null): string {
  return value === null || value === undefined ? '—' : String(value);
}

function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="mt-5">
      <h2 className="border-b border-black pb-1 text-sm font-bold uppercase tracking-wide">{title}</h2>
      <div className="mt-2">{children}</div>
    </div>
  );
}

function FieldRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex gap-2 py-0.5 text-sm">
      <span className="w-44 shrink-0 font-semibold text-gray-700">{label}</span>
      <span>{value}</span>
    </div>
  );
}

/**
 * The actual printed OPD Consultation page — same hidden-div/`.print-target` pattern as
 * InvoicePrintTemplate.tsx/LabReportPrintTemplate.tsx (index.css's print rule hides everything
 * else on the page once printing starts). OpdConsultationForm/Header have no such target, so
 * without this the "Print" button previously produced a blank page. Hospital identity (name,
 * address, phone) comes from the Branding module — the same source Invoice/Lab Report print
 * templates already use for hospitalName, extended with the two new optional fields.
 */
export function OpdConsultationPrintTemplate({ header, note }: OpdConsultationPrintTemplateProps) {
  const { data: brandingConfig } = useBrandingQuery();
  const hospitalName = brandingConfig?.hospitalName ?? branding.hospitalName;
  const address = brandingConfig?.address;
  const phoneNumber = brandingConfig?.phoneNumber;
  const logoUrl = brandingConfig?.logoUrl ?? defaultLogoUrl;

  return (
    <div className="print-target hidden bg-white p-10 text-black print:block" style={{ fontFamily: 'Georgia, "Times New Roman", serif' }}>
      <div className="flex flex-col items-center gap-1 border-b-2 border-black pb-4 text-center">
        <img src={logoUrl} alt={hospitalName} className="h-16 w-auto object-contain" />
        <span className="text-2xl font-bold tracking-tight">{hospitalName}</span>
        {address && <span className="text-xs text-gray-600">{address}</span>}
        {phoneNumber && <span className="text-xs text-gray-600">Phone: {phoneNumber}</span>}
        <span className="mt-1 text-sm font-semibold uppercase tracking-widest">OPD Consultation</span>
      </div>

      <div className="mt-6 flex items-start justify-between gap-6">
        <div className="flex flex-col gap-0.5">
          <span className="text-xs font-semibold uppercase tracking-wide text-gray-500">Patient</span>
          <span className="text-base font-semibold">{header.patientName}</span>
          <span className="text-sm text-gray-700">
            UHID: {header.uhid} · {header.age} Years / {header.gender}
          </span>
          <span className="text-sm text-gray-700">Phone: {header.phoneNumber}</span>
        </div>
        <div className="flex shrink-0 flex-col items-end gap-1 text-right">
          <span className="whitespace-nowrap text-sm text-gray-600">Appointment {formatDateTime(header.appointmentTime)}</span>
          <span className="whitespace-nowrap text-sm">
            <span className="text-gray-600">Consultant</span> {header.consultantName}
          </span>
          <span className="whitespace-nowrap text-sm">
            <span className="text-gray-600">Department</span> {header.departmentName}
          </span>
          <span className="whitespace-nowrap text-sm font-semibold">{note.status}</span>
        </div>
      </div>

      <Section title="Vitals">
        <div className="grid grid-cols-3 gap-x-6">
          <FieldRow label="Height (cm)" value={formatNumber(note.heightCm)} />
          <FieldRow label="Weight (kg)" value={formatNumber(note.weightKg)} />
          <FieldRow label="PR (bpm)" value={formatNumber(note.pulseRate)} />
          <FieldRow label="BP (mmHg)" value={note.bloodPressure || '—'} />
          <FieldRow label="Temperature (°F)" value={formatNumber(note.temperatureF)} />
          <FieldRow label="SpO2 (%)" value={formatNumber(note.spO2Percent)} />
        </div>
      </Section>

      <Section title="Clinical Assessment">
        <FieldRow label="Presenting complaints" value={note.presentingComplaints || '—'} />
        <FieldRow label="Clinical history" value={note.clinicalHistory || '—'} />
        <FieldRow label="Examination findings" value={note.examinationFindings || '—'} />
      </Section>

      <Section title="Diagnosis">
        {note.diagnoses.length === 0 ? (
          <p className="text-sm text-gray-600">No diagnosis recorded.</p>
        ) : (
          <table className="w-full border-collapse text-sm">
            <thead>
              <tr className="border-b-2 border-black">
                <th className="py-1 pr-2 text-left font-semibold">Diagnosis</th>
                <th className="py-1 text-left font-semibold">Type</th>
              </tr>
            </thead>
            <tbody>
              {note.diagnoses.map((d) => (
                <tr key={d.id ?? d.diagnosisId} className="border-b border-gray-300">
                  <td className="py-1 pr-2">{formatDiagnosisLabel(d)}</td>
                  <td className="py-1">{d.type}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </Section>

      <Section title="Investigations">
        {note.investigations.length === 0 ? (
          <p className="text-sm text-gray-600">No investigations recorded.</p>
        ) : (
          <table className="w-full border-collapse text-sm">
            <thead>
              <tr className="border-b-2 border-black">
                <th className="py-1 pr-2 text-left font-semibold">Investigation</th>
                <th className="py-1 pr-2 text-left font-semibold">Department</th>
                <th className="py-1 text-left font-semibold">Priority</th>
              </tr>
            </thead>
            <tbody>
              {note.investigations.map((i) => (
                <tr key={i.id ?? i.name} className="border-b border-gray-300">
                  <td className="py-1 pr-2">{i.name}</td>
                  <td className="py-1 pr-2">{i.department}</td>
                  <td className="py-1">{i.priority}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </Section>

      <Section title="Prescription">
        {(note.prescriptions ?? []).length === 0 ? (
          <p className="text-sm text-gray-600">No medicines prescribed.</p>
        ) : (
          <table className="w-full border-collapse text-sm">
            <thead>
              <tr className="border-b-2 border-black">
                <th className="py-1 pr-2 text-left font-semibold">Medicine</th>
                <th className="py-1 pr-2 text-left font-semibold">Dose · Route · Frequency · Duration</th>
                <th className="py-1 text-left font-semibold">Instructions</th>
              </tr>
            </thead>
            <tbody>
              {note.prescriptions.map((p, index) => (
                <tr key={p.id ?? index} className="border-b border-gray-300">
                  <td className="py-1 pr-2 font-medium">{p.drugName}</td>
                  <td className="py-1 pr-2">{formatPrescriptionDetails(p) || '—'}</td>
                  <td className="py-1">{p.instructions || '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </Section>

      <Section title="Plan of Management">
        <p className="whitespace-pre-wrap text-sm">{note.planOfManagement || '—'}</p>
      </Section>

      <Section title="Follow-up / Review">
        <FieldRow label="Review date" value={formatDate(note.reviewDate)} />
        <FieldRow label="Follow-up instructions" value={note.followUpInstructions || '—'} />
      </Section>

      <Section title="Emergency Review (SOS)">
        <p className="whitespace-pre-wrap text-sm">{note.emergencyReviewInstructions || '—'}</p>
      </Section>

      <Section title="Referral">
        <FieldRow label="Department" value={note.referralDepartmentId ? resolveRecordLabel('department', note.referralDepartmentId) : '—'} />
        <FieldRow label="Consultant" value={note.referralConsultantId ? resolveRecordLabel('consultant', note.referralConsultantId) : '—'} />
        <FieldRow label="Reason" value={note.referralReason || '—'} />
      </Section>

      <div className="mt-10 flex items-end justify-between border-t border-gray-300 pt-3 text-xs text-gray-600">
        <span>This is a system-generated clinical record.</span>
        <span>Printed {new Date().toLocaleString('en-IN')}</span>
      </div>
    </div>
  );
}
