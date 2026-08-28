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

export function DiagnosticServiceTable({ services, categoriesById, providersById, onDeleteRequested }: DiagnosticServiceTableProps) {
  const { hasPermission } = useAuth();
  const canEdit = hasPermission('diagnostics.edit');
  const canDelete = hasPermission('diagnostics.delete');

  return (
    <div className="overflow-x-auto rounded-lg border border-border">
      <table className="w-full text-sm">
        <thead className="bg-muted/50 text-left text-xs font-medium uppercase tracking-wide text-muted-foreground">
          <tr>
            <th className="px-4 py-2.5">Code</th>
            <th className="px-4 py-2.5">Name</th>
            <th className="px-4 py-2.5">Category</th>
            <th className="px-4 py-2.5">Type</th>
            <th className="px-4 py-2.5">Outsourced</th>
            <th className="px-4 py-2.5">Price</th>
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
