import { Activity } from 'lucide-react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { ProgressBar } from './ProgressBar';
import { departmentPerformance } from '../mockData';

export function DepartmentPerformanceCard() {
  return (
    <Card className="transition-shadow hover:shadow-soft-md">
      <CardHeader className="flex-row items-center gap-2.5 space-y-0 pb-3">
        <span className="flex h-8 w-8 items-center justify-center rounded-md bg-primary/10 text-primary">
          <Activity className="h-4 w-4" />
        </span>
        <div>
          <CardTitle className="text-base">Department Performance</CardTitle>
          <CardDescription className="mt-0.5">Utilization by department, today</CardDescription>
        </div>
      </CardHeader>
      <CardContent className="flex flex-col gap-3.5 pt-0">
        {departmentPerformance.map((row) => (
          <div key={row.department}>
            <div className="mb-1 flex items-center justify-between text-xs">
              <span className="font-medium text-foreground">{row.department}</span>
              <span className="tabular-nums text-muted-foreground">{row.utilization}%</span>
            </div>
            <ProgressBar value={row.utilization} />
          </div>
        ))}
      </CardContent>
    </Card>
  );
}
