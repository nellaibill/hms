import { ApiError } from '@hms/shared';
import { CalendarRange, Copy, Loader2, Pencil, Send } from 'lucide-react';
import { useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { DepartmentName } from '@/components/DepartmentName';
import { PageBanner } from '@/components/PageBanner';
import { useShiftsQuery } from '../../features/shifts';
import { useShiftAssignmentsQuery } from '../../features/shiftAssignments';
import {
  CopyWeeklyRosterDialog,
  WeeklyRosterMatrix,
  WeeklyRosterWeekNav,
  useCopyWeeklyRosterMutation,
  usePublishWeeklyRosterMutation,
  useWeeklyRosterQuery,
  useWeeklyRostersQuery,
} from '../../features/weeklyRosters';
import { getWeekDates } from '../../features/weeklyRosters/utils/week';

export default function WeeklyRosterViewPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: roster, isPending, isError } = useWeeklyRosterQuery(id);
  const publishMutation = usePublishWeeklyRosterMutation();
  const copyMutation = useCopyWeeklyRosterMutation();
  const [isCopying, setIsCopying] = useState(false);

  const weekDates = roster ? getWeekDates(roster.weekStartDate) : [];
  const shiftsQuery = useShiftsQuery({ pageSize: 100, isActive: true });
  const assignmentsQuery = useShiftAssignmentsQuery(
    {
      pageSize: 100,
      departmentId: roster?.departmentId,
      rosterDateFrom: weekDates[0],
      rosterDateTo: weekDates[6],
    },
    { enabled: Boolean(roster) },
  );
  // No "list rosters by department" endpoint exists — fetched unfiltered and narrowed
  // client-side in WeeklyRosterWeekNav, per spec §4.4.
  const departmentRostersQuery = useWeeklyRostersQuery({ pageSize: 100 });

  function handlePublish() {
    if (id) {
      publishMutation.mutate(id);
    }
  }

  function handleCopy(values: { targetWeekStartDate: string }) {
    if (!id) return;
    copyMutation.mutate(
      { id, request: values },
      {
        onSuccess: (newRoster) => {
          setIsCopying(false);
          navigate(`/admin/hr/weekly-rosters/${newRoster.id}`);
        },
      },
    );
  }

  if (isPending) {
    return (
      <div className="flex flex-1 items-center justify-center gap-2 p-6 text-sm text-muted-foreground">
        <Loader2 className="h-4 w-4 animate-spin" />
        Loading weekly roster…
      </div>
    );
  }

  if (isError || !roster) {
    return (
      <div className="p-6">
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          Weekly roster not found.
        </p>
      </div>
    );
  }

  return (
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={CalendarRange}
        title={`Week of ${roster.weekStartDate}`}
        titleExtra={<Badge variant={roster.published ? 'success' : 'secondary'}>{roster.published ? 'Published' : 'Draft'}</Badge>}
        subtitle={<WeeklyRosterWeekNav roster={roster} allRosters={departmentRostersQuery.data?.items ?? []} />}
        backTo="/admin/hr/weekly-rosters"
        backLabel="Back to weekly rosters"
        rightActions={
          <>
            {!roster.published && (
              <Button
                variant="outline"
                className="gap-1.5 border-page-banner-foreground/30 bg-page-banner-foreground/10 text-page-banner-foreground hover:bg-page-banner-foreground/20"
                onClick={handlePublish}
                disabled={publishMutation.isPending}
              >
                <Send className="h-4 w-4" />
                {publishMutation.isPending ? 'Publishing…' : 'Publish'}
              </Button>
            )}
            <Button
              variant="outline"
              className="gap-1.5 border-page-banner-foreground/30 bg-page-banner-foreground/10 text-page-banner-foreground hover:bg-page-banner-foreground/20"
              onClick={() => setIsCopying(true)}
            >
              <Copy className="h-4 w-4" />
              Copy
            </Button>
            <Button
              asChild
              variant="outline"
              className="gap-1.5 border-page-banner-foreground/30 bg-page-banner-foreground/10 text-page-banner-foreground hover:bg-page-banner-foreground/20"
            >
              <Link to={`/admin/hr/weekly-rosters/${roster.id}/edit`}>
                <Pencil className="h-4 w-4" />
                Edit
              </Link>
            </Button>
          </>
        }
      />

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
        {publishMutation.isError && (
          <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
            {publishMutation.error instanceof Error ? publishMutation.error.message : 'Failed to publish roster.'}
          </p>
        )}

        <Card>
          <CardContent className="grid grid-cols-1 gap-4 py-6 sm:grid-cols-2">
            <div>
              <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Department</p>
              <p className="mt-1 text-sm text-foreground">
                <DepartmentName departmentId={roster.departmentId} />
              </p>
            </div>
            <div>
              <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Published</p>
              <p className="mt-1 text-sm text-foreground">
                {roster.published && roster.publishedDate ? new Date(roster.publishedDate).toLocaleString('en-IN') : 'Not yet published'}
              </p>
            </div>
            <div>
              <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Created</p>
              <p className="mt-1 text-sm text-foreground">{new Date(roster.createdAt).toLocaleString('en-IN')}</p>
            </div>
            {roster.updatedAt && (
              <div>
                <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Last updated</p>
                <p className="mt-1 text-sm text-foreground">{new Date(roster.updatedAt).toLocaleString('en-IN')}</p>
              </div>
            )}
          </CardContent>
        </Card>

        <div className="flex flex-col gap-2">
          <h2 className="text-sm font-semibold text-foreground">Roster</h2>
          {shiftsQuery.isPending || assignmentsQuery.isPending ? (
            <div className="flex items-center justify-center gap-2 py-10 text-sm text-muted-foreground">
              <Loader2 className="h-4 w-4 animate-spin" />
              Loading roster…
            </div>
          ) : shiftsQuery.isError || assignmentsQuery.isError ? (
            <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
              Failed to load shifts or assignments for this week.
            </p>
          ) : (
            <WeeklyRosterMatrix
              weekStartDate={roster.weekStartDate}
              shifts={shiftsQuery.data?.items ?? []}
              assignments={assignmentsQuery.data?.items ?? []}
            />
          )}
        </div>
      </div>

      {isCopying && (
        <CopyWeeklyRosterDialog
          roster={roster}
          isSubmitting={copyMutation.isPending}
          apiError={copyMutation.error instanceof ApiError ? copyMutation.error : null}
          onSubmit={handleCopy}
          onCancel={() => {
            copyMutation.reset();
            setIsCopying(false);
          }}
        />
      )}
    </div>
  );
}
