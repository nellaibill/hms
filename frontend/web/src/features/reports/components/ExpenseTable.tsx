import { formatCurrency } from '@/features/billing';
import type { ExpenseReportRow } from '../types';

interface ExpenseTableProps {
  rows: ExpenseReportRow[];
}

export function ExpenseTable({ rows }: ExpenseTableProps) {
  return (
    <div className="overflow-x-auto rounded-lg border border-border">
      <table className="w-full min-w-[560px] text-sm">
        <thead className="bg-muted/50 text-left text-xs font-medium uppercase tracking-wide text-muted-foreground">
          <tr>
            <th className="px-4 py-2.5">Date</th>
            <th className="px-4 py-2.5">Category</th>
            <th className="px-4 py-2.5">Description</th>
            <th className="px-4 py-2.5 text-right">Amount</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {rows.map((row) => (
            <tr key={row.id} className="hover:bg-muted/30">
              <td className="px-4 py-3 font-mono text-xs text-muted-foreground">{new Date(row.date).toLocaleDateString('en-IN')}</td>
              <td className="px-4 py-3 font-medium text-foreground">{row.category}</td>
              <td className="px-4 py-3 text-muted-foreground">{row.description}</td>
              <td className="px-4 py-3 text-right font-medium text-foreground">{formatCurrency(row.amount)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
