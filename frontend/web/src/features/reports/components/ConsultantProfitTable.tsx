import { formatCurrency } from '@/features/billing';
import { resolveRecordLabel } from '@/features/masters';
import type { ConsultantProfitRow } from '../profitReport';

interface ConsultantProfitTableProps {
  rows: ConsultantProfitRow[];
}

function formatMaybeCurrency(value: number | null): string {
  return value === null ? '—' : formatCurrency(value);
}

/** One row per consultant, resolved via resolveRecordLabel('consultant', id) the same way
 * every other Masters-referencing report column does — rows come pre-sorted by revenue
 * descending (getProfitByConsultant). */
export function ConsultantProfitTable({ rows }: ConsultantProfitTableProps) {
  return (
    <div className="overflow-x-auto rounded-lg border border-border">
      <table className="w-full min-w-[640px] text-sm">
        <thead className="bg-muted/50 text-left text-xs font-medium uppercase tracking-wide text-muted-foreground">
          <tr>
            <th className="px-4 py-2.5">Consultant</th>
            <th className="px-4 py-2.5 text-right">Consultations</th>
            <th className="px-4 py-2.5 text-right">Revenue</th>
            <th className="px-4 py-2.5 text-right">Cost</th>
            <th className="px-4 py-2.5 text-right">Profit</th>
            <th className="px-4 py-2.5 text-right">Margin</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {rows.map((row) => (
            <tr key={row.consultantId} className="hover:bg-muted/30">
              <td className="px-4 py-3 font-medium text-foreground">{resolveRecordLabel('consultant', row.consultantId)}</td>
              <td className="px-4 py-3 text-right text-muted-foreground">{row.itemCount}</td>
              <td className="px-4 py-3 text-right font-medium text-foreground">{formatCurrency(row.revenue)}</td>
              <td className="px-4 py-3 text-right text-muted-foreground">{formatMaybeCurrency(row.cost)}</td>
              <td className={`px-4 py-3 text-right font-medium ${row.profit === null ? 'text-muted-foreground' : row.profit < 0 ? 'text-destructive' : 'text-success'}`}>
                {formatMaybeCurrency(row.profit)}
              </td>
              <td className={`px-4 py-3 text-right ${row.marginPercent === null ? 'text-muted-foreground' : row.marginPercent < 0 ? 'text-destructive' : 'text-success'}`}>
                {row.marginPercent === null ? '—' : `${row.marginPercent}%`}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
