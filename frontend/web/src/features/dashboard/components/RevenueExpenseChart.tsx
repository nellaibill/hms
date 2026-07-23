import { Bar, BarChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import { ChartCard } from './ChartCard';
import { revenueExpense } from '../mockData';

const legend = (
  <div className="flex items-center gap-4 text-xs text-muted-foreground">
    <span className="flex items-center gap-1.5">
      <span className="h-2 w-2 rounded-full bg-primary" />
      Revenue
    </span>
    <span className="flex items-center gap-1.5">
      <span className="h-2 w-2 rounded-full bg-muted-foreground" />
      Expense
    </span>
  </div>
);

export function RevenueExpenseChart() {
  return (
    <ChartCard title="Revenue vs Expense" description="Last 6 months, ₹ in lakhs" legend={legend}>
      <ResponsiveContainer width="100%" height="100%">
        <BarChart data={revenueExpense} margin={{ top: 8, right: 8, left: -12, bottom: 0 }} barGap={4}>
          <CartesianGrid vertical={false} stroke="hsl(var(--border))" />
          <XAxis dataKey="month" tickLine={false} axisLine={false} tick={{ fill: 'hsl(var(--muted-foreground))', fontSize: 12 }} />
          <YAxis tickLine={false} axisLine={false} tick={{ fill: 'hsl(var(--muted-foreground))', fontSize: 12 }} width={36} />
          <Tooltip
            cursor={{ fill: 'hsl(var(--accent))' }}
            contentStyle={{
              background: 'hsl(var(--popover))',
              border: '1px solid hsl(var(--border))',
              borderRadius: 8,
              fontSize: 12,
              color: 'hsl(var(--popover-foreground))',
            }}
          />
          <Bar dataKey="revenue" name="Revenue" fill="hsl(var(--primary))" radius={[4, 4, 0, 0]} maxBarSize={28} />
          <Bar dataKey="expense" name="Expense" fill="hsl(var(--muted-foreground))" radius={[4, 4, 0, 0]} maxBarSize={28} fillOpacity={0.5} />
        </BarChart>
      </ResponsiveContainer>
    </ChartCard>
  );
}
