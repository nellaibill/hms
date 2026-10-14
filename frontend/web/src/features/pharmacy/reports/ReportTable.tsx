import { useEffect, useState } from 'react';
import { flushSync } from 'react-dom';
import { Pagination } from '@/components/Pagination';
import { cn } from '@/lib/utils';
import { columnTotal, formatValue, isNumeric, type ReportColumn } from './columns';

/** Rows drawn at a time. A month's sales register at a busy pharmacy runs to thousands of
 * bills; drawing them all at once makes the screen crawl. */
const PAGE_SIZE = 100;

interface ReportTableProps<T> {
  columns: ReportColumn<T>[];
  rows: T[];
  rowKey: (row: T, index: number) => string;
  emptyText: string;
  /** Tints a row — e.g. expired stock. */
  rowClassName?: (row: T) => string | undefined;
}

/**
 * A report's table: numbers right-aligned, a totals row at the foot for the columns that ask
 * for one. Long reports are shown a page at a time; the totals, the Excel download and the
 * printed copy always cover every row.
 */
export function ReportTable<T>({ columns, rows, rowKey, emptyText, rowClassName }: ReportTableProps<T>) {
  const [page, setPage] = useState(1);
  const [printing, setPrinting] = useState(false);

  // Paper gets every row, not just the page on screen. beforeprint fires before the browser
  // lays the page out for printing, so the full table is rendered synchronously first.
  useEffect(() => {
    const showAll = () => flushSync(() => setPrinting(true));
    const showPage = () => setPrinting(false);
    window.addEventListener('beforeprint', showAll);
    window.addEventListener('afterprint', showPage);
    return () => {
      window.removeEventListener('beforeprint', showAll);
      window.removeEventListener('afterprint', showPage);
    };
  }, []);

  if (rows.length === 0) {
    return <p className="rounded-lg border border-dashed border-border bg-background p-8 text-center text-sm text-muted-foreground">{emptyText}</p>;
  }

  const hasTotals = columns.some((column) => column.total);
  const totalPages = Math.max(1, Math.ceil(rows.length / PAGE_SIZE));
  const current = Math.min(page, totalPages);
  const visible = printing ? rows : rows.slice((current - 1) * PAGE_SIZE, current * PAGE_SIZE);
  const firstIndex = printing ? 0 : (current - 1) * PAGE_SIZE;

  return (
    <div className="flex flex-col gap-2">
      <div className="overflow-x-auto rounded-lg border border-border bg-background print:overflow-visible print:border-0">
        <table className="w-full text-sm">
          <thead className="bg-sidebar-active text-left text-[11px] font-medium uppercase tracking-wide text-sidebar-active-foreground print:bg-transparent print:text-black">
            <tr>
              {columns.map((column) => (
                <th key={column.header} className={cn('whitespace-nowrap px-3 py-2', isNumeric(column.kind) && 'text-right')}>
                  {column.header}
                </th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-border">
            {visible.map((row, index) => (
              <tr key={rowKey(row, firstIndex + index)} className={rowClassName?.(row)}>
                {columns.map((column) => (
                  <td
                    key={column.header}
                    className={cn('px-3 py-1.5', isNumeric(column.kind) && 'text-right tabular-nums', column.mono && 'font-mono text-xs', (column.nowrap || isNumeric(column.kind)) && 'whitespace-nowrap')}
                  >
                    {column.render ? column.render(row) : formatValue(column.value(row), column.kind)}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
          {hasTotals && (
            <tfoot className="border-t-2 border-border bg-muted/40 font-semibold text-foreground print:bg-transparent">
              <tr>
                {columns.map((column, index) => (
                  <td key={column.header} className={cn('whitespace-nowrap px-3 py-2', isNumeric(column.kind) && 'text-right tabular-nums')}>
                    {column.total ? formatValue(columnTotal(column, rows), column.kind) : index === 0 ? (totalPages > 1 ? `Total, all ${rows.length}` : 'Total') : ''}
                  </td>
                ))}
              </tr>
            </tfoot>
          )}
        </table>
      </div>
      <div className="print:hidden">
        <Pagination meta={{ page: current, pageSize: PAGE_SIZE, totalCount: rows.length, totalPages }} onPageChange={setPage} />
      </div>
    </div>
  );
}
