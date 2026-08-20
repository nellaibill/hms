import type { StockTransactionResponse } from '@hms/shared';
import { Badge } from '@/components/ui/badge';

interface StockLedgerTableProps {
  transactions: StockTransactionResponse[];
}

export function StockLedgerTable({ transactions }: StockLedgerTableProps) {
  return (
    <div className="overflow-hidden rounded-lg border border-border">
      <table className="w-full text-sm">
        <thead className="bg-muted/50 text-left text-xs font-medium uppercase tracking-wide text-muted-foreground">
          <tr>
            <th className="px-4 py-2.5">Type</th>
            <th className="px-4 py-2.5">Product</th>
            <th className="px-4 py-2.5">Batch</th>
            <th className="px-4 py-2.5">Patient</th>
            <th className="px-4 py-2.5">Quantity</th>
            <th className="px-4 py-2.5">Balance After</th>
            <th className="px-4 py-2.5">Date</th>
            <th className="px-4 py-2.5">Remarks</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {transactions.map((txn) => (
            <tr key={txn.id} className="hover:bg-muted/30">
              <td className="px-4 py-3">
                <Badge variant={txn.transactionType === 'Receipt' ? 'success' : 'secondary'}>{txn.transactionType}</Badge>
              </td>
              <td className="px-4 py-3 font-medium text-foreground">{txn.productName}</td>
              <td className="px-4 py-3 font-mono text-xs text-muted-foreground">{txn.batchNo}</td>
              <td className="px-4 py-3 text-sm text-foreground">{txn.patientName || '—'}</td>
              <td className="px-4 py-3 tabular-nums text-foreground">{txn.quantity}</td>
              <td className="px-4 py-3 tabular-nums text-foreground">{txn.balanceAfter}</td>
              <td className="px-4 py-3 text-sm text-foreground">{new Date(txn.transactionDate).toLocaleString('en-IN')}</td>
              <td className="px-4 py-3 text-sm text-muted-foreground">{txn.remarks || '—'}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
