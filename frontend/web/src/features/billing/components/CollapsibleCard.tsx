import { ChevronDown } from 'lucide-react';
import type { ReactNode } from 'react';
import { cn } from '@/lib/utils';

interface CollapsibleCardProps {
  id: string;
  title: string;
  description?: string;
  icon: ReactNode;
  expanded: boolean;
  onToggle: () => void;
  hasError?: boolean;
  /** Shown next to the chevron when collapsed — e.g. the category's current total, so a receptionist doesn't have to reopen a card to see what's in it. */
  summary?: ReactNode;
  children: ReactNode;
}

/**
 * Collapsible variant of FormSection's card styling (same border/bg/shadow/heading tokens)
 * — used for the four optional billing categories, which start collapsed so the page isn't
 * four fully-expanded forms by default.
 */
export function CollapsibleCard({ id, title, description, icon, expanded, onToggle, hasError, summary, children }: CollapsibleCardProps) {
  const panelId = `${id}-panel`;

  return (
    <div className="rounded-lg border border-border bg-card shadow-soft-md">
      <button
        type="button"
        onClick={onToggle}
        aria-expanded={expanded}
        aria-controls={panelId}
        className="flex w-full items-center justify-between gap-3 rounded-lg p-5 text-left transition-colors hover:bg-accent/40 sm:p-6"
      >
        <span className="flex items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-accent text-accent-foreground">{icon}</span>
          <span className="flex flex-col">
            <span className="flex items-center gap-1.5 text-lg font-semibold text-primary">
              {title}
              {hasError && <span aria-label="This section has errors" className="h-1.5 w-1.5 rounded-full bg-destructive" />}
            </span>
            {description && <span className="text-xs text-muted-foreground">{description}</span>}
          </span>
        </span>
        <span className="flex shrink-0 items-center gap-3">
          {summary}
          <ChevronDown className={cn('h-5 w-5 text-muted-foreground transition-transform', expanded && 'rotate-180')} />
        </span>
      </button>
      {expanded && (
        <div id={panelId} className="flex flex-col gap-4 border-t border-border p-5 sm:p-6">
          {children}
        </div>
      )}
    </div>
  );
}
