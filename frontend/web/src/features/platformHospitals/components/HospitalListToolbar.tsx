import { Search } from 'lucide-react';
import { Input } from '@/components/ui/input';

interface HospitalListToolbarProps {
  search: string;
  onSearchChange: (value: string) => void;
  /** Total matching hospitals, shown beside the search box; undefined while loading. */
  totalCount?: number;
}

export function HospitalListToolbar({ search, onSearchChange, totalCount }: HospitalListToolbarProps) {
  return (
    <div className="flex flex-wrap items-center justify-between gap-3 px-5 py-4">
      <div className="flex items-center gap-2">
        <h2 className="text-sm font-semibold text-foreground">{search.trim() ? 'Matching hospitals' : 'All hospitals'}</h2>
        {totalCount !== undefined && <span className="rounded-full bg-muted px-2 py-0.5 text-xs font-medium tabular-nums text-muted-foreground">{totalCount}</span>}
      </div>
      <div className="relative w-full sm:w-72">
        <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
        <Input
          type="search"
          placeholder="Search by hospital name or code…"
          value={search}
          onChange={(event) => onSearchChange(event.target.value)}
          aria-label="Search hospitals"
          className="h-9 pl-9"
        />
      </div>
    </div>
  );
}
