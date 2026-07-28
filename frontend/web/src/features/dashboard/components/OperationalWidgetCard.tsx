import { ArrowRight, type LucideIcon } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Card, CardContent, CardHeader } from '@/components/ui/card';
import type { ReactNode } from 'react';

interface OperationalWidgetCardProps {
  title: string;
  icon: LucideIcon;
  count: number;
  countLabel: string;
  link: string;
  children: ReactNode;
}

export function OperationalWidgetCard({ title, icon: Icon, count, countLabel, link, children }: OperationalWidgetCardProps) {
  return (
    <Card className="flex h-full flex-col transition-shadow hover:shadow-soft-lg">
      <CardHeader className="flex-row items-center justify-between gap-2 space-y-0 pb-3">
        <div className="flex items-center gap-2.5">
          <span className="flex h-8 w-8 items-center justify-center rounded-md bg-primary/10 text-primary">
            <Icon className="h-4 w-4" />
          </span>
          <div>
            <p className="text-sm font-semibold text-foreground">{title}</p>
            <p className="text-xs text-muted-foreground">
              {count} {countLabel}
            </p>
          </div>
        </div>
      </CardHeader>
      <CardContent className="flex flex-1 flex-col justify-between gap-3 pt-0">
        <div className="flex flex-col divide-y divide-border">{children}</div>
        <Link
          to={link}
          className="inline-flex items-center gap-1 text-xs font-medium text-primary transition-colors hover:text-primary/80"
        >
          View all
          <ArrowRight className="h-3 w-3" />
        </Link>
      </CardContent>
    </Card>
  );
}
