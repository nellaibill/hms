import type { DeletedTenantListItemResponse, TenantListItemResponse } from '@hms/shared';
import { AlertTriangle, Building2, CheckCircle2, Loader2, LogOut, ShieldCheck, Trash2, XCircle } from 'lucide-react';
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Pagination } from '@/components/Pagination';
import { ThemeToggle } from '@/components/shell/ThemeToggle';
import { usePlatformAuth } from '@/features/platformAuth/PlatformAuthContext';
import {
  DeleteHospitalDialog,
  DeletedHospitalTable,
  HospitalConfigurationDialog,
  HospitalListToolbar,
  HospitalTable,
  useDeletedHospitalsQuery,
  useHospitalsQuery,
  useHospitalStatsQuery,
  useRestoreHospitalMutation,
  useUpdateHospitalStatusMutation,
} from '@/features/platformHospitals';
import { useDebouncedValue } from '@/hooks/useDebouncedValue';

type View = 'active' | 'deleted';

interface StatTile {
  key: string;
  label: string;
  value: string;
  icon: typeof Building2;
  /** 'alert' highlights the tile in a warning color — used for the provisioning-alert
   * count, which should stand out from the ground-truth hospital counts when nonzero. */
  variant?: 'default' | 'alert';
}

export default function PlatformDashboardPage() {
  const { user, logout } = usePlatformAuth();
  const navigate = useNavigate();

  const [view, setView] = useState<View>('active');
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const debouncedSearch = useDebouncedValue(search);
  const [hospitalToDelete, setHospitalToDelete] = useState<TenantListItemResponse | null>(null);
  const [hospitalToConfigure, setHospitalToConfigure] = useState<TenantListItemResponse | null>(null);

  const statsQuery = useHospitalStatsQuery();
  const hospitalsQuery = useHospitalsQuery({ page, pageSize: 20, search: debouncedSearch || undefined });
  const deletedHospitalsQuery = useDeletedHospitalsQuery(
    { page, pageSize: 20, search: debouncedSearch || undefined },
    view === 'deleted',
  );
  const statusMutation = useUpdateHospitalStatusMutation();
  const restoreMutation = useRestoreHospitalMutation();

  const stats = statsQuery.data;
  const tiles: StatTile[] = [
    { key: 'total', label: 'Total Hospitals', value: stats ? String(stats.total) : '…', icon: Building2 },
    { key: 'active', label: 'Active', value: stats ? String(stats.active) : '…', icon: CheckCircle2 },
    { key: 'inactive', label: 'Inactive', value: stats ? String(stats.inactive) : '…', icon: XCircle },
    {
      key: 'provisioningAlerts',
      label: 'Provisioning Alerts',
      value: stats ? String(stats.provisioningAlertCount) : '…',
      icon: AlertTriangle,
      variant: stats && stats.provisioningAlertCount > 0 ? 'alert' : 'default',
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

  function handleViewChange(nextView: View) {
    setView(nextView);
    setPage(1);
  }

  function handleRestore(hospital: DeletedTenantListItemResponse) {
    restoreMutation.mutate(hospital.id);
  }

  return (
    <div className="min-h-screen bg-muted/30">
      <header className="flex items-center justify-between border-b bg-background px-6 py-4">
        <div className="flex items-center gap-2 font-semibold">
          <Building2 className="size-5 text-primary" />
          Platform Portal
        </div>
        <div className="flex items-center gap-2">
          <ThemeToggle />
          <Button variant="outline" size="sm" onClick={() => navigate('/platform/security')}>
            <ShieldCheck className="mr-2 size-4" />
            Security
          </Button>
          <Button variant="outline" size="sm" onClick={logout}>
            <LogOut className="mr-2 size-4" />
            Sign out
          </Button>
        </div>
      </header>

      <main className="flex flex-col gap-6 p-6 lg:p-8">
        <p className="text-sm text-muted-foreground">Signed in as {user?.fullName ?? user?.email}</p>

        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
          {tiles.map((tile) => {
            const Icon = tile.icon;
            const isAlert = tile.variant === 'alert';
            return (
              <Card key={tile.key} className={isAlert ? 'border-destructive/50' : undefined}>
                <CardContent className="flex flex-col gap-2 py-4">
                  <span
                    className={
                      isAlert
                        ? 'flex h-8 w-8 items-center justify-center rounded-md bg-destructive/10 text-destructive'
                        : 'flex h-8 w-8 items-center justify-center rounded-md bg-primary/10 text-primary'
                    }
                  >
                    {statsQuery.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Icon className="h-4 w-4" />}
                  </span>
                  <span className={isAlert ? 'text-2xl font-semibold tabular-nums text-destructive' : 'text-2xl font-semibold tabular-nums text-foreground'}>
                    {tile.value}
                  </span>
                  <span className="text-xs text-muted-foreground">{tile.label}</span>
                </CardContent>
              </Card>
            );
          })}
        </div>

        <div className="flex items-center gap-2">
          <Button variant={view === 'active' ? 'secondary' : 'ghost'} size="sm" onClick={() => handleViewChange('active')}>
            <Building2 className="mr-2 size-4" />
            Active Hospitals
          </Button>
          <Button variant={view === 'deleted' ? 'secondary' : 'ghost'} size="sm" onClick={() => handleViewChange('deleted')}>
            <Trash2 className="mr-2 size-4" />
            Deleted Hospitals
          </Button>
        </div>

        <HospitalListToolbar search={search} onSearchChange={handleSearchChange} />

        {view === 'active' && (
          <>
            {hospitalsQuery.isPending && (
              <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
                <Loader2 className="h-4 w-4 animate-spin" />
                Loading hospitals…
              </div>
            )}

            {hospitalsQuery.isError && (
              <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
                Failed to load hospitals.
              </p>
            )}

            {!hospitalsQuery.isPending && !hospitalsQuery.isError && hospitalsQuery.data && hospitalsQuery.data.items.length === 0 && (
              <Card className="border-dashed">
                <CardContent className="flex flex-col items-center gap-2 py-16 text-center">
                  <p className="text-sm font-medium text-foreground">No hospitals found</p>
                  <p className="text-sm text-muted-foreground">
                    {debouncedSearch ? `No results for "${debouncedSearch}".` : 'Register the first hospital to get started.'}
                  </p>
                </CardContent>
              </Card>
            )}

            {!hospitalsQuery.isPending && !hospitalsQuery.isError && hospitalsQuery.data && hospitalsQuery.data.items.length > 0 && (
              <div className="flex flex-col gap-3">
                <HospitalTable
                  hospitals={hospitalsQuery.data.items}
                  onToggleStatus={handleToggleStatus}
                  isTogglingId={statusMutation.isPending ? statusMutation.variables?.id : undefined}
                  onDelete={setHospitalToDelete}
                  onConfigure={setHospitalToConfigure}
                />
                <Pagination meta={hospitalsQuery.data.meta} onPageChange={setPage} />
              </div>
            )}
          </>
        )}

        {view === 'deleted' && (
          <>
            {deletedHospitalsQuery.isPending && (
              <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
                <Loader2 className="h-4 w-4 animate-spin" />
                Loading deleted hospitals…
              </div>
            )}

            {deletedHospitalsQuery.isError && (
              <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
                Failed to load deleted hospitals.
              </p>
            )}

            {!deletedHospitalsQuery.isPending &&
              !deletedHospitalsQuery.isError &&
              deletedHospitalsQuery.data &&
              deletedHospitalsQuery.data.items.length === 0 && (
                <Card className="border-dashed">
                  <CardContent className="flex flex-col items-center gap-2 py-16 text-center">
                    <p className="text-sm font-medium text-foreground">No deleted hospitals</p>
                  </CardContent>
                </Card>
              )}

            {!deletedHospitalsQuery.isPending &&
              !deletedHospitalsQuery.isError &&
              deletedHospitalsQuery.data &&
              deletedHospitalsQuery.data.items.length > 0 && (
                <div className="flex flex-col gap-3">
                  <DeletedHospitalTable
                    hospitals={deletedHospitalsQuery.data.items}
                    onRestore={handleRestore}
                    isRestoringId={restoreMutation.isPending ? restoreMutation.variables : undefined}
                  />
                  <Pagination meta={deletedHospitalsQuery.data.meta} onPageChange={setPage} />
                </div>
              )}
          </>
        )}
      </main>

      {hospitalToDelete && <DeleteHospitalDialog hospital={hospitalToDelete} onClose={() => setHospitalToDelete(null)} />}
      {hospitalToConfigure && (
        <HospitalConfigurationDialog hospital={hospitalToConfigure} onClose={() => setHospitalToConfigure(null)} />
      )}
    </div>
  );
}
