import { ChevronDown, ChevronRight } from 'lucide-react';
import { Fragment, useState } from 'react';
import { Button } from '@/components/ui/button';
import { formatCurrency } from '@/features/billing';
import { formatReportDate } from '@/lib/reportDateRange';
import { cn } from '@/lib/utils';
import { consultantLabel } from '../consultantLabel';
import { profitPercent, type DayWiseProfit, type ProfitFigures } from '../dayWiseProfit';

interface DayWiseProfitTableProps {
  report: DayWiseProfit;
}

/**
 * The Day-wise Profit tree (ADR-142): one row per day, under it one per consultant, under that
 * one per charge head — each with lines, amount, discount, revenue, consultant share, running
 * cost, hospital profit and profit %. Days and consultants fold open and shut; the buttons above
 * the table open or fold every level at once.
 */
export function DayWiseProfitTable({ report }: DayWiseProfitTableProps) {
  const [closedDays, setClosedDays] = useState<Set<string>>(new Set());
  const [closedConsultants, setClosedConsultants] = useState<Set<string>>(new Set());

  function toggle(set: Set<string>, key: string, update: (next: Set<string>) => void) {
    const next = new Set(set);
    if (next.has(key)) next.delete(key);
    else next.add(key);
    update(next);
  }

  function showDaysOnly() {
    setClosedDays(new Set(report.days.map((day) => day.date)));
  }

  function showConsultants() {
    setClosedDays(new Set());
    setClosedConsultants(new Set(report.days.flatMap((day) => day.consultants.map((consultant) => `${day.date}|${consultant.consultantId ?? ''}`))));
  }

  function showEverything() {
    setClosedDays(new Set());
    setClosedConsultants(new Set());
  }

  return (
    <div className="flex flex-col gap-3">
      <div role="group" aria-label="Show levels" className="flex flex-wrap items-center gap-2 text-sm">
        <span className="text-muted-foreground">Show:</span>
        <Button type="button" variant="outline" size="sm" onClick={showDaysOnly}>
          Days
        </Button>
        <Button type="button" variant="outline" size="sm" onClick={showConsultants}>
          Days + consultants
        </Button>
        <Button type="button" variant="outline" size="sm" onClick={showEverything}>
          Everything
        </Button>
      </div>

      <div className="overflow-x-auto rounded-lg border border-border">
        <table className="w-full min-w-[1040px] border-collapse text-sm">
          <thead className="bg-sidebar-active text-left text-xs font-medium uppercase tracking-wide text-sidebar-active-foreground">
            <tr>
              <th scope="col" className="px-4 py-2.5">Date / Consultant / Charge head</th>
              <th scope="col" className="px-3 py-2.5 text-right">Lines</th>
              <th scope="col" className="px-3 py-2.5 text-right">Amount</th>
              <th scope="col" className="px-3 py-2.5 text-right">Discount</th>
              <th scope="col" className="px-3 py-2.5 text-right">Revenue</th>
              <th scope="col" className="px-3 py-2.5 text-right">Consultant share</th>
              <th scope="col" className="px-3 py-2.5 text-right">Running cost</th>
              <th scope="col" className="px-3 py-2.5 text-right">Hospital profit</th>
              <th scope="col" className="px-3 py-2.5 text-right">Profit %</th>
            </tr>
          </thead>
          <tbody>
            {report.days.map((day) => {
              const dayOpen = !closedDays.has(day.date);
              return (
                <Fragment key={day.date}>
                  <tr className="border-t border-border bg-amber-50 font-semibold dark:bg-amber-950/30">
                    <th scope="row" className="px-2 py-2.5 text-left">
                      <button
                        type="button"
                        aria-expanded={dayOpen}
                        onClick={() => toggle(closedDays, day.date, setClosedDays)}
                        className="inline-flex min-h-8 items-center gap-1.5 rounded px-2 text-amber-950 hover:bg-amber-100 dark:text-amber-100 dark:hover:bg-amber-900/40"
                      >
                        {dayOpen ? <ChevronDown aria-hidden="true" className="h-4 w-4" /> : <ChevronRight aria-hidden="true" className="h-4 w-4" />}
                        {formatReportDate(day.date)}
                        <span className="text-xs font-normal text-muted-foreground">· {day.consultants.length} consultant{day.consultants.length === 1 ? '' : 's'}</span>
                      </button>
                    </th>
                    <FigureCells figures={day.figures} />
                  </tr>
                  {dayOpen &&
                    day.consultants.map((consultant) => {
                      const key = `${day.date}|${consultant.consultantId ?? ''}`;
                      const consultantOpen = !closedConsultants.has(key);
                      return (
                        <Fragment key={key}>
                          <tr className="border-t border-border font-medium">
                            <th scope="row" className="py-2 pl-8 pr-2 text-left">
                              <button
                                type="button"
                                aria-expanded={consultantOpen}
                                onClick={() => toggle(closedConsultants, key, setClosedConsultants)}
                                className="inline-flex min-h-8 items-center gap-1.5 rounded px-2 text-left hover:bg-muted"
                              >
                                {consultantOpen ? <ChevronDown aria-hidden="true" className="h-4 w-4 shrink-0" /> : <ChevronRight aria-hidden="true" className="h-4 w-4 shrink-0" />}
                                <span className={cn(!consultant.consultantId && 'italic text-muted-foreground')}>{consultantLabel(consultant.consultantId)}</span>
                              </button>
                            </th>
                            <FigureCells figures={consultant.figures} />
                          </tr>
                          {consultantOpen &&
                            consultant.heads.map((head) => (
                              <tr key={`${key}|${head.head}`} className="border-t border-border/60 text-muted-foreground">
                                <th scope="row" className="py-1.5 pl-[4.75rem] pr-2 text-left font-normal text-foreground">
                                  {head.label}
                                </th>
                                <FigureCells figures={head.figures} />
                              </tr>
                            ))}
                        </Fragment>
                      );
                    })}
                </Fragment>
              );
            })}
          </tbody>
          <tfoot>
            <tr className="border-t-2 border-border bg-muted/60 font-bold">
              <th scope="row" className="px-4 py-2.5 text-left">
                Total · {report.days.length} day{report.days.length === 1 ? '' : 's'}
              </th>
              <FigureCells figures={report.total} />
            </tr>
          </tfoot>
        </table>
      </div>
    </div>
  );
}

/** The eight money columns for one row. "—" for a share or profit with nothing costed. */
export function FigureCells({ figures }: { figures: ProfitFigures }) {
  const costed = figures.revenue - figures.notCostedRevenue > 0 || figures.consultantShare > 0 || figures.runningCost > 0;
  const percent = profitPercent(figures);
  const cell = 'whitespace-nowrap px-3 py-2 text-right tabular-nums';
  return (
    <>
      <td className={cell}>{figures.lines}</td>
      <td className={cell}>{formatCurrency(figures.charge)}</td>
      <td className={cell}>{figures.discount > 0 ? `−${formatCurrency(figures.discount)}` : '—'}</td>
      <td className={cn(cell, 'font-semibold text-foreground')}>
        {formatCurrency(figures.revenue)}
        {figures.notCostedRevenue > 0 && (
          <span className="block text-[11px] font-normal text-muted-foreground" title="Revenue from lines with no cost set yet — not in the shares or profit">
            {formatCurrency(figures.notCostedRevenue)} not costed
          </span>
        )}
      </td>
      <td className={cell}>{costed && figures.consultantShare > 0 ? formatCurrency(figures.consultantShare) : '—'}</td>
      <td className={cell}>{costed && figures.runningCost > 0 ? formatCurrency(figures.runningCost) : '—'}</td>
      <td className={cn(cell, 'font-semibold', !costed ? 'text-muted-foreground' : figures.hospitalProfit < 0 ? 'text-destructive' : 'text-success')}>
        {costed ? formatCurrency(figures.hospitalProfit) : '—'}
      </td>
      <td className={cn(cell, percent !== null && percent < 0 && 'text-destructive')}>{percent === null ? '—' : `${percent}%`}</td>
    </>
  );
}
