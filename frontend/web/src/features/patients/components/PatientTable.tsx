import type { Patient } from '@hms/shared';
import { ArrowDown, ArrowUp, AlertTriangle } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { ConsultantName } from '@/components/ConsultantName';
import { DepartmentName } from '@/components/DepartmentName';

interface PatientTableProps {
  patients: Patient[];
  sort: string;
  onSortChange: (sort: string) => void;
  /** Same "Select" action as OPD Billing Entry's PatientPicker — opens the chosen patient. Edit/
   * Add Visit/Delete are all still reachable from that patient's own record page. */
  onSelect: (patient: Patient) => void;
  /** False while showing the default "Last 100 visits" list (sorted server-side by most
   * recent visit, same as OPD Billing Entry's PatientPicker) — that list ignores `sort`
   * entirely, so a clickable header there would silently do nothing. Sortable again once a
   * search narrows the list to something `sort` actually applies to. Defaults to true. */
  sortable?: boolean;
}

// Same format OPD Billing Entry's PatientPicker already uses for this field.
function formatAppointmentTime(iso?: string | null): string {
  if (!iso) return '—';
  return new Date(iso).toLocaleString('en-IN', { day: '2-digit', month: 'short', hour: 'numeric', minute: '2-digit', hour12: true });
}

export function PatientTable({ patients, sort, onSortChange, onSelect, sortable = true }: PatientTableProps) {
  const currentField = sort.startsWith('-') ? sort.slice(1) : sort;
  const isDescending = sort.startsWith('-');

  function toggleSort(field: string) {
    if (currentField !== field) {
      onSortChange(field);
      return;
    }
    onSortChange(isDescending ? field : `-${field}`);
  }

  function SortHeader({ field, label }: { field: string; label: string }) {
    if (!sortable) {
      return <th className="px-4 py-2.5">{label}</th>;
    }
    return (
      <th className="px-4 py-2.5">
        <button type="button" onClick={() => toggleSort(field)} className="inline-flex items-center gap-1 hover:text-foreground">
          {label}
          {currentField === field && (isDescending ? <ArrowDown className="h-3.5 w-3.5" /> : <ArrowUp className="h-3.5 w-3.5" />)}
        </button>
      </th>
    );
  }

  return (
    <div className="overflow-hidden rounded-lg border border-border">
      <table className="w-full text-sm">
        {/* Same column set/order as OPD Billing Entry's PatientPicker table (Patient, Age/
            Gender, UHID, Phone, Consultant, Department, Appointment Time, Action) — this page
            additionally keeps Patient/UHID sortable, since that's this table's own convention. */}
        <thead className="sticky top-0 z-10 bg-muted/95 text-left text-xs font-medium uppercase tracking-wide text-muted-foreground backdrop-blur supports-[backdrop-filter]:bg-muted/80">
          <tr>
            <SortHeader field="lastName" label="Patient" />
            <th className="px-4 py-2.5">Age / Gender</th>
            <SortHeader field="uhid" label="UHID" />
            <th className="px-4 py-2.5">Phone</th>
            <th className="px-4 py-2.5">Consultant</th>
            <th className="px-4 py-2.5">Department</th>
            <th className="px-4 py-2.5">Appointment Time</th>
            <th className="px-4 py-2.5 text-right">Action</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {patients.map((patient) => (
            <tr key={patient.id} className="hover:bg-muted/30">
              <td className="px-4 py-3">
                <div className="flex items-center gap-2">
                  <Link
                    to={`/patients/registration/${patient.id}`}
                    className="font-medium text-foreground hover:text-primary hover:underline"
                  >
                    {patient.title} {patient.firstName} {patient.lastName}
                  </Link>
                  {patient.requiresDataVerification && (
                    <Badge
                      variant="warning"
                      className="gap-1 whitespace-nowrap"
                      title="Imported from legacy records — some details are placeholders and need to be verified with the patient."
                    >
                      <AlertTriangle className="h-3 w-3" />
                      Verify Details
                    </Badge>
                  )}
                </div>
              </td>
              <td className="whitespace-nowrap px-4 py-3 text-muted-foreground">
                {patient.age} Yrs · {patient.gender}
              </td>
              <td className="px-4 py-3 font-mono text-xs text-muted-foreground">{patient.uhid}</td>
              <td className="px-4 py-3 text-muted-foreground">{patient.primaryPhone}</td>
              <td className="whitespace-nowrap px-4 py-3 text-muted-foreground">
                {patient.lastVisitConsultantId ? <ConsultantName consultantId={patient.lastVisitConsultantId} /> : '—'}
              </td>
              <td className="whitespace-nowrap px-4 py-3 text-muted-foreground">
                {patient.lastVisitDepartmentId ? <DepartmentName departmentId={patient.lastVisitDepartmentId} /> : '—'}
              </td>
              <td className="whitespace-nowrap px-4 py-3 text-muted-foreground">{formatAppointmentTime(patient.lastVisitAppointmentTime)}</td>
              <td className="px-4 py-3 text-right">
                <Button size="sm" onClick={() => onSelect(patient)}>
                  Select
                </Button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
