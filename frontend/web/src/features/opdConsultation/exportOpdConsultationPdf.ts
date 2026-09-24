import type { OpdConsultationHeader, OpdConsultationNote } from '@hms/shared';
import { jsPDF } from 'jspdf';
import autoTable from 'jspdf-autotable';
import { resolveRecordLabel } from '@/features/masters';
import { formatDiagnosisLabel, formatPrescriptionDetails } from './consultationLabels';

/** jspdf-autotable sets this on the doc at runtime (see its source) but doesn't type it — https://github.com/simonbengtsson/jsPDF-AutoTable */
interface DocWithLastAutoTable extends jsPDF {
  lastAutoTable?: { finalY: number };
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

export interface OpdConsultationPdfHospitalInfo {
  name: string;
  address?: string | null;
  phoneNumber?: string | null;
}

/**
 * Client-side PDF export for an OPD consultation note — same jsPDF + jspdf-autotable pattern as
 * exportDischargeSummaryPdf.ts (no backend PDF library exists in this codebase). Every section
 * is a borderless two-column (label/value) autotable; Diagnoses/Investigations are real headed
 * tables. Section order mirrors OpdConsultationForm.
 */
export function exportOpdConsultationPdf(header: OpdConsultationHeader, note: OpdConsultationNote, hospital: OpdConsultationPdfHospitalInfo) {
  const doc = new jsPDF() as DocWithLastAutoTable;
  const marginLeft = 14;
  const marginRight = 14;

  doc.setFontSize(14);
  doc.setFont('helvetica', 'bold');
  doc.text(hospital.name, marginLeft, 16);
  doc.setFont('helvetica', 'normal');
  doc.setFontSize(9);
  let headerY = 22;
  if (hospital.address) {
    doc.text(hospital.address, marginLeft, headerY);
    headerY += 5;
  }
  if (hospital.phoneNumber) {
    doc.text(`Phone: ${hospital.phoneNumber}`, marginLeft, headerY);
    headerY += 5;
  }

  doc.setFontSize(16);
  doc.text('OPD Consultation', marginLeft, headerY + 4);
  doc.setFontSize(10);
  doc.text(note.status, doc.internal.pageSize.getWidth() - marginRight, headerY + 4, { align: 'right' });

  let cursorY = headerY + 12;

  function section(title: string, rows: [string, string][]) {
    doc.setFontSize(11);
    doc.setFont('helvetica', 'bold');
    doc.text(title, marginLeft, cursorY);
    doc.setFont('helvetica', 'normal');
    autoTable(doc, {
      startY: cursorY + 3,
      body: rows,
      theme: 'plain',
      styles: { fontSize: 9, cellPadding: 1.5 },
      columnStyles: { 0: { fontStyle: 'bold', cellWidth: 55 }, 1: { cellWidth: 'auto' } },
      margin: { left: marginLeft, right: marginRight },
    });
    cursorY = (doc.lastAutoTable?.finalY ?? cursorY) + 8;
  }

  section('Patient Details', [
    ['Patient name', header.patientName],
    ['UHID', header.uhid],
    ['Age / Gender', `${header.age} / ${header.gender}`],
    ['Phone', header.phoneNumber],
    ['Appointment', formatDateTime(header.appointmentTime)],
    ['Consultant', header.consultantName],
    ['Department', header.departmentName],
  ]);

  section('Vitals', [
    ['Height (cm)', formatNumber(note.heightCm)],
    ['Weight (kg)', formatNumber(note.weightKg)],
    ['PR (bpm)', formatNumber(note.pulseRate)],
    ['BP (mmHg)', note.bloodPressure || '—'],
    ['Temperature (°F)', formatNumber(note.temperatureF)],
    ['SpO2 (%)', formatNumber(note.spO2Percent)],
  ]);

  section('Clinical Assessment', [
    ['Presenting complaints', note.presentingComplaints || '—'],
    ['Clinical history', note.clinicalHistory || '—'],
    ['Examination findings', note.examinationFindings || '—'],
  ]);

  doc.setFontSize(11);
  doc.setFont('helvetica', 'bold');
  doc.text('Diagnosis', marginLeft, cursorY);
  doc.setFont('helvetica', 'normal');
  const diagnosisRows =
    note.diagnoses.length > 0
      ? note.diagnoses.map((d) => [formatDiagnosisLabel(d), d.type])
      : [['No diagnosis recorded.', '']];
  autoTable(doc, {
    startY: cursorY + 3,
    head: [['Diagnosis', 'Type']],
    body: diagnosisRows,
    styles: { fontSize: 8 },
    margin: { left: marginLeft, right: marginRight },
  });
  cursorY = (doc.lastAutoTable?.finalY ?? cursorY) + 8;

  doc.setFontSize(11);
  doc.setFont('helvetica', 'bold');
  doc.text('Investigations', marginLeft, cursorY);
  doc.setFont('helvetica', 'normal');
  const investigationRows =
    note.investigations.length > 0
      ? note.investigations.map((i) => [i.name, i.department, i.priority])
      : [['No investigations recorded.', '', '']];
  autoTable(doc, {
    startY: cursorY + 3,
    head: [['Investigation', 'Department', 'Priority']],
    body: investigationRows,
    styles: { fontSize: 8 },
    margin: { left: marginLeft, right: marginRight },
  });
  cursorY = (doc.lastAutoTable?.finalY ?? cursorY) + 8;

  doc.setFontSize(11);
  doc.setFont('helvetica', 'bold');
  doc.text('Prescription', marginLeft, cursorY);
  doc.setFont('helvetica', 'normal');
  const prescriptionRows =
    (note.prescriptions ?? []).length > 0
      ? note.prescriptions.map((p) => [p.drugName, formatPrescriptionDetails(p) || '—', p.instructions || '—'])
      : [['No medicines prescribed.', '', '']];
  autoTable(doc, {
    startY: cursorY + 3,
    head: [['Medicine', 'Dose · Route · Frequency · Duration', 'Instructions']],
    body: prescriptionRows,
    styles: { fontSize: 8 },
    margin: { left: marginLeft, right: marginRight },
  });
  cursorY = (doc.lastAutoTable?.finalY ?? cursorY) + 8;

  section('Plan of Management', [['Plan', note.planOfManagement || '—']]);

  section('Follow-up / Review', [
    ['Review date', formatDate(note.reviewDate)],
    ['Follow-up instructions', note.followUpInstructions || '—'],
  ]);

  section('Emergency Review (SOS)', [['Instructions', note.emergencyReviewInstructions || '—']]);

  section('Referral', [
    ['Department', note.referralDepartmentId ? resolveRecordLabel('department', note.referralDepartmentId) : '—'],
    ['Consultant', note.referralConsultantId ? resolveRecordLabel('consultant', note.referralConsultantId) : '—'],
    ['Reason', note.referralReason || '—'],
  ]);

  doc.save(`opd-consultation-${header.uhid}.pdf`);
}
