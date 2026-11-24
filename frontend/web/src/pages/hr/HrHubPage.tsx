import {
  ArrowRight,
  CalendarCheck2,
  CalendarClock,
  CalendarDays,
  CalendarOff,
  CalendarRange,
  Clock,
  LayoutDashboard,
  Repeat,
  Users,
  UsersRound,
} from 'lucide-react';
import { Link } from 'react-router-dom';
import { PageBanner } from '@/components/PageBanner';
import { Button } from '@/components/ui/button';
import { Card, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { useAuth } from '@/features/auth/AuthContext';
import { useAttendanceQuery } from '@/features/attendance';
import { useEmployeesQuery } from '@/features/employees';
import { useLeaveRequestsQuery } from '@/features/leaveRequests';
import { useLeaveTypesQuery } from '@/features/leaveTypes';
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
  /** The tenant feature (ADR-145) a Platform Admin can switch this card's area off with. */
  feature: string;
}

export default function HrHubPage() {
  const { hasFeature } = useAuth();
  // A switched-off part's count is never fetched — its API would refuse it anyway.
  const on = (feature: string) => ({ enabled: hasFeature(feature) });

  const shiftsCount = useShiftsQuery({ page: 1, pageSize: 1 }, on('hr.shifts'));
  const staffAvailabilityCount = useStaffAvailabilityQuery({ page: 1, pageSize: 1 }, on('hr.staff-availability'));
  const weeklyRostersCount = useWeeklyRostersQuery({ page: 1, pageSize: 1 }, on('hr.weekly-rosters'));
  const shiftAssignmentsCount = useShiftAssignmentsQuery({ page: 1, pageSize: 1 }, on('hr.shift-assignments'));
  const swapRequestsCount = useSwapRequestsQuery({ page: 1, pageSize: 1 }, on('hr.shift-swap-requests'));

  const employeesCount = useEmployeesQuery({ page: 1, pageSize: 1 }, on('hr.employees'));
  const attendanceCount = useAttendanceQuery({ page: 1, pageSize: 1 }, on('hr.attendance'));
  const leaveRequestsCount = useLeaveRequestsQuery({ page: 1, pageSize: 1 }, on('hr.leave-requests'));
  const leaveTypesCount = useLeaveTypesQuery({ page: 1, pageSize: 1 }, on('hr.leave-types'));

  // Extended with a card per phase as each Duty Roster area ships (Monthly Calendar).
  const dutyRosterCards: HubCard[] = [
    {
      key: 'shifts',
      feature: 'hr.shifts',
      label: 'Shift Management',
      description: 'Define shift codes, timings, breaks, and grace periods.',
      icon: Clock,
      path: '/admin/hr/shifts',
      count: shiftsCount.data?.meta.totalCount,
    },
    {
      key: 'staff-availability',
      feature: 'hr.staff-availability',
      label: 'Staff Availability',
      description: 'Track when staff are available or unavailable, and why.',
      icon: CalendarClock,
      path: '/admin/hr/staff-availability',
      count: staffAvailabilityCount.data?.meta.totalCount,
    },
    {
      key: 'weekly-rosters',
      feature: 'hr.weekly-rosters',
      label: 'Weekly Roster',
      description: 'Manage per-department weekly roster headers and publish state.',
      icon: CalendarRange,
      path: '/admin/hr/weekly-rosters',
      count: weeklyRostersCount.data?.meta.totalCount,
    },
    {
      key: 'shift-assignments',
      feature: 'hr.shift-assignments',
      label: 'Shift Assignments',
      description: 'Assign staff to shifts on specific roster dates.',
      icon: CalendarCheck2,
      path: '/admin/hr/shift-assignments',
      count: shiftAssignmentsCount.data?.meta.totalCount,
    },
    {
      key: 'shift-swap-requests',
      feature: 'hr.shift-swap-requests',
      label: 'Shift Swap Requests',
      description: 'Requests for staff to swap shift assignments.',
      icon: Repeat,
      path: '/admin/hr/shift-swap-requests',
      count: swapRequestsCount.data?.meta.totalCount,
    },
    {
      key: 'monthly-calendar',
      feature: 'hr.monthly-calendar',
      label: 'Monthly Calendar',
      description: 'Weekly rosters whose week falls within a selected month.',
      icon: CalendarDays,
      path: '/admin/hr/monthly-calendar',
    },
  ];

  const employeeManagementCards: HubCard[] = [
    {
      key: 'employees',
      feature: 'hr.employees',
      label: 'Employees',
      description: 'Employee directory — profiles, department/designation, and status.',
      icon: Users,
      path: '/admin/hr/employees',
      count: employeesCount.data?.meta.totalCount,
    },
    {
      key: 'attendance',
      feature: 'hr.attendance',
      label: 'Attendance',
      description: 'Daily check-in/check-out records and attendance status.',
      icon: CalendarClock,
      path: '/admin/hr/attendance',
      count: attendanceCount.data?.meta.totalCount,
    },
    {
      key: 'leave-requests',
      feature: 'hr.leave-requests',
      label: 'Leave Requests',
      description: 'Submit, approve, and reject employee leave requests.',
      icon: CalendarOff,
      path: '/admin/hr/leave-requests',
      count: leaveRequestsCount.data?.meta.totalCount,
    },
    {
      key: 'leave-types',
      feature: 'hr.leave-types',
      label: 'Leave Types',
      description: 'Configure leave types, paid status, and annual day limits.',
      icon: CalendarRange,
      path: '/admin/hr/leave-types',
      count: leaveTypesCount.data?.meta.totalCount,
    },
    {
      key: 'departments',
      feature: 'settings.reference-data',
      label: 'Departments',
      description: 'Hospital department directory — shared with Patients and Masters.',
      icon: UsersRound,
      path: '/admin/masters/department',
    },
    {
      key: 'designations',
      feature: 'settings.reference-data',
      label: 'Designations',
      description: 'Employee designation/job-title directory.',
      icon: UsersRound,
      path: '/admin/masters/designation',
    },
  ];

  function renderCard(card: HubCard) {
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
  }

  // A Platform Admin can switch each area off on its own (ADR-145); a section with nothing left
  // in it disappears too.
  const sections = [
    { title: 'Employee Management', cards: employeeManagementCards.filter((card) => hasFeature(card.feature)) },
    { title: 'Duty Roster', cards: dutyRosterCards.filter((card) => hasFeature(card.feature)) },
  ].filter((section) => section.cards.length > 0);

  return (
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={UsersRound}
        title="Human Resource Management (HR)"
        subtitle={
          <>
            <span className="block">Staff directory, roster/shift assignment, leave management, and credentialing.</span>
            {hasFeature('hr.dashboard') && (
              <Button asChild variant="secondary" size="sm" className="mt-3">
                <Link to="/admin/hr/dashboard">
                  <LayoutDashboard className="h-4 w-4" />
                  Open HR Dashboard
                </Link>
              </Button>
            )}
          </>
        }
        className="gap-3"
      />

      <div className="flex flex-1 flex-col gap-8 p-6 lg:p-8">
        {sections.map((section) => (
          <section key={section.title} className="flex flex-col gap-3">
            <h2 className="text-sm font-semibold uppercase tracking-wide text-muted-foreground">{section.title}</h2>
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">{section.cards.map(renderCard)}</div>
          </section>
        ))}
        {sections.length === 0 && (
          <p className="rounded-lg border border-dashed border-border p-8 text-center text-sm text-muted-foreground">
            No HR areas are switched on for your hospital. Contact your Platform Admin to turn them on.
          </p>
        )}
      </div>
    </div>
  );
}
