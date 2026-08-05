import type { WeeklyRoster } from '@hms/shared';
import { CalendarClock, ChevronLeft, ChevronRight } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { getCurrentWeekStartDate } from '../utils/week';

interface WeeklyRosterWeekNavProps {
  roster: WeeklyRoster;
  /** Unfiltered roster list (no "list by department" endpoint exists) — narrowed to the
   * current roster's department here. */
  allRosters: WeeklyRoster[];
}

/** Previous/Current/Next week navigation (spec §4.4). */
export function WeeklyRosterWeekNav({ roster, allRosters }: WeeklyRosterWeekNavProps) {
  const navigate = useNavigate();
  const sorted = allRosters
    .filter((r) => r.departmentId === roster.departmentId)
    .sort((a, b) => a.weekStartDate.localeCompare(b.weekStartDate));

  const previous = sorted.filter((r) => r.weekStartDate < roster.weekStartDate).at(-1);
  const next = sorted.find((r) => r.weekStartDate > roster.weekStartDate);
  const currentWeekStartDate = getCurrentWeekStartDate();
  const currentWeekRoster = sorted.find((r) => r.weekStartDate === currentWeekStartDate);
  const isOnCurrentWeek = roster.weekStartDate === currentWeekStartDate;

  return (
    <div className="flex items-center gap-1.5">
      <Button
        variant="outline"
        size="sm"
        className="gap-1"
        disabled={!previous}
        onClick={() => previous && navigate(`/admin/hr/weekly-rosters/${previous.id}`)}
      >
        <ChevronLeft className="h-4 w-4" />
        Previous
      </Button>
      <Button
        variant="outline"
        size="sm"
        className="gap-1.5"
        disabled={!currentWeekRoster || isOnCurrentWeek}
        onClick={() => currentWeekRoster && navigate(`/admin/hr/weekly-rosters/${currentWeekRoster.id}`)}
      >
        <CalendarClock className="h-4 w-4" />
        Current Week
      </Button>
      <Button
        variant="outline"
        size="sm"
        className="gap-1"
        disabled={!next}
        onClick={() => next && navigate(`/admin/hr/weekly-rosters/${next.id}`)}
      >
        Next
        <ChevronRight className="h-4 w-4" />
      </Button>
    </div>
  );
}
