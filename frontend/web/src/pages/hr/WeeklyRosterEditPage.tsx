import { ApiError, type WeeklyRosterFormValues } from '@hms/shared';
import { CalendarRange, Loader2 } from 'lucide-react';
import { useNavigate, useParams } from 'react-router-dom';
import { PageBanner } from '@/components/PageBanner';
import { WeeklyRosterForm, useUpdateWeeklyRosterMutation, useWeeklyRosterQuery } from '../../features/weeklyRosters';

export default function WeeklyRosterEditPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: roster, isPending, isError } = useWeeklyRosterQuery(id);
  const mutation = useUpdateWeeklyRosterMutation();

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

  function handleSubmit(values: WeeklyRosterFormValues) {
    mutation.mutate(
      {
        id: id as string,
        // Published/PublishedDate aren't exposed on this form — they're managed by the
        // dedicated Publish action, so the roster's current values pass through unchanged.
        request: { ...values, published: roster!.published, publishedDate: roster!.publishedDate },
      },
      { onSuccess: () => navigate(`/admin/hr/weekly-rosters/${id}`) },
    );
  }

  return (
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={CalendarRange}
        title="Edit Weekly Roster"
        subtitle="Update this roster's week and department."
        backTo={`/admin/hr/weekly-rosters/${id}`}
        backLabel="Back to roster"
      />

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
        <WeeklyRosterForm
          submitLabel="Save Changes"
          isSubmitting={mutation.isPending}
          apiError={mutation.error instanceof ApiError ? mutation.error : null}
          defaultValues={{ weekStartDate: roster.weekStartDate, departmentId: roster.departmentId }}
          onSubmit={handleSubmit}
        />
      </div>
    </div>
  );
}
