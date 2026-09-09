import { Area, AreaChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import { ChartCard } from '@/features/dashboard/components/ChartCard';

interface RegistrationsTrendChartProps {
  data: { month: string; count: number }[];
}

/** Same Area-chart shape/theming as the dashboard's MonthlyCensusChart, just fed real
 * registration data instead of mock data — one series (registrations), not two, since there's
 * no OP/IP split at registration time (that's a visit-level concept, not a patient one). */
export function RegistrationsTrendChart({ data }: RegistrationsTrendChartProps) {
  return (
    <ChartCard title="Registrations Over Time" description="New patients registered per month, in the selected range">
      {data.length === 0 ? (
        <div className="flex h-full items-center justify-center text-sm text-muted-foreground">No registrations in this period.</div>
      ) : (
        <ResponsiveContainer width="100%" height="100%">
          <AreaChart data={data} margin={{ top: 8, right: 8, left: -12, bottom: 0 }}>
            <defs>
              <linearGradient id="registrationsGradient" x1="0" y1="0" x2="0" y2="1">
                <stop offset="0%" stopColor="hsl(var(--primary))" stopOpacity={0.28} />
                <stop offset="100%" stopColor="hsl(var(--primary))" stopOpacity={0.02} />
              </linearGradient>
            </defs>
            <CartesianGrid vertical={false} stroke="hsl(var(--border))" />
            <XAxis dataKey="month" tickLine={false} axisLine={false} tick={{ fill: 'hsl(var(--muted-foreground))', fontSize: 12 }} />
            <YAxis tickLine={false} axisLine={false} tick={{ fill: 'hsl(var(--muted-foreground))', fontSize: 12 }} width={36} allowDecimals={false} />
            <Tooltip
              contentStyle={{
                background: 'hsl(var(--popover))',
                border: '1px solid hsl(var(--border))',
                borderRadius: 8,
                fontSize: 12,
                color: 'hsl(var(--popover-foreground))',
              }}
            />
            <Area type="monotone" dataKey="count" name="Registrations" stroke="hsl(var(--primary))" strokeWidth={2} fill="url(#registrationsGradient)" />
          </AreaChart>
        </ResponsiveContainer>
      )}
    </ChartCard>
  );
}
