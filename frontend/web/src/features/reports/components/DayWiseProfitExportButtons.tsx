import { formatDateRangeLabel, formatReportDate } from '@/lib/reportDateRange';
import { consultantLabel } from '../consultantLabel';
import { profitPercent, type DayWiseProfit, type ProfitFigures } from '../dayWiseProfit';
import { exportReportToCsv, exportReportToExcel, exportReportToPdf, type ReportSection } from '../exportUtils';
import type { ReportDateRange } from '../types';
import { ExportMenu, type ExportFormatOption } from './ExportMenu';

interface DayWiseProfitExportButtonsProps {
  range: ReportDateRange;
  report: DayWiseProfit;
  title: string;
}

const FIGURE_HEADERS = ['Lines', 'Amount', 'Discount', 'Revenue', 'Not costed', 'Consultant share', 'Running cost', 'Hospital profit', 'Profit %'];

function figureCells(figures: ProfitFigures): (string | number)[] {
  const percent = profitPercent(figures);
  return [
    figures.lines,
    figures.charge,
    figures.discount,
    figures.revenue,
    figures.notCostedRevenue,
    figures.consultantShare,
    figures.runningCost,
    figures.hospitalProfit,
    percent === null ? '—' : percent,
  ];
}

/** The tree flattened into rows: each day's total, then each consultant's total and their
 * charge heads — so a spreadsheet can be filtered by the Level column. */
function buildSections(report: DayWiseProfit): ReportSection[] {
  const detail: (string | number)[][] = [];
  for (const day of report.days) {
    const date = formatReportDate(day.date);
    detail.push([date, 'Day total', '', '', ...figureCells(day.figures)]);
    for (const consultant of day.consultants) {
      const name = consultantLabel(consultant.consultantId);
      detail.push([date, 'Consultant total', name, '', ...figureCells(consultant.figures)]);
      for (const head of consultant.heads) detail.push([date, 'Charge head', name, head.label, ...figureCells(head.figures)]);
    }
  }
  detail.push(['', 'Grand total', '', '', ...figureCells(report.total)]);

  return [
    { heading: 'By day, consultant and charge head', headers: ['Date', 'Level', 'Consultant', 'Charge head', ...FIGURE_HEADERS], rows: detail },
    {
      heading: 'By charge head',
      headers: ['Charge head', ...FIGURE_HEADERS],
      rows: [...report.byHead.map((head) => [head.label, ...figureCells(head.figures)]), ['Total', ...figureCells(report.total)]],
    },
  ];
}

export function DayWiseProfitExportButtons({ range, report, title }: DayWiseProfitExportButtonsProps) {
  const filenameBase = `${title.toLowerCase().replace(/[^a-z0-9]+/g, '-')}_${range.from}_to_${range.to}`;

  async function handleExport(format: ExportFormatOption) {
    const sections = buildSections(report);
    if (format === 'csv') exportReportToCsv(`${filenameBase}.csv`, sections);
    else if (format === 'excel') await exportReportToExcel(`${filenameBase}.xlsx`, sections);
    else exportReportToPdf(`${filenameBase}.pdf`, `${title} (${formatDateRangeLabel(range.from, range.to)})`, sections);
  }

  return <ExportMenu onExport={handleExport} />;
}
