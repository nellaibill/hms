import type { User } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { DepartmentName } from '@/components/DepartmentName';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { consultantsApi } from '@/services/apiClient';
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
  const { data: consultant } = useQuery({
    queryKey: ['consultants', user.consultantId],
    queryFn: () => consultantsApi.getConsultantById(user.consultantId as string),
    enabled: Boolean(user.consultantId),
  });

  return (
    <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
      <Card>
        <CardHeader>
          <CardTitle>User Information</CardTitle>
          <CardDescription>Basic details and contact information.</CardDescription>
        </CardHeader>
        <CardContent>
          <dl className="grid grid-cols-1 divide-y divide-border sm:grid-cols-2 sm:divide-y-0">
            <Field label="Username" value={user.username} />
            <Field label="Name" value={`${user.firstName} ${user.lastName}`} />
            <Field label="Email" value={user.email} />
            <Field label="Phone number" value={user.phoneNumber || '—'} />
            <Field label="Role" value={user.roleName} />
            <Field label="Status" value={<StatusBadge isActive={user.isActive} />} />
            <Field label="Created" value={new Date(user.createdAt).toLocaleString('en-IN')} />
            {user.updatedAt && <Field label="Last updated" value={new Date(user.updatedAt).toLocaleString('en-IN')} />}
          </dl>
        </CardContent>
      </Card>

      {user.consultantId && (
        <Card>
          <CardHeader>
            <CardTitle>Consultant Information</CardTitle>
            <CardDescription>Linked consultant/doctor details.</CardDescription>
          </CardHeader>
          <CardContent>
            <dl className="grid grid-cols-1 divide-y divide-border sm:grid-cols-2 sm:divide-y-0">
              <Field label="Consultant Name" value={consultant?.name ?? '—'} />
              <Field label="Specialization" value={consultant?.specialization ?? '—'} />
              <Field
                label="Department"
                value={consultant?.departmentId ? <DepartmentName departmentId={consultant.departmentId} /> : '—'}
              />
            </dl>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
