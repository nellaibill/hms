import { BED_STATUSES, type BedStatus } from '@hms/shared';
import { Plus, Search } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { WardSelect } from '@/components/WardSelect';
import { useAuth } from '@/features/auth/AuthContext';

interface BedListToolbarProps {
  search: string;
  onSearchChange: (value: string) => void;
  wardId: string | undefined;
  onWardIdChange: (value: string | undefined) => void;
  status: BedStatus | undefined;
  onStatusChange: (value: BedStatus | undefined) => void;
}

export function BedListToolbar({ search, onSearchChange, wardId, onWardIdChange, status, onStatusChange }: BedListToolbarProps) {
  const { hasPermission } = useAuth();
  return (
    <div className="flex flex-wrap items-center gap-3">
      <div className="relative w-56">
        <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
        <Input
          type="search"
          placeholder="Search by bed number or type…"
          value={search}
          onChange={(event) => onSearchChange(event.target.value)}
          aria-label="Search beds"
          className="pl-9"
        />
      </div>

      <div className="w-56">
        <WardSelect
          id="wardFilter"
          value={wardId ?? ''}
          onValueChange={(value) => onWardIdChange(value || undefined)}
          ariaLabel="Filter by ward"
        />
      </div>

      <Select value={status ?? 'all'} onValueChange={(value) => onStatusChange(value === 'all' ? undefined : (value as BedStatus))}>
        <SelectTrigger className="w-44" aria-label="Filter by status">
          <SelectValue placeholder="All statuses" />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="all">All statuses</SelectItem>
          {BED_STATUSES.map((s) => (
            <SelectItem key={s} value={s}>
              {s}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>

      {hasPermission('clinical-care.create') && (
        <Button asChild className="ml-auto gap-1.5">
          <Link to="/clinical/ipd/beds/new">
            <Plus className="h-4 w-4" />
            New Bed
          </Link>
        </Button>
      )}
    </div>
  );
}
