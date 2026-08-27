import { ArrowLeft, Loader2, Pencil, Users } from 'lucide-react';
import { Link, useParams } from 'react-router-dom';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { useDepartmentNameById } from '@/features/calendarEvents/hooks/useDepartmentDirectory';
import { EmploymentStatusBadge, useDesignationNameById, useEmployeeLeaveBalancesQuery, useEmployeeQuery } from '../../features/employees';

export default function EmployeeViewPage() {
  const { id } = useParams<{ id: string }>();
  const { data: employee, isPending, isError } = useEmployeeQuery(id);
  const leaveBalancesQuery = useEmployeeLeaveBalancesQuery(id);
  const departmentNameById = useDepartmentNameById();
  const designationNameById = useDesignationNameById();

  if (isPending) {
    return (
      <div className="flex flex-1 items-center justify-center gap-2 p-6 text-sm text-muted-foreground">
        <Loader2 className="h-4 w-4 animate-spin" />
        Loading employee…
      </div>
    );
  }

  if (isError || !employee) {
    return (
      <div className="p-6">
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          Employee not found.
        </p>
      </div>
    );
  }

  const departmentName = employee.departmentName ?? departmentNameById.get(employee.departmentId) ?? '—';
  const designationName = employee.designationName ?? designationNameById.get(employee.designationId) ?? '—';

  return (
    <div className="flex flex-1 flex-col">
      <div className="px-6 pt-4 lg:px-8">
        <Link to="/admin/hr/employees" className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
          <ArrowLeft className="h-4 w-4" />
          Back to employees
        </Link>
      </div>

      <div className="relative mt-3 flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        <div className="absolute right-6 top-1/2 -translate-y-1/2">
          <Button
            asChild
            variant="outline"
            className="gap-1.5 border-page-banner-foreground/30 bg-page-banner-foreground/10 text-page-banner-foreground hover:bg-page-banner-foreground/20"
          >
            <Link to={`/admin/hr/employees/${employee.id}/edit`}>
              <Pencil className="h-4 w-4" />
              Edit
            </Link>
          </Button>
        </div>
        <div className="flex items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
            <Users className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight">
            {employee.firstName} {employee.lastName}
          </h1>
          <EmploymentStatusBadge status={employee.employmentStatus} />
        </div>
        <p className="font-mono text-sm text-page-banner-foreground/85">{employee.employeeCode}</p>
      </div>

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
        <Tabs defaultValue="overview">
          <TabsList>
            <TabsTrigger value="overview">Overview</TabsTrigger>
            <TabsTrigger value="leave-balances">Leave Balances</TabsTrigger>
          </TabsList>

          <TabsContent value="overview" className="mt-4">
            <Card>
              <CardContent className="grid grid-cols-1 gap-4 py-6 sm:grid-cols-2 lg:grid-cols-3">
                <Field label="Gender" value={employee.gender} />
                <Field label="Date of Birth" value={new Date(employee.dateOfBirth).toLocaleDateString('en-IN')} />
                <Field label="Phone" value={employee.phone} />
                <Field label="Email" value={employee.email} />
                <Field label="Address" value={employee.address} />
                <Field label="Emergency Contact" value={`${employee.emergencyContactName} (${employee.emergencyContactPhone})`} />
                <Field label="Department" value={departmentName} />
                <Field label="Designation" value={designationName} />
                <Field label="Employee Type" value={employee.employeeType} />
                <Field label="Joining Date" value={new Date(employee.joiningDate).toLocaleDateString('en-IN')} />
                <Field label="Reporting Manager" value={employee.reportingManagerName ?? '—'} />
                <div>
                  <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Active</p>
                  <p className="mt-1">
                    <Badge variant={employee.isActive ? 'success' : 'secondary'}>{employee.isActive ? 'Active' : 'Inactive'}</Badge>
                  </p>
                </div>
                <Field label="Created" value={new Date(employee.createdAt).toLocaleString('en-IN')} />
                {employee.updatedAt && <Field label="Last updated" value={new Date(employee.updatedAt).toLocaleString('en-IN')} />}
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="leave-balances" className="mt-4">
            <Card>
              <CardContent className="py-6">
                {leaveBalancesQuery.isPending && (
                  <div className="flex items-center justify-center gap-2 py-8 text-sm text-muted-foreground">
                    <Loader2 className="h-4 w-4 animate-spin" />
                    Loading leave balances…
                  </div>
                )}
                {leaveBalancesQuery.isError && (
                  <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
                    Failed to load leave balances.
                  </p>
                )}
                {leaveBalancesQuery.data && leaveBalancesQuery.data.length === 0 && (
                  <p className="py-8 text-center text-sm text-muted-foreground">No active leave types configured.</p>
                )}
                {leaveBalancesQuery.data && leaveBalancesQuery.data.length > 0 && (
                  <div className="overflow-x-auto rounded-lg border border-border">
                    <table className="w-full text-sm">
                      <thead className="bg-muted/50 text-left text-xs font-medium uppercase tracking-wide text-muted-foreground">
                        <tr>
                          <th className="px-4 py-2.5">Leave Type</th>
                          <th className="px-4 py-2.5">Max Days/Year</th>
                          <th className="px-4 py-2.5">Used Days</th>
                          <th className="px-4 py-2.5">Remaining Days</th>
                        </tr>
                      </thead>
                      <tbody className="divide-y divide-border">
                        {leaveBalancesQuery.data.map((balance) => (
                          <tr key={balance.leaveTypeId}>
                            <td className="px-4 py-3 font-medium text-foreground">{balance.leaveTypeName}</td>
                            <td className="px-4 py-3 text-muted-foreground">{balance.maxDaysPerYear ?? 'Unlimited'}</td>
                            <td className="px-4 py-3 text-muted-foreground">{balance.usedDays}</td>
                            <td className="px-4 py-3 text-muted-foreground">{balance.remainingDays ?? '—'}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}
              </CardContent>
            </Card>
          </TabsContent>
        </Tabs>
      </div>
    </div>
  );
}

function Field({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">{label}</p>
      <p className="mt-1 text-sm text-foreground">{value}</p>
    </div>
  );
}
