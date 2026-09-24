import type { ReactNode } from 'react';
import { Loader2 } from 'lucide-react';

interface LoadingOverlayProps {
  /** True while the content underneath is stale and new data is on its way. */
  active: boolean;
  label?: string;
  /** Classes for the wrapper around `children`, e.g. to keep a parent's flex gap between them. */
  className?: string;
  children: ReactNode;
}

/**
 * Dims already-rendered content and shows a spinner while it's being replaced. For list
 * queries using `placeholderData: (previous) => previous` — changing a filter keeps the old
 * rows on screen (no flash to an empty state), so `isPending` stays false and without this
 * the user sees no sign that the table is about to change. Pass the query's
 * `isPlaceholderData` as `active`.
 */
export function LoadingOverlay({
  active,
  label = 'Loading…',
  className,
  children,
}: LoadingOverlayProps) {
  return (
    <div className="relative" aria-busy={active}>
      <div
        className={`transition-opacity ${active ? 'pointer-events-none opacity-50' : ''} ${className ?? ''}`}
      >
        {children}
      </div>
      {active && (
        <div className="absolute inset-x-0 top-0 flex justify-center pt-16">
          <div
            role="status"
            className="flex items-center gap-2 rounded-md border border-border bg-background px-4 py-2 text-sm text-muted-foreground shadow-sm"
          >
            <Loader2 className="h-4 w-4 animate-spin" />
            {label}
          </div>
        </div>
      )}
    </div>
  );
}
