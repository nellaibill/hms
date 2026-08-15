import type { TenantListItemResponse } from '@hms/shared';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';

interface HospitalTableProps {
  hospitals: TenantListItemResponse[];
  onToggleStatus: (hospital: TenantListItemResponse) => void;
  isTogglingId: string | undefined;
}

export function HospitalTable({ hospitals, onToggleStatus, isTogglingId }: HospitalTableProps) {
  return (
    <div className="overflow-hidden rounded-lg border border-border">
      <table className="w-full text-sm">
        <thead className="bg-muted/50 text-left text-xs font-medium uppercase tracking-wide text-muted-foreground">
          <tr>
            <th className="px-4 py-2.5">Hospital Name</th>
            <th className="px-4 py-2.5">Code</th>
            <th className="px-4 py-2.5">Database Name</th>
            <th className="px-4 py-2.5">Status</th>
            <th className="px-4 py-2.5">Created</th>
            <th className="px-4 py-2.5 text-right">Actions</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {hospitals.map((hospital) => {
            const isActive = hospital.status === 'Active';
            return (
              <tr key={hospital.id} className="hover:bg-muted/30">
                <td className="px-4 py-3 font-medium text-foreground">{hospital.hospitalName}</td>
                <td className="px-4 py-3 font-mono text-xs text-muted-foreground">{hospital.hospitalCode}</td>
                <td className="px-4 py-3 font-mono text-xs text-muted-foreground">{hospital.databaseName}</td>
                <td className="px-4 py-3">
                  <Badge variant={isActive ? 'success' : 'secondary'}>{hospital.status}</Badge>
                </td>
                <td className="px-4 py-3 font-mono text-xs text-muted-foreground">
                  {new Date(hospital.createdAt).toLocaleDateString('en-IN')}
                </td>
                <td className="px-4 py-3">
                  <div className="flex justify-end gap-1.5">
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => onToggleStatus(hospital)}
                      disabled={isTogglingId === hospital.id}
                    >
                      {isActive ? 'Disable' : 'Enable'}
                    </Button>
                  </div>
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}
