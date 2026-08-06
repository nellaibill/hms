import { useEffect, useMemo, useState } from 'react';
import { useForm } from 'react-hook-form';
import { Sheet, SheetContent } from '@/components/ui/sheet';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Switch } from '@/components/ui/switch';
import { cn } from '@/lib/utils';
import { DepartmentSelect } from '@/components/DepartmentSelect';
import { EVENT_TYPE_META } from '../constants';
import { EVENT_TYPES } from '../types';
import { isEventFormValid, validateEventForm } from '../validation';
import { todayIso } from '../utils/date';
import type { CalendarEvent, CalendarEventFormValues, EventType } from '../types';

interface EventFormDrawerProps {
  open: boolean;
  mode: 'create' | 'edit';
  event?: CalendarEvent | null;
  defaultDate?: string | null;
  onClose: () => void;
  onSubmit: (values: CalendarEventFormValues) => void;
  isSubmitting?: boolean;
  submitError?: string | null;
}

function buildDefaultValues(event?: CalendarEvent | null, defaultDate?: string | null): CalendarEventFormValues {
  if (event) {
    return {
      title: event.title,
      description: event.description,
      eventType: event.eventType,
      departmentId: event.departmentId,
      startDate: event.startDate,
      endDate: event.endDate,
      allDay: event.allDay,
    };
  }
  const date = defaultDate ?? todayIso();
  return {
    title: '',
    description: '',
    eventType: '',
    departmentId: undefined,
    startDate: date,
    endDate: date,
    allDay: true,
  };
}

export function EventFormDrawer({
  open,
  mode,
  event,
  defaultDate,
  onClose,
  onSubmit,
  isSubmitting,
  submitError,
}: EventFormDrawerProps) {
  const { register, watch, setValue, reset, handleSubmit } = useForm<CalendarEventFormValues>({
    defaultValues: buildDefaultValues(event, defaultDate),
  });

  const [assignDepartment, setAssignDepartment] = useState(!!event?.departmentId);

  useEffect(() => {
    if (open) {
      reset(buildDefaultValues(event, defaultDate));
      setAssignDepartment(!!event?.departmentId);
    }
    // Only re-seed when the drawer opens for a (possibly new) target — not on every keystroke.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, event?.id, defaultDate]);

  const values = watch();
  const errors = useMemo(() => validateEventForm(values), [values]);
  const valid = isEventFormValid(errors);

  return (
    <Sheet open={open} onOpenChange={(next) => !next && onClose()}>
      <SheetContent side="right" className="flex w-full flex-col gap-0 overflow-y-auto p-0 sm:max-w-md">
        <div className="border-b border-border px-6 py-4">
          <h2 className="text-lg font-semibold text-foreground">{mode === 'create' ? 'Create Event' : 'Edit Event'}</h2>
          <p className="text-sm text-muted-foreground">
            {mode === 'create' ? 'Add a new entry to the hospital calendar.' : 'Update this calendar entry.'}
          </p>
        </div>

        <form onSubmit={handleSubmit(onSubmit)} noValidate className="flex flex-1 flex-col overflow-y-auto">
          <div className="flex flex-1 flex-col gap-5 px-6 py-5">
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="event-title">
                Title <span className="font-normal text-muted-foreground">(required)</span>
              </Label>
              <Input id="event-title" {...register('title')} aria-invalid={!!errors.title} placeholder="e.g. Department Heads Meeting" />
              {errors.title && (
                <p role="alert" className="text-xs text-destructive">
                  {errors.title}
                </p>
              )}
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="event-description">Description</Label>
              <textarea
                id="event-description"
                rows={3}
                {...register('description')}
                className="flex w-full rounded-md border border-input bg-background px-3 py-2 text-sm shadow-sm placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
                placeholder="Optional notes visible in the event details."
              />
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="event-type">
                Event Type <span className="font-normal text-muted-foreground">(required)</span>
              </Label>
              <Select
                value={values.eventType || undefined}
                onValueChange={(value) => setValue('eventType', value as EventType, { shouldDirty: true })}
              >
                <SelectTrigger id="event-type" aria-invalid={!!errors.eventType}>
                  <SelectValue placeholder="Select event type" />
                </SelectTrigger>
                <SelectContent>
                  {EVENT_TYPES.map((type) => (
                    <SelectItem key={type} value={type}>
                      <span className="flex items-center gap-2">
                        <span className={cn('h-2.5 w-2.5 rounded-full', EVENT_TYPE_META[type].dotClass)} aria-hidden="true" />
                        {EVENT_TYPE_META[type].label}
                      </span>
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              {errors.eventType && (
                <p role="alert" className="text-xs text-destructive">
                  {errors.eventType}
                </p>
              )}
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="event-start-date">
                  Start Date <span className="font-normal text-muted-foreground">(required)</span>
                </Label>
                <Input id="event-start-date" type="date" {...register('startDate')} aria-invalid={!!errors.startDate} />
                {errors.startDate && (
                  <p role="alert" className="text-xs text-destructive">
                    {errors.startDate}
                  </p>
                )}
              </div>
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="event-end-date">
                  End Date <span className="font-normal text-muted-foreground">(required)</span>
                </Label>
                <Input id="event-end-date" type="date" {...register('endDate')} aria-invalid={!!errors.endDate} />
                {errors.endDate && (
                  <p role="alert" className="text-xs text-destructive">
                    {errors.endDate}
                  </p>
                )}
              </div>
            </div>

            <div className="flex items-center justify-between rounded-md border border-border px-3 py-2.5">
              <Label htmlFor="event-all-day" className="cursor-pointer font-normal">
                All Day
              </Label>
              <Switch
                id="event-all-day"
                checked={values.allDay}
                onCheckedChange={(checked) => setValue('allDay', checked, { shouldDirty: true })}
              />
            </div>

            <div className="flex flex-col gap-1.5">
              <div className="flex items-center justify-between">
                <Label htmlFor="event-department" className="font-normal">
                  Department <span className="font-normal text-muted-foreground">(optional)</span>
                </Label>
                <div className="flex items-center gap-2">
                  <Label htmlFor="event-department-toggle" className="cursor-pointer text-xs font-normal text-muted-foreground">
                    Hospital-wide
                  </Label>
                  <Switch
                    id="event-department-toggle"
                    checked={!assignDepartment}
                    onCheckedChange={(checked) => {
                      setAssignDepartment(!checked);
                      setValue('departmentId', undefined, { shouldDirty: true });
                    }}
                  />
                </div>
              </div>
              {assignDepartment && (
                <DepartmentSelect
                  id="event-department"
                  value={values.departmentId ?? ''}
                  onValueChange={(value) => setValue('departmentId', value, { shouldDirty: true })}
                />
              )}
            </div>
          </div>

          {submitError && (
            <p role="alert" className="mx-6 mb-3 rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
              {submitError}
            </p>
          )}

          <div className="sticky bottom-0 flex justify-end gap-3 border-t border-border bg-background px-6 py-4">
            <Button type="button" variant="outline" onClick={onClose}>
              Cancel
            </Button>
            <Button type="submit" disabled={!valid || isSubmitting}>
              {isSubmitting ? 'Saving…' : 'Save Event'}
            </Button>
          </div>
        </form>
      </SheetContent>
    </Sheet>
  );
}
