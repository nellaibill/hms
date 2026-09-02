import { PackageSearch } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import type { LabOrder } from '../types';
import { LabStatusBadge } from './LabStatusBadge';

interface LabWorklistTableProps {
  orders: LabOrder[];
}

/**
 * One row per LabOrderResponse. Age/gender columns are deliberately omitted — LabOrderResponse
 * only snapshots PatientName/PatientUhid, not live demographics, and this table doesn't fetch
 * the Patient record separately just to add two columns (would duplicate Patients' own data
 * fetch elsewhere). Compact, matches InvoiceLedgerTable's exact visual density.
 */
export function LabWorklistTable({ orders }: LabWorklistTableProps) {
  return (
    <div className="overflow-hidden rounded-lg border border-border">
      <table className="w-full text-sm">
        <thead className="bg-muted/50 text-left text-xs font-medium uppercase tracking-wide text-muted-foreground">
          <tr>
            <th className="px-4 py-2.5">Order</th>
            <th className="px-4 py-2.5">Patient</th>
            <th className="px-4 py-2.5">Tests</th>
            <th className="px-4 py-2.5">Source</th>
            <th className="px-4 py-2.5">Priority</th>
            <th className="px-4 py-2.5">Status</th>
            <th className="px-4 py-2.5">Created</th>
            <th className="px-4 py-2.5 text-right">Actions</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {orders.map((order) => {
            const hasPackage = order.items.some((item) => item.packageId);
            return (
              <tr key={order.id} className="hover:bg-muted/30">
                <td className="whitespace-nowrap px-4 py-3">
                  <Link
                    to={`/diagnostics/lab/orders/${order.id}`}
                    className="font-mono text-xs font-medium text-foreground hover:text-primary hover:underline"
                  >
                    {order.labOrderNumber}
                  </Link>
                </td>
                <td className="whitespace-nowrap px-4 py-3">
                  <div className="font-medium text-foreground">{order.patientName}</div>
                  <div className="font-mono text-xs text-muted-foreground">{order.patientUhid}</div>
                </td>
                <td className="whitespace-nowrap px-4 py-3 text-muted-foreground">
                  <span className="inline-flex items-center gap-1.5">
                    {order.items.length} test{order.items.length === 1 ? '' : 's'}
                    {hasPackage && (
                      <span title="Includes a package">
                        <PackageSearch className="h-3.5 w-3.5" />
                      </span>
                    )}
                  </span>
                </td>
                <td className="whitespace-nowrap px-4 py-3">
                  {order.source ? (
                    <Badge variant="outline" className="text-[10px]">
                      {order.source}
                    </Badge>
                  ) : (
                    <span className="text-muted-foreground">—</span>
                  )}
                </td>
                <td className="whitespace-nowrap px-4 py-3">
                  <Badge variant={order.priority === 'Stat' ? 'destructive' : order.priority === 'Urgent' ? 'warning' : 'secondary'}>
                    {order.priority}
                  </Badge>
                </td>
                <td className="whitespace-nowrap px-4 py-3">
                  <LabStatusBadge status={order.overallStatus} />
                </td>
                <td className="whitespace-nowrap px-4 py-3 font-mono text-xs text-muted-foreground">
                  {new Date(order.createdAt).toLocaleString('en-IN')}
                </td>
                <td className="px-4 py-3">
                  <div className="flex justify-end">
                    <Button asChild variant="ghost" size="sm">
                      <Link to={`/diagnostics/lab/orders/${order.id}`}>View</Link>
                    </Button>
                  </div>
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}
