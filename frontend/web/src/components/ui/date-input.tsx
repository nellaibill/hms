import { CalendarDays } from 'lucide-react';
import * as React from 'react';
import { formatReportDate } from '@/lib/reportDateRange';
import { cn } from '@/lib/utils';

export interface DateInputProps extends Omit<React.InputHTMLAttributes<HTMLInputElement>, 'type' | 'value'> {
  /** YYYY-MM-DD, exactly like a native date input's value. */
  value: string;
}

function openPicker(input: HTMLInputElement) {
  try {
    input.showPicker?.();
  } catch {
    // Not allowed here (some browsers refuse outside a direct user gesture) — the field is
    // still focused and accepts typing, so nothing more to do.
  }
}

/**
 * A date field that always reads "06 Nov 2026", whatever date format the computer is set to.
 * A plain `<input type="date">` can't be restyled: it shows the operating system's own format
 * (e.g. 06-11-2026). So the formatted date is drawn here, and the real date input sits on top
 * of it, invisible. Clicking anywhere opens the browser's own calendar, and value, onChange,
 * min, max, aria-* and keyboard entry all behave exactly as before.
 */
export const DateInput = React.forwardRef<HTMLInputElement, DateInputProps>(({ className, value, disabled, onClick, ...props }, ref) => {
  const label = formatReportDate(value);

  return (
    <div
      className={cn(
        'relative flex h-10 items-center gap-2 rounded-md border border-input bg-background px-3 text-sm shadow-sm transition-colors focus-within:ring-2 focus-within:ring-ring focus-within:ring-offset-2 focus-within:ring-offset-background',
        disabled && 'cursor-not-allowed opacity-50',
        className,
      )}
    >
      <span className={cn('flex-1 truncate', label ? 'text-foreground' : 'text-muted-foreground')}>{label || 'Select date'}</span>
      <CalendarDays className="h-4 w-4 shrink-0 text-muted-foreground" aria-hidden />
      <input
        {...props}
        ref={ref}
        type="date"
        value={value}
        disabled={disabled}
        onClick={(event) => {
          openPicker(event.currentTarget);
          onClick?.(event);
        }}
        className="absolute inset-0 h-full w-full cursor-pointer opacity-0 disabled:cursor-not-allowed"
      />
    </div>
  );
});
DateInput.displayName = 'DateInput';
