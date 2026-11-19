import { Link } from 'react-router-dom';
import { formatRupees, isLowProfit } from '@/features/diagnostics/tariff';
import { cn } from '@/lib/utils';
import { groupBySection, type PriceRow } from '../priceListRows';

export type PriceTableVariant = 'lab' | 'outsourced' | 'packages' | 'radiology' | 'master';

interface PriceListTableProps {
  rows: PriceRow[];
  variant: PriceTableVariant;
  /** Group rows under their section headings (lab tests, radiology). */
  grouped?: boolean;
  emptyText: string;
}

const RELATED_SHOWN = 3;

/** One Price List tab's table (ADR-140). Columns follow the kind of service, like the tariff sheet. */
export function PriceListTable({ rows, variant, grouped = false, emptyText }: PriceListTableProps) {
  if (rows.length === 0) {
    return <p className="px-4 py-10 text-center text-sm text-muted-foreground">{emptyText}</p>;
  }

  const columns = columnsFor(variant);
  const groups = grouped ? groupBySection(rows) : [{ section: '', rows }];

  return (
    <div className="overflow-x-auto">
      <table className="w-full min-w-[900px] border-collapse text-sm">
        <thead>
          <tr className="border-b border-border bg-muted/50 text-left text-xs font-semibold text-muted-foreground">
            {columns.map((column) => (
              <th key={column.key} scope="col" className={cn('whitespace-nowrap px-4 py-2.5', column.numeric && 'text-right')}>
                {column.label}
              </th>
            ))}
            <th scope="col" className="px-4 py-2.5">
              <span className="sr-only">Edit</span>
            </th>
          </tr>
        </thead>
        <tbody>
          {groups.map((group) => [
            grouped ? (
              <tr key={`section-${group.section}`} className="bg-amber-50 dark:bg-amber-950/30">
                <th scope="colgroup" colSpan={columns.length + 1} className="px-4 py-2 text-left text-xs font-bold uppercase tracking-wide text-amber-900 dark:text-amber-200">
                  {group.section} · {group.rows.length}
                </th>
              </tr>
            ) : null,
            ...group.rows.map((row) => <PriceListRow key={row.id} row={row} variant={variant} />),
          ])}
        </tbody>
      </table>
    </div>
  );
}

interface Column {
  key: string;
  label: string;
  numeric?: boolean;
}

function columnsFor(variant: PriceTableVariant): Column[] {
  const money: Column[] = [
    { key: 'price', label: 'Price ₹', numeric: true },
    { key: 'profit', label: 'Profit ₹', numeric: true },
    { key: 'percent', label: 'Profit %' },
  ];
  switch (variant) {
    case 'lab':
      return [{ key: 'name', label: 'Test' }, { key: 'related', label: 'In packages' }, { key: 'cost', label: 'Running cost ₹', numeric: true }, ...money];
    case 'outsourced':
      return [{ key: 'name', label: 'Test' }, { key: 'lab', label: 'Outside lab' }, { key: 'cost', label: 'Lab charge ₹', numeric: true }, ...money];
    case 'packages':
      return [{ key: 'name', label: 'Package' }, { key: 'related', label: 'Tests' }, { key: 'cost', label: 'Running cost ₹', numeric: true }, ...money];
    case 'radiology':
      return [
        { key: 'name', label: 'Service' },
        { key: 'doctor', label: "Doctor's share ₹", numeric: true },
        { key: 'cost', label: 'Running cost ₹', numeric: true },
        ...money,
        { key: 'tags', label: 'Other rates' },
      ];
    case 'master':
      return [{ key: 'name', label: 'Name' }, { key: 'cost', label: 'Cost ₹', numeric: true }, ...money];
  }
}

function PriceListRow({ row, variant }: { row: PriceRow; variant: PriceTableVariant }) {
  const { breakdown } = row;
  const low = isLowProfit(breakdown);
  const loss = breakdown.isCosted && breakdown.profit < 0;
  const numberCell = 'whitespace-nowrap px-4 py-2.5 text-right tabular-nums';

  return (
    <tr className="border-b border-border last:border-0 hover:bg-muted/30">
      <th scope="row" className="px-4 py-2.5 text-left font-medium">
        {row.name}
      </th>

      {(variant === 'lab' || variant === 'packages') && (
        <td className="px-4 py-2.5">
          <RelatedChips names={row.related ?? []} emptyText={variant === 'lab' ? 'Billed alone' : 'No tests'} />
        </td>
      )}
      {variant === 'outsourced' && <td className="px-4 py-2.5">{row.outsideLab ?? <span className="text-muted-foreground">—</span>}</td>}
      {variant === 'radiology' && <td className={numberCell}>{formatRupees(breakdown.doctorShare)}</td>}

      <td className={numberCell}>
        {breakdown.isCosted ? formatRupees(breakdown.runningCost) : <span className="text-muted-foreground">Not costed</span>}
        {variant === 'packages' && breakdown.isCosted && (
          <span className="ml-1.5 text-xs text-muted-foreground">{row.costSource === 'own' ? '(own)' : '(tests)'}</span>
        )}
      </td>
      <td className={numberCell}>{formatRupees(breakdown.price)}</td>
      <td className={cn(numberCell, loss && 'text-destructive')}>{breakdown.isCosted ? formatRupees(breakdown.profit) : '—'}</td>
      <td className="whitespace-nowrap px-4 py-2.5">
        {breakdown.isCosted && breakdown.profitPercent !== null ? (
          <span className="inline-flex items-center gap-2">
            <span className={cn('w-10 tabular-nums', loss && 'text-destructive')}>{breakdown.profitPercent}%</span>
            <span aria-hidden="true" className="inline-block h-1.5 w-14 overflow-hidden rounded-full bg-blue-100 dark:bg-blue-950">
              <span className="block h-1.5 rounded-full bg-blue-700" style={{ width: `${Math.max(0, Math.min(100, breakdown.profitPercent))}%` }} />
            </span>
            {low && (
              <span className="rounded-full bg-amber-100 px-2 py-0.5 text-[11px] font-semibold text-amber-900 dark:bg-amber-950 dark:text-amber-200">
                {loss ? 'Loss' : 'Low'}
              </span>
            )}
          </span>
        ) : (
          <span className="text-muted-foreground">—</span>
        )}
      </td>

      {variant === 'radiology' && (
        <td className="px-4 py-2.5">
          {row.tags && row.tags.length > 0 ? <RelatedChips names={row.tags} emptyText="" /> : <span className="text-muted-foreground">—</span>}
        </td>
      )}

      <td className="px-4 py-2.5 text-right">
        {row.editPath && (
          <Link to={row.editPath} className="font-medium text-primary hover:underline" aria-label={`Edit ${row.name}`}>
            Edit
          </Link>
        )}
      </td>
    </tr>
  );
}

function RelatedChips({ names, emptyText }: { names: string[]; emptyText: string }) {
  if (names.length === 0) return <span className="text-xs text-muted-foreground">{emptyText}</span>;
  const shown = names.slice(0, RELATED_SHOWN);
  const more = names.length - shown.length;
  return (
    <span className="flex flex-wrap gap-1" title={names.join(', ')}>
      {shown.map((name) => (
        <span key={name} className="whitespace-nowrap rounded-md bg-muted px-2 py-0.5 text-xs text-foreground">
          {name}
        </span>
      ))}
      {more > 0 && <span className="whitespace-nowrap rounded-md bg-muted px-2 py-0.5 text-xs text-muted-foreground">+{more}</span>}
    </span>
  );
}
