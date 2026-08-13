import type { Bed, BedStatus } from '@hms/shared';
import { ArrowDown, ArrowUp } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';

const bedStatusVariant: Record<BedStatus, 'success' | 'destructive' | 'warning'> = {
  Available: 'success',
  Occupied: 'destructive',
  Maintenance: 'warning',
};

interface BedTableProps {
  beds: Bed[];
  /** WardId -> display label (e.g. "General Medicine Ward A (GENMED-A)"), so the table
   * doesn't need its own N+1 ward lookups — the list page already has the full ward list
   * loaded for the filter dropdown. */
  wardLabels: Record<string, string>;
  sort: string;
  onSortChange: (sort: string) => void;
  onDeleteRequested: (bed: Bed) => void;
}

const columns: Array<{ field: string; label: string }> = [{ field: 'bednumber', label: 'Bed Number' }];

export function BedTable({ beds, wardLabels, sort, onSortChange, onDeleteRequested }: BedTableProps) {
  const currentField = sort.startsWith('-') ? sort.slice(1) : sort;
  const isDescending = sort.startsWith('-');

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
            <th className="px-4 py-2.5">Ward</th>
            <th className="px-4 py-2.5">Type</th>
            <th className="px-4 py-2.5">Daily Charge</th>
            <th className="px-4 py-2.5">Status</th>
            <th className="px-4 py-2.5">Active</th>
            <th className="px-4 py-2.5 text-right">Actions</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {beds.map((bed) => (
            <tr key={bed.id} className="hover:bg-muted/30">
              <td className="px-4 py-3">
                <Link to={`/clinical/ipd/beds/${bed.id}/edit`} className="font-medium text-foreground hover:text-primary hover:underline">
                  {bed.bedNumber}
                </Link>
              </td>
              <td className="px-4 py-3 text-sm text-foreground">{wardLabels[bed.wardId] ?? bed.wardId}</td>
              <td className="px-4 py-3 text-sm text-foreground">{bed.bedType}</td>
              <td className="px-4 py-3 text-sm text-foreground">₹{bed.dailyCharge.toLocaleString('en-IN')}/day</td>
              <td className="px-4 py-3">
                <Badge variant={bedStatusVariant[bed.status]}>{bed.status}</Badge>
              </td>
              <td className="px-4 py-3">
                <Badge variant={bed.isActive ? 'success' : 'secondary'}>{bed.isActive ? 'Active' : 'Inactive'}</Badge>
              </td>
              <td className="px-4 py-3">
                <div className="flex justify-end gap-1.5">
                  <Button asChild variant="ghost" size="sm">
                    <Link to={`/clinical/ipd/beds/${bed.id}/edit`}>Edit</Link>
                  </Button>
                  <Button
                    variant="ghost"
                    size="sm"
                    className="text-destructive hover:text-destructive"
                    disabled={bed.status === 'Occupied'}
                    title={bed.status === 'Occupied' ? 'An occupied bed cannot be deleted' : undefined}
                    onClick={() => onDeleteRequested(bed)}
                  >
                    Delete
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
