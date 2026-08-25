import type { Patient } from '@hms/shared';
import { ArrowDown, ArrowUp } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { useAuth } from '../../auth/AuthContext';

interface PatientTableProps {
  patients: Patient[];
  sort: string;
  onSortChange: (sort: string) => void;
  onDeleteRequested: (patient: Patient) => void;
}

const columns: Array<{ field: string; label: string }> = [
  { field: 'uhid', label: 'UHID' },
  { field: 'lastName', label: 'Name' },
  { field: 'createdAt', label: 'Registered' },
];

export function PatientTable({ patients, sort, onSortChange, onDeleteRequested }: PatientTableProps) {
  const currentField = sort.startsWith('-') ? sort.slice(1) : sort;
  const isDescending = sort.startsWith('-');
  const { hasPermission } = useAuth();
  const canEdit = hasPermission('patient-management.edit');
  const canDelete = hasPermission('patient-management.delete');

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
        <thead className="sticky top-0 z-10 bg-muted/95 text-left text-xs font-medium uppercase tracking-wide text-muted-foreground backdrop-blur supports-[backdrop-filter]:bg-muted/80">
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
            <th className="px-4 py-2.5">Phone</th>
            <th className="px-4 py-2.5 text-right">Actions</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {patients.map((patient) => (
            <tr key={patient.id} className="hover:bg-muted/30">
              <td className="px-4 py-3 font-mono text-xs text-muted-foreground">{patient.uhid}</td>
              <td className="px-4 py-3">
                <Link
                  to={`/patients/registration/${patient.id}`}
                  className="font-medium text-foreground hover:text-primary hover:underline"
                >
                  {patient.title} {patient.firstName} {patient.lastName}
                </Link>
              </td>
              <td className="px-4 py-3 font-mono text-xs text-muted-foreground">
                {new Date(patient.createdAt).toLocaleDateString('en-IN')}
              </td>
              <td className="px-4 py-3 text-muted-foreground">{patient.primaryPhone}</td>
              <td className="px-4 py-3">
                <div className="flex justify-end gap-1.5">
                  {canEdit && (
                    <Button asChild variant="ghost" size="sm">
                      <Link to={`/patients/registration/${patient.id}/edit`}>Edit</Link>
                    </Button>
                  )}
                  {canDelete && (
                    <Button
                      variant="ghost"
                      size="sm"
                      className="text-destructive hover:text-destructive"
                      onClick={() => onDeleteRequested(patient)}
                    >
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
