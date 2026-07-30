import { Plus, Search } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';

interface MasterListToolbarProps {
  entityKey: string;
  entityLabel: string;
  search: string;
  onSearchChange: (value: string) => void;
  isActive: boolean | undefined;
  onIsActiveChange: (value: boolean | undefined) => void;
}

export function MasterListToolbar({ entityKey, entityLabel, search, onSearchChange, isActive, onIsActiveChange }: MasterListToolbarProps) {
  const statusValue = isActive === undefined ? 'all' : isActive ? 'active' : 'inactive';

  return (
    <div className="flex flex-wrap items-center gap-3">
      <div className="relative w-64">
        <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
        <Input
          type="search"
          placeholder={`Search ${entityLabel.toLowerCase()}s…`}
          value={search}
          onChange={(event) => onSearchChange(event.target.value)}
          aria-label={`Search ${entityLabel}`}
          className="pl-9"
        />
      </div>

      <Select
        value={statusValue}
        onValueChange={(value) => onIsActiveChange(value === 'all' ? undefined : value === 'active')}
      >
        <SelectTrigger className="w-44" aria-label="Filter by status">
          <SelectValue placeholder="All statuses" />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="all">All statuses</SelectItem>
          <SelectItem value="active">Active only</SelectItem>
          <SelectItem value="inactive">Inactive only</SelectItem>
        </SelectContent>
      </Select>

      <Button asChild className="ml-auto gap-1.5">
        <Link to={`/admin/masters/${entityKey}/new`}>
          <Plus className="h-4 w-4" />
          Add {entityLabel}
        </Link>
      </Button>
    </div>
  );
}
