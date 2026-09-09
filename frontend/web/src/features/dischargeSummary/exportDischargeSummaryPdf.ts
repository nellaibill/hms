import type { Admission, DischargeSummary, Patient, StaffDirectoryEntry } from '@hms/shared';
import { jsPDF } from 'jspdf';
import autoTable from 'jspdf-autotable';

/** jspdf-autotable sets this on the doc at runtime (see its source) but doesn't type it — https://github.com/simonbengtsson/jsPDF-AutoTable */
interface DocWithLastAutoTable extends jsPDF {
  lastAutoTable?: { finalY: number };
}

function formatDate(value?: string | null): string {
  if (!value) return '—';
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? '—' : date.toLocaleString('en-IN');
}

function formatNumber(value?: number | null): string {
  return value === null || value === undefined ? '—' : String(value);
}

function staffName(id: string | null | undefined, staff: StaffDirectoryEntry[]): string {
  if (!id) return '—';
  const entry = staff.find((s) => s.id === id);
  return entry ? `${entry.firstName} ${entry.lastName}` : '—';
}

const foodInstructionLabels: Record<string, string> = {
  BeforeFood: 'Before food',
  AfterFood: 'After food',
};

/**
 * Client-side PDF export for a discharge summary — follows the same pattern already used by
 * frontend/web/src/features/reports/exportUtils.ts (jsPDF + jspdf-autotable, no backend PDF
 * library exists per the approved plan). Every section is rendered as a borderless
 * two-column (label/value) autotable, which handles text wrapping and page breaks for long
 * narrative fields automatically; the medications grid is a real headed table. Section order
 * mirrors DischargeSummaryForm/DischargeSummaryDetails.
 */
export function exportDischargeSummaryPdf(summary: DischargeSummary, patient: Patient, admission: Admission, staff: StaffDirectoryEntry[]) {
  const doc = new jsPDF() as DocWithLastAutoTable;
  const marginLeft = 14;
  const marginRight = 14;

  doc.setFontSize(16);
  doc.text('Discharge Summary', marginLeft, 16);
  doc.setFontSize(10);
  doc.text(summary.status, doc.internal.pageSize.getWidth() - marginRight, 16, { align: 'right' });

  let cursorY = 24;

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

  section('Admission Details', [
    ['Patient name', `${patient.firstName} ${patient.lastName}`],
    ['UHID', patient.uhid],
    ['Age / Gender', `${patient.age} / ${patient.gender}`],
    ['Allergies', patient.allergies.length > 0 ? patient.allergies.map((allergy) => allergy.specify || allergy.allergyType).join(', ') : 'None recorded'],
    ['Admission number', admission.admissionNumber],
    ['Ward / Bed', `${admission.wardName} / ${admission.bedNumber}`],
    ['Admission date', formatDate(admission.admissionDateTime)],
    ['Consultant', admission.consultantName],
    ['Discharge date', formatDate(admission.dischargeDateTime)],
  ]);

  section('Diagnosis & Clinical Summary', [
    ['Final diagnosis', summary.finalDiagnosis || '—'],
    ['Chief complaints', summary.chiefComplaints || '—'],
    ['History of presenting illness', summary.historyOfPresentingIllness || '—'],
    ['Past medical history', summary.pastMedicalHistory || '—'],
    ['Past surgical history', summary.pastSurgicalHistory || '—'],
    ['Family history', summary.familyHistory || '—'],
    ['Personal history', summary.personalHistory || '—'],
  ]);

  section('Examination', [
    ['General examination', summary.generalExamination || '—'],
    ['CVS findings', summary.cvsFindings || '—'],
    ['RS findings', summary.rsFindings || '—'],
    ['P/A findings', summary.paFindings || '—'],
    ['CNS findings', summary.cnsFindings || '—'],
    ['Local examination', summary.localExamination || '—'],
    ['Gait', summary.gait || '—'],
    ['Height (cm)', formatNumber(summary.heightCm)],
    ['Weight (kg)', formatNumber(summary.weightKg)],
    ['Pulse rate', formatNumber(summary.pulseRate)],
    ['Respiratory rate', formatNumber(summary.respiratoryRate)],
    ['Temperature (°F)', formatNumber(summary.temperatureF)],
    ['SpO2 (%)', formatNumber(summary.spO2Percent)],
    ['Blood pressure', summary.bloodPressure || '—'],
  ]);

  section('Course in Hospital', [['Course in hospital', summary.courseInHospital || '—']]);

  section('Procedures / OT', [
    ['Procedure name', summary.procedureName || '—'],
    ['Procedure date/time', formatDate(summary.procedureDateTime)],
    ['Primary surgeon', summary.primarySurgeon || '—'],
    ['Assistant surgeon(s)', summary.assistantSurgeons || '—'],
    ['Anaesthetist', summary.anaesthetist || '—'],
    ['Anaesthesia', summary.anaesthesia || '—'],
    ['Surgical position', summary.surgicalPosition || '—'],
    ['Intra-operative findings', summary.intraOperativeFindings || '—'],
    ['Operative notes', summary.operativeNotes || '—'],
  ]);

  doc.setFontSize(11);
  doc.setFont('helvetica', 'bold');
  doc.text('Discharge Medications', marginLeft, cursorY);
  doc.setFont('helvetica', 'normal');
  const medicationRows =
    summary.medications.length > 0
      ? [...summary.medications]
          .sort((a, b) => a.sortOrder - b.sortOrder)
          .map((medication) => [
            medication.drugName,
            medication.dose,
            medication.route,
            String(medication.morningQty),
            String(medication.noonQty),
            String(medication.eveningQty),
            String(medication.nightQty),
            String(medication.durationDays),
            foodInstructionLabels[medication.foodInstruction] ?? medication.foodInstruction,
          ])
      : [['No medications recorded.', '', '', '', '', '', '', '', '']];
  autoTable(doc, {
    startY: cursorY + 3,
    head: [['Drug', 'Dose', 'Route', 'M', 'N', 'E', 'N', 'Days', 'Food']],
    body: medicationRows,
    styles: { fontSize: 8 },
    margin: { left: marginLeft, right: marginRight },
  });
  cursorY = (doc.lastAutoTable?.finalY ?? cursorY) + 8;

  section('Discharge Advice', [
    ['Diet', summary.diet || '—'],
    ['Wound care', summary.woundCare || '—'],
    ['Activity', summary.activity || '—'],
    ['Physiotherapy', summary.physiotherapy || '—'],
    ['Review instructions', summary.reviewInstructions || '—'],
    ['Emergency instructions', summary.emergencyInstructions || '—'],
    ['Condition at discharge', summary.conditionAtDischarge || '—'],
  ]);

  section('Finalization', [
    ['Prepared by', staffName(summary.preparedByUserId, staff)],
    ['Checked by', staffName(summary.checkedByUserId, staff)],
    ['Consultant approved by', staffName(summary.consultantApprovedByUserId, staff)],
    ['Finalized at', formatDate(summary.finalizedAt)],
  ]);

  doc.save(`discharge-summary-${admission.admissionNumber}.pdf`);
}
