import { ApiError, type WeeklyRosterFormValues } from '@hms/shared';
import { CalendarRange } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { PageBanner } from '@/components/PageBanner';
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
      <PageBanner
        icon={CalendarRange}
        title="New Weekly Roster"
        subtitle="New rosters start as a draft — publish once it's ready."
        backTo="/admin/hr/weekly-rosters"
        backLabel="Back to weekly rosters"
      />

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
