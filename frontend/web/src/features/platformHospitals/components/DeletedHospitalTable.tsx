import type { DeletedTenantListItemResponse } from '@hms/shared';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';

interface DeletedHospitalTableProps {
  hospitals: DeletedTenantListItemResponse[];
  onRestore: (hospital: DeletedTenantListItemResponse) => void;
  isRestoringId: string | undefined;
}

export function DeletedHospitalTable({ hospitals, onRestore, isRestoringId }: DeletedHospitalTableProps) {
  return (
    <div className="overflow-hidden rounded-lg border border-border">
      <table className="w-full text-sm">
        <thead className="bg-muted/50 text-left text-xs font-medium uppercase tracking-wide text-muted-foreground">
          <tr>
            <th className="px-4 py-2.5">Hospital Name</th>
            <th className="px-4 py-2.5">Code</th>
            <th className="px-4 py-2.5">Deleted</th>
            <th className="px-4 py-2.5 text-right">Actions</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {hospitals.map((hospital) => (
            <tr key={hospital.id} className="hover:bg-muted/30">
              <td className="px-4 py-3 font-medium text-foreground">{hospital.hospitalName}</td>
              <td className="px-4 py-3 font-mono text-xs text-muted-foreground">
                {hospital.hospitalCode}
                <Badge variant="secondary" className="ml-2">
                  Deleted
                </Badge>
              </td>
              <td className="px-4 py-3 font-mono text-xs text-muted-foreground">
                {new Date(hospital.deletedAt).toLocaleDateString('en-IN')}
              </td>
              <td className="px-4 py-3">
                <div className="flex justify-end gap-1.5">
                  <Button variant="ghost" size="sm" onClick={() => onRestore(hospital)} disabled={isRestoringId === hospital.id}>
                    Restore
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
