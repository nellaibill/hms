import type { TenantListItemResponse } from '@hms/shared';
import { AlertTriangle, Building2, CheckCircle2, DatabaseBackup, Loader2, LogOut, Plus, ShieldCheck, XCircle } from 'lucide-react';
import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { useToast } from '@/components/ui/toast-context';
import { Pagination } from '@/components/Pagination';
import { ThemeToggle } from '@/components/shell/ThemeToggle';
import { usePlatformAuth } from '@/features/platformAuth/PlatformAuthContext';
import {
  HospitalFeaturesDialog,
  HospitalListToolbar,
  HospitalTable,
  useHospitalsQuery,
  useHospitalStatsQuery,
  useMigrateHospitalMutation,
  useUpdateHospitalStatusMutation,
} from '@/features/platformHospitals';
import { useDebouncedValue } from '@/hooks/useDebouncedValue';
import { cn } from '@/lib/utils';

interface StatTile {
  key: string;
  label: string;
  value: string;
  icon: typeof Building2;
  /** 'alert' highlights the tile in a warning color — used for the provisioning-alert
   * count, which should stand out from the ground-truth hospital counts when nonzero. */
  variant?: TileVariant;
}

type TileVariant = 'default' | 'success' | 'muted' | 'alert';

const TILE_TINTS: Record<TileVariant, string> = {
  default: 'bg-primary/10 text-primary',
  success: 'bg-success/10 text-success',
  muted: 'bg-muted text-muted-foreground',
  alert: 'bg-destructive/10 text-destructive',
};

export default function PlatformDashboardPage() {
  const { user, logout } = usePlatformAuth();
  const navigate = useNavigate();

  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const debouncedSearch = useDebouncedValue(search);
  const [hospitalToManageFeatures, setHospitalToManageFeatures] = useState<TenantListItemResponse | null>(null);

  const { toast } = useToast();
  const statsQuery = useHospitalStatsQuery();
  const hospitalsQuery = useHospitalsQuery({ page, pageSize: 20, search: debouncedSearch || undefined });
  const statusMutation = useUpdateHospitalStatusMutation();
  const migrateMutation = useMigrateHospitalMutation();

  const stats = statsQuery.data;
  const tiles: StatTile[] = [
    { key: 'total', label: 'Total Hospitals', value: stats ? String(stats.total) : '…', icon: Building2 },
    { key: 'active', label: 'Active', value: stats ? String(stats.active) : '…', icon: CheckCircle2, variant: 'success' },
    { key: 'inactive', label: 'Inactive', value: stats ? String(stats.inactive) : '…', icon: XCircle, variant: 'muted' },
    {
      key: 'provisioningAlerts',
      label: 'Provisioning Alerts',
      value: stats ? String(stats.provisioningAlertCount) : '…',
      icon: AlertTriangle,
      variant: stats && stats.provisioningAlertCount > 0 ? 'alert' : 'muted',
    },
  ];

  function handleSearchChange(value: string) {
    setSearch(value);
    setPage(1);
  }

  function handleToggleStatus(hospital: TenantListItemResponse) {
    const nextStatus = hospital.status === 'Active' ? 'Inactive' : 'Active';
    statusMutation.mutate({ id: hospital.id, status: nextStatus });
  }

  function handleMigrate(hospital: TenantListItemResponse) {
    migrateMutation.mutate(hospital.id, {
      onSuccess: () =>
        toast({
          title: 'Migration applied',
          description: `${hospital.hospitalName}'s database is now up to date.`,
          variant: 'success',
        }),
      onError: (err) =>
        toast({
          title: 'Migration failed',
          description: err instanceof Error ? err.message : `Could not migrate ${hospital.hospitalName}.`,
          variant: 'error',
        }),
    });
  }

  const initials = (user?.fullName ?? user?.email ?? '?')
    .split(/\s+/)
    .map((part) => part[0])
    .join('')
    .slice(0, 2)
    .toUpperCase();

  return (
    <div className="min-h-screen bg-muted/30">
      <header className="sticky top-0 z-20 border-b border-border bg-background/95 backdrop-blur">
        <div className="mx-auto flex max-w-7xl items-center justify-between gap-3 px-4 py-3 sm:px-6">
          <div className="flex items-center gap-3">
            <span className="flex h-9 w-9 items-center justify-center rounded-lg bg-primary text-primary-foreground shadow-sm">
              <Building2 className="h-5 w-5" />
            </span>
            <div className="leading-tight">
              <p className="font-semibold text-foreground">Platform Portal</p>
              <p className="hidden text-xs text-muted-foreground sm:block">Hospitals, features and backups</p>
            </div>
          </div>
          <div className="flex items-center gap-1.5">
            <ThemeToggle />
            <Button variant="ghost" size="sm" className="gap-1.5" onClick={() => navigate('/platform/backups')}>
              <DatabaseBackup className="h-4 w-4" />
              <span className="hidden sm:inline">Backups</span>
            </Button>
            <Button variant="ghost" size="sm" className="gap-1.5" onClick={() => navigate('/platform/security')}>
              <ShieldCheck className="h-4 w-4" />
              <span className="hidden sm:inline">Security</span>
            </Button>
            <span className="mx-1 hidden h-6 w-px bg-border sm:block" aria-hidden />
            {user && (
              <div className="hidden items-center gap-2 md:flex" title={user.email}>
                <span className="flex h-8 w-8 items-center justify-center rounded-full bg-muted text-xs font-semibold text-foreground">{initials}</span>
                <span className="max-w-[10rem] truncate text-sm text-foreground">{user.fullName ?? user.email}</span>
              </div>
            )}
            <Button variant="outline" size="sm" className="gap-1.5" onClick={logout}>
              <LogOut className="h-4 w-4" />
              <span className="hidden sm:inline">Sign out</span>
            </Button>
          </div>
        </div>
      </header>

      <main className="mx-auto flex max-w-7xl flex-col gap-6 px-4 py-6 sm:px-6 lg:py-8">
        <div className="flex flex-wrap items-end justify-between gap-3">
          <div>
            <h1 className="text-xl font-semibold text-foreground">Hospitals</h1>
            <p className="text-sm text-muted-foreground">Register hospitals, choose the features each one can use, and keep their databases up to date.</p>
          </div>
          <Button asChild className="gap-1.5">
            <Link to="/platform/hospitals/new">
              <Plus className="h-4 w-4" />
              Register hospital
            </Link>
          </Button>
        </div>

        <div className="grid grid-cols-2 gap-3 sm:gap-4 lg:grid-cols-4">
          {tiles.map((tile) => {
            const Icon = tile.icon;
            return (
              <Card key={tile.key} className={cn('shadow-sm', tile.variant === 'alert' && 'border-destructive/50')}>
                <CardContent className="flex items-center gap-3 p-3 sm:gap-4 sm:p-4">
                  <span className={cn('flex h-10 w-10 shrink-0 items-center justify-center rounded-xl sm:h-11 sm:w-11', TILE_TINTS[tile.variant ?? 'default'])}>
                    {statsQuery.isPending ? <Loader2 className="h-5 w-5 animate-spin" /> : <Icon className="h-5 w-5" />}
                  </span>
                  <div className="flex min-w-0 flex-col">
                    <span className={cn('text-2xl font-semibold leading-tight tabular-nums', tile.variant === 'alert' ? 'text-destructive' : 'text-foreground')}>
                      {tile.value}
                    </span>
                    <span className="text-xs text-muted-foreground">{tile.label}</span>
                  </div>
                </CardContent>
              </Card>
            );
          })}
        </div>

        <Card className="overflow-hidden shadow-sm">
          <HospitalListToolbar search={search} onSearchChange={handleSearchChange} totalCount={hospitalsQuery.data?.meta.totalCount} />

          {hospitalsQuery.isPending && (
            <div className="flex items-center justify-center gap-2 border-t border-border py-16 text-sm text-muted-foreground">
              <Loader2 className="h-4 w-4 animate-spin" />
              Loading hospitals…
            </div>
          )}

          {hospitalsQuery.isError && (
            <p role="alert" className="mx-5 mb-5 rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
              Failed to load hospitals.
            </p>
          )}

          {!hospitalsQuery.isPending && !hospitalsQuery.isError && hospitalsQuery.data && hospitalsQuery.data.items.length === 0 && (
            <div className="flex flex-col items-center gap-2 border-t border-border py-16 text-center">
              <Building2 className="h-8 w-8 text-muted-foreground/60" />
              <p className="text-sm font-medium text-foreground">No hospitals found</p>
              <p className="text-sm text-muted-foreground">
                {debouncedSearch ? `No results for "${debouncedSearch}".` : 'Register the first hospital to get started.'}
              </p>
            </div>
          )}

          {!hospitalsQuery.isPending && !hospitalsQuery.isError && hospitalsQuery.data && hospitalsQuery.data.items.length > 0 && (
            <>
              <HospitalTable
                hospitals={hospitalsQuery.data.items}
                onToggleStatus={handleToggleStatus}
                isTogglingId={statusMutation.isPending ? statusMutation.variables?.id : undefined}
                onManageFeatures={setHospitalToManageFeatures}
                onMigrate={handleMigrate}
                isMigratingId={migrateMutation.isPending ? migrateMutation.variables : undefined}
              />
              {hospitalsQuery.data.meta.totalPages > 1 && (
                <div className="border-t border-border px-5 py-3">
                  <Pagination meta={hospitalsQuery.data.meta} onPageChange={setPage} />
                </div>
              )}
            </>
          )}
        </Card>
      </main>

      {hospitalToManageFeatures && (
        <HospitalFeaturesDialog hospital={hospitalToManageFeatures} onClose={() => setHospitalToManageFeatures(null)} />
      )}
    </div>
  );
}
