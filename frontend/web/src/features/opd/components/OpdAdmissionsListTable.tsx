import { ApiError, type Admission, type AdmissionStatus, type RequestAdmissionRequest } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { Loader2, Plus } from 'lucide-react';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { Pagination } from '@/components/Pagination';
import { Badge, type BadgeProps } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { AssignBedDialog, useAdmissionsQuery, useAssignBedMutation, useRequestAdmissionMutation } from '@/features/ipd/admissions';
import { useAuth } from '@/features/auth/AuthContext';
import { departmentsApi } from '@/services/apiClient';
import { RequestAdmissionDialog } from './RequestAdmissionDialog';
import type { OpdFilterValues } from '../types';

const PAGE_SIZE = 10;

const STATUS_VARIANTS: Record<AdmissionStatus, BadgeProps['variant']> = {
  Requested: 'warning',
  Admitted: 'success',
  Discharged: 'secondary',
  Cancelled: 'destructive',
};

interface OpdAdmissionsListTableProps {
  filters: OpdFilterValues;
  page: number;
  onPageChange: (page: number) => void;
}

/** OPD Admissions List tab — admission requests raised from OPD. Defaults to status
 * 'Requested' (an OPD consultant asking for a bed, not yet assigned one) unless the shared
 * filter bar's Status dropdown picks a different AdmissionStatus explicitly. A ward/bed is
 * assigned right here (or from IPD's own Admissions List "Requested" tab — both call the
 * same assign-bed endpoint). */
export function OpdAdmissionsListTable({ filters, page, onPageChange }: OpdAdmissionsListTableProps) {
  const { hasPermission } = useAuth();
  const [isRequestOpen, setIsRequestOpen] = useState(false);
  const [assigningAdmission, setAssigningAdmission] = useState<Admission | null>(null);

  const { data, isPending, isError, error } = useAdmissionsQuery({
    page,
    pageSize: PAGE_SIZE,
    search: filters.search || undefined,
    status: (filters.status as AdmissionStatus | undefined) ?? 'Requested',
    departmentId: filters.departmentId,
    consultantId: filters.consultantId,
  });

  const requestMutation = useRequestAdmissionMutation();
  const assignBedMutation = useAssignBedMutation();

  // Admission doesn't denormalize a department display name (only departmentId, a Guid) —
  // resolved here from the same cached department list DepartmentSelect populates, rather
  // than leaving the column blank.
  const { data: departments } = useQuery({
    queryKey: ['departments', 'select-list'],
    queryFn: () => departmentsApi.getDepartments({ pageSize: 100, isActive: true }),
  });
  const departmentNameById = new Map((departments?.items ?? []).map((department) => [department.id, department.name]));

  function handleRequestAdmission(request: RequestAdmissionRequest) {
    requestMutation.mutate(request, { onSuccess: () => setIsRequestOpen(false) });
  }

  function handleAssignBed(request: { wardId: string; bedId: string }) {
    if (!assigningAdmission) {
      return;
    }
    assignBedMutation.mutate({ id: assigningAdmission.id, request }, { onSuccess: () => setAssigningAdmission(null) });
  }

  return (
    <div className="flex flex-col gap-3">
      {hasPermission('clinical-care.create') && (
        <div className="flex justify-end">
          <Button className="gap-1.5" onClick={() => setIsRequestOpen(true)}>
            <Plus className="h-4 w-4" />
            Request Admission
          </Button>
        </div>
      )}

      {isPending && (
        <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" />
          Loading admissions…
        </div>
      )}

      {isError && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {error instanceof Error ? error.message : 'Failed to load admissions.'}
        </p>
      )}

      {!isPending && !isError && data && data.items.length === 0 && (
        <Card className="border-dashed">
          <CardContent className="flex flex-col items-center gap-2 py-16 text-center">
            <p className="text-sm font-medium text-foreground">No admission requests found</p>
            <p className="text-sm text-muted-foreground">Try a different date range or filter.</p>
          </CardContent>
        </Card>
      )}

      {!isPending && !isError && data && data.items.length > 0 && (
        <>
          <div className="overflow-hidden rounded-lg border border-border">
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead className="bg-muted/50 text-left text-xs font-medium uppercase tracking-wide text-muted-foreground">
                  <tr>
                    <th className="px-4 py-2.5">Patient</th>
                    <th className="px-4 py-2.5">UHID</th>
                    <th className="px-4 py-2.5">Age/Gender</th>
                    <th className="px-4 py-2.5">Consultant</th>
                    <th className="px-4 py-2.5">Department</th>
                    <th className="px-4 py-2.5">Admission Type</th>
                    <th className="px-4 py-2.5">Requested Date/Time</th>
                    <th className="px-4 py-2.5">Admission Status</th>
                    <th className="px-4 py-2.5">Action</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-border">
                  {data.items.map((admission) => (
                    <tr key={admission.id} className="hover:bg-muted/30">
                      <td className="whitespace-nowrap px-4 py-3 font-medium text-foreground">{admission.patientName}</td>
                      <td className="whitespace-nowrap px-4 py-3 font-mono text-xs text-muted-foreground">{admission.uhid}</td>
                      <td className="whitespace-nowrap px-4 py-3 text-foreground">
                        {admission.age} / {admission.gender}
                      </td>
                      <td className="whitespace-nowrap px-4 py-3 text-foreground">{admission.consultantName}</td>
                      <td className="whitespace-nowrap px-4 py-3 text-foreground">{departmentNameById.get(admission.departmentId) ?? '—'}</td>
                      <td className="whitespace-nowrap px-4 py-3 text-foreground">{admission.admissionType}</td>
                      <td className="whitespace-nowrap px-4 py-3 font-mono text-xs text-muted-foreground">
                        {new Date(admission.admissionDateTime).toLocaleString('en-IN')}
                      </td>
                      <td className="whitespace-nowrap px-4 py-3">
                        <Badge variant={STATUS_VARIANTS[admission.status]}>{admission.status}</Badge>
                      </td>
                      <td className="whitespace-nowrap px-4 py-3">
                        {admission.status === 'Requested' ? (
                          <Button size="sm" onClick={() => setAssigningAdmission(admission)}>
                            Assign Bed
                          </Button>
                        ) : (
                          <Button asChild size="sm" variant="outline">
                            <Link to={`/clinical/ipd/admissions/${admission.id}`}>View</Link>
                          </Button>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
          <Pagination meta={data.meta} onPageChange={onPageChange} />
        </>
      )}

      {isRequestOpen && (
        <RequestAdmissionDialog
          isSubmitting={requestMutation.isPending}
          apiError={requestMutation.error instanceof ApiError ? requestMutation.error : null}
          onSubmit={handleRequestAdmission}
          onCancel={() => setIsRequestOpen(false)}
        />
      )}

      {assigningAdmission && (
        <AssignBedDialog
          patientName={assigningAdmission.patientName}
          isSubmitting={assignBedMutation.isPending}
          apiError={assignBedMutation.error instanceof ApiError ? assignBedMutation.error : null}
          onSubmit={handleAssignBed}
          onCancel={() => setAssigningAdmission(null)}
        />
      )}
    </div>
  );
}
