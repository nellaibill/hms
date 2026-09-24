import { Area, AreaChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import { ChartCard } from './ChartCard';
import { ChartState } from './ChartState';
import { DASHBOARD_MONTHS, monthLabel, spansYears, useMonthlyAdmissionsQuery, useMonthlyOpVisitsQuery } from '../hooks/useDashboardData';

const compactNumber = new Intl.NumberFormat('en-IN', { notation: 'compact', maximumFractionDigits: 1 });

/**
 * Real monthly OP visits (Patients) and IP admissions (IPD) for the last six months — this used
 * to render hard-coded mock numbers under a "live, real time" banner (regression report
 * DASH-01). IP is shown only to viewers who can see IPD (clinical-care.view + the ipd module).
 */
export function MonthlyCensusChart({ showIp }: { showIp: boolean }) {
  const opQuery = useMonthlyOpVisitsQuery(true);
  const ipQuery = useMonthlyAdmissionsQuery(showIp);

  const op = opQuery.data ?? [];
  const ip = showIp ? (ipQuery.data ?? []) : [];
  const multiYear = spansYears(op);
  const data = op.map((month, index) => ({ month: monthLabel(month, multiYear), op: month.value, ip: ip[index]?.value ?? 0 }));

  const legend = (
    <div className="flex items-center gap-4 text-xs text-muted-foreground">
      <span className="flex items-center gap-1.5">
        <span className="h-2 w-2 rounded-full bg-primary" />
        OP visits
      </span>
      {showIp && (
        <span className="flex items-center gap-1.5">
          <span className="h-2 w-2 rounded-full bg-success" />
          IP admissions
        </span>
      )}
    </div>
  );

  const isLoading = opQuery.isPending || (showIp && ipQuery.isPending);
  const isError = opQuery.isError || (showIp && ipQuery.isError);

  return (
    <ChartCard title={showIp ? 'Monthly Patient OP/IP Census' : 'Monthly OP Census'} description={`Last ${DASHBOARD_MONTHS} months`} legend={legend}>
      {isLoading ? (
        <ChartState status="loading" />
      ) : isError ? (
        <ChartState status="error" />
      ) : (
        <ResponsiveContainer width="100%" height="100%">
          <AreaChart data={data} margin={{ top: 8, right: 8, left: 0, bottom: 0 }}>
            <defs>
              <linearGradient id="opGradient" x1="0" y1="0" x2="0" y2="1">
                <stop offset="0%" stopColor="hsl(var(--primary))" stopOpacity={0.28} />
                <stop offset="100%" stopColor="hsl(var(--primary))" stopOpacity={0.02} />
              </linearGradient>
              <linearGradient id="ipGradient" x1="0" y1="0" x2="0" y2="1">
                <stop offset="0%" stopColor="hsl(var(--success))" stopOpacity={0.28} />
                <stop offset="100%" stopColor="hsl(var(--success))" stopOpacity={0.02} />
              </linearGradient>
            </defs>
            <CartesianGrid vertical={false} stroke="hsl(var(--border))" />
            <XAxis dataKey="month" tickLine={false} axisLine={false} tick={{ fill: 'hsl(var(--muted-foreground))', fontSize: 12 }} />
            <YAxis
              tickLine={false}
              axisLine={false}
              allowDecimals={false}
              tick={{ fill: 'hsl(var(--muted-foreground))', fontSize: 12 }}
              // Compact labels ("8K") so 5-digit counts are never clipped at narrow widths.
              tickFormatter={(value: number) => compactNumber.format(value)}
              width={40}
            />
            <Tooltip
              contentStyle={{
                background: 'hsl(var(--popover))',
                border: '1px solid hsl(var(--border))',
                borderRadius: 8,
                fontSize: 12,
                color: 'hsl(var(--popover-foreground))',
              }}
            />
            <Area type="monotone" dataKey="op" name="OP visits" stroke="hsl(var(--primary))" strokeWidth={2} fill="url(#opGradient)" />
            {showIp && <Area type="monotone" dataKey="ip" name="IP admissions" stroke="hsl(var(--success))" strokeWidth={2} fill="url(#ipGradient)" />}
          </AreaChart>
        </ResponsiveContainer>
      )}
    </ChartCard>
  );
}
