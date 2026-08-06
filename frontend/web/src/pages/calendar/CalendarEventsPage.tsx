import { useMemo, useState } from 'react';
import { CalendarDays, Menu, Plus } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Sheet, SheetContent } from '@/components/ui/sheet';
import { useDebouncedValue } from '@/hooks/useDebouncedValue';
import {
  CalendarSidebar,
  CalendarToolbar,
  DeleteEventDialog,
  EmptyState,
  EventDetailsDrawer,
  EventFormDrawer,
  EVENT_TYPES,
  MonthGrid,
  SearchResultsList,
  SidebarSkeleton,
  CalendarGridSkeleton,
  createEmptyFilters,
  getUpcomingMockEvents,
  isoDateRangesOverlap,
  todayIso,
  useCalendarEventsQuery,
  useCreateCalendarEventMutation,
  useDeleteCalendarEventMutation,
  useUpdateCalendarEventMutation,
  type CalendarEvent,
  type CalendarEventFilters,
  type CalendarEventFormValues,
  type EventType,
} from '@/features/calendarEvents';
import { addMonths } from '@/features/calendarEvents/utils/date';

const now = new Date();

type FormDrawerState =
  | { open: false }
  | { open: true; mode: 'create'; defaultDate: string | null }
  | { open: true; mode: 'edit'; event: CalendarEvent };

export default function CalendarEventsPage() {
  const [viewYear, setViewYear] = useState(now.getFullYear());
  const [viewMonth, setViewMonth] = useState(now.getMonth() + 1);
  const [search, setSearch] = useState('');
  const [filters, setFilters] = useState<CalendarEventFilters>(createEmptyFilters());
  const [filterPanelOpen, setFilterPanelOpen] = useState(false);
  const [mobileSidebarOpen, setMobileSidebarOpen] = useState(false);
  const [selectedEvent, setSelectedEvent] = useState<CalendarEvent | null>(null);
  const [formDrawer, setFormDrawer] = useState<FormDrawerState>({ open: false });
  const [deleteTarget, setDeleteTarget] = useState<CalendarEvent | null>(null);
  const [submitError, setSubmitError] = useState<string | null>(null);

  const debouncedSearch = useDebouncedValue(search, 200);

  const allEventsQuery = useCalendarEventsQuery({ types: [] });
  const visibleEventsQuery = useCalendarEventsQuery({ ...filters, search: debouncedSearch || undefined });

  const createMutation = useCreateCalendarEventMutation();
  const updateMutation = useUpdateCalendarEventMutation();
  const deleteMutation = useDeleteCalendarEventMutation();

  const allEvents = useMemo(() => allEventsQuery.data ?? [], [allEventsQuery.data]);
  const visibleEvents = visibleEventsQuery.data ?? [];
  const isPending = allEventsQuery.isPending || visibleEventsQuery.isPending;

  const typeCounts = useMemo(() => {
    const counts: Partial<Record<EventType, number>> = {};
    for (const type of EVENT_TYPES) counts[type] = 0;
    for (const event of allEvents) counts[event.eventType] = (counts[event.eventType] ?? 0) + 1;
    return counts;
  }, [allEvents]);

  const eventDatesInNavMonth = useMemo(() => {
    const monthStart = `${viewYear}-${String(viewMonth).padStart(2, '0')}-01`;
    const monthEnd = `${viewYear}-${String(viewMonth).padStart(2, '0')}-31`;
    const dates = new Set<string>();
    for (const event of allEvents) {
      if (isoDateRangesOverlap(event.startDate, event.endDate, monthStart, monthEnd)) {
        dates.add(event.startDate);
      }
    }
    return dates;
  }, [allEvents, viewYear, viewMonth]);

  const upcomingEvents = useMemo(() => getUpcomingMockEvents(todayIso(), 5), [allEvents]);

  function goToMonth(delta: number) {
    const { year, month } = addMonths(viewYear, viewMonth, delta);
    setViewYear(year);
    setViewMonth(month);
  }

  function goToToday() {
    setViewYear(now.getFullYear());
    setViewMonth(now.getMonth() + 1);
  }

  function openCreateDrawer(defaultDate: string | null = null) {
    setFormDrawer({ open: true, mode: 'create', defaultDate });
    setMobileSidebarOpen(false);
  }

  function openEditDrawer(event: CalendarEvent) {
    setSelectedEvent(null);
    setFormDrawer({ open: true, mode: 'edit', event });
  }

  function closeFormDrawer() {
    setFormDrawer({ open: false });
    setSubmitError(null);
  }

  function handleSubmit(values: CalendarEventFormValues) {
    setSubmitError(null);
    if (formDrawer.open && formDrawer.mode === 'edit') {
      updateMutation.mutate(
        { id: formDrawer.event.id, values },
        { onSuccess: () => closeFormDrawer(), onError: (err) => setSubmitError(err instanceof Error ? err.message : 'Failed to save event.') },
      );
    } else {
      createMutation.mutate(values, {
        onSuccess: () => closeFormDrawer(),
        onError: (err) => setSubmitError(err instanceof Error ? err.message : 'Failed to save event.'),
      });
    }
  }

  function handleConfirmDelete() {
    if (!deleteTarget) return;
    deleteMutation.mutate(deleteTarget.id, { onSuccess: () => setDeleteTarget(null) });
  }

  const sidebarProps = {
    onCreateEvent: () => openCreateDrawer(),
    search,
    onSearchChange: setSearch,
    navYear: viewYear,
    navMonth: viewMonth,
    onNavPrevMonth: () => goToMonth(-1),
    onNavNextMonth: () => goToMonth(1),
    onNavSelectDate: (iso: string) => {
      const [y, m] = iso.split('-').map(Number);
      setViewYear(y);
      setViewMonth(m);
      setMobileSidebarOpen(false);
    },
    eventDatesInNavMonth,
    filters,
    onFiltersChange: setFilters,
    typeCounts,
    upcomingEvents,
    onSelectEvent: (event: CalendarEvent) => {
      setSelectedEvent(event);
      setMobileSidebarOpen(false);
    },
  };

  return (
    <div className="flex flex-1 flex-col">
      <div className="flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        <div className="flex items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
            <CalendarDays className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight">Calendar &amp; Events</h1>
        </div>
        <p className="max-w-2xl text-sm text-page-banner-foreground/85">
          Hospital holidays, events, doctor leave, meetings, training, and maintenance — all in one place.
        </p>
      </div>

      <div className="flex min-h-0 flex-1">
        {isPending ? (
          <SidebarSkeleton />
        ) : (
          <div className="hidden lg:flex">
            <CalendarSidebar {...sidebarProps} />
          </div>
        )}

        <Sheet open={mobileSidebarOpen} onOpenChange={setMobileSidebarOpen}>
          <SheetContent side="left" className="w-full max-w-xs gap-0 p-0 lg:hidden">
            <CalendarSidebar {...sidebarProps} />
          </SheetContent>
        </Sheet>

        <div className="flex min-w-0 flex-1 flex-col">
          <CalendarToolbar
            year={viewYear}
            month={viewMonth}
            onPrevMonth={() => goToMonth(-1)}
            onNextMonth={() => goToMonth(1)}
            onToday={goToToday}
            search={search}
            onSearchChange={setSearch}
            filters={filters}
            onFiltersChange={setFilters}
            filterPanelOpen={filterPanelOpen}
            onFilterPanelOpenChange={setFilterPanelOpen}
            onRefresh={() => {
              allEventsQuery.refetch();
              visibleEventsQuery.refetch();
            }}
            isRefreshing={allEventsQuery.isFetching || visibleEventsQuery.isFetching}
          />

          <div className="flex items-center gap-2 border-b border-border px-4 py-2 lg:hidden">
            <Button variant="outline" size="sm" onClick={() => setMobileSidebarOpen(true)}>
              <Menu className="h-4 w-4" />
              Filters &amp; Calendar
            </Button>
          </div>

          <div className="flex-1 overflow-y-auto">
            {isPending ? (
              <CalendarGridSkeleton />
            ) : debouncedSearch ? (
              <SearchResultsList query={debouncedSearch} results={visibleEvents} onSelect={setSelectedEvent} />
            ) : visibleEvents.length === 0 ? (
              <EmptyState onCreateEvent={() => openCreateDrawer()} />
            ) : (
              <div className="p-4 sm:p-6">
                <MonthGrid
                  year={viewYear}
                  month={viewMonth}
                  events={visibleEvents}
                  onSelectEvent={setSelectedEvent}
                  onCreateAt={(iso) => openCreateDrawer(iso)}
                />
              </div>
            )}
          </div>
        </div>
      </div>

      <Button
        onClick={() => openCreateDrawer()}
        size="icon"
        aria-label="Create Event"
        className="fixed bottom-6 right-6 z-30 h-14 w-14 rounded-full shadow-soft-lg lg:hidden"
      >
        <Plus className="h-6 w-6" />
      </Button>

      <EventDetailsDrawer
        event={selectedEvent}
        onClose={() => setSelectedEvent(null)}
        onEdit={openEditDrawer}
        onDelete={(event) => {
          setSelectedEvent(null);
          setDeleteTarget(event);
        }}
      />

      {formDrawer.open && (
        <EventFormDrawer
          open={formDrawer.open}
          mode={formDrawer.mode}
          event={formDrawer.mode === 'edit' ? formDrawer.event : null}
          defaultDate={formDrawer.mode === 'create' ? formDrawer.defaultDate : null}
          existingEvents={allEvents}
          onClose={closeFormDrawer}
          onSubmit={handleSubmit}
          isSubmitting={createMutation.isPending || updateMutation.isPending}
          submitError={submitError}
        />
      )}

      {deleteTarget && (
        <DeleteEventDialog
          event={deleteTarget}
          isDeleting={deleteMutation.isPending}
          onConfirm={handleConfirmDelete}
          onCancel={() => setDeleteTarget(null)}
        />
      )}
    </div>
  );
}
