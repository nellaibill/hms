import type { TenantListItemResponse } from '@hms/shared';
import {
  AlertTriangle,
  BadgeCheck,
  Building2,
  CheckCircle2,
  Loader2,
  Plus,
  XCircle,
} from 'lucide-react';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { useToast } from '@/components/ui/toast-context';
import { PageBanner } from '@/components/PageBanner';
import { Pagination } from '@/components/Pagination';
import { PlatformTopBar } from '@/features/platformAuth/PlatformTopBar';
import { PLATFORM_CONTAINER } from '@/features/platformAuth/platformLayout';
import {
  HospitalListToolbar,
  HospitalTable,
  useHospitalsQuery,
  useHospitalStatsQuery,
  useMigrateHospitalMutation,
  useUpdateHospitalStatusMutation,
} from '@/features/platformHospitals';
import { useDebouncedValue } from '@/hooks/useDebouncedValue';
import { cn } from '@/lib/utils';

/** Colour role of a stat card — its left accent, icon tile and progress bar. */
type StatTone = 'primary' | 'success' | 'muted' | 'warning' | 'danger';

const TONE_CLASSES: Record<StatTone, { accent: string; tile: string; bar: string }> = {
  primary: { accent: 'border-l-primary', tile: 'bg-primary/10 text-primary', bar: 'bg-primary' },
  success: { accent: 'border-l-success', tile: 'bg-success/10 text-success', bar: 'bg-success' },
  muted: {
    accent: 'border-l-border',
    tile: 'bg-muted text-muted-foreground',
    bar: 'bg-muted-foreground',
  },
  warning: { accent: 'border-l-warning', tile: 'bg-warning/15 text-warning', bar: 'bg-warning' },
  danger: {
    accent: 'border-l-destructive',
    tile: 'bg-destructive/10 text-destructive',
    bar: 'bg-destructive',
  },
};

interface StatCardProps {
  label: string;
  value: number | undefined;
  /** One line saying what the number means right now (e.g. "All clear"). */
  hint: string;
  icon: typeof Building2;
  tone: StatTone;
  /** 0–100: draws a thin bar along the bottom (the active share of all hospitals). */
  progress?: number;
  loading: boolean;
}

function StatCard({ label, value, hint, icon: Icon, tone, progress, loading }: StatCardProps) {
  const classes = TONE_CLASSES[tone];
  return (
    <Card
      className={cn('relative flex flex-col overflow-hidden border-l-4 shadow-sm', classes.accent)}
    >
      <div className="flex items-start justify-between gap-3 p-4">
        <div className="flex min-w-0 flex-col gap-1">
          <span className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
            {label}
          </span>
          <span className="text-3xl font-semibold leading-none tabular-nums text-foreground">
            {loading || value === undefined ? '–' : value}
          </span>
          <span className="text-xs text-muted-foreground">{loading ? 'Loading…' : hint}</span>
        </div>
        <span
          className={cn(
            'flex h-10 w-10 shrink-0 items-center justify-center rounded-xl',
            classes.tile,
          )}
        >
          {loading ? <Loader2 className="h-5 w-5 animate-spin" /> : <Icon className="h-5 w-5" />}
        </span>
      </div>
      {progress !== undefined && (
        <div className="mt-auto h-1 w-full bg-muted">
          <div
            className={cn('h-full transition-all', classes.bar)}
            style={{ width: `${progress}%` }}
          />
        </div>
      )}
    </Card>
  );
}

export default function PlatformDashboardPage() {
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const debouncedSearch = useDebouncedValue(search);

  const { toast } = useToast();
  const statsQuery = useHospitalStatsQuery();
  const hospitalsQuery = useHospitalsQuery({
    page,
    pageSize: 20,
    search: debouncedSearch || undefined,
  });
  const statusMutation = useUpdateHospitalStatusMutation();
  const migrateMutation = useMigrateHospitalMutation();

  const stats = statsQuery.data;
  const activeShare = stats && stats.total > 0 ? Math.round((stats.active / stats.total) * 100) : 0;
  const alerts = stats?.provisioningAlertCount ?? 0;

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
          description:
            err instanceof Error ? err.message : `Could not migrate ${hospital.hospitalName}.`,
          variant: 'error',
        }),
    });
  }

  const container = PLATFORM_CONTAINER;

  return (
    <div className="min-h-screen bg-muted/30">
      <PlatformTopBar />

      {/* Same brand banner every hospital-app page opens with, so the portal reads as part of HMS.
          The colour spans the screen; its content lines up with the cards below. */}
      <div className="bg-page-banner">
        <PageBanner
          className={container}
          icon={Building2}
          title="Hospitals"
          subtitle="Register hospitals, choose the features each one can use, and keep their databases up to date."
          rightActions={
            <Button
              asChild
              className="gap-1.5 bg-page-banner-foreground font-semibold text-page-banner shadow-sm hover:bg-page-banner-foreground/90"
            >
              <Link to="/platform/hospitals/new">
                <Plus className="h-4 w-4" />
                Register hospital
              </Link>
            </Button>
          }
        />
      </div>

      <main className={cn(container, 'flex flex-col gap-6 py-6')}>
        <div className="grid grid-cols-2 gap-3 sm:gap-4 xl:grid-cols-4">
          <StatCard
            label="Total hospitals"
            value={stats?.total}
            hint={stats ? `${stats.active} active · ${stats.inactive} inactive` : ''}
            icon={Building2}
            tone="primary"
            loading={statsQuery.isPending}
          />
          <StatCard
            label="Active"
            value={stats?.active}
            hint={`${activeShare}% of hospitals can sign in`}
            icon={CheckCircle2}
            tone="success"
            progress={activeShare}
            loading={statsQuery.isPending}
          />
          <StatCard
            label="Inactive"
            value={stats?.inactive}
            hint={
              stats && stats.inactive > 0
                ? 'Sign-in is blocked for these'
                : 'No hospital is disabled'
            }
            icon={XCircle}
            tone={stats && stats.inactive > 0 ? 'warning' : 'muted'}
            loading={statsQuery.isPending}
          />
          <StatCard
            label="Provisioning alerts"
            value={stats?.provisioningAlertCount}
            hint={alerts > 0 ? 'A failed setup may have left a database behind' : 'All clear'}
            icon={alerts > 0 ? AlertTriangle : BadgeCheck}
            tone={alerts > 0 ? 'danger' : 'success'}
            loading={statsQuery.isPending}
          />
        </div>

        <Card className="overflow-hidden shadow-sm">
          <HospitalListToolbar
            search={search}
            onSearchChange={handleSearchChange}
            totalCount={hospitalsQuery.data?.meta.totalCount}
          />

          {hospitalsQuery.isPending && (
            <div className="flex items-center justify-center gap-2 border-t border-border py-16 text-sm text-muted-foreground">
              <Loader2 className="h-4 w-4 animate-spin" />
              Loading hospitals…
            </div>
          )}

          {hospitalsQuery.isError && (
            <p
              role="alert"
              className="mx-5 mb-5 rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive"
            >
              Failed to load hospitals.
            </p>
          )}

          {!hospitalsQuery.isPending &&
            !hospitalsQuery.isError &&
            hospitalsQuery.data &&
            hospitalsQuery.data.items.length === 0 && (
              <div className="flex flex-col items-center gap-2 border-t border-border py-16 text-center">
                <Building2 className="h-8 w-8 text-muted-foreground/60" />
                <p className="text-sm font-medium text-foreground">No hospitals found</p>
                <p className="text-sm text-muted-foreground">
                  {debouncedSearch
                    ? `No results for "${debouncedSearch}".`
                    : 'Register the first hospital to get started.'}
                </p>
              </div>
            )}

          {!hospitalsQuery.isPending &&
            !hospitalsQuery.isError &&
            hospitalsQuery.data &&
            hospitalsQuery.data.items.length > 0 && (
              <>
                <HospitalTable
                  hospitals={hospitalsQuery.data.items}
                  onToggleStatus={handleToggleStatus}
                  isTogglingId={statusMutation.isPending ? statusMutation.variables?.id : undefined}
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
    </div>
  );
}
