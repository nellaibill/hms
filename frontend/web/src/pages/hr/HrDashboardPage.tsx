import { CalendarClock, CalendarX2, FileWarning, Link as LinkIcon, Loader2, UserCheck, Users, UserX } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Card, CardContent } from '@/components/ui/card';
import { useHrDashboardQuery } from '../../features/hr/dashboard';

interface StatTile {
  key: string;
  label: string;
  value: string;
  icon: typeof Users;
}

export default function HrDashboardPage() {
  const dashboardQuery = useHrDashboardQuery();
  const dashboard = dashboardQuery.data;

  const stats: StatTile[] = [
    { key: 'total-employees', label: 'Total Employees', value: dashboard ? String(dashboard.totalEmployees) : '…', icon: Users },
    { key: 'active-employees', label: 'Active Employees', value: dashboard ? String(dashboard.activeEmployees) : '…', icon: UserCheck },
    { key: 'present-today', label: 'Present Today', value: dashboard ? String(dashboard.presentToday) : '…', icon: UserCheck },
    { key: 'absent-today', label: 'Absent Today', value: dashboard ? String(dashboard.absentToday) : '…', icon: UserX },
    { key: 'on-leave-today', label: 'On Leave Today', value: dashboard ? String(dashboard.onLeaveToday) : '…', icon: CalendarX2 },
    {
      key: 'pending-leave-requests',
      label: 'Pending Leave Requests',
      value: dashboard ? String(dashboard.pendingLeaveRequests) : '…',
      icon: CalendarClock,
    },
    {
      key: 'expiring-documents',
      label: 'Expiring Documents (≤30 days)',
      value: dashboard ? String(dashboard.expiringDocuments) : '…',
      icon: FileWarning,
    },
  ];

  return (
    <div className="flex flex-1 flex-col">
      <div className="px-6 pt-4 lg:px-8">
        <Link to="/admin/hr" className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
          <LinkIcon className="h-3.5 w-3.5" />
          Back to HR
        </Link>
      </div>

      <div className="mt-3 flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        <div className="flex items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
            <Users className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight">HR Dashboard</h1>
        </div>
        <p className="max-w-2xl text-sm text-page-banner-foreground/85">
          Headcount, today's attendance, pending leave, and expiring staff documents at a glance.
        </p>
      </div>

      <div className="flex flex-1 flex-col gap-8 p-6 lg:p-8">
        <section className="flex flex-col gap-3">
          {dashboardQuery.isError && (
            <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
              Failed to load dashboard metrics.
            </p>
          )}
          <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-7">
            {stats.map((stat) => {
              const Icon = stat.icon;
              return (
                <Card key={stat.key}>
                  <CardContent className="flex flex-col gap-2 py-4">
                    <span className="flex h-8 w-8 items-center justify-center rounded-md bg-primary/10 text-primary">
                      {dashboardQuery.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Icon className="h-4 w-4" />}
                    </span>
                    <span className="text-2xl font-semibold tabular-nums text-foreground">{stat.value}</span>
                    <span className="text-xs text-muted-foreground">{stat.label}</span>
                  </CardContent>
                </Card>
              );
            })}
          </div>
        </section>
      </div>
    </div>
  );
}
