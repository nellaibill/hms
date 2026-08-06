import { DayCell } from './DayCell';
import { buildMonthGrid, WEEKDAY_LABELS } from '../utils/date';
import type { CalendarEvent } from '../types';

interface MonthGridProps {
  year: number;
  month: number;
  events: CalendarEvent[];
  onSelectEvent: (event: CalendarEvent) => void;
  onCreateAt: (iso: string) => void;
}

export function MonthGrid({ year, month, events, onSelectEvent, onCreateAt }: MonthGridProps) {
  const days = buildMonthGrid(year, month);

  const eventsByDate = new Map<string, CalendarEvent[]>();
  for (const day of days) {
    const dayEvents = events.filter((event) => event.startDate <= day.iso && day.iso <= event.endDate);
    if (dayEvents.length > 0) eventsByDate.set(day.iso, dayEvents);
  }

  return (
    <div className="flex flex-col rounded-lg border border-border bg-card shadow-soft" role="grid" aria-label="Month calendar">
      <div className="grid grid-cols-7 border-b border-border" role="row">
        {WEEKDAY_LABELS.map((label) => (
          <div
            key={label}
            role="columnheader"
            className="border-r border-border px-2 py-2 text-center text-xs font-semibold text-muted-foreground last:border-r-0"
          >
            {label}
          </div>
        ))}
      </div>
      <div className="grid grid-cols-7">
        {days.map((day) => (
          <DayCell
            key={day.iso}
            day={day}
            events={eventsByDate.get(day.iso) ?? []}
            onSelectEvent={onSelectEvent}
            onCreateAt={onCreateAt}
          />
        ))}
      </div>
    </div>
  );
}
