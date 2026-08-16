import { Plus } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { useAuth } from '@/features/auth/AuthContext';

export function WeeklyRosterListToolbar() {
  const { hasPermission } = useAuth();
  return (
    <div className="flex flex-wrap items-center gap-3">
      {hasPermission('workforce-admin.create') && (
        <Button asChild className="ml-auto gap-1.5">
          <Link to="/admin/hr/weekly-rosters/new">
            <Plus className="h-4 w-4" />
            New Weekly Roster
          </Link>
        </Button>
      )}
    </div>
  );
}
