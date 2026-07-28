import { LayoutDashboard } from 'lucide-react';
import { DepartmentFinanceChart } from '@/features/dashboard/components/DepartmentFinanceChart';
import { DepartmentGoalsCard } from '@/features/dashboard/components/DepartmentGoalsCard';
import { MiniCalendarCard } from '@/features/dashboard/components/MiniCalendarCard';
import { MonthlyCensusChart } from '@/features/dashboard/components/MonthlyCensusChart';
import { NotificationsCard } from '@/features/dashboard/components/NotificationsCard';
import { PresentHrCard } from '@/features/dashboard/components/PresentHrCard';
import { RevenueExpenseChart } from '@/features/dashboard/components/RevenueExpenseChart';
import { SectionHeader } from '@/features/dashboard/components/SectionHeader';

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

      {/* Section 1 — Statistical Data */}
      <section>
        <SectionHeader title="Statistical Data" description="Monthly patient OP/IP census" />
        <div className="grid grid-cols-12 gap-5">
          <div className="col-span-12">
            <MonthlyCensusChart />
          </div>
        </div>
      </section>

      {/* Section 2 — Department-wise Income & Expenses */}
      <section>
        <SectionHeader title="Department-wise Income & Expenses" description="Revenue and expense by department, with chart" />
        <div className="grid grid-cols-12 gap-5">
          <div className="col-span-12">
            <DepartmentFinanceChart />
          </div>
        </div>
      </section>

      {/* Section 3 — Month-wise Income & Expense Chart */}
      <section>
        <SectionHeader title="Month-wise Income & Expense" description="Hospital-wide financial trend" />
        <div className="grid grid-cols-12 gap-5">
          <div className="col-span-12">
            <RevenueExpenseChart />
          </div>
        </div>
      </section>

      {/* Section 4 — Calendar, Notifications and Events */}
      <section>
        <SectionHeader title="Calendar – Notifications and Events" description="This month's schedule and the latest alerts" />
        <div className="grid grid-cols-12 gap-5">
          <div className="col-span-12 lg:col-span-6">
            <MiniCalendarCard />
          </div>
          <div className="col-span-12 lg:col-span-6">
            <NotificationsCard />
          </div>
        </div>
      </section>

      {/* Section 5 — Present HR */}
      <section>
        <SectionHeader title="Present HR" description="Staff attendance today" />
        <div className="grid grid-cols-12 gap-5">
          <div className="col-span-12">
            <PresentHrCard />
          </div>
        </div>
      </section>

      {/* Section 6 — Plans and Projects */}
      <section>
        <SectionHeader title="Plans and Projects – Status" description="Department goals and targets" />
        <div className="grid grid-cols-12 gap-5">
          <div className="col-span-12">
            <DepartmentGoalsCard />
          </div>
        </div>
      </section>
    </div>
  );
}
