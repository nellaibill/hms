import type { User } from '@hms/shared';
import { ArrowDown, ArrowUp } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { StatusBadge } from './StatusBadge';

interface UserTableProps {
  users: User[];
  sort: string;
  onSortChange: (sort: string) => void;
  onDeleteRequested: (user: User) => void;
  onToggleActive: (user: User) => void;
  isTogglingId: string | undefined;
}

const columns: Array<{ field: string; label: string }> = [
  { field: 'lastName', label: 'Name' },
  { field: 'email', label: 'Email' },
  { field: 'createdAt', label: 'Created' },
];

export function UserTable({ users, sort, onSortChange, onDeleteRequested, onToggleActive, isTogglingId }: UserTableProps) {
  const currentField = sort.startsWith('-') ? sort.slice(1) : sort;
  const isDescending = sort.startsWith('-');

  function toggleSort(field: string) {
    if (currentField !== field) {
      onSortChange(field);
      return;
    }
    onSortChange(isDescending ? field : `-${field}`);
  }

  return (
    <div className="overflow-hidden rounded-lg border border-border">
      <table className="w-full text-sm">
        <thead className="bg-muted/50 text-left text-xs font-medium uppercase tracking-wide text-muted-foreground">
          <tr>
            {columns.map((column) => (
              <th key={column.field} className="px-4 py-2.5">
                <button type="button" onClick={() => toggleSort(column.field)} className="inline-flex items-center gap-1 hover:text-foreground">
                  {column.label}
                  {currentField === column.field &&
                    (isDescending ? <ArrowDown className="h-3.5 w-3.5" /> : <ArrowUp className="h-3.5 w-3.5" />)}
                </button>
              </th>
            ))}
            <th className="px-4 py-2.5">Status</th>
            <th className="px-4 py-2.5 text-right">Actions</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {users.map((user) => (
            <tr key={user.id} className="hover:bg-muted/30">
              <td className="px-4 py-3">
                <Link to={`/users/${user.id}`} className="font-medium text-foreground hover:text-primary hover:underline">
                  {user.firstName} {user.lastName}
                </Link>
              </td>
              <td className="px-4 py-3 text-muted-foreground">{user.email}</td>
              <td className="px-4 py-3 font-mono text-xs text-muted-foreground">
                {new Date(user.createdAt).toLocaleDateString('en-IN')}
              </td>
              <td className="px-4 py-3">
                <StatusBadge isActive={user.isActive} />
              </td>
              <td className="px-4 py-3">
                <div className="flex justify-end gap-1.5">
                  <Button asChild variant="ghost" size="sm">
                    <Link to={`/users/${user.id}/edit`}>Edit</Link>
                  </Button>
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => onToggleActive(user)}
                    disabled={isTogglingId === user.id}
                  >
                    {user.isActive ? 'Deactivate' : 'Activate'}
                  </Button>
                  <Button variant="ghost" size="sm" className="text-destructive hover:text-destructive" onClick={() => onDeleteRequested(user)}>
                    Delete
                  </Button>
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
