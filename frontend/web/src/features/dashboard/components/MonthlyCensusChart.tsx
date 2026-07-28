import { Area, AreaChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import { ChartCard } from './ChartCard';
import { monthlyPatientCensus } from '../mockData';

const legend = (
  <div className="flex items-center gap-4 text-xs text-muted-foreground">
    <span className="flex items-center gap-1.5">
      <span className="h-2 w-2 rounded-full bg-primary" />
      OP
    </span>
    <span className="flex items-center gap-1.5">
      <span className="h-2 w-2 rounded-full bg-info" />
      IP
    </span>
  </div>
);

export function MonthlyCensusChart() {
  return (
    <ChartCard title="Monthly Patient OP/IP Census" description="Last 6 months" legend={legend}>
      <ResponsiveContainer width="100%" height="100%">
        <AreaChart data={monthlyPatientCensus} margin={{ top: 8, right: 8, left: -12, bottom: 0 }}>
          <defs>
            <linearGradient id="opGradient" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stopColor="hsl(var(--primary))" stopOpacity={0.28} />
              <stop offset="100%" stopColor="hsl(var(--primary))" stopOpacity={0.02} />
            </linearGradient>
            <linearGradient id="ipGradient" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stopColor="hsl(var(--info))" stopOpacity={0.28} />
              <stop offset="100%" stopColor="hsl(var(--info))" stopOpacity={0.02} />
            </linearGradient>
          </defs>
          <CartesianGrid vertical={false} stroke="hsl(var(--border))" />
          <XAxis dataKey="month" tickLine={false} axisLine={false} tick={{ fill: 'hsl(var(--muted-foreground))', fontSize: 12 }} />
          <YAxis tickLine={false} axisLine={false} tick={{ fill: 'hsl(var(--muted-foreground))', fontSize: 12 }} width={36} />
          <Tooltip
            contentStyle={{
              background: 'hsl(var(--popover))',
              border: '1px solid hsl(var(--border))',
              borderRadius: 8,
              fontSize: 12,
              color: 'hsl(var(--popover-foreground))',
            }}
          />
          <Area type="monotone" dataKey="op" name="OP" stroke="hsl(var(--primary))" strokeWidth={2} fill="url(#opGradient)" />
          <Area type="monotone" dataKey="ip" name="IP" stroke="hsl(var(--info))" strokeWidth={2} fill="url(#ipGradient)" />
        </AreaChart>
      </ResponsiveContainer>
    </ChartCard>
  );
}
