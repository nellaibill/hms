import type { Admission } from '@hms/shared';
import { Link } from 'react-router-dom';
import { Badge, type BadgeProps } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { DownloadDischargeSummaryButton } from './DownloadDischargeSummaryButton';
import { PatientNameLink } from '@/components/PatientNameLink';

const STATUS_VARIANTS: Record<Admission['status'], BadgeProps['variant']> = {
  Requested: 'warning',
  Admitted: 'success',
  Discharged: 'secondary',
  Cancelled: 'destructive',
};

interface AdmissionTableProps {
  admissions: Admission[];
  /** Renders an "Assign Bed" action for Requested rows (no ward/bed yet) — omitted entirely
   * when the caller has nowhere to route the click (e.g. read-only contexts). */
  onAssignBed?: (admission: Admission) => void;
}

export function AdmissionTable({ admissions, onAssignBed }: AdmissionTableProps) {
  return (
    <div className="overflow-x-auto rounded-lg border border-border">
      <table className="w-full text-sm">
        <thead className="bg-muted/50 text-left text-xs font-medium uppercase tracking-wide text-muted-foreground">
          <tr>
            <th className="px-4 py-2.5">Admission No.</th>
            <th className="px-4 py-2.5">UHID</th>
            <th className="px-4 py-2.5">Patient</th>
            <th className="px-4 py-2.5">Age / Gender</th>
            <th className="px-4 py-2.5">Ward / Bed</th>
            <th className="px-4 py-2.5">Consultant</th>
            <th className="px-4 py-2.5">Admission Date</th>
            <th className="px-4 py-2.5">Status</th>
            <th className="px-4 py-2.5">Action</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {admissions.map((admission) => (
            <tr key={admission.id} className="hover:bg-muted/30">
              <td className="whitespace-nowrap px-4 py-3">
                <Link
                  to={`/clinical/ipd/admissions/${admission.id}`}
                  className="font-mono text-xs font-medium text-foreground hover:text-primary hover:underline"
                >
                  {admission.admissionNumber}
                </Link>
              </td>
              <td className="whitespace-nowrap px-4 py-3 font-mono text-xs text-muted-foreground">{admission.uhid}</td>
              <td className="px-4 py-3">
                <PatientNameLink patientId={admission.patientId}>{admission.patientName}</PatientNameLink>
              </td>
              <td className="whitespace-nowrap px-4 py-3 text-sm text-foreground">
                {admission.age} / {admission.gender}
              </td>
              <td className="whitespace-nowrap px-4 py-3 text-sm text-foreground">
                {admission.wardId && admission.bedId ? `${admission.wardName} / ${admission.bedNumber}` : '—'}
              </td>
              <td className="min-w-[12rem] px-4 py-3 text-sm text-foreground">{admission.consultantName}</td>
              <td className="whitespace-nowrap px-4 py-3 text-sm text-foreground">{new Date(admission.admissionDateTime).toLocaleString('en-IN')}</td>
              <td className="px-4 py-3">
                <Badge variant={STATUS_VARIANTS[admission.status]}>{admission.status}</Badge>
              </td>
              <td className="px-4 py-3">
                {admission.status === 'Discharged' && <DownloadDischargeSummaryButton admission={admission} />}
                {admission.status === 'Requested' && onAssignBed && (
                  <Button size="sm" onClick={() => onAssignBed(admission)}>
                    Assign Bed
                  </Button>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
