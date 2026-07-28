import { LayoutDashboard } from 'lucide-react';
import { BedOccupancyCard } from '@/features/dashboard/components/BedOccupancyCard';
import { DepartmentPerformanceCard } from '@/features/dashboard/components/DepartmentPerformanceCard';
import { DoctorAvailabilityCard } from '@/features/dashboard/components/DoctorAvailabilityCard';
import { KpiCard } from '@/features/dashboard/components/KpiCard';
import { MiniCalendarCard } from '@/features/dashboard/components/MiniCalendarCard';
import { NotificationsCard } from '@/features/dashboard/components/NotificationsCard';
import {
  AmbulanceStatusList,
  AppointmentsList,
  LabPendingList,
  OtScheduleList,
  PharmacyQueueList,
  RadiologyQueueList,
} from '@/features/dashboard/components/OperationalLists';
import { OperationalWidgetCard } from '@/features/dashboard/components/OperationalWidgetCard';
import { OpIpTrendChart } from '@/features/dashboard/components/OpIpTrendChart';
import { PendingTasksCard } from '@/features/dashboard/components/PendingTasksCard';
import { RecentActivityCard } from '@/features/dashboard/components/RecentActivityCard';
import { RevenueExpenseChart } from '@/features/dashboard/components/RevenueExpenseChart';
import { SectionHeader } from '@/features/dashboard/components/SectionHeader';
import {
  ambulanceStatus,
  kpiData,
  labPending,
  operationalWidgets,
  otSchedule,
  pharmacyQueue,
  radiologyQueue,
  todaysAppointments,
} from '@/features/dashboard/mockData';

const operationalContent: Record<string, React.ReactNode> = {
  "Today's Appointments": <AppointmentsList rows={todaysAppointments} />,
  'Lab Pending': <LabPendingList rows={labPending} />,
  'Radiology Queue': <RadiologyQueueList rows={radiologyQueue} />,
  'OT Schedule': <OtScheduleList rows={otSchedule} />,
  'Ambulance Status': <AmbulanceStatusList rows={ambulanceStatus} />,
  'Pharmacy Queue': <PharmacyQueueList rows={pharmacyQueue} />,
};

const today = new Date().toLocaleDateString('en-IN', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' });

export default function DashboardPage() {
  return (
    <div className="flex flex-1 flex-col gap-8 p-6 lg:p-8">
      <div className="flex flex-wrap items-start justify-between gap-3 border-b border-border pb-4">
        <div className="flex items-start gap-3">
          <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-md bg-primary/10 text-primary">
            <LayoutDashboard className="h-5 w-5" />
          </span>
          <div>
            <h1 className="text-xl font-semibold tracking-tight text-foreground">Executive Dashboard</h1>
            <p className="mt-1 text-sm text-muted-foreground">Live overview across every department, updated in real time.</p>
          </div>
        </div>
        <span className="rounded-full bg-muted px-3 py-1 text-xs font-medium text-muted-foreground">{today}</span>
      </div>

      {/* Section 1 — Executive KPI Cards */}
      <section>
        <SectionHeader title="Executive Overview" description="Hospital-wide performance at a glance" />
        <div className="grid grid-cols-12 gap-5">
          {kpiData.map((kpi) => (
            <div key={kpi.id} className="col-span-12 sm:col-span-6 lg:col-span-4">
              <KpiCard kpi={kpi} />
            </div>
          ))}
        </div>
      </section>

      {/* Section 2 — Operational Widgets */}
      <section>
        <SectionHeader title="Operations Today" description="Live queues across clinical and support services" />
        <div className="grid grid-cols-12 gap-5">
          {operationalWidgets.map((widget) => (
            <div key={widget.title} className="col-span-12 sm:col-span-6 lg:col-span-4">
              <OperationalWidgetCard {...widget}>{operationalContent[widget.title]}</OperationalWidgetCard>
            </div>
          ))}
        </div>
      </section>

      {/* Section 3 — Analytics */}
      <section>
        <SectionHeader title="Analytics" description="Volume and financial trends" />
        <div className="grid grid-cols-12 gap-5">
          <div className="col-span-12 lg:col-span-6">
            <OpIpTrendChart />
          </div>
          <div className="col-span-12 lg:col-span-6">
            <RevenueExpenseChart />
          </div>
        </div>
      </section>

      {/* Section 4 — Hospital Overview */}
      <section>
        <SectionHeader title="Hospital Overview" description="Departments, beds, and consultant availability" />
        <div className="grid grid-cols-12 gap-5">
          <div className="col-span-12 lg:col-span-4">
            <DepartmentPerformanceCard />
          </div>
          <div className="col-span-12 lg:col-span-4">
            <BedOccupancyCard />
          </div>
          <div className="col-span-12 lg:col-span-4">
            <DoctorAvailabilityCard />
          </div>
        </div>
      </section>

      {/* Section 5 — Productivity */}
      <section>
        <SectionHeader title="Productivity" description="Your calendar, alerts, tasks, and system activity" />
        <div className="grid grid-cols-12 gap-5">
          <div className="col-span-12 lg:col-span-6">
            <MiniCalendarCard />
          </div>
          <div className="col-span-12 lg:col-span-6">
            <NotificationsCard />
          </div>
          <div className="col-span-12 lg:col-span-6">
            <PendingTasksCard />
          </div>
          <div className="col-span-12 lg:col-span-6">
            <RecentActivityCard />
          </div>
        </div>
      </section>
    </div>
  );
}
