import {
  ArrowRight,
  BedDouble,
  BedSingle,
  CalendarCheck2,
  CalendarPlus,
  ClipboardList,
  Grid3x3,
  HeartPulse,
  Loader2,
  Users,
} from 'lucide-react';
import { Link } from 'react-router-dom';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { useAdmissionsQuery } from '../../features/ipd/admissions';
import { useIpdDashboardQuery } from '../../features/ipd/dashboard';

interface StatTile {
  key: string;
  label: string;
  value: string;
  caption?: string;
  icon: typeof Users;
}

interface HubCard {
  key: string;
  label: string;
  description: string;
  icon: typeof Users;
  path: string;
}

export default function IpdDashboardPage() {
  const dashboardQuery = useIpdDashboardQuery();
  const wardsCard = useAdmissionsQuery({ page: 1, pageSize: 1, status: 'Admitted' });

  const dashboard = dashboardQuery.data;

  const stats: StatTile[] = [
    { key: 'total-admitted', label: 'Total Admitted', value: dashboard ? String(dashboard.totalAdmitted) : '…', icon: Users },
    { key: 'available-beds', label: 'Available Beds', value: dashboard ? String(dashboard.availableBeds) : '…', icon: BedSingle },
    { key: 'occupied-beds', label: 'Occupied Beds', value: dashboard ? String(dashboard.occupiedBeds) : '…', icon: BedDouble },
    {
      key: 'icu-occupancy',
      label: 'ICU Occupancy',
      value: dashboard ? `${dashboard.icuOccupiedBeds}/${dashboard.icuTotalBeds}` : '…',
      caption: dashboard ? `${dashboard.icuOccupancyRate}%` : undefined,
      icon: HeartPulse,
    },
    { key: 'todays-admissions', label: "Today's Admissions", value: dashboard ? String(dashboard.todaysAdmissions) : '…', icon: CalendarPlus },
    { key: 'todays-discharges', label: "Today's Discharges", value: dashboard ? String(dashboard.todaysDischarges) : '…', icon: CalendarCheck2 },
  ];

  const cards: HubCard[] = [
    {
      key: 'admissions',
      label: 'Admissions',
      description: 'New admissions, and admitted / discharged patient lists.',
      icon: ClipboardList,
      path: '/clinical/ipd/admissions',
    },
    { key: 'wards', label: 'Wards', description: 'Manage hospital wards used for inpatient admissions.', icon: BedDouble, path: '/clinical/ipd/wards' },
    { key: 'beds', label: 'Beds', description: 'Manage individual beds within each ward.', icon: BedSingle, path: '/clinical/ipd/beds' },
    {
      key: 'bed-occupancy',
      label: 'Bed Occupancy',
      description: 'Ward-by-ward view of every bed and who is in it.',
      icon: Grid3x3,
      path: '/clinical/ipd/bed-occupancy',
    },
  ];

  return (
    <div className="flex flex-1 flex-col">
      <div className="flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        <div className="flex items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
            <BedDouble className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight">In Patient Department (IPD)</h1>
        </div>
        <p className="max-w-2xl text-sm text-page-banner-foreground/85">
          Inpatient bed/ward management, admissions, and discharge workflows.
        </p>
      </div>

      <div className="flex flex-1 flex-col gap-8 p-6 lg:p-8">
        <section className="flex flex-col gap-3">
          {dashboardQuery.isError && (
            <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
              Failed to load dashboard metrics.
            </p>
          )}
          <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-6">
            {stats.map((stat) => {
              const Icon = stat.icon;
              return (
                <Card key={stat.key}>
                  <CardContent className="flex flex-col gap-2 py-4">
                    <span className="flex h-8 w-8 items-center justify-center rounded-md bg-primary/10 text-primary">
                      {dashboardQuery.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Icon className="h-4 w-4" />}
                    </span>
                    <span className="text-2xl font-semibold tabular-nums text-foreground">
                      {stat.value}
                      {stat.caption && <span className="ml-1 text-sm font-normal text-muted-foreground">({stat.caption})</span>}
                    </span>
                    <span className="text-xs text-muted-foreground">{stat.label}</span>
                  </CardContent>
                </Card>
              );
            })}
          </div>
        </section>

        <section className="flex flex-col gap-3">
          <h2 className="text-sm font-semibold uppercase tracking-wide text-muted-foreground">Manage</h2>
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
            {cards.map((card) => {
              const Icon = card.icon;
              const count = card.key === 'admissions' ? wardsCard.data?.meta.totalCount : undefined;
              return (
                <Link key={card.key} to={card.path} className="block">
                  <Card className="h-full transition-all hover:border-primary/40 hover:bg-accent/40 hover:shadow-soft-lg">
                    <CardHeader>
                      <div className="flex items-center justify-between">
                        <span className="flex h-9 w-9 items-center justify-center rounded-md bg-primary/10 text-primary">
                          <Icon className="h-4.5 w-4.5" />
                        </span>
                        <div className="flex items-center gap-2">
                          {count !== undefined && (
                            <span className="rounded-full bg-muted px-2 py-0.5 text-[11px] font-medium text-muted-foreground">
                              {count} admitted
                            </span>
                          )}
                          <ArrowRight className="h-4 w-4 text-muted-foreground" />
                        </div>
                      </div>
                      <CardTitle className="text-base">{card.label}</CardTitle>
                      <CardDescription>{card.description}</CardDescription>
                    </CardHeader>
                  </Card>
                </Link>
              );
            })}
          </div>
        </section>
      </div>
    </div>
  );
}
