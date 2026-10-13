import type { StockTransactionResponse } from '@hms/shared';
import { Link } from 'react-router-dom';
import { Badge } from '@/components/ui/badge';
import { PatientNameLink } from '@/components/PatientNameLink';
import { dateTimeLabel, quantity } from '@/features/pharmacy/common/format';
import { isInward, sourceDocumentPath, TRANSACTION_TYPE_LABELS } from '@/features/pharmacy/common/labels';

interface StockLedgerTableProps {
  transactions: StockTransactionResponse[];
}

/** One row per stock movement: which way it went, by how much, and the document behind it. */
export function StockLedgerTable({ transactions }: StockLedgerTableProps) {
  return (
    <div className="overflow-x-auto rounded-lg border border-border bg-background">
      <table className="w-full text-sm">
        <thead className="bg-sidebar-active text-left text-[11px] font-medium uppercase tracking-wide text-sidebar-active-foreground">
          <tr>
            <th className="px-3 py-2">Date</th>
            <th className="px-3 py-2">Type</th>
            <th className="px-3 py-2">Document</th>
            <th className="px-3 py-2">Product</th>
            <th className="px-3 py-2">Batch</th>
            <th className="px-3 py-2 text-right">In</th>
            <th className="px-3 py-2 text-right">Out</th>
            <th className="px-3 py-2 text-right">Balance</th>
            <th className="px-3 py-2">Patient / remarks</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {transactions.map((txn) => {
            const inward = isInward(txn.transactionType);
            const documentPath = sourceDocumentPath(txn.sourceType, txn.sourceId);
            return (
              <tr key={txn.id} className="hover:bg-muted/30">
                <td className="whitespace-nowrap px-3 py-2 text-foreground">{dateTimeLabel(txn.transactionDate)}</td>
                <td className="px-3 py-2">
                  <Badge variant={inward ? 'success' : 'secondary'}>{TRANSACTION_TYPE_LABELS[txn.transactionType] ?? txn.transactionType}</Badge>
                </td>
                <td className="px-3 py-2 font-mono text-xs">
                  {txn.sourceNo ? (
                    documentPath ? (
                      <Link to={documentPath} className="font-medium text-primary hover:underline">
                        {txn.sourceNo}
                      </Link>
                    ) : (
                      txn.sourceNo
                    )
                  ) : (
                    <span className="text-muted-foreground">—</span>
                  )}
                </td>
                <td className="px-3 py-2 font-medium text-foreground">{txn.productName}</td>
                <td className="px-3 py-2 font-mono text-xs text-muted-foreground">{txn.batchNo}</td>
                <td className="px-3 py-2 text-right tabular-nums text-success">{inward ? quantity(txn.quantity) : ''}</td>
                <td className="px-3 py-2 text-right tabular-nums text-foreground">{inward ? '' : quantity(txn.quantity)}</td>
                <td className="px-3 py-2 text-right font-medium tabular-nums text-foreground">{quantity(txn.balanceAfter)}</td>
                <td className="px-3 py-2 text-sm text-muted-foreground">
                  {txn.patientName && txn.patientId ? (
                    <PatientNameLink patientId={txn.patientId} className="font-normal">
                      {txn.patientName}
                    </PatientNameLink>
                  ) : (
                    txn.patientName
                  )}
                  {txn.patientName && txn.remarks ? ' · ' : ''}
                  {txn.remarks}
                  {!txn.patientName && !txn.remarks ? '—' : ''}
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}
