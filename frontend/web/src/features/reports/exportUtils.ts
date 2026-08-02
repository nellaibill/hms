import ExcelJS from 'exceljs';
import { jsPDF } from 'jspdf';
import autoTable from 'jspdf-autotable';

export interface ReportSection {
  heading: string;
  headers: string[];
  rows: (string | number)[][];
}

function download(blob: Blob, filename: string) {
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = filename;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  URL.revokeObjectURL(url);
}

function escapeCsvCell(value: string | number): string {
  const str = String(value);
  return /[",\n]/.test(str) ? `"${str.replace(/"/g, '""')}"` : str;
}

export function exportReportToCsv(filename: string, sections: ReportSection[]) {
  const lines: string[] = [];
  for (const section of sections) {
    lines.push(section.heading);
    lines.push(section.headers.map(escapeCsvCell).join(','));
    for (const row of section.rows) {
      lines.push(row.map(escapeCsvCell).join(','));
    }
    lines.push('');
  }
  download(new Blob([lines.join('\n')], { type: 'text/csv;charset=utf-8;' }), filename);
}

/** One worksheet per section — Excel sheet names are capped at 31 characters. */
export async function exportReportToExcel(filename: string, sections: ReportSection[]) {
  const workbook = new ExcelJS.Workbook();
  for (const section of sections) {
    const sheet = workbook.addWorksheet(section.heading.slice(0, 31));
    sheet.addRow(section.headers).font = { bold: true };
    section.rows.forEach((row) => sheet.addRow(row));
    sheet.columns.forEach((column) => {
      column.width = 22;
    });
  }
  const buffer = await workbook.xlsx.writeBuffer();
  download(new Blob([buffer], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' }), filename);
}

/** jspdf-autotable sets this on the doc at runtime (see its source) but doesn't type it — https://github.com/simonbengtsson/jsPDF-AutoTable */
interface DocWithLastAutoTable extends jsPDF {
  lastAutoTable?: { finalY: number };
}

export function exportReportToPdf(filename: string, title: string, sections: ReportSection[]) {
  const doc = new jsPDF() as DocWithLastAutoTable;
  doc.setFontSize(14);
  doc.text(title, 14, 16);
  let cursorY = 24;

  for (const section of sections) {
    doc.setFontSize(11);
    doc.text(section.heading, 14, cursorY);
    autoTable(doc, {
      startY: cursorY + 4,
      head: [section.headers],
      body: section.rows.map((row) => row.map(String)),
      styles: { fontSize: 8 },
      margin: { left: 14, right: 14 },
    });
    cursorY = (doc.lastAutoTable?.finalY ?? cursorY) + 12;
  }

  doc.save(filename);
}
