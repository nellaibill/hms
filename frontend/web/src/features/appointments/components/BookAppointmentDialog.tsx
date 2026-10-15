import { APPOINTMENT_SOURCES, type Appointment, type AppointmentSource, type Patient } from '@hms/shared';
import { UserPlus, UserRound } from 'lucide-react';
import { useState } from 'react';
import { AppointmentTypeSelect } from '@/components/AppointmentTypeSelect';
import { ConsultantSelect } from '@/components/ConsultantSelect';
import { DepartmentSelect } from '@/components/DepartmentSelect';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { toDisplayError } from '@/features/patients/apiErrorDisplay';
import { todayDateInputValue } from '@/lib/reportDateRange';
import { cn } from '@/lib/utils';
import { SOURCE_LABELS, formatAppointmentDate, formatSlotTime } from '../format';
import { useBookAppointmentMutation } from '../hooks';
import { PatientQuickSearch } from './PatientQuickSearch';
import { SlotPicker } from './SlotPicker';

export interface BookAppointmentDefaults {
  date: string;
  departmentId?: string;
  consultantId?: string;
  startTime?: string;
  patient?: Patient;
}

interface BookAppointmentDialogProps {
  defaults: BookAppointmentDefaults;
  onClose: () => void;
  onBooked: (appointment: Appointment) => void;
}

const PHONE_PATTERN = /^[0-9]{10}$/;
const NAME_PATTERN = /^[\p{L}\p{M}][\p{L}\p{M}\s'.-]*$/u;

const textareaClassName =
  'flex w-full rounded-md border border-input bg-background px-3 py-2 text-sm shadow-sm placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2';

/** Book one seat in a consultant's slot — for a registered patient (searched by name, UHID or
 * phone) or a first-time caller known only by name and phone, who is registered on arrival. */
export function BookAppointmentDialog({ defaults, onClose, onBooked }: BookAppointmentDialogProps) {
  const mutation = useBookAppointmentMutation();
  const [mode, setMode] = useState<'registered' | 'new'>('registered');
  const [patient, setPatient] = useState<Patient | null>(defaults.patient ?? null);
  const [newName, setNewName] = useState('');
  const [newPhone, setNewPhone] = useState('');
  const [departmentId, setDepartmentId] = useState(defaults.departmentId ?? '');
  const [consultantId, setConsultantId] = useState(defaults.consultantId ?? '');
  const [date, setDate] = useState(defaults.date);
  const [startTime, setStartTime] = useState(defaults.startTime ?? '');
  const [appointmentTypeId, setAppointmentTypeId] = useState('');
  const [source, setSource] = useState<AppointmentSource>('Phone');
  const [notes, setNotes] = useState('');

  const apiError = toDisplayError(mutation.error);
  const nameError = newName.trim() && !NAME_PATTERN.test(newName.trim()) ? 'Letters only.' : null;
  const phoneError = newPhone && !PHONE_PATTERN.test(newPhone) ? 'Enter a 10-digit number.' : null;
  const hasPatient = mode === 'registered' ? Boolean(patient) : Boolean(newName.trim() && PHONE_PATTERN.test(newPhone) && !nameError);
  const canSubmit = hasPatient && Boolean(departmentId && consultantId && date && startTime) && !mutation.isPending;

  function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    if (!canSubmit) return;
    mutation.mutate(
      {
        patientId: mode === 'registered' ? patient!.id : null,
        patientName: mode === 'new' ? newName.trim() : null,
        phoneNumber: mode === 'new' ? newPhone : null,
        departmentId,
        consultantId,
        appointmentTypeId: appointmentTypeId || null,
        appointmentDate: date,
        startTime,
        source,
        notes: notes.trim() || null,
      },
      { onSuccess: onBooked },
    );
  }

  return (
    <Dialog open onOpenChange={(open) => !open && onClose()}>
      <DialogContent aria-labelledby="book-appointment-title" className="max-h-[92vh] max-w-3xl overflow-y-auto">
        <DialogHeader>
          <DialogTitle id="book-appointment-title">Book Appointment</DialogTitle>
          <DialogDescription>Pick the patient, the doctor and a free slot. The token number follows the slot.</DialogDescription>
        </DialogHeader>

        {apiError && (
          <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
            {apiError.validationErrors?.length ? apiError.validationErrors.map((item) => item.message).join(' ') : apiError.message}
          </p>
        )}

        <form onSubmit={handleSubmit} className="flex flex-col gap-5">
          <section className="flex flex-col gap-3">
            <div className="flex items-center justify-between gap-3">
              <h3 className="text-sm font-semibold text-foreground">Patient</h3>
              <div className="inline-flex rounded-md border border-border p-0.5" role="tablist" aria-label="Patient type">
                {(
                  [
                    ['registered', 'Registered patient', UserRound],
                    ['new', 'New caller', UserPlus],
                  ] as const
                ).map(([value, label, Icon]) => (
                  <button
                    key={value}
                    type="button"
                    role="tab"
                    aria-selected={mode === value}
                    onClick={() => setMode(value)}
                    className={cn(
                      'inline-flex items-center gap-1.5 rounded px-2.5 py-1 text-xs font-medium transition-colors',
                      mode === value ? 'bg-primary text-primary-foreground' : 'text-muted-foreground hover:text-foreground',
                    )}
                  >
                    <Icon className="h-3.5 w-3.5" />
                    {label}
                  </button>
                ))}
              </div>
            </div>

            {mode === 'registered' && patient && (
              <div className="flex items-center justify-between gap-3 rounded-md border border-border bg-muted/30 px-3 py-2.5 text-sm">
                <span className="font-medium text-foreground">
                  {patient.title} {patient.firstName} {patient.lastName}
                  <span className="ml-2 font-mono text-xs text-muted-foreground">{patient.uhid}</span>
                  <span className="ml-2 text-xs text-muted-foreground">{patient.primaryPhone}</span>
                </span>
                <Button type="button" variant="outline" size="sm" onClick={() => setPatient(null)}>
                  Change
                </Button>
              </div>
            )}

            {mode === 'registered' && !patient && (
              <PatientQuickSearch onSelect={setPatient} noMatchAction={{ label: 'Book as a new caller', onClick: () => setMode('new') }} />
            )}

            {mode === 'new' && (
              <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
                <div className="flex flex-col gap-1.5">
                  <Label htmlFor="book-new-name">Patient name</Label>
                  <Input id="book-new-name" value={newName} maxLength={150} onChange={(event) => setNewName(event.target.value)} placeholder="Full name" />
                  {nameError && <p className="text-xs text-destructive">{nameError}</p>}
                </div>
                <div className="flex flex-col gap-1.5">
                  <Label htmlFor="book-new-phone">Phone number</Label>
                  <Input
                    id="book-new-phone"
                    inputMode="numeric"
                    value={newPhone}
                    maxLength={10}
                    onChange={(event) => setNewPhone(event.target.value.replace(/\D/g, ''))}
                    placeholder="10-digit mobile"
                  />
                  {phoneError && <p className="text-xs text-destructive">{phoneError}</p>}
                </div>
                <p className="text-xs text-muted-foreground sm:col-span-2">They'll be registered at the desk when they arrive; the booking then links to their new UHID.</p>
              </div>
            )}
          </section>

          <section className="flex flex-col gap-3">
            <h3 className="text-sm font-semibold text-foreground">Doctor and time</h3>
            <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="book-department">Department</Label>
                <DepartmentSelect
                  id="book-department"
                  value={departmentId}
                  onValueChange={(value) => {
                    setDepartmentId(value);
                    setConsultantId('');
                    setStartTime('');
                  }}
                />
              </div>
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="book-consultant">Consultant</Label>
                <ConsultantSelect
                  id="book-consultant"
                  value={consultantId}
                  departmentId={departmentId || undefined}
                  onValueChange={(value) => {
                    setConsultantId(value);
                    setStartTime('');
                  }}
                />
              </div>
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="book-date">Date</Label>
                <Input
                  id="book-date"
                  type="date"
                  min={todayDateInputValue()}
                  value={date}
                  onChange={(event) => {
                    setDate(event.target.value);
                    setStartTime('');
                  }}
                />
              </div>
            </div>
            <SlotPicker consultantId={consultantId || undefined} date={date || undefined} value={startTime} onChange={setStartTime} />
          </section>

          <section className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="book-appointment-type">Appointment type (optional)</Label>
              <AppointmentTypeSelect id="book-appointment-type" value={appointmentTypeId} onValueChange={setAppointmentTypeId} />
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="book-source">Booked by</Label>
              <Select value={source} onValueChange={(value) => setSource(value as AppointmentSource)}>
                <SelectTrigger id="book-source" aria-label="Booked by">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {APPOINTMENT_SOURCES.map((item) => (
                    <SelectItem key={item} value={item}>
                      {SOURCE_LABELS[item]}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="flex flex-col gap-1.5 sm:col-span-2">
              <Label htmlFor="book-notes">Notes (optional)</Label>
              <textarea
                id="book-notes"
                rows={2}
                maxLength={500}
                value={notes}
                onChange={(event) => setNotes(event.target.value)}
                className={textareaClassName}
                placeholder="e.g. follow-up for BP review, bring old reports"
              />
            </div>
          </section>

          <DialogFooter className="items-center gap-2 sm:justify-between">
            <p className="text-sm text-muted-foreground">
              {startTime ? `${formatAppointmentDate(date)} at ${formatSlotTime(startTime)}` : 'No slot picked yet'}
            </p>
            <div className="flex gap-2">
              <Button type="button" variant="outline" onClick={onClose} disabled={mutation.isPending}>
                Cancel
              </Button>
              <Button type="submit" disabled={!canSubmit}>
                {mutation.isPending ? 'Booking…' : 'Book appointment'}
              </Button>
            </div>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
