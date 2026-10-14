import type { LucideIcon } from 'lucide-react';
import type { ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { cn } from '@/lib/utils';

export type StatTone = 'default' | 'success' | 'warning' | 'destructive';

const TONE: Record<StatTone, { value: string; icon: string }> = {
  default: { value: 'text-foreground', icon: 'bg-primary/10 text-primary' },
  success: { value: 'text-success', icon: 'bg-success/10 text-success' },
  warning: { value: 'text-warning', icon: 'bg-warning/10 text-warning' },
  destructive: { value: 'text-destructive', icon: 'bg-destructive/10 text-destructive' },
};

interface StatTileProps {
  label: string;
  value: ReactNode;
  hint?: ReactNode;
  tone?: StatTone;
  icon?: LucideIcon;
  /** Makes the tile a link to the screen behind the figure. */
  to?: string;
  /** Smaller figure and no icon, for the tiles above a report where space is shared with the report list. */
  compact?: boolean;
}

/** One headline figure — on the dashboard, and above each report's table. */
export function StatTile({ label, value, hint, tone = 'default', icon: Icon, to, compact }: StatTileProps) {
  const body = (
    <>
      <div className="min-w-0">
        <p className="truncate text-[11px] font-medium uppercase tracking-wide text-muted-foreground">{label}</p>
        <p
          className={cn('mt-0.5 truncate font-semibold tabular-nums leading-tight', compact ? 'text-lg' : 'text-xl', TONE[tone].value)}
          title={typeof value === 'string' || typeof value === 'number' ? String(value) : undefined}
        >
          {value}
        </p>
        {hint && <p className="mt-0.5 text-xs leading-snug text-muted-foreground">{hint}</p>}
      </div>
      {Icon && !compact && (
        <span className={cn('flex h-9 w-9 shrink-0 items-center justify-center rounded-md print:hidden', TONE[tone].icon)}>
          <Icon className="h-4.5 w-4.5" />
        </span>
      )}
    </>
  );

  const className = 'flex items-start justify-between gap-3 rounded-lg border border-border bg-background px-3.5 py-3';

  return to ? (
    <Link to={to} className={cn(className, 'transition-colors hover:border-primary/40 hover:bg-accent/40')}>
      {body}
    </Link>
  ) : (
    <div className={className}>{body}</div>
  );
}
