import { CheckCircle2, Target } from 'lucide-react';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import type { DepartmentGoalStatus } from '../mockData';
import { departmentGoals } from '../mockData';
import { ProgressBar } from './ProgressBar';

const statusVariant: Record<DepartmentGoalStatus, 'success' | 'warning' | 'destructive' | 'outline'> = {
  'On Track': 'success',
  'At Risk': 'warning',
  Delayed: 'destructive',
  Completed: 'outline',
};

// The progress bar under each goal echoes its status color — a blue bar under an
// "At Risk" badge read as contradictory (calm blue under an amber warning).
const statusBarClass: Record<DepartmentGoalStatus, string> = {
  'On Track': 'bg-success',
  'At Risk': 'bg-warning',
  Delayed: 'bg-destructive',
  Completed: 'bg-success',
};

export function DepartmentGoalsCard() {
  return (
    <Card className="transition-shadow hover:shadow-soft-lg">
      <CardHeader className="flex-row items-center gap-2.5 space-y-0 pb-3">
        <span className="flex h-8 w-8 items-center justify-center rounded-md bg-primary/10 text-primary">
          <Target className="h-4 w-4" />
        </span>
        <div>
          <CardTitle className="text-base">Plans and Projects</CardTitle>
          <CardDescription className="mt-0.5">Department goals & targets — status</CardDescription>
        </div>
      </CardHeader>
      <CardContent className="flex flex-col divide-y divide-border pt-0">
        {departmentGoals.map((goal) => (
          <div key={goal.id} className="flex flex-col gap-2 py-3 first:pt-0 last:pb-0">
            <div className="flex flex-wrap items-start justify-between gap-2">
              <div className="min-w-0">
                <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">{goal.department}</p>
                <p className="text-sm font-medium text-foreground">{goal.goal}</p>
              </div>
              <Badge variant={statusVariant[goal.status]} className="shrink-0 gap-1">
                {goal.status === 'Completed' && <CheckCircle2 className="h-3 w-3" />}
                {goal.status}
              </Badge>
            </div>
            <ProgressBar value={goal.progress} barClassName={statusBarClass[goal.status]} />
          </div>
        ))}
      </CardContent>
    </Card>
  );
}
