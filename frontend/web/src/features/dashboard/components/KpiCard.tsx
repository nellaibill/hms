import { ArrowDownRight, ArrowUpRight } from 'lucide-react';
import { Card, CardContent } from '@/components/ui/card';
import { cn } from '@/lib/utils';
import { Sparkline } from './Sparkline';
import type { KpiDatum } from '../mockData';

interface KpiCardProps {
  kpi: KpiDatum;
}

export function KpiCard({ kpi }: KpiCardProps) {
  const Icon = kpi.icon;
  const isUp = kpi.changePct >= 0;
  const isGood = isUp ? kpi.goodWhen === 'up' : kpi.goodWhen === 'down';

  const colorVar = isGood ? '--success' : kpi.goodWhen === (isUp ? 'down' : 'up') ? '--destructive' : '--warning';
  const badgeClasses = isGood
    ? 'bg-success/10 text-success'
    : colorVar === '--destructive'
      ? 'bg-destructive/10 text-destructive'
      : 'bg-warning/10 text-warning';

  return (
    <Card className="transition-shadow hover:shadow-soft-lg">
      <CardContent className="flex flex-col gap-3 p-5">
        <div className="flex items-start justify-between">
          <span className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10 text-primary">
            <Icon className="h-5 w-5" />
          </span>
          <span className={cn('flex items-center gap-0.5 rounded-full px-2 py-0.5 text-xs font-medium', badgeClasses)}>
            {isUp ? <ArrowUpRight className="h-3.5 w-3.5" /> : <ArrowDownRight className="h-3.5 w-3.5" />}
            {Math.abs(kpi.changePct).toFixed(1)}%
          </span>
        </div>

        <div>
          <p className="text-2xl font-semibold tracking-tight text-foreground">{kpi.value}</p>
          <p className="mt-0.5 text-sm text-muted-foreground">{kpi.label}</p>
        </div>

        <div className="mt-auto">
          <Sparkline data={kpi.sparkline} colorVar={colorVar} />
        </div>
      </CardContent>
    </Card>
  );
}
