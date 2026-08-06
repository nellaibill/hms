import { CalendarClock } from 'lucide-react';
import { cn } from '@/lib/utils';
import { EVENT_TYPE_META } from '../constants';
import { formatDisplayDate } from '../utils/date';
import type { CalendarEvent } from '../types';

interface UpcomingEventsListProps {
  events: CalendarEvent[];
  onSelect: (event: CalendarEvent) => void;
}

export function UpcomingEventsList({ events, onSelect }: UpcomingEventsListProps) {
  return (
    <div>
      <p className="mb-1.5 text-xs font-semibold uppercase tracking-wide text-muted-foreground">Upcoming Events</p>
      {events.length === 0 ? (
        <p className="rounded-md border border-dashed border-border px-2.5 py-3 text-xs text-muted-foreground">
          Nothing coming up.
        </p>
      ) : (
        <ul className="flex flex-col gap-1">
          {events.map((event) => {
            const meta = EVENT_TYPE_META[event.eventType];
            return (
              <li key={event.id}>
                <button
                  type="button"
                  onClick={() => onSelect(event)}
                  className="flex w-full items-start gap-2 rounded-md px-2 py-1.5 text-left hover:bg-accent focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                >
                  <span className={cn('mt-0.5 flex h-6 w-6 shrink-0 items-center justify-center rounded-md', meta.chipClass)}>
                    <CalendarClock className="h-3.5 w-3.5" aria-hidden="true" />
                  </span>
                  <span className="min-w-0 flex-1">
                    <span className="block truncate text-sm text-foreground">{event.title}</span>
                    <span className="block text-xs text-muted-foreground">{formatDisplayDate(event.startDate)}</span>
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
