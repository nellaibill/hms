import { Bar, BarChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import { ChartCard } from '@/features/dashboard/components/ChartCard';
import type { CountBreakdownRow } from '../patientReport';

interface PatientBreakdownChartProps {
  title: string;
  description?: string;
  rows: CountBreakdownRow[];
  /** Resolves a row's raw `label` (which may be a Guid — department/consultant ids aren't
   * resolved to names until render time, same as ConsultantProfitTable's own
   * resolveRecordLabel calls) to what should actually display on the chart's x-axis/tooltip. */
  formatLabel?: (label: string) => string;
  /** Caps how many bars render — a "top N" view for breakdowns that can have many distinct
   * values (e.g. individual consultants), same reasoning CategoryBreakdownCard's maxRows has:
   * an unbounded chart becomes unreadable long before it becomes useful. */
  maxBars?: number;
}

/** One bar chart per breakdown (gender, marital status, arrival source, allergy type, visit
 * type, department, consultant) — same Recharts + `hsl(var(--token))` theming the dashboard's
 * existing charts (DepartmentFinanceChart, MonthlyCensusChart) already use, so Patient Reports
 * doesn't introduce a second charting convention. */
export function PatientBreakdownChart({ title, description, rows, formatLabel, maxBars = 10 }: PatientBreakdownChartProps) {
  const data = rows.slice(0, maxBars).map((row) => ({ label: formatLabel ? formatLabel(row.label) : row.label, count: row.count }));

  return (
    <ChartCard title={title} description={description}>
      {data.length === 0 ? (
        <div className="flex h-full items-center justify-center text-sm text-muted-foreground">No data in this period.</div>
      ) : (
        <ResponsiveContainer width="100%" height="100%">
          <BarChart data={data} margin={{ top: 8, right: 8, left: -12, bottom: 0 }}>
            <CartesianGrid vertical={false} stroke="hsl(var(--border))" />
            <XAxis
              dataKey="label"
              tickLine={false}
              axisLine={false}
              tick={{ fill: 'hsl(var(--muted-foreground))', fontSize: 11 }}
              interval={0}
              angle={-20}
              textAnchor="end"
              height={48}
            />
            <YAxis tickLine={false} axisLine={false} tick={{ fill: 'hsl(var(--muted-foreground))', fontSize: 12 }} width={36} allowDecimals={false} />
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
            <Bar dataKey="count" name="Count" fill="hsl(var(--primary))" radius={[4, 4, 0, 0]} maxBarSize={36} />
          </BarChart>
        </ResponsiveContainer>
      )}
    </ChartCard>
  );
}
