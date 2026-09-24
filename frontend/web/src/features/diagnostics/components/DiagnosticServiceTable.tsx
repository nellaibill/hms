import type { DiagnosticCategory, DiagnosticProvider, DiagnosticService } from '@hms/shared';
import { Link } from 'react-router-dom';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { useAuth } from '@/features/auth/AuthContext';

interface DiagnosticServiceTableProps {
  services: DiagnosticService[];
  categoriesById: Map<string, DiagnosticCategory>;
  providersById: Map<string, DiagnosticProvider>;
  onDeleteRequested: (service: DiagnosticService) => void;
}

/** costPrice of 0 means "not yet costed" (see DiagnosticService.CostPrice's own doc comment),
 * not "free to run" — showing a fake 100% margin for every un-costed test would be actively
 * misleading, so those render as "Not costed" instead of a number. */
function renderMargin(price: number, costPrice: number) {
  if (costPrice <= 0) {
    return <span className="text-muted-foreground">Not costed</span>;
  }

  const margin = price - costPrice;
  const marginPercent = price > 0 ? (margin / price) * 100 : 0;
  const sign = margin < 0 ? '-' : '';

  return (
    <span className={margin < 0 ? 'text-destructive' : 'text-success'}>
      {sign}₹{Math.abs(margin).toLocaleString('en-IN')} ({marginPercent.toFixed(0)}%)
    </span>
  );
}

export function DiagnosticServiceTable({ services, categoriesById, providersById, onDeleteRequested }: DiagnosticServiceTableProps) {
  const { hasPermission } = useAuth();
  const canEdit = hasPermission('diagnostics.edit');
  const canDelete = hasPermission('diagnostics.delete');

  return (
    <div className="overflow-x-auto rounded-lg border border-border">
      <table className="w-full text-sm">
        <thead className="bg-sidebar-active text-left text-xs font-medium uppercase tracking-wide text-sidebar-active-foreground">
          <tr>
            <th className="px-4 py-2.5">Code</th>
            <th className="px-4 py-2.5">Name</th>
            <th className="px-4 py-2.5">Category</th>
            <th className="px-4 py-2.5">Type</th>
            <th className="px-4 py-2.5">Outsourced</th>
            <th className="px-4 py-2.5">Price</th>
            <th className="px-4 py-2.5">Running Cost</th>
            <th className="px-4 py-2.5">Margin</th>
            <th className="px-4 py-2.5">Status</th>
            <th className="px-4 py-2.5 text-right">Actions</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {services.map((service) => (
            <tr key={service.id} className="hover:bg-muted/30">
              <td className="px-4 py-3 font-mono text-xs text-muted-foreground">{service.code}</td>
              <td className="px-4 py-3 font-medium text-foreground">{service.name}</td>
              <td className="px-4 py-3 text-muted-foreground">{categoriesById.get(service.categoryId)?.name ?? '—'}</td>
              <td className="px-4 py-3 text-muted-foreground">{service.serviceType}</td>
              <td className="px-4 py-3 text-muted-foreground">
                {service.isOutsourced ? providersById.get(service.providerId ?? '')?.name ?? 'Outsourced' : 'No'}
              </td>
              <td className="px-4 py-3 text-muted-foreground">₹{service.price.toLocaleString('en-IN')}</td>
              <td className="px-4 py-3 text-muted-foreground">₹{service.costPrice.toLocaleString('en-IN')}</td>
              <td className="px-4 py-3">{renderMargin(service.price, service.costPrice)}</td>
              <td className="px-4 py-3">
                <Badge variant={service.isActive ? 'success' : 'secondary'}>{service.isActive ? 'Active' : 'Inactive'}</Badge>
              </td>
              <td className="px-4 py-3">
                <div className="flex justify-end gap-1.5">
                  {canEdit && (
                    <Button variant="ghost" size="sm" asChild>
                      <Link to={`/diagnostics/lab/services/${service.id}/edit`}>Edit</Link>
                    </Button>
                  )}
                  {canDelete && (
                    <Button variant="ghost" size="sm" className="text-destructive hover:text-destructive" onClick={() => onDeleteRequested(service)}>
                      Delete
                    </Button>
                  )}
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
