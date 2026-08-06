import type { Shift, ShiftAssignment } from '@hms/shared';
import { AlertTriangle, CalendarOff, PartyPopper, Repeat, UserCheck } from 'lucide-react';
import { StaffName } from '@/components/StaffName';
import { DAY_LABELS, formatDisplayDate, getWeekDates } from '../utils/week';

const SHIFT_ROWS = ['Morning', 'Evening', 'Night'] as const;
type ShiftRow = (typeof SHIFT_ROWS)[number];

// Shift has no "category" field (only name + isNightShift), so there is no authoritative
// way to bucket a shift into Morning/Evening/Night — this heuristic (isNightShift wins,
// otherwise split on start hour) is a stand-in until the team confirms a real mapping rule
// (see docs/modules/HR/WeeklyRosterPlanningBoard.md §4.5).
function resolveShiftRow(shift: Shift): ShiftRow {
  if (shift.isNightShift) {
    return 'Night';
  }
  const startHour = Number(shift.startTime.slice(0, 2));
  return startHour < 14 ? 'Morning' : 'Evening';
}

interface WeeklyRosterMatrixProps {
  weekStartDate: string;
  shifts: Shift[];
  assignments: ShiftAssignment[];
}

const RESERVED_PLACEHOLDERS = [
  { icon: CalendarOff, label: 'Leave' },
  { icon: PartyPopper, label: 'Holiday' },
  { icon: UserCheck, label: 'Availability' },
  { icon: Repeat, label: 'Swap requests' },
  { icon: AlertTriangle, label: 'Conflicts' },
];

export function WeeklyRosterMatrix({ weekStartDate, shifts, assignments }: WeeklyRosterMatrixProps) {
  const weekDates = getWeekDates(weekStartDate);
  const shiftById = new Map(shifts.map((shift) => [shift.id, shift]));

  const cellsByKey = new Map<string, ShiftAssignment[]>();
  for (const assignment of assignments) {
    const shift = shiftById.get(assignment.shiftId);
    if (!shift) {
      continue;
    }
    const key = `${resolveShiftRow(shift)}|${assignment.rosterDate}`;
    const existing = cellsByKey.get(key);
    if (existing) {
      existing.push(assignment);
    } else {
      cellsByKey.set(key, [assignment]);
    }
  }

  return (
    <div className="overflow-x-auto rounded-lg border border-border">
      <table className="w-full min-w-[800px] border-collapse text-sm">
        <thead>
          <tr className="bg-muted/50 text-left text-xs font-medium uppercase tracking-wide text-muted-foreground">
            <th className="w-32 px-4 py-2.5">Shift</th>
            {weekDates.map((date, index) => (
              <th key={date} className="px-4 py-2.5">
                {DAY_LABELS[index]}
                <span className="ml-1.5 font-normal normal-case text-muted-foreground/80">{formatDisplayDate(date)}</span>
              </th>
            ))}
          </tr>
          {/* Reserved (not implemented) space for Leave/Holiday/Availability/Swap/Conflict
              indicators — visually inert, no data wiring, per spec §4.6. */}
          <tr className="border-t border-border/60">
            <th className="px-4 py-1.5" />
            {weekDates.map((date) => (
              <th key={date} className="px-4 py-1.5 font-normal">
                <div className="flex items-center gap-1.5 opacity-40">
                  {RESERVED_PLACEHOLDERS.map(({ icon: Icon, label }) => (
                    <Icon key={label} className="h-3.5 w-3.5" aria-label={label} />
                  ))}
                </div>
              </th>
            ))}
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {SHIFT_ROWS.map((row) => (
            <tr key={row}>
              <td className="px-4 py-3 font-medium text-foreground">{row}</td>
              {weekDates.map((date) => {
                const cellAssignments = cellsByKey.get(`${row}|${date}`) ?? [];
                return (
                  <td key={date} className="px-4 py-3 align-top text-foreground">
                    {cellAssignments.length === 0 ? (
                      <span className="text-muted-foreground">—</span>
                    ) : (
                      <div className="flex flex-col gap-1">
                        {cellAssignments.map((assignment) => (
                          <span key={assignment.id}>
                            <StaffName staffId={assignment.staffId} />
                          </span>
                        ))}
                      </div>
                    )}
                  </td>
                );
              })}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
