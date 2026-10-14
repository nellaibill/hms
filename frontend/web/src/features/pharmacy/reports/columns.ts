import type { ReactNode } from 'react';
import { money, quantity, round2 } from '@/features/pharmacy/common/format';
import type { ReportSection } from '@/features/reports/exportUtils';

export type ColumnKind = 'text' | 'money' | 'quantity' | 'count' | 'percent';

/**
 * One column of a report: where its value comes from, how it reads, and whether it is added
 * up at the foot. The same definition draws the table on screen and fills the Excel sheet,
 * so the two can never disagree.
 */
export interface ReportColumn<T> {
  header: string;
  value: (row: T) => string | number | null | undefined;
  kind?: ColumnKind;
  /** Sum the column in the footer (numeric kinds only). */
  total?: boolean;
  /** Replaces the default cell, e.g. to make a document number a link. */
  render?: (row: T) => ReactNode;
  mono?: boolean;
  /** Keeps the cell on one line — dates and document numbers. */
  nowrap?: boolean;
}

export function isNumeric(kind: ColumnKind | undefined): boolean {
  return kind !== undefined && kind !== 'text';
}

export function formatValue(value: string | number | null | undefined, kind: ColumnKind | undefined): string {
  if (value === null || value === undefined || value === '') return '—';
  if (typeof value !== 'number') return value;
  switch (kind) {
    case 'money':
      return money(value);
    case 'quantity':
      return quantity(value);
    case 'percent':
      return `${value.toFixed(1)}%`;
    default:
      return String(value);
  }
}

export function columnTotal<T>(column: ReportColumn<T>, rows: T[]): number {
  return round2(
    rows.reduce((sum, row) => {
      const value = column.value(row);
      return sum + (typeof value === 'number' ? value : 0);
    }, 0),
  );
}

/** The table as a worksheet: the rows as shown, then the totals row when there is one. */
export function toSection<T>(heading: string, columns: ReportColumn<T>[], rows: T[]): ReportSection {
  const body: (string | number)[][] = rows.map((row) => columns.map((column) => column.value(row) ?? ''));
  if (rows.length > 0 && columns.some((column) => column.total)) {
    body.push(columns.map((column, index) => (column.total ? columnTotal(column, rows) : index === 0 ? 'Total' : '')));
  }
  return { heading, headers: columns.map((column) => column.header), rows: body };
}

export function sum<T>(rows: T[], pick: (row: T) => number | null | undefined): number {
  return round2(rows.reduce((total, row) => total + (pick(row) ?? 0), 0));
}
