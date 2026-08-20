import type { DispenseResponse } from '@hms/shared';
import { Link } from 'react-router-dom';
import { Badge } from '@/components/ui/badge';

interface DispenseTableProps {
  dispenses: DispenseResponse[];
}

export function DispenseTable({ dispenses }: DispenseTableProps) {
  return (
    <div className="overflow-hidden rounded-lg border border-border">
      <table className="w-full text-sm">
        <thead className="bg-muted/50 text-left text-xs font-medium uppercase tracking-wide text-muted-foreground">
          <tr>
            <th className="px-4 py-2.5">Product</th>
            <th className="px-4 py-2.5">Batch</th>
            <th className="px-4 py-2.5">Patient</th>
            <th className="px-4 py-2.5">Quantity</th>
            <th className="px-4 py-2.5">Balance After</th>
            <th className="px-4 py-2.5">Date</th>
            <th className="px-4 py-2.5">Billing</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {dispenses.map((dispense) => (
            <tr key={dispense.id} className="hover:bg-muted/30">
              <td className="px-4 py-3 font-medium text-foreground">{dispense.productName}</td>
              <td className="px-4 py-3 font-mono text-xs text-muted-foreground">{dispense.batchNo}</td>
              <td className="px-4 py-3 text-sm text-foreground">{dispense.patientName}</td>
              <td className="px-4 py-3 tabular-nums text-foreground">{dispense.quantity}</td>
              <td className="px-4 py-3 tabular-nums text-foreground">{dispense.balanceAfter}</td>
              <td className="px-4 py-3 text-sm text-foreground">{new Date(dispense.transactionDate).toLocaleString('en-IN')}</td>
              <td className="px-4 py-3">
                {dispense.invoiceId ? (
                  <Link to={`/finance/accounts/${dispense.invoiceId}`} className="text-sm font-medium text-primary underline-offset-2 hover:underline">
                    View invoice
                  </Link>
                ) : (
                  <Badge variant="warning">Not billed</Badge>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
