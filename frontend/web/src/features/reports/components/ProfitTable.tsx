import { Link } from 'react-router-dom';
import { formatCurrency } from '@/features/billing';
import { resolveRecordLabel } from '@/features/masters';
import type { ProfitReportRow } from '../profitReport';

interface ProfitTableProps {
  rows: ProfitReportRow[];
}

/** "—" for cost/profit/margin means unknown (Pharmacy, a Laboratory package, or a service
 * never costed) — never rendered as ₹0 or 0%, which would misreport it as confirmed free. */
function formatMaybeCurrency(value: number | null): string {
  return value === null ? '—' : formatCurrency(value);
}

export function ProfitTable({ rows }: ProfitTableProps) {
  return (
    <div className="overflow-x-auto rounded-lg border border-border">
      <table className="w-full min-w-[1100px] text-sm">
        <thead className="bg-muted/50 text-left text-xs font-medium uppercase tracking-wide text-muted-foreground">
          <tr>
            <th className="px-4 py-2.5">Date</th>
            <th className="px-4 py-2.5">Invoice</th>
            <th className="px-4 py-2.5">Patient</th>
            <th className="px-4 py-2.5">Type</th>
            <th className="px-4 py-2.5">Service</th>
            <th className="px-4 py-2.5">Department</th>
            <th className="px-4 py-2.5">Consultant</th>
            <th className="px-4 py-2.5 text-right">Qty</th>
            <th className="px-4 py-2.5 text-right">Revenue</th>
            <th className="px-4 py-2.5 text-right">Cost</th>
            <th className="px-4 py-2.5 text-right">Profit</th>
            <th className="px-4 py-2.5 text-right">Margin</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {rows.map((row) => (
            <tr key={row.id} className="hover:bg-muted/30">
              <td className="px-4 py-3 font-mono text-xs text-muted-foreground">{new Date(row.date).toLocaleDateString('en-IN')}</td>
              <td className="px-4 py-3">
                <Link to={`/finance/accounts/${row.invoiceId}`} className="font-mono text-xs text-primary hover:underline">
                  {row.invoiceNumber ?? row.invoiceId}
                </Link>
              </td>
              <td className="px-4 py-3 text-foreground">{row.patientName}</td>
              <td className="px-4 py-3 text-muted-foreground">{row.billingType}</td>
              <td className="px-4 py-3 text-muted-foreground">{row.serviceLabel}</td>
              <td className="px-4 py-3 text-muted-foreground">{resolveRecordLabel('department', row.departmentId)}</td>
              <td className="px-4 py-3 text-muted-foreground">{resolveRecordLabel('consultant', row.billedConsultantId)}</td>
              <td className="px-4 py-3 text-right text-muted-foreground">{row.quantity}</td>
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
