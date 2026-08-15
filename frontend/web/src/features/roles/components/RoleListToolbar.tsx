import { Plus, Search } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { useAuth } from '../../auth/AuthContext';
import type { RoleStatus } from '../types';

interface RoleListToolbarProps {
  search: string;
  onSearchChange: (value: string) => void;
  status: RoleStatus | undefined;
  onStatusChange: (value: RoleStatus | undefined) => void;
}

export function RoleListToolbar({ search, onSearchChange, status, onStatusChange }: RoleListToolbarProps) {
  const { hasPermission } = useAuth();

  return (
    <div className="flex flex-wrap items-center gap-3">
      <div className="relative w-64">
        <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
        <Input
          type="search"
          placeholder="Search by role name…"
          value={search}
          onChange={(event) => onSearchChange(event.target.value)}
          aria-label="Search roles"
          className="pl-9"
        />
      </div>

      <Select value={status ?? 'all'} onValueChange={(value) => onStatusChange(value === 'all' ? undefined : (value as RoleStatus))}>
        <SelectTrigger className="w-44" aria-label="Filter by status">
          <SelectValue placeholder="All statuses" />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="all">All statuses</SelectItem>
          <SelectItem value="Active">Active only</SelectItem>
          <SelectItem value="Inactive">Inactive only</SelectItem>
        </SelectContent>
      </Select>

      {hasPermission('identity-administration.create') && (
        <Button asChild className="ml-auto gap-1.5">
          <Link to="/admin/roles/new">
            <Plus className="h-4 w-4" />
            Add Role
          </Link>
        </Button>
      )}
    </div>
  );
}
