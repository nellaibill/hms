import type { WeeklyRoster } from '@hms/shared';
import { CalendarDays, Loader2 } from 'lucide-react';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { Card, CardContent } from '@/components/ui/card';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Pagination } from '@/components/Pagination';
import {
  DeleteWeeklyRosterDialog,
  WeeklyRosterTable,
  useDeleteWeeklyRosterMutation,
  useMonthlyWeeklyRostersQuery,
  usePublishWeeklyRosterMutation,
} from '../../features/weeklyRosters';

const MONTHS = [
  'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December',
];

const now = new Date();
const YEAR_OPTIONS = Array.from({ length: 5 }, (_, i) => now.getFullYear() - 2 + i);

export default function MonthlyRosterCalendarPage() {
  const [year, setYear] = useState(now.getFullYear());
  const [month, setMonth] = useState(now.getMonth() + 1);
  const [page, setPage] = useState(1);
  const [sort, setSort] = useState('weekStartDate');
  const [rosterPendingDelete, setRosterPendingDelete] = useState<WeeklyRoster | null>(null);

  const { data, isPending, isError, error } = useMonthlyWeeklyRostersQuery({ year, month, page, pageSize: 20, sort });
  const deleteMutation = useDeleteWeeklyRosterMutation();
  const publishMutation = usePublishWeeklyRosterMutation();

  function handleSortChange(value: string) {
    setSort(value);
    setPage(1);
  }

  function handleConfirmDelete() {
    if (!rosterPendingDelete) {
      return;
    }
    deleteMutation.mutate(rosterPendingDelete.id, {
      onSuccess: () => setRosterPendingDelete(null),
    });
  }

  return (
    <div className="flex flex-1 flex-col">
      <div className="px-6 pt-4 lg:px-8">
        <Link to="/admin/hr" className="text-sm text-muted-foreground hover:text-foreground">
          &larr; Back to HR
        </Link>
      </div>

      <div className="mt-3 flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        <div className="flex items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
            <CalendarDays className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight">Monthly Calendar</h1>
        </div>
        <p className="max-w-2xl text-sm text-page-banner-foreground/85">Weekly rosters whose week falls within the selected month.</p>
      </div>

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
        <div className="flex flex-wrap items-center gap-3">
          <Select
            value={String(month)}
            onValueChange={(value) => {
              setMonth(Number(value));
              setPage(1);
            }}
          >
            <SelectTrigger className="w-40" aria-label="Month">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {MONTHS.map((label, index) => (
                <SelectItem key={label} value={String(index + 1)}>
                  {label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>

          <Select
            value={String(year)}
            onValueChange={(value) => {
              setYear(Number(value));
              setPage(1);
            }}
          >
            <SelectTrigger className="w-28" aria-label="Year">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {YEAR_OPTIONS.map((y) => (
                <SelectItem key={y} value={String(y)}>
                  {y}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        {isPending && (
          <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" />
            Loading rosters…
          </div>
        )}

        {isError && (
          <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
            {error instanceof Error ? error.message : 'Failed to load rosters for this month.'}
          </p>
        )}

        {!isPending && !isError && data && data.items.length === 0 && (
          <Card className="border-dashed">
            <CardContent className="flex flex-col items-center gap-2 py-16 text-center">
              <p className="text-sm font-medium text-foreground">No roster published for {MONTHS[month - 1]} {year}</p>
              <p className="text-sm text-muted-foreground">
                <Link to="/admin/hr/weekly-rosters/new" className="text-primary hover:underline">
                  Build a roster
                </Link>{' '}
                for a week in this month.
              </p>
            </CardContent>
          </Card>
        )}

        {!isPending && !isError && data && data.items.length > 0 && (
          <div className="flex flex-col gap-3">
            <WeeklyRosterTable
              rosters={data.items}
              sort={sort}
              onSortChange={handleSortChange}
              onDeleteRequested={setRosterPendingDelete}
              onPublishRequested={(roster) => publishMutation.mutate(roster.id)}
              isPublishingId={publishMutation.isPending ? (publishMutation.variables as string | undefined) : undefined}
            />
            <Pagination meta={data.meta} onPageChange={setPage} />
          </div>
        )}

        {rosterPendingDelete && (
          <DeleteWeeklyRosterDialog
            roster={rosterPendingDelete}
            isDeleting={deleteMutation.isPending}
            onConfirm={handleConfirmDelete}
            onCancel={() => setRosterPendingDelete(null)}
          />
        )}
      </div>
    </div>
  );
}
