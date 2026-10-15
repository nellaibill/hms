import type { Appointment } from '@hms/shared';
import { CalendarOff, Loader2, Trash2 } from 'lucide-react';
import { useState } from 'react';
import { ConsultantSelect } from '@/components/ConsultantSelect';
import { DepartmentSelect } from '@/components/DepartmentSelect';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { toDisplayError } from '@/features/patients/apiErrorDisplay';
import { todayDateInputValue } from '@/lib/reportDateRange';
import { formatAppointmentDate, formatSlotTime } from '../format';
import { useConsultantBlocksQuery, useCreateConsultantBlockMutation, useRemoveConsultantBlockMutation } from '../hooks';

const SESSION_OPTIONS = [
  { value: 'all', label: 'Whole day' },
  { value: '1', label: 'Session 1 only' },
  { value: '2', label: 'Session 2 only' },
] as const;

interface ConsultantLeaveDialogProps {
  departmentId?: string;
  consultantId?: string;
  onClose: () => void;
}

function sessionLabel(session?: number | null): string {
  return session ? `Session ${session}` : 'Whole day';
}

/** Doctor leave: close a consultant's slots for a range of days (or one session of them), and
 * see the bookings that now need moving. Bookings are never cancelled automatically. */
export function ConsultantLeaveDialog({ departmentId: initialDepartmentId, consultantId: initialConsultantId, onClose }: ConsultantLeaveDialogProps) {
  const today = todayDateInputValue();
  const [departmentId, setDepartmentId] = useState(initialDepartmentId ?? '');
  const [consultantId, setConsultantId] = useState(initialConsultantId ?? '');
  const [fromDate, setFromDate] = useState(today);
  const [toDate, setToDate] = useState(today);
  const [session, setSession] = useState<string>('all');
  const [reason, setReason] = useState('');
  const [affected, setAffected] = useState<Appointment[] | null>(null);

  const blocks = useConsultantBlocksQuery({ consultantId, fromDate: today }, { enabled: Boolean(consultantId) });
  const createMutation = useCreateConsultantBlockMutation();
  const removeMutation = useRemoveConsultantBlockMutation();
  const apiError = toDisplayError(createMutation.error ?? removeMutation.error);
  const canSubmit = Boolean(consultantId && fromDate && toDate && toDate >= fromDate && reason.trim()) && !createMutation.isPending;

  function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    if (!canSubmit) return;
    createMutation.mutate(
      { consultantId, fromDate, toDate, session: session === 'all' ? null : Number(session), reason: reason.trim() },
      {
        onSuccess: (created) => {
          setAffected(created.affectedAppointments);
          setReason('');
        },
      },
    );
  }

  return (
    <Dialog open onOpenChange={(open) => !open && onClose()}>
      <DialogContent aria-labelledby="leave-title" className="max-h-[92vh] max-w-2xl overflow-y-auto">
        <DialogHeader>
          <DialogTitle id="leave-title">Doctor leave</DialogTitle>
          <DialogDescription>Close a consultant's slots so they can't be booked. Existing bookings stay — move or cancel them from the list.</DialogDescription>
        </DialogHeader>

        {apiError && (
          <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
            {apiError.validationErrors?.length ? apiError.validationErrors.map((item) => item.message).join(' ') : apiError.message}
          </p>
        )}

        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="leave-department">Department</Label>
            <DepartmentSelect
              id="leave-department"
              value={departmentId}
              onValueChange={(value) => {
                setDepartmentId(value);
                setConsultantId('');
                setAffected(null);
              }}
            />
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="leave-consultant">Consultant</Label>
            <ConsultantSelect
              id="leave-consultant"
              value={consultantId}
              departmentId={departmentId || undefined}
              onValueChange={(value) => {
                setConsultantId(value);
                setAffected(null);
              }}
            />
          </div>
        </div>

        {consultantId && (
          <section className="flex flex-col gap-2">
            <h3 className="text-sm font-semibold text-foreground">Upcoming leave</h3>
            {blocks.isPending && (
              <p className="flex items-center gap-2 text-sm text-muted-foreground">
                <Loader2 className="h-4 w-4 animate-spin" /> Loading…
              </p>
            )}
            {blocks.data && blocks.data.length === 0 && <p className="text-sm text-muted-foreground">None.</p>}
            {blocks.data && blocks.data.length > 0 && (
              <ul className="divide-y divide-border rounded-md border border-border">
                {blocks.data.map((block) => (
                  <li key={block.id} className="flex items-center justify-between gap-3 px-3 py-2 text-sm">
                    <span>
                      <span className="font-medium text-foreground">
                        {formatAppointmentDate(block.fromDate)}
                        {block.toDate !== block.fromDate && ` – ${formatAppointmentDate(block.toDate)}`}
                      </span>
                      <span className="ml-2 text-muted-foreground">
                        {sessionLabel(block.session)} · {block.reason}
                      </span>
                    </span>
                    <Button
                      type="button"
                      size="sm"
                      variant="ghost"
                      aria-label="Remove leave"
                      disabled={removeMutation.isPending}
                      onClick={() => removeMutation.mutate(block.id)}
                    >
                      <Trash2 className="h-4 w-4" />
                    </Button>
                  </li>
                ))}
              </ul>
            )}
          </section>
        )}

        <form onSubmit={handleSubmit} className="flex flex-col gap-3 rounded-md border border-border p-3">
          <h3 className="text-sm font-semibold text-foreground">Add leave</h3>
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="leave-from">From</Label>
              <Input
                id="leave-from"
                type="date"
                min={today}
                value={fromDate}
                onChange={(event) => {
                  setFromDate(event.target.value);
                  if (toDate < event.target.value) setToDate(event.target.value);
                }}
              />
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="leave-to">To</Label>
              <Input id="leave-to" type="date" min={fromDate} value={toDate} onChange={(event) => setToDate(event.target.value)} />
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="leave-session">Covers</Label>
              <Select value={session} onValueChange={setSession}>
                <SelectTrigger id="leave-session" aria-label="Covers">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {SESSION_OPTIONS.map((option) => (
                    <SelectItem key={option.value} value={option.value}>
                      {option.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="leave-reason">Reason</Label>
            <Input id="leave-reason" value={reason} maxLength={300} onChange={(event) => setReason(event.target.value)} placeholder="e.g. Conference, personal leave, OT list" />
          </div>
          <div className="flex justify-end">
            <Button type="submit" disabled={!canSubmit}>
              <CalendarOff className="h-4 w-4" />
              {createMutation.isPending ? 'Saving…' : 'Block these slots'}
            </Button>
          </div>
        </form>

        {affected && (
          <section className="flex flex-col gap-2" aria-live="polite">
            {affected.length === 0 ? (
              <p className="rounded-md bg-success/10 px-3 py-2 text-sm text-foreground">Leave saved. No bookings fall inside it.</p>
            ) : (
              <>
                <p className="rounded-md bg-warning/10 px-3 py-2 text-sm text-foreground">
                  Leave saved. {affected.length === 1 ? '1 booking falls' : `${affected.length} bookings fall`} inside it — call these patients and reschedule or cancel from the
                  appointment list:
                </p>
                <ul className="divide-y divide-border rounded-md border border-border text-sm">
                  {affected.map((appointment) => (
                    <li key={appointment.id} className="flex justify-between gap-3 px-3 py-2">
                      <span className="font-medium text-foreground">{appointment.patientName}</span>
                      <span className="text-muted-foreground">
                        {formatAppointmentDate(appointment.appointmentDate)} {formatSlotTime(appointment.startTime)} · {appointment.phoneNumber}
                      </span>
                    </li>
                  ))}
                </ul>
              </>
            )}
          </section>
        )}

        <DialogFooter>
          <Button type="button" variant="outline" onClick={onClose}>
            Done
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
