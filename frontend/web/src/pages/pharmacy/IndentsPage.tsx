import { ApiError, type IndentResponse } from '@hms/shared';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Loader2, Plus, ReceiptText, RefreshCw } from 'lucide-react';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { Pagination } from '@/components/Pagination';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { useToast } from '@/components/ui/toast-context';
import { useAuth } from '@/features/auth/AuthContext';
import { dateTimeLabel, quantity } from '@/features/pharmacy/common/format';
import { INDENT_STATUS } from '@/features/pharmacy/common/labels';
import { PharmacyPage } from '@/features/pharmacy/shell/PharmacyPage';
import { cn } from '@/lib/utils';
import { pharmacyApi } from '@/services/apiClient';

/**
 * Ward requests for inpatients' medicines. Open indents are at the top — urgent first, then
 * oldest — because this is a queue to be worked. "Issue" opens the counter with the patient,
 * their admission and the pending items loaded; the bill goes on the admission.
 */
export default function IndentsPage() {
  const queryClient = useQueryClient();
  const { toast } = useToast();
  const { hasPermission } = useAuth();
  const [openOnly, setOpenOnly] = useState(true);
  const [page, setPage] = useState(1);
  const canIssue = hasPermission('pharmacy.create');

  const query = { page, pageSize: 20, openOnly: openOnly || undefined };
  const { data, isPending, isError, isFetching, refetch } = useQuery({
    queryKey: ['pharmacy', 'indents', 'list', query],
    queryFn: () => pharmacyApi.getIndents(query),
    placeholderData: (previous) => previous,
    refetchInterval: openOnly ? 30_000 : false,
  });

  const cancelMutation = useMutation({
    mutationFn: (indent: IndentResponse) => pharmacyApi.cancelIndent(indent.id),
    onSuccess: (indent) => {
      void queryClient.invalidateQueries({ queryKey: ['pharmacy', 'indents'] });
      toast({ title: `Indent ${indent.indentNo} closed`, variant: 'success' });
    },
    onError: (failure) => toast({ title: 'Could not close the indent', description: failure instanceof ApiError ? failure.message : undefined, variant: 'error' }),
  });

  return (
    <PharmacyPage
      title="IP indents"
      subtitle="Medicines the wards have asked for, for admitted patients."
      actions={
        <>
          <div className="flex overflow-hidden rounded-md border border-border text-xs font-medium">
            {[true, false].map((value) => (
              <button key={String(value)} type="button" onClick={() => { setOpenOnly(value); setPage(1); }} className={cn('px-3 py-1.5', openOnly === value ? 'bg-primary text-primary-foreground' : 'text-muted-foreground hover:bg-accent')}>
                {value ? 'To issue' : 'All'}
              </button>
            ))}
          </div>
          <Button size="sm" variant="outline" onClick={() => void refetch()} disabled={isFetching}>
            <RefreshCw className={cn('h-4 w-4', isFetching && 'animate-spin')} />
            Refresh
          </Button>
          <Button size="sm" asChild>
            <Link to="/pharmacy/indents/new">
              <Plus className="h-4 w-4" />
              New indent
            </Link>
          </Button>
        </>
      }
    >
      {isPending && (
        <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" />
          Loading indents…
        </div>
      )}
      {isError && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          Indents could not be loaded.
        </p>
      )}

      {data && data.items.length === 0 && (
        <p className="rounded-lg border border-dashed border-border bg-background p-10 text-center text-sm text-muted-foreground">
          {openOnly ? 'No indents are waiting to be issued.' : 'No indents have been raised yet.'}
        </p>
      )}

      <div className="grid gap-3 xl:grid-cols-2">
        {data?.items.map((indent) => {
          const open = indent.status === 'Pending' || indent.status === 'PartlyIssued';
          const status = INDENT_STATUS[indent.status];
          return (
            <article key={indent.id} className={cn('rounded-lg border bg-background', open ? (indent.isUrgent ? 'border-destructive/50' : 'border-primary/30') : 'border-border')}>
              <header className="flex flex-wrap items-start justify-between gap-3 border-b border-border px-4 py-3">
                <div className="min-w-0">
                  <p className="truncate font-medium text-foreground">
                    {indent.patientName}
                    {indent.isUrgent && open && <Badge variant="destructive" className="ml-2">Urgent</Badge>}
                  </p>
                  <p className="truncate text-xs text-muted-foreground">
                    {[indent.patientUhid, indent.admissionNumber, indent.location, indent.doctorName].filter(Boolean).join(' · ')}
                  </p>
                  <p className="text-xs text-muted-foreground">
                    <span className="font-mono">{indent.indentNo}</span> · {dateTimeLabel(indent.requestedAt)}
                  </p>
                </div>
                <div className="flex flex-col items-end gap-1.5">
                  <Badge variant={status.variant}>{status.label}</Badge>
                  {open && canIssue && (
                    <Button size="sm" asChild>
                      <Link to={`/pharmacy/counter?indent=${indent.id}`}>
                        <ReceiptText className="h-4 w-4" />
                        Issue
                      </Link>
                    </Button>
                  )}
                  {open && (
                    <button type="button" onClick={() => cancelMutation.mutate(indent)} disabled={cancelMutation.isPending} className="text-xs text-muted-foreground hover:text-destructive hover:underline">
                      {indent.status === 'PartlyIssued' ? 'Close the rest' : 'Cancel'}
                    </button>
                  )}
                </div>
              </header>
              <ul className="divide-y divide-border text-sm">
                {indent.lines.map((line) => (
                  <li key={line.id} className="flex items-baseline justify-between gap-3 px-4 py-1.5">
                    <span className="min-w-0">
                      <span className="font-medium text-foreground">{line.productName}</span>
                      {line.instructions && <span className="ml-2 text-xs text-muted-foreground">{line.instructions}</span>}
                    </span>
                    <span className="shrink-0 tabular-nums text-muted-foreground">
                      {line.issuedQuantity > 0 ? `${quantity(line.issuedQuantity)} of ${quantity(line.quantity)} issued` : `× ${quantity(line.quantity)}`}
                    </span>
                  </li>
                ))}
                {indent.remarks && <li className="px-4 py-1.5 text-xs text-muted-foreground">{indent.remarks}</li>}
              </ul>
            </article>
          );
        })}
      </div>
      {data && data.meta.totalPages > 1 && <Pagination meta={data.meta} onPageChange={setPage} />}
    </PharmacyPage>
  );
}
