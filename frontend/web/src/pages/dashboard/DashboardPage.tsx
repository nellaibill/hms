import { LayoutDashboard } from 'lucide-react';
import { DepartmentFinanceChart } from '@/features/dashboard/components/DepartmentFinanceChart';
import { DepartmentGoalsCard } from '@/features/dashboard/components/DepartmentGoalsCard';
import { MiniCalendarCard } from '@/features/dashboard/components/MiniCalendarCard';
import { MonthlyCensusChart } from '@/features/dashboard/components/MonthlyCensusChart';
import { NotificationsCard } from '@/features/dashboard/components/NotificationsCard';
import { PresentHrCard } from '@/features/dashboard/components/PresentHrCard';
import { RevenueExpenseChart } from '@/features/dashboard/components/RevenueExpenseChart';
import { SectionHeader } from '@/features/dashboard/components/SectionHeader';
import { useAuth } from '@/features/auth/AuthContext';

const today = new Date().toLocaleDateString('en-IN', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' });

export default function DashboardPage() {
  const { hasPermission } = useAuth();
  const canViewPatients = hasPermission('patient-management.view');
  const canViewFinance = hasPermission('finance-billing.view');
  const canViewWorkforce = hasPermission('workforce-admin.view');
  return (
    <div className="flex flex-1 flex-col">
      {/* Centered, brand-colored banner — matches the Page banner style used
          across module pages (Theme & Branding → Section headers). */}
      <div className="relative flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        <span className="absolute right-6 top-5 hidden rounded-full bg-page-banner-foreground/15 px-3 py-1 text-xs font-medium sm:inline-block">
          {today}
        </span>
        <div className="flex items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
            <LayoutDashboard className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight">Executive Dashboard</h1>
        </div>
        <p className="max-w-2xl text-sm text-page-banner-foreground/85">Live overview across every department, updated in real time.</p>
      </div>

      <div className="flex flex-1 flex-col gap-8 p-6 lg:p-8">
      {/* Section 1 — Statistical Data (patient census — hidden without patient-management.view) */}
      {canViewPatients && (
        <section>
          <SectionHeader title="Statistical Data" description="Monthly patient OP/IP census" />
          <div className="grid grid-cols-12 gap-5">
            <div className="col-span-12">
              <MonthlyCensusChart />
            </div>
          </div>
        </section>
      )}

      {/* Sections 2-3 — Finance charts, hidden without finance-billing.view (e.g. a Doctor
          shouldn't see hospital-wide revenue/expense figures on their landing page). */}
      {canViewFinance && (
        <section>
          <SectionHeader title="Department-wise Income & Expenses" description="Revenue and expense by department, with chart" />
          <div className="grid grid-cols-12 gap-5">
            <div className="col-span-12">
              <DepartmentFinanceChart />
            </div>
          </div>
        </section>
      )}

      {canViewFinance && (
        <section>
          <SectionHeader title="Month-wise Income & Expense" description="Hospital-wide financial trend" />
          <div className="grid grid-cols-12 gap-5">
            <div className="col-span-12">
              <RevenueExpenseChart />
            </div>
          </div>
        </section>
      )}

      {/* Section 4 — Calendar, Notifications and Events (always visible — general-purpose, no module-specific permission) */}
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

      {/* Section 5 — Present HR, hidden without workforce-admin.view */}
      {canViewWorkforce && (
        <section>
          <SectionHeader title="Present HR" description="Staff attendance today" />
          <div className="grid grid-cols-12 gap-5">
            <div className="col-span-12">
              <PresentHrCard />
            </div>
          </div>
        </section>
      )}

      {/* Section 6 — Plans and Projects (always visible — general hospital-wide goals, no single owning permission) */}
      <section>
        <SectionHeader title="Plans and Projects – Status" description="Department goals and targets" />
        <div className="grid grid-cols-12 gap-5">
          <div className="col-span-12">
            <DepartmentGoalsCard />
          </div>
        </div>
      </section>
      </div>
    </div>
  );
}
