import { APPOINTMENT_STATUSES, type Appointment, type AppointmentStatus } from '@hms/shared';
import { CalendarClock, CalendarOff, ChevronLeft, ChevronRight, Loader2, Plus, Search } from 'lucide-react';
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { ConsultantSelect } from '@/components/ConsultantSelect';
import { DepartmentSelect } from '@/components/DepartmentSelect';
import { LoadingOverlay } from '@/components/LoadingOverlay';
import { PageBanner } from '@/components/PageBanner';
import { Pagination } from '@/components/Pagination';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { useToast } from '@/components/ui/toast-context';
import {
  AppointmentsTable,
  BookAppointmentDialog,
  CancelAppointmentDialog,
  CheckInRegisteredPatientDialog,
  ConsultantLeaveDialog,
  RescheduleAppointmentDialog,
  SlotPicker,
  addDays,
  appointmentStatusLabel,
  checkInPath,
  formatAppointmentDate,
  formatSlotTime,
  useAppointmentSummaryQuery,
  useAppointmentsQuery,
  useMarkNoShowMutation,
  type AppointmentAction,
  type BookAppointmentDefaults,
} from '@/features/appointments';
import { useAuth } from '@/features/auth/AuthContext';
import { useDebouncedValue } from '@/hooks/useDebouncedValue';
import { todayDateInputValue } from '@/lib/reportDateRange';
import { cn } from '@/lib/utils';

const PAGE_SIZE = 50;
const ALL = 'all';

type OpenDialog =
  | { kind: 'book'; defaults: BookAppointmentDefaults }
  | { kind: 'reschedule'; appointment: Appointment }
  | { kind: 'cancel'; appointment: Appointment }
  | { kind: 'check-in-registered'; appointment: Appointment }
  | { kind: 'leave' }
  | null;

/**
 * The appointment diary: one day at a time, every consultant or one. With a consultant picked,
 * their slot grid sits above the list — click a free slot to book it. "Check in" on the day
 * sends reception to the normal visit registration, pre-filled from the booking.
 */
export default function AppointmentsPage() {
  const navigate = useNavigate();
  const { toast } = useToast();
  const { hasPermission } = useAuth();
  const canBook = hasPermission('patient-management.create');
  const canEdit = hasPermission('patient-management.edit');

  const today = todayDateInputValue();
  const [date, setDate] = useState(today);
  const [departmentId, setDepartmentId] = useState('');
  const [consultantId, setConsultantId] = useState('');
  const [status, setStatus] = useState<string>(ALL);
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [dialog, setDialog] = useState<OpenDialog>(null);
  const debouncedSearch = useDebouncedValue(search.trim());
  const noShow = useMarkNoShowMutation();

  const filters = { consultantId: consultantId || undefined, departmentId: departmentId || undefined };
  const summary = useAppointmentSummaryQuery(date, filters);
  const list = useAppointmentsQuery({
    fromDate: date,
    toDate: date,
    ...filters,
    status: status === ALL ? undefined : (status as AppointmentStatus),
    search: debouncedSearch || undefined,
    page,
    pageSize: PAGE_SIZE,
  });

  function changeDate(next: string) {
    setDate(next);
    setPage(1);
  }

  function handleAction(action: AppointmentAction, appointment: Appointment) {
    switch (action) {
      case 'check-in':
        navigate(checkInPath(appointment));
        return;
      case 'check-in-registered':
        setDialog({ kind: 'check-in-registered', appointment });
        return;
      case 'reschedule':
        setDialog({ kind: 'reschedule', appointment });
        return;
      case 'cancel':
        setDialog({ kind: 'cancel', appointment });
        return;
      case 'no-show':
        noShow.mutate(appointment.id, {
          onSuccess: (updated) => toast({ title: 'Marked as no-show', description: `${updated.patientName} · token ${updated.tokenNumber}`, variant: 'success' }),
          onError: (error) => toast({ title: 'Could not mark no-show', description: error.message, variant: 'error' }),
        });
    }
  }

  function openBooking(startTime?: string) {
    setDialog({ kind: 'book', defaults: { date: date < today ? today : date, departmentId: departmentId || undefined, consultantId: consultantId || undefined, startTime } });
  }

  const counts = summary.data;
  const tiles: { label: string; value: number | undefined; tone: string; status?: AppointmentStatus }[] = [
    { label: 'Total', value: counts?.total, tone: 'text-foreground' },
    { label: appointmentStatusLabel('Booked'), value: counts?.booked, tone: 'text-warning-foreground', status: 'Booked' },
    { label: appointmentStatusLabel('Arrived'), value: counts?.arrived, tone: 'text-success', status: 'Arrived' },
    { label: appointmentStatusLabel('NoShow'), value: counts?.noShow, tone: 'text-muted-foreground', status: 'NoShow' },
    { label: appointmentStatusLabel('Cancelled'), value: counts?.cancelled, tone: 'text-destructive', status: 'Cancelled' },
  ];

  return (
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={CalendarClock}
        title="Appointments"
        subtitle="Book patients into a consultant's slots, move or cancel bookings, and check them in when they arrive."
        rightActions={
          <div className="flex gap-2">
            {canEdit && (
              <Button
                variant="outline"
                className="gap-1.5 border-page-banner-foreground/30 bg-page-banner-foreground/10 text-page-banner-foreground hover:bg-page-banner-foreground/20"
                onClick={() => setDialog({ kind: 'leave' })}
              >
                <CalendarOff className="h-4 w-4" />
                Doctor leave
              </Button>
            )}
            {canBook && (
              <Button className="gap-1.5" onClick={() => openBooking()}>
                <Plus className="h-4 w-4" />
                Book appointment
              </Button>
            )}
          </div>
        }
      />

      <div className="flex flex-1 flex-col gap-4 p-4 sm:p-6 lg:p-8">
        <div className="flex flex-col gap-3 rounded-lg border border-border bg-card p-3 lg:flex-row lg:flex-wrap lg:items-end">
          <div className="flex items-end gap-1.5">
            <Button variant="outline" size="icon" aria-label="Previous day" onClick={() => changeDate(addDays(date, -1))}>
              <ChevronLeft className="h-4 w-4" />
            </Button>
            <Input type="date" aria-label="Date" value={date} onChange={(event) => event.target.value && changeDate(event.target.value)} className="w-40" />
            <Button variant="outline" size="icon" aria-label="Next day" onClick={() => changeDate(addDays(date, 1))}>
              <ChevronRight className="h-4 w-4" />
            </Button>
            <Button variant={date === today ? 'secondary' : 'outline'} onClick={() => changeDate(today)}>
              Today
            </Button>
          </div>
          <div className="grid flex-1 grid-cols-1 gap-2 sm:grid-cols-2 lg:grid-cols-4">
            <DepartmentSelect
              id="appointments-department"
              value={departmentId}
              allOptionLabel="All departments"
              onValueChange={(value) => {
                setDepartmentId(value);
                setConsultantId('');
                setPage(1);
              }}
            />
            <ConsultantSelect
              id="appointments-consultant"
              value={consultantId}
              departmentId={departmentId || undefined}
              allOptionLabel="All consultants"
              onValueChange={(value) => {
                setConsultantId(value);
                setPage(1);
              }}
            />
            <Select
              value={status}
              onValueChange={(value) => {
                setStatus(value);
                setPage(1);
              }}
            >
              <SelectTrigger aria-label="Status">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value={ALL}>All statuses</SelectItem>
                {APPOINTMENT_STATUSES.map((item) => (
                  <SelectItem key={item} value={item}>
                    {appointmentStatusLabel(item)}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            <div className="relative">
              <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                value={search}
                onChange={(event) => {
                  setSearch(event.target.value);
                  setPage(1);
                }}
                placeholder="Name, phone, UHID, APT no…"
                aria-label="Search appointments"
                className="pl-9"
              />
            </div>
          </div>
        </div>

        <div className="flex flex-col gap-1">
          <h2 className="text-lg font-semibold text-foreground">{formatAppointmentDate(date)}</h2>
          <div className="grid grid-cols-2 gap-2 sm:grid-cols-5">
            {tiles.map((tile) => (
              <button
                key={tile.label}
                type="button"
                onClick={() => {
                  setStatus(tile.status ?? ALL);
                  setPage(1);
                }}
                className={cn(
                  'flex flex-col items-start rounded-lg border bg-card px-3 py-2 text-left transition-colors hover:border-primary/40',
                  (tile.status ?? ALL) === status ? 'border-primary/60 ring-1 ring-primary/30' : 'border-border',
                )}
              >
                <span className="text-xs text-muted-foreground">{tile.label}</span>
                <span className={cn('text-2xl font-semibold', tile.tone)}>{tile.value ?? '–'}</span>
              </button>
            ))}
          </div>
        </div>

        {consultantId && (
          <section className="flex flex-col gap-2 rounded-lg border border-border bg-card p-3" aria-label="Slots">
            <h3 className="text-sm font-semibold text-foreground">Slots{canBook ? ' — click a free one to book it' : ''}</h3>
            <SlotPicker compact consultantId={consultantId} date={date} value="" onChange={(startTime) => canBook && openBooking(startTime)} />
          </section>
        )}

        {list.isPending && (
          <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" />
            Loading appointments…
          </div>
        )}

        {list.isError && (
          <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
            {list.error instanceof Error ? list.error.message : 'Failed to load appointments.'}
          </p>
        )}

        {list.data && (
          <LoadingOverlay active={list.isPlaceholderData} label="Loading appointments…" className="flex flex-col gap-3">
            {list.data.items.length === 0 ? (
              <Card className="border-dashed">
                <CardContent className="flex flex-col items-center gap-2 py-14 text-center">
                  <CalendarClock className="h-8 w-8 text-muted-foreground" />
                  <p className="text-sm font-medium text-foreground">No appointments for this day</p>
                  <p className="text-sm text-muted-foreground">{canBook ? 'Book one, or pick another day.' : 'Pick another day or filter.'}</p>
                </CardContent>
              </Card>
            ) : (
              <>
                <AppointmentsTable
                  appointments={list.data.items}
                  today={today}
                  canEdit={canEdit}
                  busyId={noShow.isPending ? noShow.variables : null}
                  onAction={handleAction}
                />
                {list.data.meta.totalPages > 1 && <Pagination meta={list.data.meta} onPageChange={setPage} />}
              </>
            )}
          </LoadingOverlay>
        )}
      </div>

      {dialog?.kind === 'book' && (
        <BookAppointmentDialog
          defaults={dialog.defaults}
          onClose={() => setDialog(null)}
          onBooked={(appointment) => {
            setDialog(null);
            changeDate(appointment.appointmentDate);
            toast({
              title: `Booked — token ${appointment.tokenNumber}`,
              description: `${appointment.patientName}, ${formatAppointmentDate(appointment.appointmentDate)} at ${formatSlotTime(appointment.startTime)} (${appointment.appointmentNumber}).`,
              variant: 'success',
            });
          }}
        />
      )}
      {dialog?.kind === 'reschedule' && (
        <RescheduleAppointmentDialog
          appointment={dialog.appointment}
          onClose={() => setDialog(null)}
          onRescheduled={(appointment) => {
            setDialog(null);
            toast({
              title: 'Appointment moved',
              description: `${appointment.patientName} → ${formatAppointmentDate(appointment.appointmentDate)} at ${formatSlotTime(appointment.startTime)}, token ${appointment.tokenNumber}.`,
              variant: 'success',
            });
          }}
        />
      )}
      {dialog?.kind === 'cancel' && (
        <CancelAppointmentDialog
          appointment={dialog.appointment}
          onClose={() => setDialog(null)}
          onCancelled={(appointment) => {
            setDialog(null);
            toast({ title: 'Appointment cancelled', description: `${appointment.patientName} · ${appointment.appointmentNumber}`, variant: 'success' });
          }}
        />
      )}
      {dialog?.kind === 'check-in-registered' && <CheckInRegisteredPatientDialog appointment={dialog.appointment} onClose={() => setDialog(null)} />}
      {dialog?.kind === 'leave' && <ConsultantLeaveDialog departmentId={departmentId || undefined} consultantId={consultantId || undefined} onClose={() => setDialog(null)} />}
    </div>
  );
}
