import { Plus, Search } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';

interface ProductListToolbarProps {
  search: string;
  onSearchChange: (value: string) => void;
  isActive: boolean | undefined;
  onIsActiveChange: (value: boolean | undefined) => void;
}

export function ProductListToolbar({ search, onSearchChange, isActive, onIsActiveChange }: ProductListToolbarProps) {
  return (
    <div className="flex flex-wrap items-center gap-3">
      <div className="relative w-64">
        <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
        <Input
          type="search"
          placeholder="Search by name, SKU, or code…"
          value={search}
          onChange={(event) => onSearchChange(event.target.value)}
          aria-label="Search products"
          className="pl-9"
        />
      </div>

      <Select
        value={isActive === undefined ? 'all' : String(isActive)}
        onValueChange={(value) => onIsActiveChange(value === 'all' ? undefined : value === 'true')}
      >
        <SelectTrigger className="w-44" aria-label="Filter by status">
          <SelectValue placeholder="All statuses" />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="all">All statuses</SelectItem>
          <SelectItem value="true">Active only</SelectItem>
          <SelectItem value="false">Inactive only</SelectItem>
        </SelectContent>
      </Select>

      <Button asChild className="ml-auto gap-1.5">
        <Link to="/support/inventory/new">
          <Plus className="h-4 w-4" />
          New Product
        </Link>
      </Button>
    </div>
  );
}
