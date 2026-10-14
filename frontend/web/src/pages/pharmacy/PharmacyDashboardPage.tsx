import type { SalesFigures } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import {
  AlertTriangle,
  BarChart3,
  Boxes,
  CalendarClock,
  ClipboardList,
  IndianRupee,
  Loader2,
  PackageMinus,
  PackagePlus,
  Percent,
  ReceiptText,
  ShoppingCart,
  Stethoscope,
  Truck,
  Undo2,
  Wallet,
} from 'lucide-react';
import { useState } from 'react';
import type { LucideIcon } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Bar, BarChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import { Button } from '@/components/ui/button';
import { useAuth } from '@/features/auth/AuthContext';
import { dateLabel, quantity, rupees, todayIso } from '@/features/pharmacy/common/format';
import { PAYMENT_METHOD_LABELS } from '@/features/pharmacy/common/labels';
import { StatTile } from '@/features/pharmacy/reports/StatTile';
import { PharmacyPage } from '@/features/pharmacy/shell/PharmacyPage';
import { StoreSelect } from '@/features/pharmacy/stores';
import { cn } from '@/lib/utils';
import { pharmacyApi } from '@/services/apiClient';

const compactRupees = new Intl.NumberFormat('en-IN', { notation: 'compact', maximumFractionDigits: 1 });
const tooltipStyle = { background: 'hsl(var(--popover))', border: '1px solid hsl(var(--border))', borderRadius: 8, fontSize: 12, color: 'hsl(var(--popover-foreground))' };

/** "+12% on yesterday", or nothing when there is no yesterday to compare with. */
function versus(today: number, yesterday: number): string | undefined {
  if (yesterday <= 0) return undefined;
  const change = ((today - yesterday) / yesterday) * 100;
  return `${change >= 0 ? '+' : ''}${change.toFixed(0)}% on yesterday`;
}

function Panel({ title, action, children }: { title: string; action?: React.ReactNode; children: React.ReactNode }) {
  return (
    <section className="flex min-w-0 flex-col gap-2 rounded-lg border border-border bg-background p-3">
      <div className="flex items-center justify-between gap-2">
        <h2 className="text-sm font-semibold text-foreground">{title}</h2>
        {action}
      </div>
      {children}
    </section>
  );
}

type Tone = 'default' | 'warning' | 'destructive';

const ROW_TONE: Record<Tone, string> = {
  default: 'text-foreground',
  warning: 'text-warning',
  destructive: 'text-destructive',
};

/** One line of a panel: what it is, how many, and a link to deal with it. */
function AlertRow({ icon: Icon, label, value, hint, tone = 'default', to }: { icon: LucideIcon; label: string; value: React.ReactNode; hint?: string; tone?: Tone; to: string }) {
  return (
    <Link to={to} className="flex items-center gap-2.5 rounded-md px-2 py-1.5 transition-colors hover:bg-accent">
      <Icon className={cn('h-4 w-4 shrink-0', tone === 'default' ? 'text-muted-foreground' : ROW_TONE[tone])} />
      <span className="min-w-0 flex-1">
        <span className="block text-sm text-foreground">{label}</span>
        {hint && <span className="block text-xs text-muted-foreground">{hint}</span>}
      </span>
      <span className={cn('text-base font-semibold tabular-nums', ROW_TONE[tone])}>{value}</span>
    </Link>
  );
}

function FigureRow({ label, figures }: { label: string; figures: SalesFigures }) {
  return (
    <tr>
      <td className="py-1.5 pr-3 font-medium text-foreground">{label}</td>
      <td className="py-1.5 pr-3 text-right tabular-nums">{figures.bills}</td>
      <td className="py-1.5 pr-3 text-right tabular-nums">{rupees(figures.netSales)}</td>
      <td className="py-1.5 pr-3 text-right tabular-nums">{rupees(figures.collected)}</td>
      {figures.margin != null && <td className="py-1.5 text-right tabular-nums">{rupees(figures.margin)}</td>}
    </tr>
  );
}

/**
 * The pharmacy's landing page: how today is going against yesterday and the month, the last
 * fortnight at a glance, what on the shelf needs attention, and what is waiting to be done —
 * each figure a link to the screen that deals with it.
 */
export default function PharmacyDashboardPage() {
  const { hasPermission } = useAuth();
  const canSell = hasPermission('pharmacy.create');
  const [storeId, setStoreId] = useState('');

  const { data, isPending, isError } = useQuery({
    queryKey: ['pharmacy', 'dashboard', storeId],
    queryFn: () => pharmacyApi.getDashboard(storeId || undefined),
    refetchInterval: 60_000,
  });

  // Today's OPD prescriptions not yet billed — read from the same queue the Prescriptions screen shows.
  const today = todayIso();
  const { data: prescriptions } = useQuery({
    queryKey: ['pharmacy', 'dashboard', 'prescriptions', today],
    queryFn: () => pharmacyApi.getPrescriptionQueue(today, today),
    refetchInterval: 60_000,
  });
  const waitingPrescriptions = prescriptions?.filter((item) => item.bills.length === 0).length;

  const trend = (data?.trend ?? []).map((day) => ({ day: dateLabel(day.date).slice(0, 6), sales: day.netSales, collected: day.collected }));
  const withCost = data?.today.margin != null;

  return (
    <PharmacyPage
      title="Pharmacy dashboard"
      subtitle={data ? `${dateLabel(data.date)} · refreshes every minute` : 'Today at a glance.'}
      actions={
        <>
          <StoreSelect id="dashboard-store" value={storeId} onValueChange={setStoreId} allowAll />
          {canSell && (
            <>
              <Button size="sm" variant="outline" asChild>
                <Link to="/pharmacy/purchases/new">
                  <PackagePlus className="h-4 w-4" />
                  New purchase
                </Link>
              </Button>
              <Button size="sm" asChild>
                <Link to="/pharmacy/counter">
                  <ShoppingCart className="h-4 w-4" />
                  Billing counter
                </Link>
              </Button>
            </>
          )}
        </>
      }
    >
      {isPending && (
        <div className="flex items-center justify-center gap-2 py-24 text-sm text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" />
          Loading today's figures…
        </div>
      )}
      {isError && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          The dashboard could not be loaded.
        </p>
      )}

      {data && (
        <>
          <div className="grid grid-cols-2 gap-2 md:grid-cols-3 2xl:grid-cols-6">
            <StatTile label="Today's sales" value={rupees(data.today.netSales)} hint={versus(data.today.netSales, data.yesterday.netSales) ?? 'Net of returns'} tone="success" icon={IndianRupee} to="/pharmacy/reports?r=daily" />
            <StatTile label="Bills today" value={data.today.bills} hint={`${data.yesterday.bills} yesterday`} icon={ReceiptText} to="/pharmacy/sales" />
            <StatTile label="Collected today" value={rupees(data.today.collected)} hint={data.todayCollections.map((mode) => `${PAYMENT_METHOD_LABELS[mode.method]} ${compactRupees.format(mode.amount)}`).join(' · ') || 'Nothing yet'} icon={Wallet} to="/pharmacy/reports?r=collections" />
            <StatTile label="Returns today" value={rupees(data.today.returnAmount)} tone={data.today.returnAmount > 0 ? 'warning' : 'default'} icon={Undo2} to="/pharmacy/sales" />
            {withCost ? (
              <StatTile label="Margin today" value={rupees(data.today.margin)} hint={`${rupees(data.monthToDate.margin)} this month`} tone="success" icon={Percent} to="/pharmacy/reports?r=items" />
            ) : (
              <StatTile label="Bills this month" value={data.monthToDate.bills} icon={ReceiptText} />
            )}
            <StatTile label="This month" value={rupees(data.monthToDate.netSales)} hint={`${data.monthToDate.bills} bills`} icon={BarChart3} to="/pharmacy/reports?r=daily" />
          </div>

          <div className="grid gap-4 xl:grid-cols-3">
            <div className="xl:col-span-2">
              <Panel title="Last 14 days" action={<Link to="/pharmacy/reports?r=daily" className="text-xs font-medium text-primary hover:underline">Day-wise report</Link>}>
                <div className="h-60">
                  <ResponsiveContainer width="100%" height="100%">
                    <BarChart data={trend} margin={{ top: 8, right: 8, left: 0, bottom: 0 }}>
                      <CartesianGrid vertical={false} stroke="hsl(var(--border))" />
                      <XAxis dataKey="day" tickLine={false} axisLine={false} tick={{ fill: 'hsl(var(--muted-foreground))', fontSize: 11 }} />
                      <YAxis tickLine={false} axisLine={false} tick={{ fill: 'hsl(var(--muted-foreground))', fontSize: 11 }} tickFormatter={(value: number) => compactRupees.format(value)} width={44} />
                      <Tooltip cursor={{ fill: 'hsl(var(--accent))' }} contentStyle={tooltipStyle} formatter={(value) => rupees(Number(value ?? 0))} />
                      <Bar dataKey="sales" name="Net sales" fill="hsl(var(--primary))" radius={[3, 3, 0, 0]} maxBarSize={26} />
                      <Bar dataKey="collected" name="Collected" fill="hsl(var(--primary) / 0.35)" radius={[3, 3, 0, 0]} maxBarSize={26} />
                    </BarChart>
                  </ResponsiveContainer>
                </div>
              </Panel>
            </div>

            <Panel title="Sales so far">
              <table className="w-full text-sm">
                <thead className="text-left text-[11px] font-medium uppercase tracking-wide text-muted-foreground">
                  <tr>
                    <th className="pb-1 pr-3" />
                    <th className="pb-1 pr-3 text-right">Bills</th>
                    <th className="pb-1 pr-3 text-right">Net sales</th>
                    <th className="pb-1 pr-3 text-right">Collected</th>
                    {withCost && <th className="pb-1 text-right">Margin</th>}
                  </tr>
                </thead>
                <tbody className="divide-y divide-border">
                  <FigureRow label="Today" figures={data.today} />
                  <FigureRow label="Yesterday" figures={data.yesterday} />
                  <FigureRow label="This month" figures={data.monthToDate} />
                </tbody>
              </table>
              <p className="text-xs text-muted-foreground">Collected is money taken at the counter; inpatient and credit bills are collected later.</p>
            </Panel>
          </div>

          <div className="grid gap-4 lg:grid-cols-2 xl:grid-cols-3">
            <Panel title="On the shelf" action={<Link to="/pharmacy/stock" className="text-xs font-medium text-primary hover:underline">Current stock</Link>}>
              <div className="flex flex-col divide-y divide-border">
                <AlertRow label="Low stock" hint="At or below the reorder level" value={data.stock.lowStock} tone={data.stock.lowStock > 0 ? 'warning' : 'default'} icon={PackageMinus} to="/pharmacy/reorder" />
                <AlertRow label="Out of stock" value={data.stock.outOfStock} tone={data.stock.outOfStock > 0 ? 'destructive' : 'default'} icon={AlertTriangle} to="/pharmacy/reorder" />
                <AlertRow label="Expired batches on the shelf" hint={data.stock.expiredValue ? `${rupees(data.stock.expiredValue)} at cost` : undefined} value={data.stock.expiredBatches} tone={data.stock.expiredBatches > 0 ? 'destructive' : 'default'} icon={CalendarClock} to="/pharmacy/expiry" />
                <AlertRow label={`Expiring within ${data.stock.expiryAlertDays} days`} value={data.stock.expiringSoonBatches} tone={data.stock.expiringSoonBatches > 0 ? 'warning' : 'default'} icon={CalendarClock} to="/pharmacy/expiry" />
              </div>
              <p className="text-xs text-muted-foreground">
                {data.stock.productsInStock} products in stock{data.stock.stockValue != null ? ` · worth ${rupees(data.stock.stockValue)} at cost` : ''}
              </p>
            </Panel>

            <Panel title="Waiting to be done">
              <div className="flex flex-col divide-y divide-border">
                <AlertRow label="IP indents to issue" value={data.pending.pendingIndents} tone={data.pending.pendingIndents > 0 ? 'warning' : 'default'} icon={ClipboardList} to="/pharmacy/indents" />
                <AlertRow label="OPD prescriptions not yet billed" hint="Today" value={waitingPrescriptions ?? '—'} tone={waitingPrescriptions ? 'warning' : 'default'} icon={Stethoscope} to="/pharmacy/prescriptions" />
                <AlertRow label="Purchase orders awaited" value={data.pending.openPurchaseOrders} icon={Truck} to="/pharmacy/purchase-orders" />
                <AlertRow label="Customer dues" hint={`${data.pending.billsWithDues} ${data.pending.billsWithDues === 1 ? 'bill' : 'bills'}, not counting IP credit`} value={rupees(data.pending.customerDues)} tone={data.pending.customerDues > 0 ? 'warning' : 'default'} icon={Wallet} to="/pharmacy/sales" />
                {data.pending.supplierPayable != null && <AlertRow label="Owed to suppliers" value={rupees(data.pending.supplierPayable)} icon={Truck} to="/pharmacy/suppliers" />}
              </div>
            </Panel>

            <Panel title="Best sellers, last 30 days" action={<Link to="/pharmacy/reports?r=items" className="text-xs font-medium text-primary hover:underline">Item-wise report</Link>}>
              {data.topProducts.length === 0 ? (
                <p className="py-6 text-center text-sm text-muted-foreground">Nothing sold in the last 30 days.</p>
              ) : (
                <ol className="flex flex-col divide-y divide-border text-sm">
                  {data.topProducts.map((product, index) => (
                    <li key={product.productId} className="flex items-center gap-2 py-1.5">
                      <span className="w-4 shrink-0 text-xs text-muted-foreground">{index + 1}</span>
                      <span className="min-w-0 flex-1">
                        <span className="block truncate font-medium text-foreground" title={product.productName}>
                          {product.productName}
                        </span>
                        <span className="block text-xs text-muted-foreground">
                          {quantity(product.netQuantity)} units · {product.bills} {product.bills === 1 ? 'bill' : 'bills'}
                        </span>
                      </span>
                      <span className="shrink-0 text-right tabular-nums">{rupees(product.amount)}</span>
                    </li>
                  ))}
                </ol>
              )}
            </Panel>
          </div>

          <p className="flex items-center gap-1.5 text-xs text-muted-foreground">
            <Boxes className="h-3.5 w-3.5" />
            Figures are summed from the bills, returns and stock as they stand.
          </p>
        </>
      )}
    </PharmacyPage>
  );
}
