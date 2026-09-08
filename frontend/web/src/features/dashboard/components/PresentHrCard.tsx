import { UserCheck } from 'lucide-react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { ProgressBar } from './ProgressBar';
import { useHrDashboardQuery } from '@/features/hr/dashboard';

export function PresentHrCard() {
  const dashboardQuery = useHrDashboardQuery();
  const dashboard = dashboardQuery.data;

  const present = dashboard?.presentToday ?? 0;
  const total = dashboard?.activeEmployees ?? 0;
  const onLeave = dashboard?.onLeaveToday ?? 0;
  const pct = total > 0 ? Math.round((present / total) * 100) : 0;

  return (
    <Card className="transition-shadow hover:shadow-soft-lg">
      <CardHeader className="flex-row items-center gap-2.5 space-y-0 pb-3">
        <span className="flex h-8 w-8 items-center justify-center rounded-md bg-success/10 text-success">
          <UserCheck className="h-4 w-4" />
        </span>
        <div>
          <CardTitle className="text-base">Present HR</CardTitle>
          <CardDescription className="mt-0.5">Staff attendance today</CardDescription>
        </div>
      </CardHeader>
      <CardContent className="flex flex-col gap-2 pt-0 sm:w-40">
        <div className="flex items-baseline gap-1.5">
          <span className="text-3xl font-semibold tabular-nums text-foreground">
            {dashboardQuery.isPending ? '…' : present}
          </span>
          <span className="text-sm text-muted-foreground">of {dashboardQuery.isPending ? '…' : total} present</span>
        </div>
        <ProgressBar value={pct} barClassName="bg-success" />
        <p className="text-xs text-muted-foreground">
          {pct}% present · {dashboardQuery.isPending ? '…' : onLeave} on leave
        </p>
        {dashboardQuery.isError && <p className="text-xs text-destructive">Failed to load attendance.</p>}
      </CardContent>
    </Card>
  );
}
