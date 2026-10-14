import type { TenantListItemResponse } from '@hms/shared';
import { DatabaseZap, Loader2, MoreHorizontal, Power, PowerOff, SlidersHorizontal } from 'lucide-react';
import { Button } from '@/components/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { cn } from '@/lib/utils';

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

function StatusPill({ active, label }: { active: boolean; label: string }) {
  return (
    <span
      className={cn(
        'inline-flex items-center gap-1.5 rounded-full px-2 py-0.5 text-xs font-medium',
        active ? 'bg-success/10 text-success' : 'bg-muted text-muted-foreground',
      )}
    >
      <span className={cn('h-1.5 w-1.5 rounded-full', active ? 'bg-success' : 'bg-muted-foreground/60')} />
      {label}
    </span>
  );
}

export function HospitalTable({ hospitals, onToggleStatus, isTogglingId, onManageFeatures, onMigrate, isMigratingId }: HospitalTableProps) {
  return (
    <div className="overflow-x-auto">
      <table className="w-full text-sm">
        <thead className="border-y border-border bg-muted/40 text-left text-xs font-medium uppercase tracking-wide text-muted-foreground">
          <tr>
            <th className="px-5 py-2.5 font-medium">Hospital</th>
            <th className="px-4 py-2.5 font-medium">Code</th>
            <th className="px-4 py-2.5 font-medium">Status</th>
            <th className="hidden px-4 py-2.5 font-medium md:table-cell">Plan</th>
            <th className="hidden px-4 py-2.5 font-medium md:table-cell">Registered</th>
            <th className="px-5 py-2.5 text-right font-medium">Actions</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {hospitals.map((hospital) => {
            const isActive = hospital.status === 'Active';
            const isMigrating = isMigratingId === hospital.id;
            return (
              <tr key={hospital.id} className="transition-colors hover:bg-muted/30">
                <td className="px-5 py-3">
                  <div className="flex items-center gap-3">
                    <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg bg-primary/10 text-xs font-semibold uppercase text-primary">
                      {hospital.hospitalName.trim().slice(0, 2)}
                    </span>
                    <span className="font-medium text-foreground">{hospital.hospitalName}</span>
                  </div>
                </td>
                <td className="px-4 py-3">
                  <code className="rounded bg-muted px-1.5 py-0.5 font-mono text-xs text-foreground">{hospital.hospitalCode}</code>
                </td>
                <td className="px-4 py-3">
                  <StatusPill active={isActive} label={hospital.status} />
                </td>
                <td className="hidden px-4 py-3 text-muted-foreground md:table-cell">{hospital.subscriptionTier}</td>
                <td className="hidden whitespace-nowrap px-4 py-3 text-muted-foreground md:table-cell">
                  {new Date(hospital.createdAt).toLocaleDateString('en-IN', { day: 'numeric', month: 'short', year: 'numeric' })}
                </td>
                <td className="px-5 py-3">
                  <div className="flex items-center justify-end gap-1.5">
                    <Button variant="outline" size="sm" className="gap-1.5" onClick={() => onManageFeatures(hospital)}>
                      <SlidersHorizontal className="h-3.5 w-3.5" />
                      Features
                    </Button>
                    <Button
                      variant="ghost"
                      size="sm"
                      className="gap-1.5"
                      onClick={() => onMigrate(hospital)}
                      disabled={isMigrating}
                      title="Apply any pending database migrations to this hospital"
                    >
                      {isMigrating ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <DatabaseZap className="h-3.5 w-3.5" />}
                      {isMigrating ? 'Migrating…' : 'Migrate'}
                    </Button>
                    {/* Disabling blocks every sign-in for the hospital, so it sits behind a
                        menu rather than next to the everyday actions. */}
                    <DropdownMenu>
                      <DropdownMenuTrigger asChild>
                        <Button variant="ghost" size="icon" className="h-8 w-8" aria-label={`More actions for ${hospital.hospitalName}`} disabled={isTogglingId === hospital.id}>
                          {isTogglingId === hospital.id ? <Loader2 className="h-4 w-4 animate-spin" /> : <MoreHorizontal className="h-4 w-4" />}
                        </Button>
                      </DropdownMenuTrigger>
                      <DropdownMenuContent align="end">
                        {isActive ? (
                          <DropdownMenuItem className="text-destructive focus:text-destructive" onClick={() => onToggleStatus(hospital)}>
                            <PowerOff className="h-4 w-4" />
                            Disable hospital
                          </DropdownMenuItem>
                        ) : (
                          <DropdownMenuItem onClick={() => onToggleStatus(hospital)}>
                            <Power className="h-4 w-4" />
                            Enable hospital
                          </DropdownMenuItem>
                        )}
                      </DropdownMenuContent>
                    </DropdownMenu>
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
