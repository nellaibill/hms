import type { LabOrderItemStatus } from '@hms/shared';
import { Loader2 } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Pagination } from '@/components/Pagination';
import { Badge } from '@/components/ui/badge';
import { LoadingOverlay } from '@/components/LoadingOverlay';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { ConsultantName } from '@/components/ConsultantName';
import { LabStatusBadge } from '@/features/laboratory';
import { useOpdInvestigationsQuery } from '../hooks/useOpdInvestigationsQuery';
import { toRangeEnd, toRangeStart, type OpdFilterValues } from '../types';

const PAGE_SIZE = 10;

interface OpdInvestigationsListTableProps {
  filters: OpdFilterValues;
  page: number;
  onPageChange: (page: number) => void;
}

/** OPD Investigations List tab — every lab/radiology order item placed from OPD (Laboratory's
 * `source=OP` filter), one row per test/service rather than per order (an order covering 3
 * tests renders as 3 rows, each independently actionable from the worklist). */
export function OpdInvestigationsListTable({ filters, page, onPageChange }: OpdInvestigationsListTableProps) {
  const { data, isPending, isPlaceholderData, isError, error } = useOpdInvestigationsQuery({
    page,
    pageSize: PAGE_SIZE,
    dateFrom: toRangeStart(filters.from),
    dateTo: toRangeEnd(filters.to),
    departmentId: filters.departmentId,
    consultantId: filters.consultantId,
    status: filters.status as LabOrderItemStatus | undefined,
    search: filters.search || undefined,
  });

  if (isPending) {
    return (
      <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
        <Loader2 className="h-4 w-4 animate-spin" />
        Loading investigations…
      </div>
    );
  }

  if (isError) {
    return (
      <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
        {error instanceof Error ? error.message : 'Failed to load investigations.'}
      </p>
    );
  }

  if (!data || data.rows.length === 0) {
    return (
      <LoadingOverlay active={isPlaceholderData} label="Loading investigations…">
        <Card className="border-dashed">
          <CardContent className="flex flex-col items-center gap-2 py-16 text-center">
            <p className="text-sm font-medium text-foreground">No investigations found</p>
            <p className="text-sm text-muted-foreground">Try a different date range or filter.</p>
          </CardContent>
        </Card>
      </LoadingOverlay>
    );
  }

  return (
    <LoadingOverlay active={isPlaceholderData} label="Loading investigations…">
      <div className="flex flex-col gap-3">
        <div className="overflow-x-auto rounded-lg border border-border">
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="bg-muted/50 text-left text-xs font-medium uppercase tracking-wide text-muted-foreground">
                <tr>
                  <th className="px-4 py-2.5">Patient</th>
                  <th className="px-4 py-2.5">UHID</th>
                  <th className="px-4 py-2.5">Investigation</th>
                  <th className="px-4 py-2.5">Type</th>
                  <th className="px-4 py-2.5">Consultant</th>
                  <th className="px-4 py-2.5">Date/Time</th>
                  <th className="px-4 py-2.5">Status</th>
                  <th className="px-4 py-2.5 text-right">Action</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border">
                {data.rows.map(({ order, item }) => (
                  <tr key={item.id} className="hover:bg-muted/30">
                    <td className="whitespace-nowrap px-4 py-3 font-medium text-foreground">{order.patientName}</td>
                    <td className="whitespace-nowrap px-4 py-3 font-mono text-xs text-muted-foreground">{order.patientUhid}</td>
                    <td className="whitespace-nowrap px-4 py-3 text-foreground">{item.testName}</td>
                    <td className="whitespace-nowrap px-4 py-3">
                      {item.serviceType ? <Badge variant="outline">{item.serviceType}</Badge> : <span className="text-muted-foreground">—</span>}
                    </td>
                    {/* LabOrderItemResponse only carries consultantId (a Guid), not a display
                        name — unlike OpdPatientListItem/ProcedureListItem, Laboratory doesn't
                        denormalize the consultant's name onto its order items, so it's resolved
                        here from the cached consultants list instead of showing the raw Guid. */}
                    <td className="whitespace-nowrap px-4 py-3 text-foreground">
                      {item.consultantId ? <ConsultantName consultantId={item.consultantId} /> : <span className="text-muted-foreground">—</span>}
                    </td>
                    <td className="whitespace-nowrap px-4 py-3 font-mono text-xs text-muted-foreground">
                      {new Date(order.createdAt).toLocaleString('en-IN')}
                    </td>
                    <td className="whitespace-nowrap px-4 py-3">
                      <LabStatusBadge status={item.status} />
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex justify-end">
                        <Button asChild variant="ghost" size="sm">
                          <Link to={`/diagnostics/lab/orders/${order.id}`}>View</Link>
                        </Button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
        <Pagination meta={data.meta} onPageChange={onPageChange} />
      </div>
    </LoadingOverlay>
  );
}
