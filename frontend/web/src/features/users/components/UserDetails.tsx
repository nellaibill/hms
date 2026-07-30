import type { User } from '@hms/shared';
import { StatusBadge } from './StatusBadge';

interface UserDetailsProps {
  user: User;
}

function Field({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div className="flex flex-col gap-0.5 py-3">
      <dt className="text-xs font-medium uppercase tracking-wide text-muted-foreground">{label}</dt>
      <dd className="text-sm text-foreground">{value}</dd>
    </div>
  );
}

export function UserDetails({ user }: UserDetailsProps) {
  return (
    <dl className="grid grid-cols-1 divide-y divide-border rounded-lg border border-border px-4 sm:grid-cols-2 sm:divide-y-0 sm:divide-x sm:px-0 sm:[&>*]:px-6">
      <Field label="Username" value={user.username} />
      <Field label="Name" value={`${user.firstName} ${user.lastName}`} />
      <Field label="Email" value={user.email} />
      <Field label="Phone number" value={user.phoneNumber || '—'} />
      <Field label="Status" value={<StatusBadge isActive={user.isActive} />} />
      <Field label="Created" value={new Date(user.createdAt).toLocaleString('en-IN')} />
      {user.updatedAt && <Field label="Last updated" value={new Date(user.updatedAt).toLocaleString('en-IN')} />}
    </dl>
  );
}
