import { Plus, Search } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';

interface PatientListToolbarProps {
  search: string;
  onSearchChange: (value: string) => void;
}

/** Search by Name, UHID, or Phone — duplicate-confidence ranking is deferred (docs/PatientRegistrationModule.md §8). */
export function PatientListToolbar({ search, onSearchChange }: PatientListToolbarProps) {
  return (
    <div className="flex flex-wrap items-center gap-3">
      <div className="relative w-72">
        <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
        <Input
          type="search"
          placeholder="Search by name, UHID, or phone…"
          value={search}
          onChange={(event) => onSearchChange(event.target.value)}
          aria-label="Search patients"
          className="pl-9"
        />
      </div>

      <Button asChild className="ml-auto gap-1.5">
        <Link to="/patients/registration/new">
          <Plus className="h-4 w-4" />
          New Patient
        </Link>
      </Button>
    </div>
  );
}
