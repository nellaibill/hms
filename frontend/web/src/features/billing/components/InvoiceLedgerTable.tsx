import { ArrowDown, ArrowUp } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { formatCurrency, getOverallPaymentStatus } from '../billingCalculations';
import type { Billing } from '../types';
import { PaymentStatusBadge } from './PaymentStatusBadge';

interface InvoiceLedgerTableProps {
  billings: Billing[];
  sort: string;
  onSortChange: (sort: string) => void;
}

export function InvoiceLedgerTable({ billings, sort, onSortChange }: InvoiceLedgerTableProps) {
  const currentField = sort.startsWith('-') ? sort.slice(1) : sort;
  const isDescending = sort.startsWith('-');

  function toggleSort(field: string) {
    if (currentField !== field) {
      onSortChange(field);
      return;
    }
    onSortChange(isDescending ? field : `-${field}`);
  }

  function sortButton(field: string, label: string) {
    return (
      <button type="button" onClick={() => toggleSort(field)} className="inline-flex items-center gap-1 hover:text-foreground">
        {label}
        {currentField === field && (isDescending ? <ArrowDown className="h-3.5 w-3.5" /> : <ArrowUp className="h-3.5 w-3.5" />)}
      </button>
    );
  }

  return (
    <div className="overflow-hidden rounded-lg border border-border">
      <table className="w-full text-sm">
        <thead className="bg-muted/50 text-left text-xs font-medium uppercase tracking-wide text-muted-foreground">
          <tr>
            <th className="px-4 py-2.5">Invoice</th>
            <th className="px-4 py-2.5">{sortButton('patientName', 'Patient')}</th>
            <th className="px-4 py-2.5">{sortButton('createdAt', 'Date')}</th>
            <th className="px-4 py-2.5">Items</th>
            <th className="px-4 py-2.5 text-right">{sortButton('netAmount', 'Net Amount')}</th>
            <th className="px-4 py-2.5">Status</th>
            <th className="px-4 py-2.5 text-right">Actions</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {billings.map((billing) => (
            <tr key={billing.id} className="hover:bg-muted/30">
              <td className="px-4 py-3 font-mono text-xs text-muted-foreground">{billing.id}</td>
              <td className="px-4 py-3">
                <Link to={`/finance/accounts/${billing.id}`} className="font-medium text-foreground hover:text-primary hover:underline">
                  {billing.patientName}
                </Link>
                <div className="text-xs text-muted-foreground">{billing.patientUhid}</div>
              </td>
              <td className="px-4 py-3 font-mono text-xs text-muted-foreground">{new Date(billing.createdAt).toLocaleDateString('en-IN')}</td>
              <td className="px-4 py-3 text-muted-foreground">
                {billing.items.length} item{billing.items.length === 1 ? '' : 's'}
              </td>
              <td className="px-4 py-3 text-right font-medium text-foreground">{formatCurrency(billing.netAmount)}</td>
              <td className="px-4 py-3">
                <PaymentStatusBadge status={getOverallPaymentStatus(billing.items)} />
              </td>
              <td className="px-4 py-3">
                <div className="flex justify-end">
                  <Button asChild variant="ghost" size="sm">
                    <Link to={`/finance/accounts/${billing.id}`}>View</Link>
                  </Button>
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
