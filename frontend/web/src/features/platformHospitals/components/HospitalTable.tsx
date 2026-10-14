import type { TenantListItemResponse } from '@hms/shared';
import {
  DatabaseZap,
  Loader2,
  MoreHorizontal,
  Power,
  PowerOff,
  SlidersHorizontal,
} from 'lucide-react';
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

/** Avatar colours, picked by hospital code so each hospital keeps the same one. Full class
 * strings so Tailwind's JIT sees them. */
const AVATAR_TINTS = [
  'bg-blue-500/10 text-blue-600 dark:text-blue-400',
  'bg-emerald-500/10 text-emerald-600 dark:text-emerald-400',
  'bg-violet-500/10 text-violet-600 dark:text-violet-400',
  'bg-amber-500/15 text-amber-700 dark:text-amber-400',
  'bg-rose-500/10 text-rose-600 dark:text-rose-400',
  'bg-sky-500/10 text-sky-600 dark:text-sky-400',
];

function avatarTint(code: string): string {
  const hash = [...code].reduce((sum, char) => sum + char.charCodeAt(0), 0);
  return AVATAR_TINTS[hash % AVATAR_TINTS.length];
}

function initialsOf(name: string): string {
  const words = name.replace(/[_-]+/g, ' ').trim().split(/\s+/);
  return (words.length > 1 ? words[0][0] + words[1][0] : name.slice(0, 2)).toUpperCase();
}

function StatusPill({ active, label }: { active: boolean; label: string }) {
  return (
    <span
      className={cn(
        'inline-flex items-center gap-1.5 rounded-full px-2.5 py-0.5 text-xs font-medium',
        active ? 'bg-success/10 text-success' : 'bg-muted text-muted-foreground',
      )}
    >
      <span
        className={cn('h-1.5 w-1.5 rounded-full', active ? 'bg-success' : 'bg-muted-foreground/60')}
      />
      {label}
    </span>
  );
}

export function HospitalTable({
  hospitals,
  onToggleStatus,
  isTogglingId,
  onManageFeatures,
  onMigrate,
  isMigratingId,
}: HospitalTableProps) {
  return (
    <div className="overflow-x-auto">
      <table className="w-full text-sm">
        {/* Same themed header row as the hospital app's tables (patients, invoices). */}
        <thead className="bg-sidebar-active text-left text-xs font-medium uppercase tracking-wide text-sidebar-active-foreground">
          <tr>
            <th className="px-5 py-3 font-semibold">Hospital</th>
            <th className="px-4 py-3 font-semibold">Status</th>
            <th className="hidden px-4 py-3 font-semibold md:table-cell">Plan</th>
            <th className="hidden px-4 py-3 font-semibold md:table-cell">Registered</th>
            <th className="px-5 py-3 text-right font-semibold">Actions</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {hospitals.map((hospital) => {
            const isActive = hospital.status === 'Active';
            const isMigrating = isMigratingId === hospital.id;
            return (
              <tr key={hospital.id} className="group transition-colors hover:bg-primary/[0.03]">
                <td className="px-5 py-3">
                  <div className="flex items-center gap-3">
                    <span
                      className={cn(
                        'flex h-9 w-9 shrink-0 items-center justify-center rounded-lg text-xs font-bold',
                        avatarTint(hospital.hospitalCode),
                      )}
                    >
                      {initialsOf(hospital.hospitalName)}
                    </span>
                    <div className="flex min-w-0 flex-col">
                      <span className="truncate font-semibold text-foreground">
                        {hospital.hospitalName}
                      </span>
                      <span className="text-xs text-muted-foreground">
                        Code{' '}
                        <code className="font-mono text-foreground/80">
                          {hospital.hospitalCode}
                        </code>
                      </span>
                    </div>
                  </div>
                </td>
                <td className="px-4 py-3">
                  <StatusPill active={isActive} label={hospital.status} />
                </td>
                <td className="hidden px-4 py-3 md:table-cell">
                  <span className="rounded-md border border-border px-2 py-0.5 text-xs font-medium text-foreground">
                    {hospital.subscriptionTier}
                  </span>
                </td>
                <td className="hidden whitespace-nowrap px-4 py-3 text-muted-foreground md:table-cell">
                  {new Date(hospital.createdAt).toLocaleDateString('en-IN', {
                    day: 'numeric',
                    month: 'short',
                    year: 'numeric',
                  })}
                </td>
                <td className="px-5 py-3">
                  <div className="flex items-center justify-end gap-1.5">
                    <Button
                      variant="outline"
                      size="sm"
                      className="gap-1.5"
                      onClick={() => onManageFeatures(hospital)}
                    >
                      <SlidersHorizontal className="h-3.5 w-3.5" />
                      Features
                    </Button>
                    <Button
                      variant="outline"
                      size="sm"
                      className="gap-1.5"
                      onClick={() => onMigrate(hospital)}
                      disabled={isMigrating}
                      title="Apply any pending database migrations to this hospital"
                    >
                      {isMigrating ? (
                        <Loader2 className="h-3.5 w-3.5 animate-spin" />
                      ) : (
                        <DatabaseZap className="h-3.5 w-3.5" />
                      )}
                      {isMigrating ? 'Migrating…' : 'Migrate'}
                    </Button>
                    {/* Disabling blocks every sign-in for the hospital, so it sits behind a
                        menu rather than next to the everyday actions. */}
                    <DropdownMenu>
                      <DropdownMenuTrigger asChild>
                        <Button
                          variant="ghost"
                          size="icon"
                          className="h-8 w-8"
                          aria-label={`More actions for ${hospital.hospitalName}`}
                          disabled={isTogglingId === hospital.id}
                        >
                          {isTogglingId === hospital.id ? (
                            <Loader2 className="h-4 w-4 animate-spin" />
                          ) : (
                            <MoreHorizontal className="h-4 w-4" />
                          )}
                        </Button>
                      </DropdownMenuTrigger>
                      <DropdownMenuContent align="end">
                        {isActive ? (
                          <DropdownMenuItem
                            className="text-destructive focus:text-destructive"
                            onClick={() => onToggleStatus(hospital)}
                          >
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
