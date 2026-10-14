import { cn } from '@/lib/utils';
import { columnTotal, formatValue, isNumeric, type ReportColumn } from './columns';

interface ReportTableProps<T> {
  columns: ReportColumn<T>[];
  rows: T[];
  rowKey: (row: T, index: number) => string;
  emptyText: string;
  /** Tints a row — e.g. expired stock. */
  rowClassName?: (row: T) => string | undefined;
}

/** A report's table: numbers right-aligned, a totals row at the foot for the columns that ask for one. */
export function ReportTable<T>({ columns, rows, rowKey, emptyText, rowClassName }: ReportTableProps<T>) {
  if (rows.length === 0) {
    return <p className="rounded-lg border border-dashed border-border bg-background p-8 text-center text-sm text-muted-foreground">{emptyText}</p>;
  }

  const hasTotals = columns.some((column) => column.total);

  return (
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
          {rows.map((row, index) => (
            <tr key={rowKey(row, index)} className={rowClassName?.(row)}>
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
                  {column.total ? formatValue(columnTotal(column, rows), column.kind) : index === 0 ? 'Total' : ''}
                </td>
              ))}
            </tr>
          </tfoot>
        )}
      </table>
    </div>
  );
}
