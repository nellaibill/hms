import type { DispenseResponse } from '@hms/shared';

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
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
