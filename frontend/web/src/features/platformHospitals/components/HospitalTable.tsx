import type { TenantListItemResponse } from '@hms/shared';
import { Loader2 } from 'lucide-react';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';

interface HospitalTableProps {
  hospitals: TenantListItemResponse[];
  onToggleStatus: (hospital: TenantListItemResponse) => void;
  isTogglingId: string | undefined;
  onManageFeatures: (hospital: TenantListItemResponse) => void;
  /** Applies any pending EF Core migrations to this hospital's database (HospitalsController's
   * PlatformSuperAdmin-only migrate action) — the operator-triggered alternative to running
   * `dotnet ef database update` by hand against the tenant's connection string. */
  onMigrate: (hospital: TenantListItemResponse) => void;
  isMigratingId: string | undefined;
}

export function HospitalTable({ hospitals, onToggleStatus, isTogglingId, onManageFeatures, onMigrate, isMigratingId }: HospitalTableProps) {
  return (
    <div className="overflow-hidden rounded-lg border border-border">
      <table className="w-full text-sm">
        <thead className="bg-muted/50 text-left text-xs font-medium uppercase tracking-wide text-muted-foreground">
          <tr>
            <th className="px-4 py-2.5">Hospital Name</th>
            <th className="px-4 py-2.5">Code</th>
            <th className="px-4 py-2.5">Status</th>
            <th className="px-4 py-2.5">Tier</th>
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
                <td className="px-4 py-3">
                  <Badge variant={isActive ? 'success' : 'secondary'}>{hospital.status}</Badge>
                </td>
                <td className="px-4 py-3 text-xs text-muted-foreground">{hospital.subscriptionTier}</td>
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
                    <Button variant="ghost" size="sm" onClick={() => onManageFeatures(hospital)}>
                      Manage Features
                    </Button>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => onMigrate(hospital)}
                      disabled={isMigratingId === hospital.id}
                      title="Apply any pending database migrations to this hospital"
                    >
                      {isMigratingId === hospital.id ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : 'Migrate'}
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
