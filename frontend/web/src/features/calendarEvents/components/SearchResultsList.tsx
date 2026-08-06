import { CalendarDays } from 'lucide-react';
import { cn } from '@/lib/utils';
import { EVENT_TYPE_META } from '../constants';
import { formatDisplayDate } from '../utils/date';
import type { CalendarEvent } from '../types';

interface SearchResultsListProps {
  query: string;
  results: CalendarEvent[];
  onSelect: (event: CalendarEvent) => void;
}

export function SearchResultsList({ query, results, onSelect }: SearchResultsListProps) {
  return (
    <div className="p-4 sm:p-6">
      <p className="mb-3 text-sm text-muted-foreground" aria-live="polite">
        {results.length} result{results.length === 1 ? '' : 's'} for <span className="font-medium text-foreground">&ldquo;{query}&rdquo;</span>
      </p>

      {results.length === 0 ? (
        <div className="flex flex-col items-center gap-1 rounded-lg border border-dashed border-border py-16 text-center">
          <CalendarDays className="mb-2 h-8 w-8 text-muted-foreground/50" aria-hidden="true" />
          <p className="text-sm font-medium text-foreground">No events found.</p>
          <p className="text-sm text-muted-foreground">Try a different title, department, or event type.</p>
        </div>
      ) : (
        <ul className="flex flex-col gap-2">
          {results.map((event) => {
            const meta = EVENT_TYPE_META[event.eventType];
            return (
              <li key={event.id}>
                <button
                  type="button"
                  onClick={() => onSelect(event)}
                  className="flex w-full items-center gap-3 rounded-lg border border-border bg-card px-4 py-3 text-left shadow-soft transition-shadow hover:shadow-soft-md focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                >
                  <span className={cn('flex h-9 w-9 shrink-0 items-center justify-center rounded-md', meta.chipClass)}>
                    <CalendarDays className="h-4 w-4" aria-hidden="true" />
                  </span>
                  <span className="min-w-0 flex-1">
                    <span className="block truncate text-sm font-medium text-foreground">{event.title}</span>
                    <span className="block truncate text-xs text-muted-foreground">
                      {meta.label}
                      {event.department ? ` · ${event.department}` : ''} · {formatDisplayDate(event.startDate)}
                      {event.startDate !== event.endDate ? ` – ${formatDisplayDate(event.endDate)}` : ''}
                    </span>
                  </span>
                </button>
              </li>
            );
          })}
        </ul>
      )}
    </div>
  );
}
