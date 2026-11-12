import type { SyncResolution, SyncStreamListQuery } from '@hms/shared';
import { CloudCog, ExternalLink, Loader2, Pause, Play, RefreshCw, RotateCcw, Search, UploadCloud } from 'lucide-react';
import { useState } from 'react';
import { PageBanner } from '@/components/PageBanner';
import { Pagination } from '@/components/Pagination';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { useToast } from '@/components/ui/toast-context';
import { useAuth } from '@/features/auth/AuthContext';
import { RequirePermission } from '../../features/auth/RequirePermission';
import { CLOUD_SYNC_EDIT_PERMISSION, CLOUD_SYNC_VIEW_PERMISSION, overallStatusView } from '../../features/cloudSync/cloudSyncUtils';
import { SyncConfirmDialog, SyncHistoryDrawer, SyncResolveDialog, type ResolveTarget } from '../../features/cloudSync/components/SyncDialogs';
import { SyncStatusCards } from '../../features/cloudSync/components/SyncStatusCards';
import { SyncFailedEventsTable, SyncPatientsTable } from '../../features/cloudSync/components/SyncTables';
import {
  usePauseMutation,
  useResolveEventMutation,
  useResumeMutation,
  useRetryAllMutation,
  useRetryEventMutation,
  useSyncFailedEventsQuery,
  useSyncStreamsQuery,
  useSyncSummaryQuery,
  useTriggerMutation,
} from '../../features/cloudSync/hooks/useCloudSyncQueries';
import { env } from '../../config/env';
import { syncMonitorApi } from '../../services/apiClient';

const PAGE_SIZE = 20;
const ALL = '__all__';

const STATUS_FILTERS: { value: string; label: string }[] = [
  { value: 'Unsynced', label: 'Not yet synced' },
  { value: 'NeedsAttention', label: 'Needs attention' },
  { value: 'Conflict', label: 'Conflict' },
  { value: 'Failed', label: 'Retrying' },
  { value: 'Pending', label: 'Waiting' },
  { value: 'Synced', label: 'Synced' },
  { value: ALL, label: 'All patients' },
];

type ConfirmKind = 'retryAll' | 'pause' | null;

function errorMessage(error: unknown, fallback: string): string {
  return error instanceof Error && error.message ? error.message : fallback;
}

/**
 * Cloud Sync (ADR-133): business-level monitoring of this local server's offline-first sync to
 * the cloud. Reads need identity-administration.view; every action needs .edit and is audited
 * by the API. Talks only to this server's API — never to the cloud or the outbox tables.
 */
export default function CloudSyncPage() {
  const { toast } = useToast();
  const { hasPermission } = useAuth();
  const canEdit = hasPermission(CLOUD_SYNC_EDIT_PERMISSION);
  const canSeePatients = hasPermission('patient-management.view');

  const [tab, setTab] = useState('patients');
  const [searchDraft, setSearchDraft] = useState('');
  const [streamQuery, setStreamQuery] = useState<SyncStreamListQuery>({ page: 1, pageSize: PAGE_SIZE, status: 'Unsynced' });
  const [eventsPage, setEventsPage] = useState(1);
  const [historyPatientId, setHistoryPatientId] = useState<string | null>(null);
  const [resolveTarget, setResolveTarget] = useState<ResolveTarget | null>(null);
  const [confirm, setConfirm] = useState<ConfirmKind>(null);
  const [openingDashboard, setOpeningDashboard] = useState(false);

  const summaryQuery = useSyncSummaryQuery();
  const summary = summaryQuery.data;
  const isEdge = summary?.role === 'Edge';

  const streams = useSyncStreamsQuery(streamQuery, isEdge && tab === 'patients');
  const failedEvents = useSyncFailedEventsQuery({ page: eventsPage, pageSize: PAGE_SIZE }, isEdge && tab === 'failed');

  const retryEvent = useRetryEventMutation();
  const retryAll = useRetryAllMutation();
  const resolveEvent = useResolveEventMutation();
  const pause = usePauseMutation();
  const resume = useResumeMutation();
  const trigger = useTriggerMutation();
  const busy = retryEvent.isPending || retryAll.isPending || resolveEvent.isPending || pause.isPending || resume.isPending || trigger.isPending;

  function report(promise: Promise<{ message: string }>, failure: string, onDone?: () => void) {
    promise
      .then((result) => toast({ title: result.message, variant: 'success' }))
      .catch((error: unknown) => toast({ title: failure, description: errorMessage(error, 'Please try again.'), variant: 'error' }))
      .finally(() => onDone?.());
  }

  function applySearch() {
    setStreamQuery((q) => ({ ...q, page: 1, search: searchDraft.trim() || undefined }));
  }

  function handleResolve(resolution: SyncResolution, note: string) {
    if (!resolveTarget) {
      return;
    }
    report(
      resolveEvent.mutateAsync({ eventId: resolveTarget.eventId, request: { resolution, note: note || undefined } }),
      'Could not resolve the change',
      () => setResolveTarget(null),
    );
  }

  async function openDashboard() {
    // Opened before the request so the browser does not treat it as an unrequested pop-up.
    const tab = window.open('', '_blank');
    setOpeningDashboard(true);
    try {
      const session = await syncMonitorApi.createDashboardSession();
      const url = new URL(session.url, new URL(env.apiBaseUrl).origin).toString();
      if (tab) {
        tab.opener = null;
        tab.location.href = url;
      } else {
        window.location.assign(url);
      }
    } catch (error) {
      tab?.close();
      toast({ title: 'Could not open the job dashboard', description: errorMessage(error, 'Please try again.'), variant: 'error' });
    } finally {
      setOpeningDashboard(false);
    }
  }

  const header = (
    <PageBanner
      icon={CloudCog}
      title="Cloud Sync"
      subtitle="Patient changes saved on this server, and their delivery to the cloud."
    />
  );

  return (
    <RequirePermission permission={CLOUD_SYNC_VIEW_PERMISSION}>
      <div className="flex flex-1 flex-col">
        {header}

        <div className="flex flex-1 flex-col gap-4 p-4 lg:p-6">
          {summaryQuery.isPending && (
            <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
              <Loader2 className="h-4 w-4 animate-spin" />
              Loading sync status…
            </div>
          )}

          {summaryQuery.isError && (
            <div role="alert" className="flex flex-wrap items-center justify-between gap-2 rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
              <span>{errorMessage(summaryQuery.error, 'Could not load the sync status.')}</span>
              <Button variant="outline" size="sm" onClick={() => summaryQuery.refetch()}>
                Retry
              </Button>
            </div>
          )}

          {summary && !isEdge && (
            <Card>
              <CardContent className="flex flex-col gap-1 p-6 text-sm">
                <p className="font-semibold text-foreground">{overallStatusView(summary).label}</p>
                <p className="text-muted-foreground">{overallStatusView(summary).description}</p>
                <p className="text-muted-foreground">
                  This page monitors a hospital&apos;s local server, which saves every patient change locally first and sends it to the cloud in the background.
                </p>
              </CardContent>
            </Card>
          )}

          {summary && isEdge && (
            <>
              <div className="flex flex-wrap items-center justify-end gap-2">
                <Button variant="outline" size="sm" onClick={() => summaryQuery.refetch()} disabled={summaryQuery.isFetching}>
                  <RefreshCw className={`mr-2 h-4 w-4 ${summaryQuery.isFetching ? 'animate-spin' : ''}`} />
                  Refresh
                </Button>
                {canEdit && (
                  <>
                    <Button size="sm" onClick={() => report(trigger.mutateAsync(), 'Could not start a sync run')} disabled={busy || summary.isPaused}>
                      <UploadCloud className="mr-2 h-4 w-4" />
                      Sync now
                    </Button>
                    {summary.isPaused ? (
                      <Button variant="success" size="sm" onClick={() => report(resume.mutateAsync(), 'Could not resume sync')} disabled={busy}>
                        <Play className="mr-2 h-4 w-4" />
                        Resume
                      </Button>
                    ) : (
                      <Button variant="outline" size="sm" onClick={() => setConfirm('pause')} disabled={busy}>
                        <Pause className="mr-2 h-4 w-4" />
                        Pause
                      </Button>
                    )}
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => setConfirm('retryAll')}
                      disabled={busy || summary.failedEvents + summary.needsAttentionEvents === 0}
                    >
                      <RotateCcw className="mr-2 h-4 w-4" />
                      Retry all failed
                    </Button>
                    {summary.dashboardAvailable && (
                      <Button variant="outline" size="sm" onClick={openDashboard} disabled={openingDashboard}>
                        <ExternalLink className="mr-2 h-4 w-4" />
                        Job dashboard
                      </Button>
                    )}
                  </>
                )}
              </div>

              <SyncStatusCards summary={summary} />

              <Card>
                <CardContent className="flex flex-col gap-3 p-4">
                  <Tabs value={tab} onValueChange={setTab}>
                    <TabsList>
                      <TabsTrigger value="patients">Patients</TabsTrigger>
                      <TabsTrigger value="failed">
                        Failed changes
                        {summary.failedEvents + summary.needsAttentionEvents + summary.conflictEvents > 0 &&
                          ` (${summary.failedEvents + summary.needsAttentionEvents + summary.conflictEvents})`}
                      </TabsTrigger>
                    </TabsList>

                    <TabsContent value="patients" className="flex flex-col gap-3 pt-3">
                      <div className="flex flex-wrap items-end gap-2">
                        <div className="flex min-w-[220px] flex-1 items-center gap-2">
                          <Input
                            value={searchDraft}
                            onChange={(e) => setSearchDraft(e.target.value)}
                            onKeyDown={(e) => e.key === 'Enter' && applySearch()}
                            placeholder={canSeePatients ? 'Search by name, UHID or patient id' : 'Search by patient id'}
                            aria-label="Search patients"
                          />
                          <Button variant="outline" onClick={applySearch} aria-label="Search">
                            <Search className="h-4 w-4" />
                          </Button>
                        </div>
                        <Select
                          value={streamQuery.status ?? ALL}
                          onValueChange={(v) => setStreamQuery((q) => ({ ...q, page: 1, status: v === ALL ? undefined : (v as SyncStreamListQuery['status']) }))}
                        >
                          <SelectTrigger className="w-[200px]" aria-label="Filter by status">
                            <SelectValue />
                          </SelectTrigger>
                          <SelectContent>
                            {STATUS_FILTERS.map((f) => (
                              <SelectItem key={f.value} value={f.value}>
                                {f.label}
                              </SelectItem>
                            ))}
                          </SelectContent>
                        </Select>
                      </div>

                      {!canSeePatients && (
                        <p className="text-xs text-muted-foreground">Patient names are hidden: your role cannot view patient records.</p>
                      )}

                      {streams.isError && (
                        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
                          {errorMessage(streams.error, 'Could not load patients.')}
                        </p>
                      )}
                      {streams.data && streams.data.items.length === 0 && (
                        <p className="rounded-lg border border-dashed border-border py-10 text-center text-sm text-muted-foreground">
                          No patients match. {streamQuery.status === 'Unsynced' ? 'Everything saved here has reached the cloud.' : ''}
                        </p>
                      )}
                      {streams.data && streams.data.items.length > 0 && (
                        <>
                          <SyncPatientsTable
                            rows={streams.data.items}
                            canEdit={canEdit}
                            busy={busy}
                            onHistory={setHistoryPatientId}
                            onRetry={(eventId) => report(retryEvent.mutateAsync(eventId), 'Could not retry')}
                            onResolve={setResolveTarget}
                          />
                          <Pagination meta={streams.data.meta} onPageChange={(page) => setStreamQuery((q) => ({ ...q, page }))} />
                        </>
                      )}
                    </TabsContent>

                    <TabsContent value="failed" className="flex flex-col gap-3 pt-3">
                      {failedEvents.isError && (
                        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
                          {errorMessage(failedEvents.error, 'Could not load failed changes.')}
                        </p>
                      )}
                      {failedEvents.data && failedEvents.data.items.length === 0 && (
                        <p className="rounded-lg border border-dashed border-border py-10 text-center text-sm text-muted-foreground">No failed changes.</p>
                      )}
                      {failedEvents.data && failedEvents.data.items.length > 0 && (
                        <>
                          <SyncFailedEventsTable
                            rows={failedEvents.data.items}
                            canEdit={canEdit}
                            busy={busy}
                            onHistory={setHistoryPatientId}
                            onRetry={(eventId) => report(retryEvent.mutateAsync(eventId), 'Could not retry')}
                            onResolve={setResolveTarget}
                          />
                          <Pagination meta={failedEvents.data.meta} onPageChange={setEventsPage} />
                        </>
                      )}
                    </TabsContent>
                  </Tabs>
                </CardContent>
              </Card>
            </>
          )}
        </div>

        <SyncHistoryDrawer patientId={historyPatientId} onClose={() => setHistoryPatientId(null)} />
        <SyncResolveDialog target={resolveTarget} busy={resolveEvent.isPending} onCancel={() => setResolveTarget(null)} onConfirm={handleResolve} />
        <SyncConfirmDialog
          open={confirm === 'retryAll'}
          title="Retry all failed changes?"
          description="Every change that is retrying or needs attention goes back in the queue with a fresh retry count. Conflicts are not included: resolve those one by one."
          confirmLabel="Retry all"
          busy={retryAll.isPending}
          onCancel={() => setConfirm(null)}
          onConfirm={() => report(retryAll.mutateAsync(), 'Could not retry', () => setConfirm(null))}
        />
        <SyncConfirmDialog
          open={confirm === 'pause'}
          title="Pause cloud sync?"
          description="Nothing is sent to the cloud until you resume. Everyone can keep working: changes are saved here and queued."
          confirmLabel="Pause"
          busy={pause.isPending}
          onCancel={() => setConfirm(null)}
          onConfirm={() => report(pause.mutateAsync(), 'Could not pause sync', () => setConfirm(null))}
        />
      </div>
    </RequirePermission>
  );
}
