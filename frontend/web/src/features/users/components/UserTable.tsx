import type { User } from '@hms/shared';
import { Link } from 'react-router-dom';
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
    <table>
      <thead>
        <tr>
          {columns.map((column) => (
            <th key={column.field}>
              <button type="button" onClick={() => toggleSort(column.field)}>
                {column.label}
                {currentField === column.field ? (isDescending ? ' ▼' : ' ▲') : ''}
              </button>
            </th>
          ))}
          <th>Status</th>
          <th>Actions</th>
        </tr>
      </thead>
      <tbody>
        {users.map((user) => (
          <tr key={user.id}>
            <td>
              <Link to={`/users/${user.id}`}>
                {user.firstName} {user.lastName}
              </Link>
            </td>
            <td>{user.email}</td>
            <td>{new Date(user.createdAt).toLocaleDateString()}</td>
            <td>
              <StatusBadge isActive={user.isActive} />
            </td>
            <td>
              <Link to={`/users/${user.id}/edit`}>Edit</Link>{' '}
              <button type="button" onClick={() => onToggleActive(user)} disabled={isTogglingId === user.id}>
                {user.isActive ? 'Deactivate' : 'Activate'}
              </button>{' '}
              <button type="button" onClick={() => onDeleteRequested(user)}>
                Delete
              </button>
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
