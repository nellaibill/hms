import { ApiError, type WeeklyRosterFormValues } from '@hms/shared';
import { ArrowLeft, CalendarRange } from 'lucide-react';
import { Link, useNavigate } from 'react-router-dom';
import { WeeklyRosterForm, useCreateWeeklyRosterMutation } from '../../features/weeklyRosters';

export default function WeeklyRosterCreatePage() {
  const navigate = useNavigate();
  const mutation = useCreateWeeklyRosterMutation();

  function handleSubmit(values: WeeklyRosterFormValues) {
    mutation.mutate(
      { ...values, published: false, publishedDate: null },
      { onSuccess: (roster) => navigate(`/admin/hr/weekly-rosters/${roster.id}`) },
    );
  }

  return (
    <div className="flex flex-1 flex-col">
      <div className="px-6 pt-4 lg:px-8">
        <Link to="/admin/hr/weekly-rosters" className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
          <ArrowLeft className="h-4 w-4" />
          Back to weekly rosters
        </Link>
      </div>

      <div className="mt-3 flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        <div className="flex items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
            <CalendarRange className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight">New Weekly Roster</h1>
        </div>
        <p className="text-sm text-page-banner-foreground/85">New rosters start as a draft — publish once it's ready.</p>
      </div>

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
        <WeeklyRosterForm
          submitLabel="Create Roster"
          isSubmitting={mutation.isPending}
          apiError={mutation.error instanceof ApiError ? mutation.error : null}
          onSubmit={handleSubmit}
        />
      </div>
    </div>
  );
}
