import { Plus, Search } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { useAuth } from '@/features/auth/AuthContext';

interface ShiftAssignmentListToolbarProps {
  search: string;
  onSearchChange: (value: string) => void;
}

export function ShiftAssignmentListToolbar({ search, onSearchChange }: ShiftAssignmentListToolbarProps) {
  const { hasPermission } = useAuth();
  return (
    <div className="flex flex-wrap items-center gap-3">
      <div className="relative w-64">
        <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
        <Input
          type="search"
          placeholder="Search by remarks…"
          value={search}
          onChange={(event) => onSearchChange(event.target.value)}
          aria-label="Search shift assignments"
          className="pl-9"
        />
      </div>

      {hasPermission('workforce-admin.create') && (
        <Button asChild className="ml-auto gap-1.5">
          <Link to="/admin/hr/shift-assignments/new">
            <Plus className="h-4 w-4" />
            New Assignment
          </Link>
        </Button>
      )}
    </div>
  );
}
