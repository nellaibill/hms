import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip';
import { cn } from '@/lib/utils';
import { EVENT_TYPE_META } from '../constants';
import { formatDisplayDate } from '../utils/date';
import type { CalendarEvent } from '../types';

interface EventPillProps {
  event: CalendarEvent;
  onSelect: (event: CalendarEvent) => void;
}

export function EventPill({ event, onSelect }: EventPillProps) {
  const meta = EVENT_TYPE_META[event.eventType];

  return (
    <Tooltip>
      <TooltipTrigger asChild>
        <button
          type="button"
          onClick={() => onSelect(event)}
          aria-label={`${event.title} — ${meta.label}${event.department ? `, ${event.department}` : ''}`}
          className={cn(
            'flex w-full min-h-[20px] items-center truncate rounded px-1.5 py-0.5 text-left text-[11px] font-medium leading-4 transition-opacity hover:opacity-80 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring',
            meta.pillClass,
          )}
        >
          <span className="truncate">{event.title}</span>
        </button>
      </TooltipTrigger>
      <TooltipContent side="right" className="max-w-[240px]">
        <p className="text-sm font-semibold text-popover-foreground">{event.title}</p>
        <p className="mt-1 flex items-center gap-1.5 text-xs text-muted-foreground">
          <span className={cn('h-2 w-2 rounded-full', meta.dotClass)} aria-hidden="true" />
          {meta.label}
          {event.department ? ` · ${event.department}` : ''}
        </p>
        <p className="mt-1 text-xs text-muted-foreground">
          {formatDisplayDate(event.startDate)} – {formatDisplayDate(event.endDate)}
        </p>
      </TooltipContent>
    </Tooltip>
  );
}
