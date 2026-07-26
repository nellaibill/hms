import { CalendarDays } from 'lucide-react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { cn } from '@/lib/utils';
import { calendarEvents } from '../mockData';

const WEEKDAYS = ['S', 'M', 'T', 'W', 'T', 'F', 'S'];

export function MiniCalendarCard() {
  const today = new Date();
  const year = today.getFullYear();
  const month = today.getMonth();
  const monthLabel = today.toLocaleDateString('en-IN', { month: 'long', year: 'numeric' });

  const firstWeekday = new Date(year, month, 1).getDay();
  const daysInMonth = new Date(year, month + 1, 0).getDate();
  const eventDates = new Set(calendarEvents.map((event) => event.date));

  const cells: (number | null)[] = [...Array(firstWeekday).fill(null), ...Array.from({ length: daysInMonth }, (_, i) => i + 1)];

  return (
    <Card className="flex h-full flex-col transition-shadow hover:shadow-soft-md">
      <CardHeader className="flex-row items-center gap-2.5 space-y-0 pb-3">
        <span className="flex h-8 w-8 items-center justify-center rounded-md bg-primary/10 text-primary">
          <CalendarDays className="h-4 w-4" />
        </span>
        <div>
          <CardTitle className="text-base">Calendar</CardTitle>
          <CardDescription className="mt-0.5">{monthLabel}</CardDescription>
        </div>
      </CardHeader>
      <CardContent className="flex flex-1 flex-col gap-4 pt-0">
        <div>
          <div className="grid grid-cols-7 gap-1 text-center text-[11px] font-medium text-muted-foreground">
            {WEEKDAYS.map((day, i) => (
              <span key={`${day}-${i}`}>{day}</span>
            ))}
          </div>
          <div className="mt-1 grid grid-cols-7 gap-1">
            {cells.map((day, i) => {
              const isToday = day === today.getDate();
              const hasEvent = day !== null && eventDates.has(day);
              return (
                <div
                  key={i}
                  className={cn(
                    'relative flex h-7 items-center justify-center rounded-md text-xs',
                    day === null && 'invisible',
                    isToday ? 'bg-primary font-semibold text-primary-foreground' : 'text-foreground hover:bg-accent',
                  )}
                >
                  {day}
                  {hasEvent && !isToday && <span className="absolute bottom-0.5 h-1 w-1 rounded-full bg-primary" />}
                </div>
              );
            })}
          </div>
        </div>

        <div className="mt-auto flex flex-col gap-2 border-t border-border pt-3">
          {calendarEvents.map((event) => (
            <div key={event.date} className="flex items-center gap-2 text-xs">
              <span className="flex h-5 w-8 shrink-0 items-center justify-center rounded bg-accent font-medium tabular-nums text-accent-foreground">
                {event.date}
              </span>
              <span className="truncate text-muted-foreground">{event.label}</span>
            </div>
          ))}
        </div>
      </CardContent>
    </Card>
  );
}
