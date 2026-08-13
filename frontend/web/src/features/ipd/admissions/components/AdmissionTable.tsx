import type { Admission } from '@hms/shared';
import { Link } from 'react-router-dom';
import { Badge } from '@/components/ui/badge';

interface AdmissionTableProps {
  admissions: Admission[];
}

export function AdmissionTable({ admissions }: AdmissionTableProps) {
  return (
    <div className="overflow-hidden rounded-lg border border-border">
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
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {admissions.map((admission) => (
            <tr key={admission.id} className="hover:bg-muted/30">
              <td className="px-4 py-3">
                <Link
                  to={`/clinical/ipd/admissions/${admission.id}`}
                  className="font-mono text-xs font-medium text-foreground hover:text-primary hover:underline"
                >
                  {admission.admissionNumber}
                </Link>
              </td>
              <td className="px-4 py-3 font-mono text-xs text-muted-foreground">{admission.uhid}</td>
              <td className="px-4 py-3 font-medium text-foreground">{admission.patientName}</td>
              <td className="px-4 py-3 text-sm text-foreground">
                {admission.age} / {admission.gender}
              </td>
              <td className="px-4 py-3 text-sm text-foreground">
                {admission.wardName} / {admission.bedNumber}
              </td>
              <td className="px-4 py-3 text-sm text-foreground">{admission.consultantName}</td>
              <td className="px-4 py-3 text-sm text-foreground">{new Date(admission.admissionDateTime).toLocaleString('en-IN')}</td>
              <td className="px-4 py-3">
                <Badge variant={admission.status === 'Admitted' ? 'success' : 'secondary'}>{admission.status}</Badge>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
