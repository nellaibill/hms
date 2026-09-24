import { LayoutDashboard } from 'lucide-react';
import { PageBanner } from '@/components/PageBanner';
import { MiniCalendarCard } from '@/features/dashboard/components/MiniCalendarCard';
import { MonthlyCensusChart } from '@/features/dashboard/components/MonthlyCensusChart';
import { NotificationsCard } from '@/features/dashboard/components/NotificationsCard';
import { PresentHrCard } from '@/features/dashboard/components/PresentHrCard';
import { MonthlyRevenueChart, RevenueByServiceChart } from '@/features/dashboard/components/RevenueCharts';
import { SectionHeader } from '@/features/dashboard/components/SectionHeader';
import { useAuth } from '@/features/auth/AuthContext';

const today = new Date().toLocaleDateString('en-IN', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' });

export default function DashboardPage() {
  const { hasPermission, hasFeature } = useAuth();
  const canViewPatients = hasPermission('patient-management.view');
  // IP admissions come from the IPD module — only shown to viewers who can see IPD.
  const canViewIpd = hasPermission('clinical-care.view') && hasFeature('ipd');
  const canViewFinance = hasPermission('finance-billing.view');
  const canViewWorkforce = hasPermission('workforce-admin.view');
  return (
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={LayoutDashboard}
        title="Executive Dashboard"
        subtitle="Live overview across every department, updated in real time."
        rightActions={
          <span className="hidden rounded-full bg-page-banner-foreground/15 px-3 py-1 text-xs font-medium sm:inline-block">
            {today}
          </span>
        }
      />

      <div className="flex flex-1 flex-col gap-8 p-6 lg:p-8">
      {/* Section 1 — Statistical Data (patient census — hidden without patient-management.view) */}
      {canViewPatients && (
        <section>
          <SectionHeader title="Statistical Data" description={canViewIpd ? 'Monthly patient OP/IP census' : 'Monthly OP census'} />
          <div className="grid grid-cols-12 gap-5">
            <div className="col-span-12">
              <MonthlyCensusChart showIp={canViewIpd} />
            </div>
          </div>
        </section>
      )}

      {/* Sections 2-3 — Finance charts, hidden without finance-billing.view (e.g. a Doctor
          shouldn't see hospital-wide revenue/expense figures on their landing page). */}
      {canViewFinance && (
        <section>
          <SectionHeader title="Revenue by Service" description="This month's billed revenue by service" />
          <div className="grid grid-cols-12 gap-5">
            <div className="col-span-12">
              <RevenueByServiceChart />
            </div>
          </div>
        </section>
      )}

      {canViewFinance && (
        <section>
          <SectionHeader title="Month-wise Revenue" description="Hospital-wide billed revenue trend" />
          <div className="grid grid-cols-12 gap-5">
            <div className="col-span-12">
              <MonthlyRevenueChart />
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

      </div>
    </div>
  );
}
