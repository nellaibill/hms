import { ChevronLeft, ChevronRight } from 'lucide-react';
import { cn } from '@/lib/utils';
import { buildMonthGrid, MONTH_LABELS } from '../utils/date';

interface MiniNavCalendarProps {
  year: number;
  month: number;
  onPrevMonth: () => void;
  onNextMonth: () => void;
  onSelectDate: (iso: string) => void;
  eventDates: Set<string>;
}

const WEEKDAY_INITIALS = ['S', 'M', 'T', 'W', 'T', 'F', 'S'];

export function MiniNavCalendar({ year, month, onPrevMonth, onNextMonth, onSelectDate, eventDates }: MiniNavCalendarProps) {
  const days = buildMonthGrid(year, month);

  return (
    <div>
      <div className="mb-2 flex items-center justify-between">
        <p className="text-xs font-semibold text-foreground">
          {MONTH_LABELS[month - 1]} {year}
        </p>
        <div className="flex items-center gap-0.5">
          <button
            type="button"
            aria-label="Previous month"
            onClick={onPrevMonth}
            className="flex h-5 w-5 items-center justify-center rounded text-muted-foreground hover:bg-accent hover:text-foreground"
          >
            <ChevronLeft className="h-3.5 w-3.5" />
          </button>
          <button
            type="button"
            aria-label="Next month"
            onClick={onNextMonth}
            className="flex h-5 w-5 items-center justify-center rounded text-muted-foreground hover:bg-accent hover:text-foreground"
          >
            <ChevronRight className="h-3.5 w-3.5" />
          </button>
        </div>
      </div>

      <div className="grid grid-cols-7 gap-y-1 text-center text-[10px] font-medium text-muted-foreground">
        {WEEKDAY_INITIALS.map((d, i) => (
          <span key={`${d}-${i}`}>{d}</span>
        ))}
      </div>
      <div className="grid grid-cols-7 gap-y-1">
        {days.map((day) => {
          const hasEvent = eventDates.has(day.iso);
          return (
            <button
              key={day.iso}
              type="button"
              onClick={() => onSelectDate(day.iso)}
              className={cn(
                'relative mx-auto flex h-6 w-6 items-center justify-center rounded-full text-[11px]',
                !day.isCurrentMonth && 'text-muted-foreground/40',
                day.isCurrentMonth && !day.isToday && 'text-foreground hover:bg-accent',
                day.isToday && 'bg-primary font-semibold text-primary-foreground',
              )}
            >
              {day.dayOfMonth}
              {hasEvent && !day.isToday && (
                <span className="absolute bottom-0 h-1 w-1 rounded-full bg-primary" aria-hidden="true" />
              )}
            </button>
          );
        })}
      </div>
    </div>
  );
}
