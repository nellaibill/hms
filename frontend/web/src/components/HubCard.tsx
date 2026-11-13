import { ArrowRight } from 'lucide-react';
import type { ElementType, ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { Card, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { cn } from '@/lib/utils';

interface HubCardProps {
  to: string;
  title: string;
  description: string;
  icon: ElementType;
  /** Extra content beside the arrow, e.g. a pending count. */
  badge?: ReactNode;
  className?: string;
}

/** One big clickable choice on a hub page (Reception & Registration, Accounts and Finance) —
 * icon, title and a line of description, the whole card a link. */
export function HubCard({ to, title, description, icon: Icon, badge, className }: HubCardProps) {
  return (
    <Link to={to} className={cn('block w-full max-w-sm sm:max-w-md', className)}>
      <Card className="h-full transition-all hover:border-primary/40 hover:bg-accent/40 hover:shadow-soft-lg">
        <CardHeader className="p-7 sm:p-8">
          <div className="flex items-center justify-between gap-2">
            <span className="flex h-10 w-10 items-center justify-center rounded-md bg-primary/10 text-primary">
              <Icon className="h-5 w-5" />
            </span>
            <span className="flex items-center gap-2">
              {badge}
              <ArrowRight className="h-4 w-4 text-muted-foreground" />
            </span>
          </div>
          <CardTitle className="text-base">{title}</CardTitle>
          <CardDescription>{description}</CardDescription>
        </CardHeader>
      </Card>
    </Link>
  );
}
