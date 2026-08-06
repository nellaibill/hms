import { Plus, Search, X } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { ScrollArea } from '@/components/ui/scroll-area';
import { useDepartmentNames } from '../hooks/useDepartmentDirectory';
import { EventTypeFilterList } from './EventTypeFilterList';
import { MiniNavCalendar } from './MiniNavCalendar';
import { UpcomingEventsList } from './UpcomingEventsList';
import { isFiltersEmpty, type CalendarEventFilters } from '../types';
import type { CalendarEvent, EventType } from '../types';

interface CalendarSidebarProps {
  onCreateEvent: () => void;
  search: string;
  onSearchChange: (value: string) => void;
  navYear: number;
  navMonth: number;
  onNavPrevMonth: () => void;
  onNavNextMonth: () => void;
  onNavSelectDate: (iso: string) => void;
  eventDatesInNavMonth: Set<string>;
  filters: CalendarEventFilters;
  onFiltersChange: (filters: CalendarEventFilters) => void;
  typeCounts: Partial<Record<EventType, number>>;
  upcomingEvents: CalendarEvent[];
  onSelectEvent: (event: CalendarEvent) => void;
}

export function CalendarSidebar({
  onCreateEvent,
  search,
  onSearchChange,
  navYear,
  navMonth,
  onNavPrevMonth,
  onNavNextMonth,
  onNavSelectDate,
  eventDatesInNavMonth,
  filters,
  onFiltersChange,
  typeCounts,
  upcomingEvents,
  onSelectEvent,
}: CalendarSidebarProps) {
  const filtersActive = !isFiltersEmpty(filters);
  const departmentNames = useDepartmentNames();

  return (
    <aside className="flex h-full w-full flex-col border-r border-border bg-card sm:w-[280px] sm:shrink-0">
      <ScrollArea className="h-full">
        <div className="flex flex-col gap-5 p-4">
          <Button onClick={onCreateEvent} className="w-full justify-center">
            <Plus className="h-4 w-4" />
            Create Event
          </Button>

          <div className="relative">
            <Search className="pointer-events-none absolute left-2.5 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              value={search}
              onChange={(e) => onSearchChange(e.target.value)}
              placeholder="Search events…"
              aria-label="Search events"
              className="pl-8"
            />
          </div>

          <MiniNavCalendar
            year={navYear}
            month={navMonth}
            onPrevMonth={onNavPrevMonth}
            onNextMonth={onNavNextMonth}
            onSelectDate={onNavSelectDate}
            eventDates={eventDatesInNavMonth}
          />

          <EventTypeFilterList
            selected={filters.types}
            onChange={(types) => onFiltersChange({ ...filters, types })}
            counts={typeCounts}
          />

          <div>
            <label htmlFor="calendar-department-filter" className="mb-1.5 block text-xs font-semibold uppercase tracking-wide text-muted-foreground">
              Department
            </label>
            <Select
              value={filters.department ?? 'all'}
              onValueChange={(value) => onFiltersChange({ ...filters, department: value === 'all' ? undefined : value })}
            >
              <SelectTrigger id="calendar-department-filter" className="h-9 text-sm">
                <SelectValue placeholder="All departments" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All departments</SelectItem>
                {departmentNames.map((dept) => (
                  <SelectItem key={dept} value={dept}>
                    {dept}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <UpcomingEventsList events={upcomingEvents} onSelect={onSelectEvent} />

          {filtersActive && (
            <Button variant="ghost" size="sm" onClick={() => onFiltersChange({ types: [] })} className="justify-center text-muted-foreground">
              <X className="h-3.5 w-3.5" />
              Clear Filters
            </Button>
          )}
        </div>
      </ScrollArea>
    </aside>
  );
}
