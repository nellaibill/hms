import { ChevronDown, Plus } from 'lucide-react';
import type { ReactNode } from 'react';
import { Button } from '@/components/ui/button';
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
  /** Renders an "Add another …" button in the header itself, right before the chevron —
   * only while expanded, so it's reachable without scrolling past a long row list first
   * (previously every category buried this at the very bottom of its rows). Omit to render
   * no header action, e.g. for a card with nothing to add. */
  onAdd?: () => void;
  addLabel?: string;
  children: ReactNode;
}

/**
 * Collapsible variant of FormSection's card styling (same border/bg/shadow/heading tokens)
 * — used for the four optional billing categories, which start collapsed so the page isn't
 * four fully-expanded forms by default.
 */
export function CollapsibleCard({ id, title, description, icon, expanded, onToggle, hasError, summary, onAdd, addLabel, children }: CollapsibleCardProps) {
  const panelId = `${id}-panel`;

  return (
    <div className="rounded-lg border border-border bg-card shadow-soft-md">
      <div className="flex w-full items-center justify-between gap-3 p-5 sm:p-6">
        <button
          type="button"
          onClick={onToggle}
          aria-expanded={expanded}
          aria-controls={panelId}
          className="-m-2 flex min-w-0 flex-1 items-center gap-3 rounded-lg p-2 text-left transition-colors hover:bg-accent/40"
        >
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-accent text-accent-foreground">{icon}</span>
          <span className="flex min-w-0 flex-col">
            <span className="flex items-center gap-1.5 text-lg font-semibold text-primary">
              {title}
              {hasError && <span aria-label="This section has errors" className="h-1.5 w-1.5 rounded-full bg-destructive" />}
            </span>
            {description && <span className="text-xs text-muted-foreground">{description}</span>}
          </span>
        </button>
        <span className="flex shrink-0 items-center gap-3">
          {summary}
          {expanded && onAdd && (
            <Button
              type="button"
              variant="outline"
              size="sm"
              className="gap-1.5"
              onClick={(e) => {
                e.stopPropagation();
                onAdd();
              }}
            >
              <Plus className="h-4 w-4" />
              {addLabel}
            </Button>
          )}
          <button
            type="button"
            onClick={onToggle}
            aria-expanded={expanded}
            aria-controls={panelId}
            aria-label={expanded ? `Collapse ${title}` : `Expand ${title}`}
            className="rounded-md p-1 text-muted-foreground transition-colors hover:bg-accent/40"
          >
            <ChevronDown className={cn('h-5 w-5 transition-transform', expanded && 'rotate-180')} />
          </button>
        </span>
      </div>
      {expanded && (
        <div id={panelId} className="flex flex-col gap-4 border-t border-border p-5 sm:p-6">
          {children}
        </div>
      )}
    </div>
  );
}
