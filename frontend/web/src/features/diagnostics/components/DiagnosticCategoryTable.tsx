import type { DiagnosticCategory } from '@hms/shared';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { useAuth } from '@/features/auth/AuthContext';

interface DiagnosticCategoryTableProps {
  categories: DiagnosticCategory[];
  onEditRequested: (category: DiagnosticCategory) => void;
  onDeleteRequested: (category: DiagnosticCategory) => void;
}

export function DiagnosticCategoryTable({ categories, onEditRequested, onDeleteRequested }: DiagnosticCategoryTableProps) {
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
            <th className="px-4 py-2.5">Description</th>
            <th className="px-4 py-2.5">Status</th>
            <th className="px-4 py-2.5 text-right">Actions</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {categories.map((category) => (
            <tr key={category.id} className="hover:bg-muted/30">
              <td className="px-4 py-3 font-mono text-xs text-muted-foreground">{category.code}</td>
              <td className="px-4 py-3 font-medium text-foreground">{category.name}</td>
              <td className="px-4 py-3 text-muted-foreground">{category.description || '—'}</td>
              <td className="px-4 py-3">
                <Badge variant={category.isActive ? 'success' : 'secondary'}>{category.isActive ? 'Active' : 'Inactive'}</Badge>
              </td>
              <td className="px-4 py-3">
                <div className="flex justify-end gap-1.5">
                  {canEdit && (
                    <Button variant="ghost" size="sm" onClick={() => onEditRequested(category)}>
                      Edit
                    </Button>
                  )}
                  {canDelete && (
                    <Button variant="ghost" size="sm" className="text-destructive hover:text-destructive" onClick={() => onDeleteRequested(category)}>
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
