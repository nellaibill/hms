import { ApiError } from '@hms/shared';
import { Download, Loader2, Printer } from 'lucide-react';
import type { ReactNode } from 'react';
import { Button } from '@/components/ui/button';
import { PrintPageStyle } from '@/features/branding/components/PrintPageStyle';
import { exportReportToExcel, type ReportSection } from '@/features/reports/exportUtils';

interface ReportFrameProps {
  title: string;
  /** The period or store the figures are for — printed under the title. */
  caption?: string;
  /** Sheets for the Excel download; the button is off while there is nothing to export. */
  sections: ReportSection[];
  filename: string;
  isPending: boolean;
  error: unknown;
  /** Headline figures above the tables. */
  tiles?: ReactNode;
  /** Report-specific controls (sort order, idle days…) beside the export buttons. */
  controls?: ReactNode;
  children: ReactNode;
}

/** What every report shares: a title line with Excel and Print, the loading and error states,
 * and a printable body. */
export function ReportFrame({ title, caption, sections, filename, isPending, error, tiles, controls, children }: ReportFrameProps) {
  const hasRows = sections.some((section) => section.rows.length > 0);

  return (
    <section className="print-target flex min-w-0 flex-1 flex-col gap-3">
      <PrintPageStyle document="report" />
      <div className="flex flex-wrap items-end justify-between gap-2">
        <div className="min-w-0">
          <h2 className="text-base font-semibold text-foreground">{title}</h2>
          {caption && <p className="text-xs text-muted-foreground">{caption}</p>}
        </div>
        <div className="flex flex-wrap items-center gap-2 print:hidden">
          {controls}
          <Button size="sm" variant="outline" onClick={() => void exportReportToExcel(filename, sections)} disabled={!hasRows}>
            <Download className="h-4 w-4" />
            Excel
          </Button>
          <Button size="sm" variant="outline" onClick={() => window.print()} disabled={!hasRows}>
            <Printer className="h-4 w-4" />
            Print
          </Button>
        </div>
      </div>

      {isPending ? (
        <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" />
          Working out the figures…
        </div>
      ) : error ? (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {error instanceof ApiError ? (error.status === 403 ? 'You do not have access to this report.' : error.message) : 'The report could not be loaded.'}
        </p>
      ) : (
        <>
          {tiles && <div className="grid grid-cols-[repeat(auto-fill,minmax(9.5rem,1fr))] gap-2">{tiles}</div>}
          {children}
        </>
      )}
    </section>
  );
}
