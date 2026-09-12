import type { InvoicePaymentStatus } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { Loader2 } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Pagination } from '@/components/Pagination';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { formatCurrency, PaymentStatusBadge } from '@/features/billing';
import { consultantsApi, departmentsApi } from '@/services/apiClient';
import { useOpdProceduresQuery } from '../hooks/useOpdProceduresQuery';
import { toRangeEnd, toRangeStart, type OpdFilterValues } from '../types';

const PAGE_SIZE = 10;

interface OpdProceduresListTableProps {
  filters: OpdFilterValues;
  page: number;
  onPageChange: (page: number) => void;
}

/**
 * OPD Procedures List tab — one row per Procedure invoice line item, across every invoice.
 *
 * ProcedureListItem's serviceId/consultantId/departmentId are free-text display strings on
 * the backend (Procedure billing lines store plain text, not lookup ids — see
 * ProcedureListItem's own doc comment in dtos/billing/invoice.ts), while the shared
 * OpdFilterBar's Department/Consultant selects hand back Guids. To actually filter, this
 * component resolves the selected department/consultant Guid to its display name (reusing
 * the same cached department/consultant list DepartmentSelect/ConsultantSelect already
 * populate, so this adds no extra round-trip) and sends that name string to the query instead
 * of the raw id.
 */
export function OpdProceduresListTable({ filters, page, onPageChange }: OpdProceduresListTableProps) {
  const { data: departments } = useQuery({
    queryKey: ['departments', 'select-list'],
    queryFn: () => departmentsApi.getDepartments({ pageSize: 100, isActive: true }),
  });
  const { data: consultants } = useQuery({
    queryKey: ['consultants', 'select-list', filters.departmentId],
    queryFn: () => consultantsApi.getConsultants({ pageSize: 100, isActive: true, departmentId: filters.departmentId }),
    enabled: Boolean(filters.departmentId),
  });

  const departmentName = departments?.items.find((department) => department.id === filters.departmentId)?.name;
  const consultantName = consultants?.items.find((consultant) => consultant.id === filters.consultantId)?.name;

  const { data, isPending, isError, error } = useOpdProceduresQuery({
    page,
    pageSize: PAGE_SIZE,
    from: toRangeStart(filters.from),
    to: toRangeEnd(filters.to),
    departmentId: departmentName,
    consultantId: consultantName,
    paymentStatus: filters.status as InvoicePaymentStatus | undefined,
    search: filters.search || undefined,
  });

  if (isPending) {
    return (
      <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
        <Loader2 className="h-4 w-4 animate-spin" />
        Loading procedures…
      </div>
    );
  }

  if (isError) {
    return (
      <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
        {error instanceof Error ? error.message : 'Failed to load procedures.'}
      </p>
    );
  }

  if (!data || data.items.length === 0) {
    return (
      <Card className="border-dashed">
        <CardContent className="flex flex-col items-center gap-2 py-16 text-center">
          <p className="text-sm font-medium text-foreground">No procedures found</p>
          <p className="text-sm text-muted-foreground">Try a different date range or filter.</p>
        </CardContent>
      </Card>
    );
  }

  return (
    <div className="flex flex-col gap-3">
      <div className="overflow-hidden rounded-lg border border-border">
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="bg-muted/50 text-left text-xs font-medium uppercase tracking-wide text-muted-foreground">
              <tr>
                <th className="px-4 py-2.5">Patient</th>
                <th className="px-4 py-2.5">UHID</th>
                <th className="px-4 py-2.5">Procedure</th>
                <th className="px-4 py-2.5">Consultant</th>
                <th className="px-4 py-2.5">Department</th>
                <th className="px-4 py-2.5">Date/Time</th>
                <th className="px-4 py-2.5">Status</th>
                <th className="px-4 py-2.5">Charges</th>
                <th className="px-4 py-2.5 text-right">Action</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {data.items.map((item) => {
                // serviceId is actually the procedure's display NAME, not a lookup id — see
                // ProcedureListItem's own doc comment.
                const procedureName = item.serviceId;
                return (
                  <tr key={item.invoiceLineItemId} className="hover:bg-muted/30">
                    <td className="whitespace-nowrap px-4 py-3 font-medium text-foreground">{item.patientName}</td>
                    <td className="whitespace-nowrap px-4 py-3 font-mono text-xs text-muted-foreground">{item.patientUhid}</td>
                    <td className="whitespace-nowrap px-4 py-3 text-foreground">{procedureName ?? '—'}</td>
                    <td className="whitespace-nowrap px-4 py-3 text-foreground">{item.consultantId ?? '—'}</td>
                    <td className="whitespace-nowrap px-4 py-3 text-foreground">{item.departmentId ?? '—'}</td>
                    <td className="whitespace-nowrap px-4 py-3 font-mono text-xs text-muted-foreground">
                      {new Date(item.createdAt).toLocaleString('en-IN')}
                    </td>
                    <td className="whitespace-nowrap px-4 py-3">
                      <PaymentStatusBadge status={item.paymentStatus} />
                    </td>
                    <td className="whitespace-nowrap px-4 py-3 font-medium text-foreground">{formatCurrency(item.total)}</td>
                    <td className="px-4 py-3">
                      <div className="flex justify-end">
                        <Button asChild variant="ghost" size="sm">
                          <Link to={`/finance/accounts/${item.invoiceId}`}>View</Link>
                        </Button>
                      </div>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      </div>
      <Pagination meta={data.meta} onPageChange={onPageChange} />
    </div>
  );
}
