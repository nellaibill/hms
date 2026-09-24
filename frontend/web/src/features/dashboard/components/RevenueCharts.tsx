import { Bar, BarChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import { formatCurrency } from '@/features/billing';
import { ChartCard } from './ChartCard';
import { ChartState } from './ChartState';
import { DASHBOARD_MONTHS, monthLabel, spansYears, useBillingDashboardQuery } from '../hooks/useDashboardData';

const compactRupees = new Intl.NumberFormat('en-IN', { notation: 'compact', maximumFractionDigits: 1 });

const tooltipStyle = {
  background: 'hsl(var(--popover))',
  border: '1px solid hsl(var(--border))',
  borderRadius: 8,
  fontSize: 12,
  color: 'hsl(var(--popover-foreground))',
};

const BILLING_TYPE_LABELS: Record<string, string> = {
  Consultation: 'Consultation',
  Laboratory: 'Laboratory',
  Radiology: 'Radiology',
  Procedure: 'Procedures',
  Injection: 'Injections',
  File: 'File charges',
  Pharmacy: 'Pharmacy',
  InpatientCharge: 'Inpatient charges',
};

/**
 * This month's billed revenue split by service (billing type) — real invoice data, replacing the
 * old hard-coded "Department-wise Income & Expenses" (regression report DASH-01). There's no
 * expense ledger in the system yet, so no expense figures are shown rather than invented ones.
 */
export function RevenueByServiceChart() {
  const query = useBillingDashboardQuery(true);
  const data = (query.data?.currentMonthByType ?? []).map((row) => ({ service: BILLING_TYPE_LABELS[row.billingType] ?? row.billingType, revenue: row.amount }));

  return (
    <ChartCard title="Revenue by Service" description="This month, billed (excluding voided invoices)">
      {query.isPending ? (
        <ChartState status="loading" />
      ) : query.isError ? (
        <ChartState status="error" />
      ) : data.length === 0 ? (
        <ChartState status="empty" emptyMessage="Nothing billed yet this month." />
      ) : (
        <ResponsiveContainer width="100%" height="100%">
          <BarChart data={data} margin={{ top: 8, right: 8, left: 0, bottom: 0 }}>
            <CartesianGrid vertical={false} stroke="hsl(var(--border))" />
            <XAxis dataKey="service" tickLine={false} axisLine={false} interval={0} tick={{ fill: 'hsl(var(--muted-foreground))', fontSize: 12 }} />
            <YAxis tickLine={false} axisLine={false} tick={{ fill: 'hsl(var(--muted-foreground))', fontSize: 12 }} tickFormatter={(value: number) => compactRupees.format(value)} width={44} />
            <Tooltip cursor={{ fill: 'hsl(var(--accent))' }} contentStyle={tooltipStyle} formatter={(value) => formatCurrency(Number(value ?? 0))} />
            <Bar dataKey="revenue" name="Revenue" fill="hsl(var(--primary))" radius={[4, 4, 0, 0]} maxBarSize={40} />
          </BarChart>
        </ResponsiveContainer>
      )}
    </ChartCard>
  );
}

/** Billed revenue per month for the last six months — real invoice data (DASH-01). */
export function MonthlyRevenueChart() {
  const query = useBillingDashboardQuery(true);
  const series = query.data?.monthlyRevenue ?? [];
  const multiYear = spansYears(series);
  const data = series.map((month) => ({ month: monthLabel(month, multiYear), revenue: month.value }));

  return (
    <ChartCard title="Month-wise Revenue" description={`Last ${DASHBOARD_MONTHS} months, billed. Expense tracking isn't available in the system yet.`}>
      {query.isPending ? (
        <ChartState status="loading" />
      ) : query.isError ? (
        <ChartState status="error" />
      ) : (
        <ResponsiveContainer width="100%" height="100%">
          <BarChart data={data} margin={{ top: 8, right: 8, left: 0, bottom: 0 }}>
            <CartesianGrid vertical={false} stroke="hsl(var(--border))" />
            <XAxis dataKey="month" tickLine={false} axisLine={false} tick={{ fill: 'hsl(var(--muted-foreground))', fontSize: 12 }} />
            <YAxis tickLine={false} axisLine={false} tick={{ fill: 'hsl(var(--muted-foreground))', fontSize: 12 }} tickFormatter={(value: number) => compactRupees.format(value)} width={44} />
            <Tooltip cursor={{ fill: 'hsl(var(--accent))' }} contentStyle={tooltipStyle} formatter={(value) => formatCurrency(Number(value ?? 0))} />
            <Bar dataKey="revenue" name="Revenue" fill="hsl(var(--primary))" radius={[4, 4, 0, 0]} maxBarSize={28} />
          </BarChart>
        </ResponsiveContainer>
      )}
    </ChartCard>
  );
}
