import type { ActivityLogEntry, ActivityLogListQuery } from '@hms/shared';
import { History, Loader2 } from 'lucide-react';
import { useCallback, useMemo, useState } from 'react';
import { PageBanner } from '@/components/PageBanner';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { useToast } from '@/components/ui/toast-context';
import { ActivityDetailsDrawer } from '../../features/activityLog/components/ActivityDetailsDrawer';
import { ActivityLogFilters } from '../../features/activityLog/components/ActivityLogFilters';
import { ActivityLogPagination } from '../../features/activityLog/components/ActivityLogPagination';
import { ActivityLogTable } from '../../features/activityLog/components/ActivityLogTable';
import {
  actionLabel,
  EMPTY_FILTERS,
  entityLabel,
  formatDateTime,
  type ActivityLogFilterValues,
  type ExportFormat,
} from '../../features/activityLog/activityLogUtils';
import { useActivityLogsQuery } from '../../features/activityLog/hooks/useActivityLogQueries';
import { RequirePermission } from '../../features/auth/RequirePermission';
import { exportReportToCsv, exportReportToExcel, exportReportToPdf } from '../../features/reports/exportUtils';
import { useUsersQuery } from '../../features/users';
import { activityLogApi } from '../../services/apiClient';

const PAGE_SIZE = 10;
// The API caps a page at 100 rows; exports walk pages up to this many rows so an unfiltered
// export can't pull the whole audit table into the browser.
const EXPORT_PAGE_SIZE = 100;
const EXPORT_MAX_ROWS = 1000;

function toQuery(filters: ActivityLogFilterValues, page: number, pageSize: number): ActivityLogListQuery {
  return {
    page,
    pageSize,
    from: filters.from || undefined,
    to: filters.to || undefined,
    userId: filters.userId || undefined,
    module: filters.module || undefined,
    action: filters.action || undefined,
    search: filters.entity.trim() || undefined,
  };
}

export default function ActivityLogPage() {
  const { toast } = useToast();
  const [draft, setDraft] = useState<ActivityLogFilterValues>(EMPTY_FILTERS);
  const [applied, setApplied] = useState<ActivityLogFilterValues>(EMPTY_FILTERS);
  const [page, setPage] = useState(1);
  const [selected, setSelected] = useState<ActivityLogEntry | null>(null);
  const [isExporting, setIsExporting] = useState(false);

  const { data, isPending, isError, error, refetch, isFetching } = useActivityLogsQuery(toQuery(applied, page, PAGE_SIZE));

  // Reuses the existing Users list purely to turn the API's opaque userId into a name (and to
  // fill the User filter) — the activity API itself returns only the id.
  const { data: usersData } = useUsersQuery({ page: 1, pageSize: 100 });
  const users = useMemo(
    () => (usersData?.items ?? []).map((u) => ({ id: u.id, name: `${u.firstName} ${u.lastName}`.trim() || u.username })),
    [usersData],
  );
  const userName = useCallback(
    (userId: string | null | undefined) => {
      if (!userId) {
        return 'System';
      }
      return users.find((u) => u.id === userId)?.name ?? `User ${userId.slice(0, 8)}`;
    },
    [users],
  );

  function handleSearch() {
    setApplied(draft);
    setPage(1);
  }

  function handleReset() {
    setDraft(EMPTY_FILTERS);
    setApplied(EMPTY_FILTERS);
    setPage(1);
  }

  async function handleExport(format: ExportFormat) {
    setIsExporting(true);
    try {
      const rows: ActivityLogEntry[] = [];
      for (let p = 1; rows.length < EXPORT_MAX_ROWS; p++) {
        const result = await activityLogApi.getActivityLogs(toQuery(applied, p, EXPORT_PAGE_SIZE));
        rows.push(...result.items);
        if (p >= result.meta.totalPages) {
          break;
        }
      }

      const headers = ['Date & Time', 'User', 'Module', 'Action', 'Entity', 'Description', 'Result'];
      const body = rows.map((r) => [
        formatDateTime(r.createdAt),
        userName(r.userId),
        r.module,
        actionLabel(r.action),
        [r.entityType, r.entityId].filter(Boolean).join(' · ') || entityLabel(r),
        r.description ?? '',
        r.isSuccess ? 'Success' : 'Failed',
      ]);
      const sections = [{ heading: 'Activity Log', headers, rows: body }];
      const stamp = new Date().toISOString().slice(0, 10);

      if (format === 'csv') {
        exportReportToCsv(`activity-log-${stamp}.csv`, sections);
      } else if (format === 'excel') {
        await exportReportToExcel(`activity-log-${stamp}.xlsx`, sections);
      } else {
        exportReportToPdf(`activity-log-${stamp}.pdf`, 'Activity Log', sections);
      }
    } catch (err) {
      toast({ title: 'Export failed', description: err instanceof Error ? err.message : 'Could not export the activity log.', variant: 'error' });
    } finally {
      setIsExporting(false);
    }
  }

  return (
    <RequirePermission permission="identity-administration.view">
      <div className="flex flex-1 flex-col">
        <PageBanner icon={History} title="Activity Log" subtitle="Track user activity and changes across the hospital system." />

        <div className="flex flex-1 flex-col gap-4 p-4 lg:p-6">
          <Card>
            <CardContent className="p-4">
              <ActivityLogFilters
                values={draft}
                onChange={setDraft}
                onSearch={handleSearch}
                onReset={handleReset}
                onExport={handleExport}
                isExporting={isExporting}
                users={users}
              />
            </CardContent>
          </Card>

          <Card>
            <CardContent className="flex flex-col gap-3 p-4">
              {isPending && (
                <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
                  <Loader2 className="h-4 w-4 animate-spin" />
                  Loading activity…
                </div>
              )}

              {isError && (
                <div role="alert" className="flex flex-wrap items-center justify-between gap-2 rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
                  <span>{error instanceof Error ? error.message : 'Failed to load the activity log.'}</span>
                  <Button variant="outline" size="sm" onClick={() => refetch()} disabled={isFetching}>
                    Retry
                  </Button>
                </div>
              )}

              {!isPending && !isError && data && data.items.length === 0 && (
                <div className="flex flex-col items-center gap-1 rounded-lg border border-dashed border-border py-12 text-center">
                  <p className="text-sm font-medium text-foreground">No activity found</p>
                  <p className="text-sm text-muted-foreground">Try widening the date range or clearing some filters.</p>
                </div>
              )}

              {!isPending && !isError && data && data.items.length > 0 && (
                <>
                  <ActivityLogTable entries={data.items} userName={userName} onView={setSelected} selectedId={selected?.id ?? null} />
                  <ActivityLogPagination meta={data.meta} onPageChange={setPage} />
                </>
              )}
            </CardContent>
          </Card>
        </div>

        <ActivityDetailsDrawer entry={selected} userName={userName} onClose={() => setSelected(null)} />
      </div>
    </RequirePermission>
  );
}
