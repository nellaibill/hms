import { Bell, Pencil, Trash2 } from 'lucide-react';
import { Sheet, SheetContent } from '@/components/ui/sheet';
import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';
import { EVENT_TYPE_META, REMINDER_TYPE_LABELS } from '../constants';
import { formatDisplayDate } from '../utils/date';
import type { CalendarEvent } from '../types';

interface DetailRowProps {
  label: string;
  children: React.ReactNode;
}

function DetailRow({ label, children }: DetailRowProps) {
  return (
    <div className="flex flex-col gap-0.5">
      <span className="text-xs font-medium uppercase tracking-wide text-muted-foreground">{label}</span>
      <span className="text-sm text-foreground">{children}</span>
    </div>
  );
}

function SectionHeading({ children }: { children: React.ReactNode }) {
  return <h3 className="text-sm font-semibold text-foreground">{children}</h3>;
}

interface EventDetailsDrawerProps {
  event: CalendarEvent | null;
  onClose: () => void;
  onEdit: (event: CalendarEvent) => void;
  onDelete: (event: CalendarEvent) => void;
}

export function EventDetailsDrawer({ event, onClose, onEdit, onDelete }: EventDetailsDrawerProps) {
  const open = event !== null;
  const meta = event ? EVENT_TYPE_META[event.eventType] : null;

  return (
    <Sheet open={open} onOpenChange={(next) => !next && onClose()}>
      <SheetContent side="right" className="flex w-full flex-col gap-0 overflow-y-auto p-0 sm:max-w-md">
        {event && meta && (
          <>
            <div className="border-b border-border px-6 py-4">
              <span className={cn('inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 text-xs font-semibold', meta.chipClass)}>
                <span className={cn('h-2 w-2 rounded-full', meta.dotClass)} aria-hidden="true" />
                {meta.label}
              </span>
              <h2 className="mt-2 text-lg font-semibold text-foreground">{event.title}</h2>
            </div>

            <div className="flex flex-1 flex-col gap-6 overflow-y-auto px-6 py-5">
              <section className="flex flex-col gap-3">
                <SectionHeading>Event Information</SectionHeading>
                <DetailRow label="Description">{event.description || <span className="text-muted-foreground">No description provided.</span>}</DetailRow>
                <div className="grid grid-cols-2 gap-3">
                  <DetailRow label="Event Type">{meta.label}</DetailRow>
                  <DetailRow label="Department">{event.department ?? 'Hospital-wide'}</DetailRow>
                  <DetailRow label="Created By">{event.createdBy}</DetailRow>
                  <DetailRow label="Created Date">{formatDisplayDate(event.createdAt.slice(0, 10))}</DetailRow>
                  <DetailRow label="Updated Date">{formatDisplayDate(event.updatedAt.slice(0, 10))}</DetailRow>
                </div>
              </section>

              <section className="flex flex-col gap-3 border-t border-border pt-5">
                <SectionHeading>Schedule</SectionHeading>
                <div className="grid grid-cols-2 gap-3">
                  <DetailRow label="Start Date">{formatDisplayDate(event.startDate)}</DetailRow>
                  <DetailRow label="End Date">{formatDisplayDate(event.endDate)}</DetailRow>
                </div>
                <DetailRow label="All Day">{event.allDay ? 'Yes' : 'No'}</DetailRow>
              </section>

              <section className="flex flex-col gap-3 border-t border-border pt-5">
                <SectionHeading>Reminder</SectionHeading>
                {event.reminderEnabled ? (
                  <div className="flex items-start gap-2 rounded-md border border-border bg-muted/40 px-3 py-2.5">
                    <Bell className="mt-0.5 h-4 w-4 shrink-0 text-primary" aria-hidden="true" />
                    <div className="flex flex-col gap-0.5 text-sm">
                      <span className="text-foreground">
                        {event.reminderType ? REMINDER_TYPE_LABELS[event.reminderType] : 'Reminder'}
                      </span>
                      {event.reminderAt && <span className="text-muted-foreground">{event.reminderAt.replace('T', ' ').slice(0, 16)}</span>}
                    </div>
                  </div>
                ) : (
                  <p className="text-sm text-muted-foreground">No reminder set for this event.</p>
                )}
              </section>
            </div>

            <div className="sticky bottom-0 flex items-center justify-between gap-3 border-t border-border bg-background px-6 py-4">
              <Button variant="ghost" onClick={onClose}>
                Close
              </Button>
              <div className="flex gap-2">
                <Button variant="destructive" onClick={() => onDelete(event)}>
                  <Trash2 className="h-4 w-4" />
                  Delete
                </Button>
                <Button onClick={() => onEdit(event)}>
                  <Pencil className="h-4 w-4" />
                  Edit
                </Button>
              </div>
            </div>
          </>
        )}
      </SheetContent>
    </Sheet>
  );
}
