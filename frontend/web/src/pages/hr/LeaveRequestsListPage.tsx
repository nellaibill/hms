import type { LeaveRequestResponse, LeaveRequestStatus } from '@hms/shared';
import { LEAVE_REQUEST_STATUSES } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { CalendarRange, Loader2, Plus } from 'lucide-react';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { EmployeeSelect } from '@/components/EmployeeSelect';
import { Pagination } from '@/components/Pagination';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { SearchableSelect } from '@/components/ui/searchable-select';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { useAuth } from '@/features/auth/AuthContext';
import { leaveTypesApi } from '@/services/apiClient';
import {
  ApproveLeaveRequestDialog,
  LeaveRequestTable,
  NewLeaveRequestDialog,
  RejectLeaveRequestDialog,
  useCancelLeaveRequestMutation,
  useLeaveRequestsQuery,
} from '../../features/leaveRequests';

export default function LeaveRequestsListPage() {
  const [employeeId, setEmployeeId] = useState<string | undefined>(undefined);
  const [leaveTypeId, setLeaveTypeId] = useState<string | undefined>(undefined);
  const [status, setStatus] = useState<string | undefined>(undefined);
  const [dateFrom, setDateFrom] = useState('');
  const [dateTo, setDateTo] = useState('');
  const [page, setPage] = useState(1);

  const [showNewRequest, setShowNewRequest] = useState(false);
  const [approving, setApproving] = useState<LeaveRequestResponse | null>(null);
  const [rejecting, setRejecting] = useState<LeaveRequestResponse | null>(null);

  const { hasPermission } = useAuth();
  const canCreate = hasPermission('workforce-admin.create');

  const { data, isPending, isError, error } = useLeaveRequestsQuery({
    page,
    pageSize: 20,
    sort: '-createdAt',
    employeeId,
    leaveTypeId,
    status: status as LeaveRequestStatus | undefined,
    dateFrom: dateFrom || undefined,
    dateTo: dateTo || undefined,
  });

  const cancelMutation = useCancelLeaveRequestMutation();

  const leaveTypesQuery = useQuery({
    queryKey: ['leaveTypes', 'select-list'],
    queryFn: () => leaveTypesApi.getLeaveTypes({ pageSize: 100, isActive: true }),
  });
  const leaveTypeOptions = [
    { value: '', label: 'All leave types' },
    ...(leaveTypesQuery.data?.items ?? []).map((leaveType) => ({
      value: leaveType.id,
      label: `${leaveType.name} (${leaveType.code})`,
      keywords: leaveType.code,
    })),
  ];

  function resetPage() {
    setPage(1);
  }

  return (
    <div className="flex flex-1 flex-col">
      <div className="px-6 pt-4 lg:px-8">
        <Link to="/admin/hr" className="text-sm text-muted-foreground hover:text-foreground">
          &larr; Back to HR
        </Link>
      </div>

      <div className="mt-3 flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        <div className="flex items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
            <CalendarRange className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight">Leave Requests</h1>
        </div>
        <p className="max-w-2xl text-sm text-page-banner-foreground/85">Submit, approve, reject, and track employee leave requests.</p>
      </div>

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
        <div className="flex flex-wrap items-end gap-3">
          <div className="w-56">
            <Label htmlFor="lr-filter-employee" className="mb-1.5 block text-xs text-muted-foreground">
              Employee
            </Label>
            <EmployeeSelect
              id="lr-filter-employee"
              value={employeeId ?? ''}
              onValueChange={(value) => {
                setEmployeeId(value || undefined);
                resetPage();
              }}
              includeNoneOption
              noneLabel="All employees"
              ariaLabel="Filter by employee"
            />
          </div>

          <div className="w-56">
            <Label htmlFor="lr-filter-leaveType" className="mb-1.5 block text-xs text-muted-foreground">
              Leave Type
            </Label>
            <SearchableSelect
              id="lr-filter-leaveType"
              value={leaveTypeId ?? ''}
              onValueChange={(value) => {
                setLeaveTypeId(value || undefined);
                resetPage();
              }}
              options={leaveTypeOptions}
              placeholder="All leave types"
              searchPlaceholder="Search by name or code…"
              ariaLabel="Filter by leave type"
            />
          </div>

          <div className="w-40">
            <Label htmlFor="lr-filter-status" className="mb-1.5 block text-xs text-muted-foreground">
              Status
            </Label>
            <Select
              value={status ?? 'all'}
              onValueChange={(value) => {
                setStatus(value === 'all' ? undefined : value);
                resetPage();
              }}
            >
              <SelectTrigger id="lr-filter-status" aria-label="Filter by status">
                <SelectValue placeholder="All statuses" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All statuses</SelectItem>
                {LEAVE_REQUEST_STATUSES.map((s) => (
                  <SelectItem key={s} value={s}>
                    {s}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div className="w-40">
            <Label htmlFor="lr-filter-from" className="mb-1.5 block text-xs text-muted-foreground">
              Start From
            </Label>
            <Input
              id="lr-filter-from"
              type="date"
              value={dateFrom}
              onChange={(event) => {
                setDateFrom(event.target.value);
                resetPage();
              }}
            />
          </div>

          <div className="w-40">
            <Label htmlFor="lr-filter-to" className="mb-1.5 block text-xs text-muted-foreground">
              Start To
            </Label>
            <Input
              id="lr-filter-to"
              type="date"
              value={dateTo}
              onChange={(event) => {
                setDateTo(event.target.value);
                resetPage();
              }}
            />
          </div>

          {canCreate && (
            <Button className="ml-auto gap-1.5" onClick={() => setShowNewRequest(true)}>
              <Plus className="h-4 w-4" />
              New Leave Request
            </Button>
          )}
        </div>

        {isPending && (
          <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" />
            Loading leave requests…
          </div>
        )}

        {isError && (
          <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
            {error instanceof Error ? error.message : 'Failed to load leave requests.'}
          </p>
        )}

        {!isPending && !isError && data && data.items.length === 0 && (
          <Card className="border-dashed">
            <CardContent className="flex flex-col items-center gap-2 py-16 text-center">
              <p className="text-sm font-medium text-foreground">No leave requests found</p>
              <p className="text-sm text-muted-foreground">Submit a new leave request to get started.</p>
            </CardContent>
          </Card>
        )}

        {!isPending && !isError && data && data.items.length > 0 && (
          <div className="flex flex-col gap-3">
            <LeaveRequestTable
              leaveRequests={data.items}
              onApproveRequested={setApproving}
              onRejectRequested={setRejecting}
              onCancelRequested={(leaveRequest) => cancelMutation.mutate(leaveRequest.id)}
              isCancellingId={cancelMutation.isPending ? (cancelMutation.variables as string | undefined) : undefined}
            />
            <Pagination meta={data.meta} onPageChange={setPage} />
          </div>
        )}

        {showNewRequest && <NewLeaveRequestDialog onClose={() => setShowNewRequest(false)} />}
        {approving && <ApproveLeaveRequestDialog leaveRequest={approving} onClose={() => setApproving(null)} />}
        {rejecting && <RejectLeaveRequestDialog leaveRequest={rejecting} onClose={() => setRejecting(null)} />}
      </div>
    </div>
  );
}
