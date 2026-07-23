import { Link } from 'react-router-dom';

interface UserListToolbarProps {
  search: string;
  onSearchChange: (value: string) => void;
  isActive: boolean | undefined;
  onIsActiveChange: (value: boolean | undefined) => void;
}

export function UserListToolbar({ search, onSearchChange, isActive, onIsActiveChange }: UserListToolbarProps) {
  return (
    <div className="toolbar">
      <input
        type="search"
        placeholder="Search by name or email…"
        value={search}
        onChange={(event) => onSearchChange(event.target.value)}
        aria-label="Search users"
      />

      <select
        value={isActive === undefined ? 'all' : String(isActive)}
        onChange={(event) => {
          const value = event.target.value;
          onIsActiveChange(value === 'all' ? undefined : value === 'true');
        }}
        aria-label="Filter by status"
      >
        <option value="all">All statuses</option>
        <option value="true">Active only</option>
        <option value="false">Inactive only</option>
      </select>

      <Link to="/users/new">New User</Link>
    </div>
  );
}
