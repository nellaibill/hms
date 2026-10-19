import type { Admission, DischargeSummary, Patient, StaffDirectoryEntry } from '@hms/shared';
import { GState, jsPDF } from 'jspdf';
import autoTable, { type RowInput, type UserOptions } from 'jspdf-autotable';
import type { PdfImage } from '@/lib/loadImageForPdf';

/** jspdf-autotable sets this on the doc at runtime (see its source) but doesn't type it — https://github.com/simonbengtsson/jsPDF-AutoTable */
interface DocWithLastAutoTable extends jsPDF {
  lastAutoTable?: { finalY: number };
}

/** A consultant as printed on the letterhead — the Consultant master has no qualifications or registration number yet. */
export interface LetterheadDoctor {
  name: string;
  specialization?: string | null;
}

/** Hospital identity for the letterhead, gathered by useDischargeSummaryPdfExport. */
export interface DischargeSummaryLetterhead {
  hospitalName: string;
  address?: string | null;
  phoneNumber?: string | null;
  /** The Print logo, already rasterized — null prints the hospital name in its place. */
  logo?: PdfImage | null;
  /** The admission's consultant, named in the header's dark panel on page 1. */
  consultant?: LetterheadDoctor | null;
  /** The hospital's consultants for page 1's footer band, in display order. */
  consultants: LetterheadDoctor[];
}

type Rgb = [number, number, number];

const COLORS = {
  ink: [33, 33, 33],
  muted: [100, 100, 100],
  panel: [74, 74, 77],
  blue: [33, 118, 210],
  blueLight: [126, 176, 232],
  band: [240, 240, 240],
  frame: [125, 125, 125],
  draft: [185, 40, 40],
} satisfies Record<string, Rgb>;

// A4 portrait, in mm. Every page frames its body in a dashed box like the printed form; the
// footer band below it is the consultants list on page 1 and contact details after that.
const PAGE_WIDTH = 210;
const FRAME_LEFT = 12;
const FRAME_RIGHT = 198;
const CONTENT_LEFT = 16;
const CONTENT_RIGHT = 194;
const CONTINUATION_FRAME_TOP = 34;
const FRAME_BOTTOM = 264;
const CONTENT_BOTTOM = FRAME_BOTTOM - 3;
const FOOTER_TOP = 268;
const MAX_FOOTER_CONSULTANTS = 9;

const foodInstructionLabels: Record<string, string> = {
  BeforeFood: 'Before food',
  AfterFood: 'After food',
};

function formatDateTime(value?: string | null): string {
  if (!value) return '—';
  const date = new Date(value);
  return Number.isNaN(date.getTime())
    ? '—'
    : date.toLocaleString('en-IN', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit', hour12: true });
}

function withUnit(value: number | null | undefined, unit: string): string {
  return value === null || value === undefined ? '—' : `${value} ${unit}`;
}

function formatBmi(heightCm?: number | null, weightKg?: number | null): string {
  if (!heightCm || !weightKg) return '—';
  const metres = heightCm / 100;
  return (weightKg / (metres * metres)).toFixed(1);
}

function staffName(id: string | null | undefined, staff: StaffDirectoryEntry[]): string | null {
  if (!id) return null;
  const entry = staff.find((s) => s.id === id);
  return entry ? `${entry.firstName} ${entry.lastName}` : null;
}

/** Consultant master names are stored with or without the "Dr." prefix — print it exactly once. */
function doctorName(name: string): string {
  return /^dr\b/i.test(name.trim()) ? name.trim() : `Dr. ${name.trim()}`;
}

function fitBox(image: PdfImage, maxWidth: number, maxHeight: number): { width: number; height: number } {
  const scale = Math.min(maxWidth / image.width, maxHeight / image.height);
  return { width: image.width * scale, height: image.height * scale };
}

function fillPolygon(doc: jsPDF, points: [number, number][], color: Rgb) {
  const [start, ...rest] = points;
  let [previousX, previousY] = start;
  const deltas = rest.map(([x, y]) => {
    const delta = [x - previousX, y - previousY];
    previousX = x;
    previousY = y;
    return delta;
  });
  doc.setFillColor(...color);
  doc.lines(deltas, start[0], start[1], [1, 1], 'F', true);
}

function dashedRect(doc: jsPDF, x: number, y: number, width: number, height: number) {
  doc.setDrawColor(...COLORS.frame);
  doc.setLineWidth(0.3);
  doc.setLineDashPattern([1.2, 0.9], 0);
  doc.rect(x, y, width, height, 'S');
  doc.setLineDashPattern([], 0);
}

/** Height of one line of text at `fontSize` pt, in mm, at jsPDF's default 1.15 line-height factor. */
function lineGap(fontSize: number): number {
  return fontSize * 1.15 * 0.3528;
}

interface FormField {
  label: string;
  value: string;
  x: number;
  width: number;
}

/** "Label: value" cells on one baseline, values wrapping under themselves. Returns the tallest cell's line count. */
function drawFieldRow(doc: jsPDF, fields: FormField[], baseline: number): number {
  let lineCount = 1;
  doc.setFontSize(9);
  doc.setTextColor(...COLORS.ink);
  for (const field of fields) {
    const label = `${field.label}:`;
    doc.setFont('helvetica', 'bold');
    doc.text(label, field.x, baseline);
    const labelWidth = doc.getTextWidth(label) + 1.5;
    doc.setFont('helvetica', 'normal');
    const lines: string[] = doc.splitTextToSize(field.value, Math.max(field.width - labelWidth, 12));
    doc.text(lines, field.x + labelWidth, baseline);
    lineCount = Math.max(lineCount, lines.length);
  }
  return lineCount;
}

/** Stacks field rows inside a dashed box starting at `top`; returns the box's bottom edge. */
function drawFieldBox(doc: jsPDF, rows: FormField[][], top: number): number {
  const gap = lineGap(9);
  let baseline = top + 5.2;
  let bottom = top;
  rows.forEach((row, index) => {
    const lines = drawFieldRow(doc, row, baseline);
    bottom = baseline + (lines - 1) * gap + 2.8;
    if (index < rows.length - 1) baseline += (lines - 1) * gap + 5.8;
  });
  dashedRect(doc, FRAME_LEFT, top, FRAME_RIGHT - FRAME_LEFT, bottom - top);
  return bottom;
}

function drawHeader(doc: jsPDF, letterhead: DischargeSummaryLetterhead, showConsultant: boolean) {
  if (letterhead.logo) {
    const { width, height } = fitBox(letterhead.logo, 92, 19);
    doc.addImage(letterhead.logo.dataUrl, 'PNG', 10, 5 + (19 - height) / 2, width, height, 'letterhead-logo', 'FAST');
  } else {
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(16);
    doc.setTextColor(...COLORS.ink);
    doc.text(letterhead.hospitalName, 10, 16, { maxWidth: 98 });
  }

  // The slanted blue stripes and dark panel of the printed letterhead.
  fillPolygon(doc, [[112, 0], [116, 0], [126, 27], [122, 27]], COLORS.blueLight);
  fillPolygon(doc, [[119, 0], [128, 0], [138, 27], [129, 27]], COLORS.blue);
  fillPolygon(doc, [[131, 0], [PAGE_WIDTH, 0], [PAGE_WIDTH, 27], [141, 27]], COLORS.panel);

  if (!showConsultant || !letterhead.consultant) return;
  const right = PAGE_WIDTH - 6;
  doc.setTextColor(255, 255, 255);
  doc.setFont('helvetica', 'bold');
  doc.setFontSize(9.5);
  doc.text(doctorName(letterhead.consultant.name), right, 9.5, { align: 'right' });
  if (letterhead.consultant.specialization) {
    doc.setFont('helvetica', 'normal');
    doc.setFontSize(7.5);
    const lines: string[] = doc.splitTextToSize(letterhead.consultant.specialization, 60);
    doc.text(lines.slice(0, 3), right, 14, { align: 'right' });
  }
}

function drawWatermark(doc: jsPDF, logo: PdfImage | null | undefined) {
  if (!logo) return;
  const { width, height } = fitBox(logo, 120, 60);
  doc.saveGraphicsState();
  doc.setGState(new GState({ opacity: 0.07 }));
  doc.addImage(logo.dataUrl, 'PNG', (PAGE_WIDTH - width) / 2, 150 - height / 2, width, height, 'letterhead-logo', 'FAST');
  doc.restoreGraphicsState();
}

function drawConsultantsBand(doc: jsPDF, letterhead: DischargeSummaryLetterhead) {
  doc.setFillColor(...COLORS.panel);
  doc.rect(0, FOOTER_TOP, PAGE_WIDTH, 1.4, 'F');
  fillPolygon(doc, [[146, FOOTER_TOP], [PAGE_WIDTH, FOOTER_TOP], [PAGE_WIDTH, FOOTER_TOP + 2.6], [147, FOOTER_TOP + 2.6]], COLORS.blue);
  doc.setFillColor(...COLORS.band);
  doc.rect(0, FOOTER_TOP + 2.6, PAGE_WIDTH, 297 - FOOTER_TOP - 2.6, 'F');

  const consultants = letterhead.consultants.slice(0, MAX_FOOTER_CONSULTANTS);
  if (consultants.length === 0) {
    // No consultant list to show — keep the hospital's own contact line here instead.
    drawCentredContact(doc, letterhead, FOOTER_TOP + 11);
    return;
  }

  const rows: LetterheadDoctor[][] = [];
  for (let i = 0; i < consultants.length; i += 3) rows.push(consultants.slice(i, i + 3));
  // Two lines of specialization fit while there are at most two rows; a third row gets one.
  const specLines = rows.length <= 2 ? 2 : 1;
  const pitch = rows.length <= 2 ? 11.5 : 8.2;
  const bandTop = FOOTER_TOP + 2.6;
  const blockHeight = (rows.length - 1) * pitch + 3.3 + specLines * lineGap(7);
  let baseline = bandTop + (297 - bandTop - blockHeight) / 2 + 2.4;
  for (const row of rows) {
    // Columns 64 mm apart, centred on the page whatever the row's length.
    const firstCentre = PAGE_WIDTH / 2 - ((row.length - 1) * 64) / 2;
    row.forEach((doctor, index) => {
      const centre = firstCentre + index * 64;
      doc.setTextColor(...COLORS.ink);
      doc.setFont('helvetica', 'bold');
      doc.setFontSize(8);
      doc.text(doctorName(doctor.name), centre, baseline, { align: 'center', maxWidth: 62 });
      if (doctor.specialization) {
        doc.setFont('helvetica', 'normal');
        doc.setFontSize(7);
        doc.setTextColor(...COLORS.muted);
        const lines = doc.splitTextToSize(doctor.specialization, 60) as string[];
        const shown = lines.slice(0, specLines);
        if (lines.length > specLines) shown[specLines - 1] = `${shown[specLines - 1]}…`;
        doc.text(shown, centre, baseline + 3.3, { align: 'center' });
      }
    });
    baseline += pitch;
  }
}

function drawCentredContact(doc: jsPDF, letterhead: DischargeSummaryLetterhead, baseline: number) {
  doc.setTextColor(...COLORS.ink);
  doc.setFont('helvetica', 'bold');
  doc.setFontSize(9);
  doc.text(letterhead.hospitalName, PAGE_WIDTH / 2, baseline, { align: 'center' });
  doc.setFont('helvetica', 'normal');
  doc.setFontSize(7.5);
  const details = [letterhead.address, letterhead.phoneNumber ? `Phone: ${letterhead.phoneNumber}` : null].filter(Boolean).join('  ·  ');
  if (details) doc.text(doc.splitTextToSize(details, 180) as string[], PAGE_WIDTH / 2, baseline + 4.5, { align: 'center' });
}

function drawContactBand(doc: jsPDF, letterhead: DischargeSummaryLetterhead) {
  const top = FOOTER_TOP + 3;
  fillPolygon(doc, [[0, top], [124, top], [114, 297], [0, 297]], COLORS.panel);
  fillPolygon(doc, [[129, top + 6], [PAGE_WIDTH, top + 6], [PAGE_WIDTH, 297], [121, 297]], COLORS.blue);

  // Text is centred vertically within each panel.
  doc.setTextColor(255, 255, 255);
  doc.setFont('helvetica', 'normal');
  doc.setFontSize(8);
  const phoneLines = letterhead.phoneNumber ? (doc.splitTextToSize(`Phone: ${letterhead.phoneNumber}`, 100) as string[]).slice(0, 3) : [];
  const nameBaseline = top + (297 - top - (3.4 + phoneLines.length * lineGap(8))) / 2 + 2.6;
  doc.setFont('helvetica', 'bold');
  doc.setFontSize(9);
  doc.text(letterhead.hospitalName, 10, nameBaseline, { maxWidth: 100 });
  if (phoneLines.length > 0) {
    doc.setFont('helvetica', 'normal');
    doc.setFontSize(8);
    doc.text(phoneLines, 10, nameBaseline + 4.6);
  }
  if (letterhead.address) {
    doc.setFont('helvetica', 'normal');
    doc.setFontSize(8);
    const addressLines = (doc.splitTextToSize(letterhead.address, 74) as string[]).slice(0, 4);
    const blueTop = top + 6;
    doc.text(addressLines, PAGE_WIDTH - 6, blueTop + (297 - blueTop - addressLines.length * lineGap(8)) / 2 + 2.4, { align: 'right' });
  }
}

/**
 * Client-side PDF export for a discharge summary, laid out like the hospital's printed
 * consultation form: logo letterhead with the consultant's panel, a dashed patient/vitals box,
 * "Heading:" sections inside a dashed frame, a faint logo watermark, the consultants band on
 * page 1, the contact band on later pages, and a doctor's signature line at the end. As on the
 * form, page 1 holds complaints, history and examination; diagnosis onwards starts page 2.
 * Uses jsPDF + jspdf-autotable (no backend PDF library exists); autotable does the wrapping and
 * page breaks for the long narrative fields.
 */
export function exportDischargeSummaryPdf(
  summary: DischargeSummary,
  patient: Patient,
  admission: Admission,
  staff: StaffDirectoryEntry[],
  letterhead: DischargeSummaryLetterhead,
  patientAddress: string,
) {
  const doc = new jsPDF({ compress: true }) as DocWithLastAutoTable;
  const patientName = `${patient.firstName} ${patient.lastName}`;
  const decoratedPages = new Set<number>();

  /** Watermark and letterhead go down before a page's content, once per page. */
  function decoratePage() {
    const page = doc.getCurrentPageInfo().pageNumber;
    if (decoratedPages.has(page)) return;
    decoratedPages.add(page);
    drawWatermark(doc, letterhead.logo);
    drawHeader(doc, letterhead, page === 1);
    if (page > 1) {
      doc.setFont('helvetica', 'normal');
      doc.setFontSize(7.5);
      doc.setTextColor(...COLORS.muted);
      doc.text(`${patientName}  ·  UHID: ${patient.uhid}  ·  IP No: ${admission.admissionNumber}`, FRAME_LEFT, 31.5);
      doc.text('Discharge Summary', FRAME_RIGHT, 31.5, { align: 'right' });
    }
  }

  decoratePage();

  // Title.
  const title = 'DISCHARGE SUMMARY';
  doc.setFont('helvetica', 'bold');
  doc.setFontSize(12.5);
  doc.setTextColor(...COLORS.ink);
  doc.text(title, PAGE_WIDTH / 2, 34.5, { align: 'center' });
  const titleWidth = doc.getTextWidth(title);
  doc.setDrawColor(...COLORS.ink);
  doc.setLineWidth(0.35);
  doc.line(PAGE_WIDTH / 2 - titleWidth / 2, 35.7, PAGE_WIDTH / 2 + titleWidth / 2, 35.7);
  if (summary.status !== 'Finalized') {
    doc.setFontSize(8);
    doc.setTextColor(...COLORS.draft);
    doc.text(`${summary.status.toUpperCase()} — NOT FINALIZED`, FRAME_RIGHT, 34.5, { align: 'right' });
  }

  // Patient box, then the vitals box sharing its bottom edge.
  const allergies =
    patient.allergies.length > 0 ? patient.allergies.map((allergy) => allergy.specify || allergy.allergyType).join(', ') : 'None recorded';
  const colA = CONTENT_LEFT - 1;
  const colB = 98;
  const colC = 150;
  const patientBottom = drawFieldBox(
    doc,
    [
      [
        { label: 'Patient Name', value: patientName, x: colA, width: colB - colA - 3 },
        { label: 'Age/Sex', value: `${patient.age} Yrs / ${patient.gender}`, x: colB, width: colC - colB - 3 },
        { label: 'UHID', value: patient.uhid, x: colC, width: CONTENT_RIGHT - colC },
      ],
      [
        { label: 'Address', value: patientAddress || '—', x: colA, width: colC - colA - 3 },
        { label: 'IP No', value: admission.admissionNumber, x: colC, width: CONTENT_RIGHT - colC },
      ],
      [
        { label: 'DOA', value: formatDateTime(admission.admissionDateTime), x: colA, width: colB - colA - 3 },
        { label: 'DOD', value: formatDateTime(admission.dischargeDateTime), x: colB, width: colC - colB - 3 },
        { label: 'Ward/Bed', value: `${admission.wardName || '—'} / ${admission.bedNumber || '—'}`, x: colC, width: CONTENT_RIGHT - colC },
      ],
      [
        { label: 'Consultant', value: admission.consultantName ? doctorName(admission.consultantName) : '—', x: colA, width: colB - colA - 3 },
        { label: 'Allergies', value: allergies, x: colB, width: CONTENT_RIGHT - colB },
      ],
    ],
    38.5,
  );
  const vitalsBottom = drawFieldBox(
    doc,
    [
      [
        { label: 'Height', value: withUnit(summary.heightCm, 'cm'), x: colA, width: 60 },
        { label: 'Weight', value: withUnit(summary.weightKg, 'kg'), x: colB - 20, width: 50 },
        { label: 'BMI', value: formatBmi(summary.heightCm, summary.weightKg), x: colC - 10, width: 40 },
      ],
      [
        { label: 'PR', value: withUnit(summary.pulseRate, '/min'), x: colA, width: 34 },
        { label: 'BP', value: summary.bloodPressure ? `${summary.bloodPressure} mmHg` : '—', x: 51, width: 40 },
        { label: 'RR', value: withUnit(summary.respiratoryRate, '/min'), x: 93, width: 32 },
        { label: 'Temp', value: withUnit(summary.temperatureF, '°F'), x: 127, width: 34 },
        { label: 'SpO2', value: withUnit(summary.spO2Percent, '%'), x: 163, width: CONTENT_RIGHT - 163 },
      ],
    ],
    patientBottom,
  );
  const firstFrameTop = vitalsBottom;

  // Body sections.
  let cursorY = firstFrameTop + 6;
  const tableMargin = { left: CONTENT_LEFT + 4, right: PAGE_WIDTH - CONTENT_RIGHT, top: CONTINUATION_FRAME_TOP + 6, bottom: 297 - CONTENT_BOTTOM };

  function newPage() {
    doc.addPage();
    decoratePage();
    cursorY = CONTINUATION_FRAME_TOP + 6;
  }

  function ensureSpace(needed: number) {
    if (cursorY + needed > CONTENT_BOTTOM) newPage();
  }

  function heading(text: string, needed = 14) {
    ensureSpace(needed);
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(10);
    doc.setTextColor(...COLORS.ink);
    doc.text(`${text}:`, CONTENT_LEFT, cursorY);
    cursorY += 1.8;
  }

  function subheading(text: string) {
    ensureSpace(12);
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(9);
    doc.setTextColor(...COLORS.muted);
    doc.text(text, CONTENT_LEFT + 4, cursorY + 2.6);
    cursorY += 3.6;
  }

  function table(options: UserOptions, spacingAfter = 5.5) {
    autoTable(doc, { startY: cursorY, margin: tableMargin, rowPageBreak: 'auto', willDrawPage: decoratePage, ...options });
    cursorY = (doc.lastAutoTable?.finalY ?? cursorY) + spacingAfter;
  }

  const plainStyles: UserOptions['styles'] = {
    font: 'helvetica',
    fontSize: 9.5,
    textColor: COLORS.ink,
    cellPadding: { top: 0.9, bottom: 0.9, left: 0, right: 2 },
    overflow: 'linebreak',
  };

  function narrative(text: string, value?: string | null) {
    heading(text);
    table({ body: [[value?.trim() || '—']], theme: 'plain', styles: plainStyles });
  }

  /** A sub-labelled section; blank sub-fields are left out, an all-blank section prints "—". */
  function fields(text: string, rows: [string, string | null | undefined][]) {
    heading(text);
    const filled: RowInput[] = rows.filter(([, value]) => value?.trim()).map(([label, value]) => [label, value!.trim()]);
    table({
      body: filled.length > 0 ? filled : [['—']],
      theme: 'plain',
      styles: plainStyles,
      columnStyles: filled.length > 0 ? { 0: { fontStyle: 'bold', cellWidth: 48, textColor: COLORS.muted } } : {},
    });
  }

  narrative('Presenting Complaints', summary.chiefComplaints);

  fields('Clinical History', [
    ['History of presenting illness', summary.historyOfPresentingIllness],
    ['Past medical history', summary.pastMedicalHistory],
    ['Past surgical history', summary.pastSurgicalHistory],
    ['Family history', summary.familyHistory],
    ['Personal history', summary.personalHistory],
  ]);

  fields('Examination Findings', [
    ['General examination', summary.generalExamination],
    ['CVS', summary.cvsFindings],
    ['RS', summary.rsFindings],
    ['P/A', summary.paFindings],
    ['CNS', summary.cnsFindings],
    ['Local examination', summary.localExamination],
    ['Gait', summary.gait],
  ]);

  // As on the printed form, diagnosis onwards begins on the second sheet.
  if (doc.getCurrentPageInfo().pageNumber === 1) newPage();

  narrative('Diagnosis', summary.finalDiagnosis);
  narrative('Course in Hospital', summary.courseInHospital);

  const procedureRows: [string, string | null | undefined][] = [
    ['Procedure', summary.procedureName],
    ['Date / time', summary.procedureDateTime ? formatDateTime(summary.procedureDateTime) : null],
    ['Primary surgeon', summary.primarySurgeon],
    ['Assistant surgeon(s)', summary.assistantSurgeons],
    ['Anaesthetist', summary.anaesthetist],
    ['Anaesthesia', summary.anaesthesia],
    ['Surgical position', summary.surgicalPosition],
    ['Intra-operative findings', summary.intraOperativeFindings],
    ['Operative notes', summary.operativeNotes],
  ];
  // Medical admissions have no procedure — skip the section rather than print a row of dashes.
  if (procedureRows.some(([, value]) => value?.trim())) fields('Procedure / Operation Details', procedureRows);

  narrative('Condition at Discharge', summary.conditionAtDischarge);

  const medications = [...summary.medications].sort((a, b) => a.sortOrder - b.sortOrder);
  heading('Plan of Management', medications.length > 0 ? 26 : 14);
  if (medications.length > 0) {
    subheading('Discharge medications');
    table(
      {
        head: [['#', 'Medicine', 'Dose', 'Route', 'M-N-E-N', 'Days', 'Food']],
        body: medications.map((medication, index) => [
          String(index + 1),
          medication.drugName,
          medication.dose,
          medication.route,
          `${medication.morningQty}-${medication.noonQty}-${medication.eveningQty}-${medication.nightQty}`,
          String(medication.durationDays),
          foodInstructionLabels[medication.foodInstruction] ?? medication.foodInstruction,
        ]),
        theme: 'grid',
        styles: { font: 'helvetica', fontSize: 8.5, cellPadding: 1.4, textColor: COLORS.ink, fillColor: false, lineColor: [170, 170, 170], lineWidth: 0.2 },
        headStyles: { fillColor: [235, 235, 235], fontStyle: 'bold' },
        columnStyles: { 0: { cellWidth: 7, halign: 'center' }, 4: { cellWidth: 20, halign: 'center' }, 5: { cellWidth: 11, halign: 'center' } },
      },
      3,
    );
  }
  const adviceRows: RowInput[] = (
    [
      ['Diet', summary.diet],
      ['Wound care', summary.woundCare],
      ['Activity', summary.activity],
      ['Physiotherapy', summary.physiotherapy],
    ] as [string, string | null | undefined][]
  )
    .filter(([, value]) => value?.trim())
    .map(([label, value]) => [label, value!.trim()]);
  if (medications.length === 0) adviceRows.unshift(['Discharge medications', 'None prescribed']);
  if (adviceRows.length > 0) {
    table({
      body: adviceRows,
      theme: 'plain',
      styles: plainStyles,
      columnStyles: { 0: { fontStyle: 'bold', cellWidth: 48, textColor: COLORS.muted } },
    });
  } else {
    cursorY += 2.5;
  }

  narrative('Review', summary.reviewInstructions);
  narrative('Emergency Review (SOS)', summary.emergencyInstructions);

  // Sign-off, pinned to the bottom of the last page's frame like the form's signature line.
  const signatureHeight = 21;
  ensureSpace(signatureHeight);
  const signatureTop = CONTENT_BOTTOM - signatureHeight;
  const signer = staffName(summary.consultantApprovedByUserId, staff) ?? admission.consultantName;
  const signedBy = signer ? doctorName(signer) : null;
  doc.setDrawColor(...COLORS.ink);
  doc.setLineWidth(0.3);
  doc.setLineDashPattern([0.4, 0.8], 0);
  doc.line(136, signatureTop + 11, CONTENT_RIGHT, signatureTop + 11);
  doc.setLineDashPattern([], 0);
  doc.setTextColor(...COLORS.ink);
  doc.setFont('helvetica', 'bold');
  doc.setFontSize(9.5);
  doc.text("Doctor's Signature", CONTENT_RIGHT, signatureTop + 15.5, { align: 'right' });
  if (signedBy) {
    doc.setFont('helvetica', 'normal');
    doc.setFontSize(8);
    doc.text(signedBy, CONTENT_RIGHT, signatureTop + 19.5, { align: 'right' });
  }
  const signOff = [
    ['Prepared by', staffName(summary.preparedByUserId, staff)],
    ['Checked by', staffName(summary.checkedByUserId, staff)],
    ['Finalized on', summary.finalizedAt ? formatDateTime(summary.finalizedAt) : null],
  ].filter((entry): entry is [string, string] => Boolean(entry[1]));
  doc.setFontSize(8);
  signOff.forEach(([label, value], index) => {
    const baseline = signatureTop + 11 + index * 4.2;
    doc.setFont('helvetica', 'bold');
    doc.setTextColor(...COLORS.muted);
    doc.text(`${label}:`, CONTENT_LEFT, baseline);
    doc.setFont('helvetica', 'normal');
    doc.setTextColor(...COLORS.ink);
    doc.text(value, CONTENT_LEFT + 22, baseline);
  });

  // Frames, footers and page numbers, now that the page count is known.
  const pageCount = doc.getNumberOfPages();
  for (let page = 1; page <= pageCount; page++) {
    doc.setPage(page);
    const frameTop = page === 1 ? firstFrameTop : CONTINUATION_FRAME_TOP;
    dashedRect(doc, FRAME_LEFT, frameTop, FRAME_RIGHT - FRAME_LEFT, FRAME_BOTTOM - frameTop);
    if (page === 1) drawConsultantsBand(doc, letterhead);
    else drawContactBand(doc, letterhead);
    doc.setFont('helvetica', 'normal');
    doc.setFontSize(7);
    doc.setTextColor(...COLORS.muted);
    doc.text(`Page ${page} of ${pageCount}`, FRAME_RIGHT, FRAME_BOTTOM + 2.8, { align: 'right' });
  }

  doc.save(`discharge-summary-${admission.admissionNumber}.pdf`);
}
