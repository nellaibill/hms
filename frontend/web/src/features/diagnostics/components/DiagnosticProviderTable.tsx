import type { DiagnosticProvider } from '@hms/shared';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { useAuth } from '@/features/auth/AuthContext';

interface DiagnosticProviderTableProps {
  providers: DiagnosticProvider[];
  onEditRequested: (provider: DiagnosticProvider) => void;
  onDeleteRequested: (provider: DiagnosticProvider) => void;
}

export function DiagnosticProviderTable({ providers, onEditRequested, onDeleteRequested }: DiagnosticProviderTableProps) {
  const { hasPermission } = useAuth();
  const canEdit = hasPermission('diagnostics.edit');
  const canDelete = hasPermission('diagnostics.delete');

  return (
    <div className="overflow-hidden rounded-lg border border-border">
      <table className="w-full text-sm">
        <thead className="bg-muted/50 text-left text-xs font-medium uppercase tracking-wide text-muted-foreground">
          <tr>
            <th className="px-4 py-2.5">Code</th>
            <th className="px-4 py-2.5">Name</th>
            <th className="px-4 py-2.5">Contact Details</th>
            <th className="px-4 py-2.5">Status</th>
            <th className="px-4 py-2.5 text-right">Actions</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {providers.map((provider) => (
            <tr key={provider.id} className="hover:bg-muted/30">
              <td className="px-4 py-3 font-mono text-xs text-muted-foreground">{provider.code}</td>
              <td className="px-4 py-3 font-medium text-foreground">{provider.name}</td>
              <td className="px-4 py-3 text-muted-foreground">{provider.contactDetails || '—'}</td>
              <td className="px-4 py-3">
                <Badge variant={provider.isActive ? 'success' : 'secondary'}>{provider.isActive ? 'Active' : 'Inactive'}</Badge>
              </td>
              <td className="px-4 py-3">
                <div className="flex justify-end gap-1.5">
                  {canEdit && (
                    <Button variant="ghost" size="sm" onClick={() => onEditRequested(provider)}>
                      Edit
                    </Button>
                  )}
                  {canDelete && (
                    <Button variant="ghost" size="sm" className="text-destructive hover:text-destructive" onClick={() => onDeleteRequested(provider)}>
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
