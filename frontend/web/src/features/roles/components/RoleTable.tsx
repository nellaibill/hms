import { ArrowDown, ArrowUp } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { useAuth } from '../../auth/AuthContext';
import { countGrantedPermissions, type Role } from '../types';
import { RoleStatusBadge } from './RoleStatusBadge';

interface RoleTableProps {
  roles: Role[];
  sort: string;
  onSortChange: (sort: string) => void;
}

export function RoleTable({ roles, sort, onSortChange }: RoleTableProps) {
  const currentField = sort.startsWith('-') ? sort.slice(1) : sort;
  const isDescending = sort.startsWith('-');
  const { hasPermission } = useAuth();
  const canEdit = hasPermission('identity-administration.edit');

  function toggleSort(field: string) {
    if (currentField !== field) {
      onSortChange(field);
      return;
    }
    onSortChange(isDescending ? field : `-${field}`);
  }

  function sortButton(field: string, label: string) {
    return (
      <button type="button" onClick={() => toggleSort(field)} className="inline-flex items-center gap-1 hover:text-foreground">
        {label}
        {currentField === field && (isDescending ? <ArrowDown className="h-3.5 w-3.5" /> : <ArrowUp className="h-3.5 w-3.5" />)}
      </button>
    );
  }

  return (
    <div className="overflow-hidden rounded-lg border border-border">
      <table className="w-full text-sm">
        <thead className="bg-muted/50 text-left text-xs font-medium uppercase tracking-wide text-muted-foreground">
          <tr>
            <th className="px-4 py-2.5">{sortButton('name', 'Name')}</th>
            <th className="px-4 py-2.5">Description</th>
            <th className="px-4 py-2.5">Status</th>
            <th className="px-4 py-2.5">Permissions</th>
            <th className="px-4 py-2.5">{sortButton('updatedAt', 'Last Updated')}</th>
            <th className="px-4 py-2.5 text-right">Actions</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {roles.map((role) => {
            const { granted, total } = countGrantedPermissions(role.permissions);
            return (
              <tr key={role.id} className="hover:bg-muted/30">
                <td className="px-4 py-3">
                  <Link to={`/admin/roles/${role.id}`} className="font-medium text-foreground hover:text-primary hover:underline">
                    {role.name}
                  </Link>
                </td>
                <td className="max-w-xs truncate px-4 py-3 text-muted-foreground" title={role.description}>
                  {role.description}
                </td>
                <td className="px-4 py-3">
                  <RoleStatusBadge status={role.status} />
                </td>
                <td className="px-4 py-3 font-mono text-xs text-muted-foreground">
                  {granted} / {total}
                </td>
                <td className="px-4 py-3 font-mono text-xs text-muted-foreground">{new Date(role.updatedAt).toLocaleDateString('en-IN')}</td>
                <td className="px-4 py-3">
                  <div className="flex justify-end gap-1.5">
                    <Button asChild variant="ghost" size="sm">
                      <Link to={`/admin/roles/${role.id}`}>View</Link>
                    </Button>
                    {canEdit && (
                      <Button asChild variant="ghost" size="sm">
                        <Link to={`/admin/roles/${role.id}/edit`}>Edit</Link>
                      </Button>
                    )}
                  </div>
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}
