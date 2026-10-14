import { Pill } from 'lucide-react';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { PageBanner } from '@/components/PageBanner';
import { PeriodPicker } from '@/features/pharmacy/reports/PeriodPicker';
import { presetPeriod, type ReportPeriod } from '@/features/pharmacy/reports/period';
import { CollectionsReport, DailySalesReport, ProductSalesReport } from '@/features/pharmacy/reports/SalesReports';
import { GstReport } from '@/features/pharmacy/reports/TaxReports';
import { AccountsNavTabs } from '@/features/reports/components/AccountsNavTabs';
import { cn } from '@/lib/utils';

const VIEWS = [
  { key: 'daily', label: 'Day-wise sales' },
  { key: 'collections', label: 'Collections' },
  { key: 'gst', label: 'GST' },
  { key: 'items', label: 'Item-wise margin' },
] as const;

type View = (typeof VIEWS)[number]['key'];

/**
 * Pharmacy revenue for Accounts and Finance. Pharmacy bills are the pharmacy's own documents
 * (ADR-094) and never become hospital invoices, so they are not in the ledger or the other
 * reports here; this tab reads the same figures the pharmacy's own reports do.
 */
export default function PharmacyReportPage() {
  const [period, setPeriod] = useState<ReportPeriod>(() => presetPeriod('month'));
  const [view, setView] = useState<View>('daily');

  return (
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={Pill}
        title="Pharmacy Report"
        subtitle="Counter sales, collections, GST and margin from pharmacy bills. Pharmacy bills are kept apart from hospital invoices, so they appear here rather than in the ledger."
        backTo="/finance/accounts"
        backLabel="Back to Accounts and Finance"
      />

      <div className="flex flex-1 flex-col gap-4 p-6 lg:p-8">
        <AccountsNavTabs />

        <div className="flex flex-wrap items-center justify-between gap-3 print:hidden">
          <div className="flex overflow-hidden rounded-md border border-border text-sm font-medium">
            {VIEWS.map((option) => (
              <button key={option.key} type="button" onClick={() => setView(option.key)} className={cn('px-3 py-1.5', view === option.key ? 'bg-primary text-primary-foreground' : 'text-muted-foreground hover:bg-accent')}>
                {option.label}
              </button>
            ))}
          </div>
          <PeriodPicker value={period} onChange={setPeriod} />
        </div>

        {view === 'daily' && <DailySalesReport period={period} storeId="" />}
        {view === 'collections' && <CollectionsReport period={period} storeId="" />}
        {view === 'gst' && <GstReport period={period} storeId="" />}
        {view === 'items' && <ProductSalesReport period={period} storeId="" />}

        <p className="text-xs text-muted-foreground print:hidden">
          More pharmacy reports — sales and purchase registers, stock valuation — are under{' '}
          <Link to="/pharmacy/reports" className="text-primary hover:underline">
            Pharmacy → Reports
          </Link>
          , for users with pharmacy access.
        </p>
      </div>
    </div>
  );
}
