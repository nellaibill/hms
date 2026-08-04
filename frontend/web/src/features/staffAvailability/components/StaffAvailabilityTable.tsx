import type { StaffAvailability } from '@hms/shared';
import { ArrowDown, ArrowUp } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { StaffName } from '@/components/StaffName';

interface StaffAvailabilityTableProps {
  records: StaffAvailability[];
  sort: string;
  onSortChange: (sort: string) => void;
  onDeleteRequested: (record: StaffAvailability) => void;
}

const columns: Array<{ field: string; label: string }> = [
  { field: 'startDate', label: 'Start' },
  { field: 'endDate', label: 'End' },
];

export function StaffAvailabilityTable({ records, sort, onSortChange, onDeleteRequested }: StaffAvailabilityTableProps) {
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
            <th className="px-4 py-2.5">Staff</th>
            {columns.map((column) => (
              <th key={column.field} className="px-4 py-2.5">
                <button type="button" onClick={() => toggleSort(column.field)} className="inline-flex items-center gap-1 hover:text-foreground">
                  {column.label}
                  {currentField === column.field &&
                    (isDescending ? <ArrowDown className="h-3.5 w-3.5" /> : <ArrowUp className="h-3.5 w-3.5" />)}
                </button>
              </th>
            ))}
            <th className="px-4 py-2.5">Status</th>
            <th className="px-4 py-2.5">Reason</th>
            <th className="px-4 py-2.5 text-right">Actions</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {records.map((record) => (
            <tr key={record.id} className="hover:bg-muted/30">
              <td className="px-4 py-3">
                <Link to={`/admin/hr/staff-availability/${record.id}`} className="font-medium text-foreground hover:text-primary hover:underline">
                  <StaffName staffId={record.staffId} />
                </Link>
              </td>
              <td className="px-4 py-3 font-mono text-xs text-muted-foreground">{record.startDate}</td>
              <td className="px-4 py-3 font-mono text-xs text-muted-foreground">{record.endDate}</td>
              <td className="px-4 py-3">
                <Badge variant={record.availabilityStatus === 'Available' ? 'success' : 'secondary'}>{record.availabilityStatus}</Badge>
              </td>
              <td className="px-4 py-3 text-muted-foreground">{record.reason || '—'}</td>
              <td className="px-4 py-3">
                <div className="flex justify-end gap-1.5">
                  <Button asChild variant="ghost" size="sm">
                    <Link to={`/admin/hr/staff-availability/${record.id}/edit`}>Edit</Link>
                  </Button>
                  <Button variant="ghost" size="sm" className="text-destructive hover:text-destructive" onClick={() => onDeleteRequested(record)}>
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
