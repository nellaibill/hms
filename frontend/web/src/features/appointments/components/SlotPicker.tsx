import type { AppointmentSlot } from '@hms/shared';
import { CalendarOff, Loader2 } from 'lucide-react';
import { useState } from 'react';
import { cn } from '@/lib/utils';
import { formatSlotRange, formatSlotTime } from '../format';
import { useAppointmentSlotsQuery } from '../hooks';

interface SlotPickerProps {
  consultantId: string | undefined;
  date: string | undefined;
  /** The selected slot's start time ("HH:mm:ss"), or '' for none. */
  value: string;
  onChange: (startTime: string) => void;
  /** When rescheduling, the appointment's current slot — shown as "current", not selectable. */
  currentStartTime?: string;
  /** Read-only overview (the diary page): every slot is shown, clicking a bookable one still
   * calls onChange so the page can open the booking dialog on it. */
  compact?: boolean;
}

function slotHint(slot: AppointmentSlot): string {
  if (slot.isBlocked) return 'Leave';
  if (slot.isPast) return 'Passed';
  if (slot.available === 0) return 'Full';
  if (slot.capacity > 1) return `${slot.available} of ${slot.capacity} free`;
  return `Token ${slot.firstToken}`;
}

/** A consultant's day as a grid of slot buttons, one row per visiting session. Only bookable
 * slots can be picked; full, passed and leave-blocked ones are shown greyed so reception can
 * see the whole day at a glance. */
export function SlotPicker({ consultantId, date, value, onChange, currentStartTime, compact }: SlotPickerProps) {
  const { data, isPending, isError, error } = useAppointmentSlotsQuery(consultantId, date);
  // Today, the slots already gone are folded away — on a long clinic day they'd otherwise fill
  // most of the grid with nothing anyone can pick.
  const [showPast, setShowPast] = useState(false);

  if (!consultantId || !date) {
    return <p className="rounded-md border border-dashed border-border px-3 py-6 text-center text-sm text-muted-foreground">Pick a consultant and a date to see the free slots.</p>;
  }

  if (isPending) {
    return (
      <div className="flex items-center justify-center gap-2 py-6 text-sm text-muted-foreground">
        <Loader2 className="h-4 w-4 animate-spin" />
        Loading slots…
      </div>
    );
  }

  if (isError) {
    return (
      <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
        {error instanceof Error ? error.message : 'Could not load the slots.'}
      </p>
    );
  }

  if (!data.isWorkingDay || data.slots.length === 0) {
    return (
      <div className="flex items-center gap-2 rounded-md border border-dashed border-border px-3 py-4 text-sm text-muted-foreground">
        <CalendarOff className="h-4 w-4 shrink-0" />
        {data.message ?? 'No slots on this day.'}
      </div>
    );
  }

  const pastCount = data.slots.filter((slot) => slot.isPast).length;
  const allPast = pastCount === data.slots.length;
  const hidePast = !showPast && !allPast;
  const sessions = [1, 2]
    .map((session) => {
      const all = data.slots.filter((slot) => slot.session === session);
      return { session, all, slots: hidePast ? all.filter((slot) => !slot.isPast) : all };
    })
    .filter((group) => group.all.length > 0);
  const freeCount = data.slots.filter((slot) => slot.isBookable).length;

  return (
    <div className="flex flex-col gap-3">
      {data.message && (
        <p className="flex items-center gap-2 rounded-md bg-warning/10 px-3 py-2 text-sm text-warning-foreground">
          <CalendarOff className="h-4 w-4 shrink-0" />
          {data.message}
        </p>
      )}
      <p className="flex flex-wrap items-center gap-x-2 text-xs text-muted-foreground">
        <span>
          {data.slotMinutes}-minute slots{data.patientsPerSlot > 1 ? `, ${data.patientsPerSlot} patients each` : ''} · {freeCount} free
        </span>
        {pastCount > 0 && !allPast && (
          <button type="button" className="font-medium text-link hover:underline" onClick={() => setShowPast((shown) => !shown)}>
            {showPast ? 'Hide earlier slots' : `Show ${pastCount} earlier slot${pastCount === 1 ? '' : 's'}`}
          </button>
        )}
      </p>
      {sessions.map(({ session, all, slots }) => (
        <div key={session} className="flex flex-col gap-1.5">
          <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
            Session {session} · {formatSlotRange(all[0].startTime, all[all.length - 1].endTime)}
            {slots.length === 0 && ' · over'}
          </p>
          <div className={cn('grid gap-1.5', compact ? 'grid-cols-[repeat(auto-fill,minmax(5.5rem,1fr))]' : 'grid-cols-[repeat(auto-fill,minmax(6.5rem,1fr))]')}>
            {slots.map((slot) => {
              const isCurrent = currentStartTime === slot.startTime;
              const isSelected = value === slot.startTime;
              const selectable = slot.isBookable && !isCurrent;
              return (
                <button
                  key={slot.startTime}
                  type="button"
                  disabled={!selectable}
                  aria-pressed={isSelected}
                  title={slot.isBlocked && slot.blockReason ? `Leave: ${slot.blockReason}` : formatSlotRange(slot.startTime, slot.endTime)}
                  onClick={() => onChange(slot.startTime)}
                  className={cn(
                    'flex flex-col items-center rounded-md border px-2 py-1.5 text-center transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring',
                    isSelected && 'border-primary bg-primary text-primary-foreground',
                    !isSelected && selectable && 'border-input bg-background hover:border-primary/50 hover:bg-accent',
                    !isSelected && isCurrent && 'border-primary/60 bg-primary/10 text-foreground',
                    !isSelected && !selectable && !isCurrent && 'cursor-not-allowed border-transparent bg-muted/60 text-muted-foreground',
                    slot.isBlocked && !isSelected && 'bg-destructive/5 text-destructive/70',
                  )}
                >
                  <span className="text-sm font-medium">{formatSlotTime(slot.startTime)}</span>
                  <span className={cn('text-[11px]', isSelected ? 'text-primary-foreground/80' : 'text-muted-foreground')}>
                    {isCurrent ? 'Current' : slotHint(slot)}
                  </span>
                </button>
              );
            })}
          </div>
        </div>
      ))}
    </div>
  );
}
