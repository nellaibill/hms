import { Plus } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Button } from '@/components/ui/button';

export function WeeklyRosterListToolbar() {
  return (
    <div className="flex flex-wrap items-center gap-3">
      <Button asChild className="ml-auto gap-1.5">
        <Link to="/admin/hr/weekly-rosters/new">
          <Plus className="h-4 w-4" />
          New Weekly Roster
        </Link>
      </Button>
    </div>
  );
}
