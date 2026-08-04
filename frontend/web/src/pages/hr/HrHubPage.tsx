import { ArrowRight, CalendarCheck2, CalendarClock, CalendarDays, CalendarRange, Clock, Repeat, UsersRound } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Card, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { useShiftAssignmentsQuery } from '@/features/shiftAssignments';
import { useShiftsQuery } from '@/features/shifts';
import { useSwapRequestsQuery } from '@/features/shiftSwapRequests';
import { useStaffAvailabilityQuery } from '@/features/staffAvailability';
import { useWeeklyRostersQuery } from '@/features/weeklyRosters';

interface HubCard {
  key: string;
  label: string;
  description: string;
  icon: typeof Clock;
  path: string;
  /** undefined while a count query is loading; omitted entirely (no badge) for cards that
   * aren't a record count, e.g. Monthly Calendar (a filtered view, not its own resource). */
  count?: number;
}

export default function HrHubPage() {
  const shiftsCount = useShiftsQuery({ page: 1, pageSize: 1 });
  const staffAvailabilityCount = useStaffAvailabilityQuery({ page: 1, pageSize: 1 });
  const weeklyRostersCount = useWeeklyRostersQuery({ page: 1, pageSize: 1 });
  const shiftAssignmentsCount = useShiftAssignmentsQuery({ page: 1, pageSize: 1 });
  const swapRequestsCount = useSwapRequestsQuery({ page: 1, pageSize: 1 });

  // Extended with a card per phase as each Duty Roster area ships (Monthly Calendar).
  const cards: HubCard[] = [
    {
      key: 'shifts',
      label: 'Shift Management',
      description: 'Define shift codes, timings, breaks, and grace periods.',
      icon: Clock,
      path: '/admin/hr/shifts',
      count: shiftsCount.data?.meta.totalCount,
    },
    {
      key: 'staff-availability',
      label: 'Staff Availability',
      description: 'Track when staff are available or unavailable, and why.',
      icon: CalendarClock,
      path: '/admin/hr/staff-availability',
      count: staffAvailabilityCount.data?.meta.totalCount,
    },
    {
      key: 'weekly-rosters',
      label: 'Weekly Roster',
      description: 'Manage per-department weekly roster headers and publish state.',
      icon: CalendarRange,
      path: '/admin/hr/weekly-rosters',
      count: weeklyRostersCount.data?.meta.totalCount,
    },
    {
      key: 'shift-assignments',
      label: 'Shift Assignments',
      description: 'Assign staff to shifts on specific roster dates.',
      icon: CalendarCheck2,
      path: '/admin/hr/shift-assignments',
      count: shiftAssignmentsCount.data?.meta.totalCount,
    },
    {
      key: 'shift-swap-requests',
      label: 'Shift Swap Requests',
      description: 'Requests for staff to swap shift assignments.',
      icon: Repeat,
      path: '/admin/hr/shift-swap-requests',
      count: swapRequestsCount.data?.meta.totalCount,
    },
    {
      key: 'monthly-calendar',
      label: 'Monthly Calendar',
      description: 'Weekly rosters whose week falls within a selected month.',
      icon: CalendarDays,
      path: '/admin/hr/monthly-calendar',
    },
  ];

  return (
    <div className="flex flex-1 flex-col">
      <div className="flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        <div className="flex items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
            <UsersRound className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight">Human Resource Management (HR)</h1>
        </div>
        <p className="max-w-2xl text-sm text-page-banner-foreground/85">
          Staff directory, roster/shift assignment, leave management, and credentialing.
        </p>
      </div>

      <div className="flex flex-1 flex-col gap-8 p-6 lg:p-8">
        <section className="flex flex-col gap-3">
          <h2 className="text-sm font-semibold uppercase tracking-wide text-muted-foreground">Duty Roster</h2>
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {cards.map((card) => {
              const Icon = card.icon;
              return (
                <Link key={card.key} to={card.path} className="block">
                  <Card className="h-full transition-all hover:border-primary/40 hover:bg-accent/40 hover:shadow-soft-lg">
                    <CardHeader>
                      <div className="flex items-center justify-between">
                        <span className="flex h-9 w-9 items-center justify-center rounded-md bg-primary/10 text-primary">
                          <Icon className="h-4.5 w-4.5" />
                        </span>
                        <div className="flex items-center gap-2">
                          {'count' in card && (
                            <span className="rounded-full bg-muted px-2 py-0.5 text-[11px] font-medium text-muted-foreground">
                              {card.count === undefined ? '…' : `${card.count} record${card.count === 1 ? '' : 's'}`}
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
