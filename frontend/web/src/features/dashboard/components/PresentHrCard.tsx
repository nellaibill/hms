import { UserCheck } from 'lucide-react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { ProgressBar } from './ProgressBar';
import { staffAttendanceToday } from '../mockData';

export function PresentHrCard() {
  const { present, total, onLeave, byDepartment } = staffAttendanceToday;
  const pct = Math.round((present / total) * 100);

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
      <CardContent className="flex flex-col gap-5 pt-0 sm:flex-row sm:gap-8">
        <div className="flex shrink-0 flex-col gap-2 sm:w-40">
          <div className="flex items-baseline gap-1.5">
            <span className="text-3xl font-semibold tabular-nums text-foreground">{present}</span>
            <span className="text-sm text-muted-foreground">of {total} present</span>
          </div>
          <ProgressBar value={pct} barClassName="bg-success" />
          <p className="text-xs text-muted-foreground">
            {pct}% present · {onLeave} on leave
          </p>
        </div>

        <div className="flex flex-1 flex-col gap-3.5 border-t border-border pt-4 sm:border-l sm:border-t-0 sm:pl-8 sm:pt-0">
          {byDepartment.map((row) => (
            <div key={row.department}>
              <div className="mb-1 flex items-center justify-between text-xs">
                <span className="font-medium text-foreground">{row.department}</span>
                <span className="tabular-nums text-muted-foreground">
                  {row.present}/{row.total}
                </span>
              </div>
              <ProgressBar value={row.present} max={row.total} barClassName="bg-success" />
            </div>
          ))}
        </div>
      </CardContent>
    </Card>
  );
}
