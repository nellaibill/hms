import type { User } from '@hms/shared';
import { StatusBadge } from './StatusBadge';

interface UserDetailsProps {
  user: User;
}

export function UserDetails({ user }: UserDetailsProps) {
  return (
    <dl>
      <dt>Name</dt>
      <dd>
        {user.firstName} {user.lastName}
      </dd>

      <dt>Email</dt>
      <dd>{user.email}</dd>

      <dt>Phone number</dt>
      <dd>{user.phoneNumber || '—'}</dd>

      <dt>Status</dt>
      <dd>
        <StatusBadge isActive={user.isActive} />
      </dd>

      <dt>Created</dt>
      <dd>{new Date(user.createdAt).toLocaleString()}</dd>

      {user.updatedAt && (
        <>
          <dt>Last updated</dt>
          <dd>{new Date(user.updatedAt).toLocaleString()}</dd>
        </>
      )}
    </dl>
  );
}
