import type { WeeklyRoster } from '@hms/shared';
import { CalendarRange, Loader2 } from 'lucide-react';
import { useState } from 'react';
import { Card, CardContent } from '@/components/ui/card';
import { Pagination } from '@/components/Pagination';
import { PageBanner } from '@/components/PageBanner';
import {
  DeleteWeeklyRosterDialog,
  WeeklyRosterListToolbar,
  WeeklyRosterTable,
  useDeleteWeeklyRosterMutation,
  usePublishWeeklyRosterMutation,
  useWeeklyRostersQuery,
} from '../../features/weeklyRosters';

export default function WeeklyRostersListPage() {
  const [page, setPage] = useState(1);
  const [sort, setSort] = useState('-weekStartDate');
  const [rosterPendingDelete, setRosterPendingDelete] = useState<WeeklyRoster | null>(null);

  const { data, isPending, isError, error } = useWeeklyRostersQuery({ page, pageSize: 20, sort });
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
      <PageBanner
        icon={CalendarRange}
        title="Weekly Roster"
        subtitle="Manage per-department weekly roster headers and publish state."
        backTo="/admin/hr"
        backLabel="Back to HR"
      />

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
        <WeeklyRosterListToolbar />

        {isPending && (
          <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" />
            Loading weekly rosters…
          </div>
        )}

        {isError && (
          <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
            {error instanceof Error ? error.message : 'Failed to load weekly rosters.'}
          </p>
        )}

        {!isPending && !isError && data && data.items.length === 0 && (
          <Card className="border-dashed">
            <CardContent className="flex flex-col items-center gap-2 py-16 text-center">
              <p className="text-sm font-medium text-foreground">No roster published for next week</p>
              <p className="text-sm text-muted-foreground">Create the first weekly roster to get started.</p>
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
