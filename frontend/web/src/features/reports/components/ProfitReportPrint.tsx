import { branding } from '@/config/branding';
import { PrintPageStyle } from '@/features/branding/components/PrintPageStyle';
import { useBrandingQuery } from '@/features/branding/hooks/useBrandingQuery';
import { formatCurrency } from '@/features/billing';
import { resolveRecordLabel } from '@/features/masters';
import { formatDateRangeLabel, formatReportDate } from '@/lib/reportDateRange';
import { DEFAULT_PROFIT_LABELS, type HiddenProfitColumns, type ProfitLabels, type ProfitReportRow, type ProfitTotals } from '../profitReport';
import type { ReportDateRange } from '../types';

interface ProfitReportPrintProps {
  title: string;
  range: ReportDateRange;
  /** Human-readable filters in force, e.g. "Department: Orthopaedics" — printed under the title. */
  filterSummary: string[];
  /** Every row matching the filters, not just the page on screen. */
  rows: ProfitReportRow[];
  totals: ProfitTotals;
  labels?: ProfitLabels;
  /** Columns to leave out, as on screen (ADR-126). */
  hidden?: HiddenProfitColumns;
}

function money(value: number | null): string {
  return value === null ? '—' : formatCurrency(value);
}

/**
 * The printed profit report (ADR-124): hospital name, report title, date range and filters,
 * the summary figures, then every billed line with a totals row. Hidden on screen and shown
 * alone once printing starts (index.css's `.print-target` rule), the same convention as
 * PatientReportsPage. Paper size and orientation come from Theme & Branding → Print's
 * "Reports" setting via PrintPageStyle; the full 14 columns read best on landscape. Dates read
 * "06 Nov 2026", and columns the filters already fix are left out, as on screen (ADR-126).
 */
export function ProfitReportPrint({ title, range, filterSummary, rows, totals, labels = DEFAULT_PROFIT_LABELS, hidden = {} }: ProfitReportPrintProps) {
  const { data: brandingConfig } = useBrandingQuery();
  const hospitalName = brandingConfig?.hospitalName ?? branding.hospitalName;
  const summary: [string, string][] = [
    ['Total Revenue', formatCurrency(totals.totalRevenue)],
    ['Total Discount', formatCurrency(totals.totalDiscount)],
    [`Total ${labels.cost}`, formatCurrency(totals.totalCost)],
    [`Total ${labels.profit}`, formatCurrency(totals.totalProfit)],
    ['Margin', totals.overallMarginPercent === null ? '—' : `${totals.overallMarginPercent}%`],
  ];
  const summaryCells = hidden.margin ? summary.slice(0, -1) : summary;
  // Columns before Charge that the totals row spans: Date, Invoice, Patient, Type, Service,
  // Department, Consultant, Qty — less whichever are hidden.
  const leadingColumns = 8 - (['date', 'type', 'department', 'consultant'] as const).filter((column) => hidden[column]).length;

  return (
    <div className="print-target hidden bg-white p-6 text-black print:block" style={{ fontFamily: 'Georgia, "Times New Roman", serif' }}>
      <PrintPageStyle document="report" />
      <div className="flex flex-col items-center gap-1 border-b-2 border-black pb-3 text-center">
        <span className="text-2xl font-bold tracking-tight">{hospitalName}</span>
        <span className="text-sm font-semibold">{title}</span>
      </div>

      <div className="mt-3 flex items-start justify-between gap-6 text-sm">
        <span>
          {formatDateRangeLabel(range.from, range.to)}
          {filterSummary.length > 0 ? ` · ${filterSummary.join(' · ')}` : ''}
        </span>
        <span className="whitespace-nowrap text-gray-600">Printed {new Date().toLocaleString('en-IN')}</span>
      </div>

      <div className={`mt-3 grid border border-black text-sm ${hidden.margin ? 'grid-cols-4' : 'grid-cols-5'}`}>
        {summaryCells.map(([label, value]) => (
          <div key={label} className="flex flex-col gap-0.5 border-r border-black px-2 py-1.5 last:border-r-0">
            <span className="text-xs text-gray-600">{label}</span>
            <span className="font-semibold">{value}</span>
          </div>
        ))}
      </div>
      {totals.revenueWithUnknownCost > 0 && (
        <p className="mt-1 text-xs text-gray-600">{formatCurrency(totals.revenueWithUnknownCost)} of revenue has no cost data yet and isn't reflected in the figures above.</p>
      )}

      <table className="mt-3 w-full border-collapse text-[11px]">
        <thead>
          <tr className="border-y-2 border-black text-left">
            {!hidden.date && <th className="py-1 pr-2 font-semibold">Date</th>}
            <th className="py-1 pr-2 font-semibold">Invoice</th>
            <th className="py-1 pr-2 font-semibold">Patient</th>
            {!hidden.type && <th className="py-1 pr-2 font-semibold">Type</th>}
            <th className="py-1 pr-2 font-semibold">Service</th>
            {!hidden.department && <th className="py-1 pr-2 font-semibold">Department</th>}
            {!hidden.consultant && <th className="py-1 pr-2 font-semibold">Consultant</th>}
            <th className="py-1 pr-2 text-right font-semibold">Qty</th>
            <th className="py-1 pr-2 text-right font-semibold">Charge</th>
            <th className="py-1 pr-2 text-right font-semibold">Discount</th>
            <th className="py-1 pr-2 text-right font-semibold">Revenue</th>
            <th className="py-1 pr-2 text-right font-semibold">{labels.cost}</th>
            <th className="py-1 pr-2 text-right font-semibold">{labels.profit}</th>
            {!hidden.margin && <th className="py-1 text-right font-semibold">Margin</th>}
          </tr>
        </thead>
        <tbody>
          {rows.map((row) => (
            <tr key={row.id} className="break-inside-avoid border-b border-gray-300 align-top">
              {!hidden.date && <td className="py-1 pr-2 whitespace-nowrap">{formatReportDate(row.date)}</td>}
              <td className="py-1 pr-2 whitespace-nowrap">{row.invoiceNumber ?? row.invoiceId}</td>
              <td className="py-1 pr-2">{row.patientName}</td>
              {!hidden.type && <td className="py-1 pr-2">{row.billingType}</td>}
              <td className="py-1 pr-2">{row.serviceLabel}</td>
              {!hidden.department && <td className="py-1 pr-2">{resolveRecordLabel('department', row.departmentId)}</td>}
              {!hidden.consultant && <td className="py-1 pr-2">{resolveRecordLabel('consultant', row.billedConsultantId)}</td>}
              <td className="py-1 pr-2 text-right">{row.quantity}</td>
              <td className="py-1 pr-2 text-right whitespace-nowrap">{formatCurrency(row.charge)}</td>
              <td className="py-1 pr-2 text-right whitespace-nowrap">{row.discount > 0 ? `−${formatCurrency(row.discount)}` : '—'}</td>
              <td className="py-1 pr-2 text-right whitespace-nowrap">{formatCurrency(row.revenue)}</td>
              <td className="py-1 pr-2 text-right whitespace-nowrap">{money(row.cost)}</td>
              <td className="py-1 pr-2 text-right whitespace-nowrap">{money(row.profit)}</td>
              {!hidden.margin && <td className="py-1 text-right">{row.marginPercent === null ? '—' : `${row.marginPercent}%`}</td>}
            </tr>
          ))}
        </tbody>
        <tfoot>
          <tr className="border-y-2 border-black font-bold">
            <td className="py-1.5 pr-2" colSpan={leadingColumns}>
              Total · {rows.length} line item{rows.length === 1 ? '' : 's'}
            </td>
            <td className="py-1.5 pr-2 text-right whitespace-nowrap">{formatCurrency(totals.totalCharge)}</td>
            <td className="py-1.5 pr-2 text-right whitespace-nowrap">{totals.totalDiscount > 0 ? `−${formatCurrency(totals.totalDiscount)}` : '—'}</td>
            <td className="py-1.5 pr-2 text-right whitespace-nowrap">{formatCurrency(totals.totalRevenue)}</td>
            <td className="py-1.5 pr-2 text-right whitespace-nowrap">{formatCurrency(totals.totalCost)}</td>
            <td className="py-1.5 pr-2 text-right whitespace-nowrap">{formatCurrency(totals.totalProfit)}</td>
            {!hidden.margin && <td className="py-1.5 text-right">{totals.overallMarginPercent === null ? '—' : `${totals.overallMarginPercent}%`}</td>}
          </tr>
        </tfoot>
      </table>
    </div>
  );
}
