import type { DiagnosticPackage } from '@hms/shared';
import { Link } from 'react-router-dom';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { useAuth } from '@/features/auth/AuthContext';

interface DiagnosticPackageTableProps {
  packages: DiagnosticPackage[];
  onDeleteRequested: (pkg: DiagnosticPackage) => void;
}

export function DiagnosticPackageTable({ packages, onDeleteRequested }: DiagnosticPackageTableProps) {
  const { hasPermission } = useAuth();
  const canDelete = hasPermission('diagnostics.delete');

  return (
    <div className="overflow-x-auto rounded-lg border border-border">
      <table className="w-full text-sm">
        <thead className="bg-muted/50 text-left text-xs font-medium uppercase tracking-wide text-muted-foreground">
          <tr>
            <th className="px-4 py-2.5">Code</th>
            <th className="px-4 py-2.5">Name</th>
            <th className="px-4 py-2.5">Description</th>
            <th className="px-4 py-2.5">Total Price</th>
            <th className="px-4 py-2.5"># Tests</th>
            <th className="px-4 py-2.5">Status</th>
            <th className="px-4 py-2.5 text-right">Actions</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {packages.map((pkg) => (
            <tr key={pkg.id} className="hover:bg-muted/30">
              <td className="px-4 py-3 font-mono text-xs text-muted-foreground">{pkg.code}</td>
              <td className="px-4 py-3 font-medium text-foreground">
                <Link to={`/diagnostics/lab/packages/${pkg.id}`} className="hover:underline">
                  {pkg.name}
                </Link>
              </td>
              <td className="px-4 py-3 text-muted-foreground">{pkg.description || '—'}</td>
              <td className="px-4 py-3 text-muted-foreground">₹{pkg.totalPrice.toLocaleString('en-IN')}</td>
              <td className="px-4 py-3 text-muted-foreground">{pkg.items.length}</td>
              <td className="px-4 py-3">
                <Badge variant={pkg.isActive ? 'success' : 'secondary'}>{pkg.isActive ? 'Active' : 'Inactive'}</Badge>
              </td>
              <td className="px-4 py-3">
                <div className="flex justify-end gap-1.5">
                  <Button variant="ghost" size="sm" asChild>
                    <Link to={`/diagnostics/lab/packages/${pkg.id}`}>View</Link>
                  </Button>
                  {canDelete && (
                    <Button variant="ghost" size="sm" className="text-destructive hover:text-destructive" onClick={() => onDeleteRequested(pkg)}>
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
