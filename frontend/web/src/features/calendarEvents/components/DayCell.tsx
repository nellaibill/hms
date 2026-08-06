import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover';
import { cn } from '@/lib/utils';
import { EventPill } from './EventPill';
import { EVENT_TYPE_META } from '../constants';
import type { MonthGridDay } from '../utils/date';
import type { CalendarEvent } from '../types';

const MAX_VISIBLE_EVENTS = 3;

interface DayCellProps {
  day: MonthGridDay;
  events: CalendarEvent[];
  onSelectEvent: (event: CalendarEvent) => void;
  onCreateAt: (iso: string) => void;
}

export function DayCell({ day, events, onSelectEvent, onCreateAt }: DayCellProps) {
  const visible = events.slice(0, MAX_VISIBLE_EVENTS);
  const overflow = events.length - visible.length;

  return (
    <div
      className={cn(
        'group flex min-h-[104px] flex-col gap-1 border-b border-r border-border p-1.5 sm:p-2',
        !day.isCurrentMonth && 'bg-muted/40',
        day.isWeekend && day.isCurrentMonth && 'bg-muted/20',
      )}
    >
      <div className="flex items-center justify-between">
        <span
          className={cn(
            'flex h-6 w-6 items-center justify-center rounded-full text-xs font-medium',
            day.isToday
              ? 'bg-primary text-primary-foreground'
              : day.isCurrentMonth
                ? 'text-foreground'
                : 'text-muted-foreground/60',
          )}
        >
          {day.dayOfMonth}
        </span>
        <button
          type="button"
          onClick={() => onCreateAt(day.iso)}
          aria-label={`Create event on ${day.iso}`}
          className="hidden h-5 w-5 items-center justify-center rounded text-muted-foreground opacity-0 transition-opacity hover:bg-accent hover:text-foreground focus-visible:opacity-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring group-hover:opacity-100 sm:flex"
        >
          +
        </button>
      </div>

      <div className="flex flex-1 flex-col gap-1">
        {visible.map((event) => (
          <EventPill key={event.id} event={event} onSelect={onSelectEvent} />
        ))}

        {overflow > 0 && (
          <Popover>
            <PopoverTrigger asChild>
              <button
                type="button"
                className="w-fit rounded px-1.5 text-left text-[11px] font-medium text-muted-foreground hover:text-foreground hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
              >
                +{overflow} more
              </button>
            </PopoverTrigger>
            <PopoverContent align="start" className="w-64 p-2">
              <p className="px-1 pb-1.5 text-xs font-semibold text-muted-foreground">{day.iso}</p>
              <div className="flex flex-col gap-1">
                {events.map((event) => {
                  const meta = EVENT_TYPE_META[event.eventType];
                  return (
                    <button
                      key={event.id}
                      type="button"
                      onClick={() => onSelectEvent(event)}
                      className="flex items-center gap-2 rounded px-1.5 py-1 text-left text-xs hover:bg-accent"
                    >
                      <span className={cn('h-2 w-2 shrink-0 rounded-full', meta.dotClass)} aria-hidden="true" />
                      <span className="truncate">{event.title}</span>
                    </button>
                  );
                })}
              </div>
            </PopoverContent>
          </Popover>
        )}
      </div>
    </div>
  );
}
