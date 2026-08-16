import type { Ward } from '@hms/shared';
import { ArrowDown, ArrowUp } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { useAuth } from '@/features/auth/AuthContext';

const wardTypeLabels: Record<Ward['wardType'], string> = {
  General: 'General',
  SemiPrivate: 'Semi-Private',
  Private: 'Private',
  ICU: 'ICU',
};

interface WardTableProps {
  wards: Ward[];
  sort: string;
  onSortChange: (sort: string) => void;
  onDeleteRequested: (ward: Ward) => void;
}

const columns: Array<{ field: string; label: string }> = [
  { field: 'code', label: 'Code' },
  { field: 'name', label: 'Name' },
];

export function WardTable({ wards, sort, onSortChange, onDeleteRequested }: WardTableProps) {
  const currentField = sort.startsWith('-') ? sort.slice(1) : sort;
  const isDescending = sort.startsWith('-');
  const { hasPermission } = useAuth();
  const canEdit = hasPermission('clinical-care.edit');
  const canDelete = hasPermission('clinical-care.delete');

  function toggleSort(field: string) {
    if (currentField !== field) {
      onSortChange(field);
      return;
    }
    onSortChange(isDescending ? field : `-${field}`);
  }

  return (
    <div className="overflow-hidden rounded-lg border border-border">
      <table className="w-full text-sm">
        <thead className="bg-muted/50 text-left text-xs font-medium uppercase tracking-wide text-muted-foreground">
          <tr>
            {columns.map((column) => (
              <th key={column.field} className="px-4 py-2.5">
                <button type="button" onClick={() => toggleSort(column.field)} className="inline-flex items-center gap-1 hover:text-foreground">
                  {column.label}
                  {currentField === column.field &&
                    (isDescending ? <ArrowDown className="h-3.5 w-3.5" /> : <ArrowUp className="h-3.5 w-3.5" />)}
                </button>
              </th>
            ))}
            <th className="px-4 py-2.5">Type</th>
            <th className="px-4 py-2.5">Status</th>
            <th className="px-4 py-2.5 text-right">Actions</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {wards.map((ward) => (
            <tr key={ward.id} className="hover:bg-muted/30">
              <td className="px-4 py-3 font-mono text-xs text-muted-foreground">{ward.code}</td>
              <td className="px-4 py-3">
                {canEdit ? (
                  <Link to={`/clinical/ipd/wards/${ward.id}/edit`} className="font-medium text-foreground hover:text-primary hover:underline">
                    {ward.name}
                  </Link>
                ) : (
                  <span className="font-medium text-foreground">{ward.name}</span>
                )}
              </td>
              <td className="px-4 py-3 text-sm text-foreground">{wardTypeLabels[ward.wardType]}</td>
              <td className="px-4 py-3">
                <Badge variant={ward.isActive ? 'success' : 'secondary'}>{ward.isActive ? 'Active' : 'Inactive'}</Badge>
              </td>
              <td className="px-4 py-3">
                <div className="flex justify-end gap-1.5">
                  {canEdit && (
                    <Button asChild variant="ghost" size="sm">
                      <Link to={`/clinical/ipd/wards/${ward.id}/edit`}>Edit</Link>
                    </Button>
                  )}
                  {canDelete && (
                    <Button variant="ghost" size="sm" className="text-destructive hover:text-destructive" onClick={() => onDeleteRequested(ward)}>
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
