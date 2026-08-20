import type { StockReceiptResponse } from '@hms/shared';

interface StockReceiptTableProps {
  receipts: StockReceiptResponse[];
}

export function StockReceiptTable({ receipts }: StockReceiptTableProps) {
  return (
    <div className="overflow-hidden rounded-lg border border-border">
      <table className="w-full text-sm">
        <thead className="bg-muted/50 text-left text-xs font-medium uppercase tracking-wide text-muted-foreground">
          <tr>
            <th className="px-4 py-2.5">Product</th>
            <th className="px-4 py-2.5">Batch</th>
            <th className="px-4 py-2.5">Quantity</th>
            <th className="px-4 py-2.5">Balance After</th>
            <th className="px-4 py-2.5">Date</th>
            <th className="px-4 py-2.5">Remarks</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {receipts.map((receipt) => (
            <tr key={receipt.id} className="hover:bg-muted/30">
              <td className="px-4 py-3 font-medium text-foreground">{receipt.productName}</td>
              <td className="px-4 py-3 font-mono text-xs text-muted-foreground">{receipt.batchNo}</td>
              <td className="px-4 py-3 tabular-nums text-foreground">{receipt.quantity}</td>
              <td className="px-4 py-3 tabular-nums text-foreground">{receipt.balanceAfter}</td>
              <td className="px-4 py-3 text-sm text-foreground">{new Date(receipt.transactionDate).toLocaleString('en-IN')}</td>
              <td className="px-4 py-3 text-sm text-muted-foreground">{receipt.remarks || '—'}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
