import { Plus, Search } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';

interface HospitalListToolbarProps {
  search: string;
  onSearchChange: (value: string) => void;
}

export function HospitalListToolbar({ search, onSearchChange }: HospitalListToolbarProps) {
  return (
    <div className="flex flex-wrap items-center gap-3">
      <div className="relative w-64">
        <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
        <Input
          type="search"
          placeholder="Search by hospital name or code…"
          value={search}
          onChange={(event) => onSearchChange(event.target.value)}
          aria-label="Search hospitals"
          className="pl-9"
        />
      </div>

      <Button asChild className="ml-auto gap-1.5">
        <Link to="/platform/hospitals/new">
          <Plus className="h-4 w-4" />
          Register Hospital
        </Link>
      </Button>
    </div>
  );
}
