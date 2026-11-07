import { Fragment, type ReactNode } from 'react';
import { formatCurrency } from '@/features/billing';
import { resolveRecordLabel } from '@/features/masters';
import { formatReportDate } from '@/lib/reportDateRange';
import { cn } from '@/lib/utils';
import {
  COLLECTION_HEADS,
  type CollectionHead,
  type CollectionTotals,
  type ConsultantCollectionRow,
  type DailyCollectionRow,
  type HeadCollectionSection,
} from '../collectionReport';

export interface CollectionSummaryData {
  days: DailyCollectionRow[];
  dayTotals: Omit<DailyCollectionRow, 'date'>;
  consultants: ConsultantCollectionRow[];
  heads: HeadCollectionSection[];
  grandTotal: CollectionTotals;
}

interface CollectionSummarySectionsProps {
  data: CollectionSummaryData;
  variant: 'screen' | 'print';
  /** One day picked: the day-wise table drops its Date column (ADR-126). */
  singleDay?: boolean;
  /** One consultant picked: the consultant summary is left out and each head shows one line,
   * since every row would name the same consultant (ADR-126). */
  singleConsultant?: boolean;
}

const DAY_HEADS: CollectionHead[] = [...COLLECTION_HEADS, 'Other'];

const STYLES = {
  screen: {
    title: 'text-sm font-semibold text-foreground',
    frame: 'overflow-x-auto rounded-lg border border-border',
    table: 'w-full min-w-[640px] text-sm',
    headRow: 'bg-sidebar-active text-left text-xs font-medium uppercase tracking-wide text-sidebar-active-foreground',
    th: 'px-4 py-2.5',
    td: 'px-4 py-2.5',
    body: 'divide-y divide-border',
    groupRow: 'bg-muted/40 font-semibold text-foreground',
    subtotalRow: 'font-semibold text-foreground',
    totalRow: 'border-t-2 border-border bg-muted/60 font-bold text-foreground',
    muted: 'text-muted-foreground',
  },
  print: {
    title: 'text-sm font-bold',
    frame: '',
    table: 'w-full border-collapse text-[11px]',
    headRow: 'border-y-2 border-black text-left',
    th: 'py-1 pr-2 font-semibold',
    td: 'py-1 pr-2',
    body: '',
    groupRow: 'border-b border-gray-400 font-semibold',
    subtotalRow: 'border-b border-gray-400 font-semibold',
    totalRow: 'border-y-2 border-black font-bold',
    muted: '',
  },
} as const;

function consultantName(consultantId: string | undefined): string {
  return consultantId ? resolveRecordLabel('consultant', consultantId) : 'No consultant';
}

/**
 * The Collection Summary's three sections (ADR-125), drawn once for the screen and once for the
 * printout: money taken per day by head, per consultant across every head, and per head with
 * its consultants, ending in the grand total of every head together.
 */
export function CollectionSummarySections({ data, variant, singleDay = false, singleConsultant = false }: CollectionSummarySectionsProps) {
  const s = STYLES[variant];
  const right = 'text-right whitespace-nowrap';
  const amountCells = (totals: CollectionTotals, className?: string) => (
    <>
      <td className={cn(s.td, right, className)}>{formatCurrency(totals.consultantAmount)}</td>
      <td className={cn(s.td, right, className)}>{formatCurrency(totals.hospitalAmount)}</td>
      <td className={cn(s.td, right, 'font-semibold', className)}>{formatCurrency(totals.collected)}</td>
    </>
  );
  const amountHeaders = (
    <>
      <th className={cn(s.th, 'text-right')}>Consultant Amount</th>
      <th className={cn(s.th, 'text-right')}>Hospital Amount</th>
      <th className={cn(s.th, 'text-right')}>Total</th>
    </>
  );
  const section = (title: string, table: ReactNode) => (
    <section className="flex flex-col gap-2 break-inside-avoid-page">
      <h3 className={s.title}>{title}</h3>
      <div className={s.frame}>{table}</div>
    </section>
  );

  return (
    <div className={cn('flex flex-col', variant === 'screen' ? 'gap-5' : 'mt-4 gap-4')}>
      {section(
        'Day-wise collection',
        <table className={s.table}>
          <thead>
            <tr className={s.headRow}>
              {!singleDay && <th className={s.th}>Date</th>}
              {DAY_HEADS.map((head) => (
                <th key={head} className={cn(s.th, 'text-right')}>
                  {head === 'Laboratory' ? 'Lab' : head}
                </th>
              ))}
              <th className={cn(s.th, 'text-right')}>Total</th>
            </tr>
          </thead>
          <tbody className={s.body}>
            {data.days.map((day) => (
              <tr key={day.date}>
                {!singleDay && <td className={cn(s.td, 'whitespace-nowrap')}>{formatReportDate(day.date)}</td>}
                {DAY_HEADS.map((head) => (
                  <td key={head} className={cn(s.td, right, day.byHead[head] === 0 && s.muted)}>
                    {day.byHead[head] === 0 ? '—' : formatCurrency(day.byHead[head])}
                  </td>
                ))}
                <td className={cn(s.td, right, 'font-semibold')}>{formatCurrency(day.total)}</td>
              </tr>
            ))}
          </tbody>
          {!singleDay && (
            <tfoot>
              <tr className={s.totalRow}>
                <td className={s.td}>Total</td>
                {DAY_HEADS.map((head) => (
                  <td key={head} className={cn(s.td, right)}>
                    {formatCurrency(data.dayTotals.byHead[head])}
                  </td>
                ))}
                <td className={cn(s.td, right)}>{formatCurrency(data.dayTotals.total)}</td>
              </tr>
            </tfoot>
          )}
        </table>,
      )}

      {!singleConsultant &&
        section(
          'Consultant summary (all heads)',
          <table className={s.table}>
            <thead>
              <tr className={s.headRow}>
                <th className={s.th}>Consultant</th>
                {amountHeaders}
              </tr>
            </thead>
            <tbody className={s.body}>
              {data.consultants.map((row) => (
                <tr key={row.consultantId ?? 'none'}>
                  <td className={s.td}>{consultantName(row.consultantId)}</td>
                  {amountCells(row)}
                </tr>
              ))}
            </tbody>
            <tfoot>
              <tr className={s.totalRow}>
                <td className={s.td}>All consultants</td>
                {amountCells(data.grandTotal)}
              </tr>
            </tfoot>
          </table>,
        )}

      {section(
        'Head-wise summary',
        <table className={s.table}>
          <thead>
            <tr className={s.headRow}>
              <th className={s.th}>{singleConsultant ? 'Head' : 'Head / Consultant'}</th>
              {amountHeaders}
            </tr>
          </thead>
          <tbody className={s.body}>
            {data.heads.map((head) =>
              singleConsultant ? (
                <tr key={head.billingType}>
                  <td className={s.td}>{head.billingType}</td>
                  {amountCells(head.subtotal)}
                </tr>
              ) : (
                <Fragment key={head.billingType}>
                  <tr className={s.groupRow}>
                    <td className={s.td} colSpan={4}>
                      {head.billingType}
                    </td>
                  </tr>
                  {head.consultants.map((row) => (
                    <tr key={row.consultantId ?? 'none'}>
                      <td className={cn(s.td, 'pl-8')}>{consultantName(row.consultantId)}</td>
                      {amountCells(row)}
                    </tr>
                  ))}
                  <tr className={s.subtotalRow}>
                    <td className={cn(s.td, 'pl-8')}>{head.billingType} subtotal</td>
                    {amountCells(head.subtotal)}
                  </tr>
                </Fragment>
              ),
            )}
          </tbody>
          <tfoot>
            <tr className={s.totalRow}>
              <td className={s.td}>Grand total (all heads)</td>
              {amountCells(data.grandTotal)}
            </tr>
          </tfoot>
        </table>,
      )}

      {data.grandTotal.collectedWithUnknownCost > 0 && (
        <p className={cn('text-xs', variant === 'screen' ? 'text-muted-foreground' : 'text-gray-600')}>
          {formatCurrency(data.grandTotal.collectedWithUnknownCost)} collected has no cost data yet (a service not yet costed, a package, or a
          consultant with no charge set), so it is in Total but not split into consultant and hospital amounts.
        </p>
      )}
    </div>
  );
}
