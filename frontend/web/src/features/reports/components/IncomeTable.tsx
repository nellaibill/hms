import { Link } from 'react-router-dom';
import { formatCurrency, PaymentStatusBadge } from '@/features/billing';
import type { IncomeReportRow } from '../types';
import { PatientNameLink } from '@/components/PatientNameLink';

interface IncomeTableProps {
  rows: IncomeReportRow[];
}

export function IncomeTable({ rows }: IncomeTableProps) {
  return (
    <div className="overflow-x-auto rounded-lg border border-border">
      <table className="w-full min-w-[720px] text-sm">
        <thead className="bg-muted/50 text-left text-xs font-medium uppercase tracking-wide text-muted-foreground">
          <tr>
            <th className="px-4 py-2.5">Date</th>
            <th className="px-4 py-2.5">Invoice</th>
            <th className="px-4 py-2.5">Patient</th>
            <th className="px-4 py-2.5">Billing Type(s)</th>
            <th className="px-4 py-2.5 text-right">Amount</th>
            <th className="px-4 py-2.5">Status</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {rows.map((row) => (
            <tr key={row.id} className="hover:bg-muted/30">
              <td className="px-4 py-3 font-mono text-xs text-muted-foreground">{new Date(row.date).toLocaleDateString('en-IN')}</td>
              <td className="px-4 py-3">
                <Link to={`/finance/accounts/${row.id}`} className="font-mono text-xs text-primary hover:underline">
                  {row.invoiceNumber ?? row.id}
                </Link>
              </td>
              <td className="px-4 py-3">
                <PatientNameLink patientId={row.patientId}>{row.patientName}</PatientNameLink>
                <div className="text-xs text-muted-foreground">{row.patientUhid}</div>
              </td>
              <td className="px-4 py-3 text-muted-foreground">{row.billingTypes}</td>
              <td className="px-4 py-3 text-right font-medium text-foreground">{formatCurrency(row.amount)}</td>
              <td className="px-4 py-3">
                <PaymentStatusBadge status={row.paymentStatus} />
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
