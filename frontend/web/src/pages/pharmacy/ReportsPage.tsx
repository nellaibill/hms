import { BarChart3, Boxes, CalendarDays, FileSpreadsheet, Hourglass, Landmark, PackagePlus, ReceiptText, Wallet, type LucideIcon } from 'lucide-react';
import { useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useAuth } from '@/features/auth/AuthContext';
import { PeriodPicker } from '@/features/pharmacy/reports/PeriodPicker';
import { presetPeriod, type ReportPeriod } from '@/features/pharmacy/reports/period';
import { CollectionsReport, DailySalesReport, ProductSalesReport, SalesRegisterReport } from '@/features/pharmacy/reports/SalesReports';
import { NonMovingStockReport, StockValuationReport } from '@/features/pharmacy/reports/StockReports';
import { GstReport, PurchaseRegisterReport } from '@/features/pharmacy/reports/TaxReports';
import { PharmacyPage } from '@/features/pharmacy/shell/PharmacyPage';
import { StoreSelect } from '@/features/pharmacy/stores';
import { cn } from '@/lib/utils';

interface ReportEntry {
  key: string;
  label: string;
  group: 'Sales' | 'Tax and purchases' | 'Stock';
  icon: LucideIcon;
  /** Stock reports are as of today and take no period. */
  periodic: boolean;
  /** Needs the right to see what the pharmacy pays. */
  cost?: boolean;
}

const REPORTS: ReportEntry[] = [
  { key: 'daily', label: 'Day-wise sales', group: 'Sales', icon: CalendarDays, periodic: true },
  { key: 'register', label: 'Sales register', group: 'Sales', icon: ReceiptText, periodic: true },
  { key: 'items', label: 'Item-wise sales', group: 'Sales', icon: BarChart3, periodic: true },
  { key: 'collections', label: 'Collections', group: 'Sales', icon: Wallet, periodic: true },
  { key: 'gst', label: 'GST summary', group: 'Tax and purchases', icon: Landmark, periodic: true },
  { key: 'purchases', label: 'Purchase register', group: 'Tax and purchases', icon: PackagePlus, periodic: true, cost: true },
  { key: 'valuation', label: 'Stock valuation', group: 'Stock', icon: Boxes, periodic: false },
  { key: 'non-moving', label: 'Non-moving stock', group: 'Stock', icon: Hourglass, periodic: false },
];

const GROUPS: ReportEntry['group'][] = ['Sales', 'Tax and purchases', 'Stock'];

/**
 * The pharmacy's reports, listed down the side the way billing software keeps them — pick one,
 * set the period and store once, and every report reads them. The open report is in the URL
 * (?r=), so a report can be bookmarked or sent to someone.
 */
export default function ReportsPage() {
  const { hasPermission } = useAuth();
  const canSeeCost = hasPermission('pharmacy.view-cost') || hasPermission('finance-billing.view');
  const [params, setParams] = useSearchParams();
  const [period, setPeriod] = useState<ReportPeriod>(() => presetPeriod('month'));
  const [storeId, setStoreId] = useState('');

  const available = REPORTS.filter((report) => !report.cost || canSeeCost);
  const current = available.find((report) => report.key === params.get('r')) ?? available[0];

  function open(key: string) {
    setParams((previous) => {
      const next = new URLSearchParams(previous);
      next.set('r', key);
      return next;
    });
  }

  return (
    <PharmacyPage title="Reports" subtitle="Sales, GST, purchases and stock — summed from the bills and documents themselves.">
      <div className="flex min-h-0 flex-1 flex-col gap-4 lg:flex-row">
        <nav aria-label="Reports" className="flex shrink-0 flex-col gap-3 rounded-lg border border-border bg-background p-2 print:hidden lg:w-56">
          {GROUPS.map((group) => (
            <div key={group} className="flex flex-col gap-0.5">
              <p className="px-2 pb-1 pt-1 text-[11px] font-semibold uppercase tracking-wide text-muted-foreground">{group}</p>
              {available
                .filter((report) => report.group === group)
                .map((report) => {
                  const Icon = report.icon;
                  return (
                    <button
                      key={report.key}
                      type="button"
                      onClick={() => open(report.key)}
                      aria-current={report.key === current.key ? 'page' : undefined}
                      className={cn(
                        'flex items-center gap-2 rounded-md px-2 py-1.5 text-left text-sm font-medium transition-colors',
                        report.key === current.key ? 'bg-primary/10 text-primary' : 'text-muted-foreground hover:bg-accent hover:text-foreground',
                      )}
                    >
                      <Icon className="h-4 w-4 shrink-0" />
                      {report.label}
                    </button>
                  );
                })}
            </div>
          ))}
          <p className="mt-auto flex items-start gap-1.5 px-2 pb-1 text-[11px] text-muted-foreground">
            <FileSpreadsheet className="mt-px h-3.5 w-3.5 shrink-0" />
            Every report downloads to Excel and prints as shown.
          </p>
        </nav>

        <div className="flex min-w-0 flex-1 flex-col gap-3">
          <div className="flex flex-wrap items-end gap-3 rounded-lg border border-border bg-background px-3 py-2 print:hidden">
            {current.periodic && <PeriodPicker value={period} onChange={setPeriod} />}
            <StoreSelect id="report-store" value={storeId} onValueChange={setStoreId} allowAll />
          </div>

          {current.key === 'daily' && <DailySalesReport period={period} storeId={storeId} />}
          {current.key === 'register' && <SalesRegisterReport period={period} storeId={storeId} />}
          {current.key === 'items' && <ProductSalesReport period={period} storeId={storeId} />}
          {current.key === 'collections' && <CollectionsReport period={period} storeId={storeId} />}
          {current.key === 'gst' && <GstReport period={period} storeId={storeId} />}
          {current.key === 'purchases' && <PurchaseRegisterReport period={period} storeId={storeId} />}
          {current.key === 'valuation' && <StockValuationReport storeId={storeId} />}
          {current.key === 'non-moving' && <NonMovingStockReport storeId={storeId} />}
        </div>
      </div>
    </PharmacyPage>
  );
}
