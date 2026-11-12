import type { SyncSummary } from '@hms/shared';
import { AlertTriangle, CheckCircle2, Clock, Cloud, CloudOff, Database, KeyRound, Laptop, type LucideIcon } from 'lucide-react';
import { useEffect, useState } from 'react';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent } from '@/components/ui/card';
import { cn } from '@/lib/utils';
import { formatDateTime, formatRelative, overallStatusView } from '../cloudSyncUtils';

interface StatusTileProps {
  icon: LucideIcon;
  label: string;
  value: string;
  detail?: string;
  tone: 'good' | 'warn' | 'bad' | 'neutral';
}

const TONE_CLASSES: Record<StatusTileProps['tone'], string> = {
  good: 'text-success',
  warn: 'text-warning',
  bad: 'text-destructive',
  neutral: 'text-muted-foreground',
};

function StatusTile({ icon: Icon, label, value, detail, tone }: StatusTileProps) {
  return (
    <Card>
      <CardContent className="flex items-start gap-3 p-4">
        <Icon className={cn('mt-0.5 h-5 w-5 shrink-0', TONE_CLASSES[tone])} aria-hidden />
        <div className="min-w-0">
          <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">{label}</p>
          <p className="font-semibold text-foreground">{value}</p>
          {detail && <p className="truncate text-xs text-muted-foreground" title={detail}>{detail}</p>}
        </div>
      </CardContent>
    </Card>
  );
}

function CountTile({ label, value, tone }: { label: string; value: number; tone: StatusTileProps['tone'] }) {
  return (
    <div className="flex flex-col rounded-md border border-border px-3 py-2">
      <span className="text-xs text-muted-foreground">{label}</span>
      <span className={cn('text-xl font-semibold', value > 0 ? TONE_CLASSES[tone] : 'text-foreground')}>{value.toLocaleString()}</span>
    </div>
  );
}

/** Tracks this browser's own network connection (separate from the server's cloud link). */
function useBrowserOnline(): boolean {
  const [online, setOnline] = useState(() => (typeof navigator === 'undefined' ? true : navigator.onLine));
  useEffect(() => {
    const update = () => setOnline(navigator.onLine);
    window.addEventListener('online', update);
    window.addEventListener('offline', update);
    return () => {
      window.removeEventListener('online', update);
      window.removeEventListener('offline', update);
    };
  }, []);
  return online;
}

export function SyncStatusCards({ summary }: { summary: SyncSummary }) {
  const browserOnline = useBrowserOnline();
  const overall = overallStatusView(summary);

  const cloud: Omit<StatusTileProps, 'label' | 'icon'> & { icon: LucideIcon } = summary.authFailed
    ? { icon: KeyRound, value: 'Key rejected', tone: 'bad', detail: summary.lastProbeError ?? undefined }
    : summary.cloudReachable === true
      ? {
          icon: Cloud,
          value: 'Connected',
          tone: 'good',
          detail: `Checked ${formatRelative(summary.lastProbeAt)}${summary.lastProbeLatencyMs != null ? ` · ${summary.lastProbeLatencyMs} ms` : ''}`,
        }
      : summary.cloudReachable === false
        ? { icon: CloudOff, value: 'Unreachable', tone: 'warn', detail: summary.lastProbeError ?? `Checked ${formatRelative(summary.lastProbeAt)}` }
        : { icon: Cloud, value: 'Not checked yet', tone: 'neutral' };

  return (
    <div className="flex flex-col gap-3">
      <Card>
        <CardContent className="flex flex-wrap items-center gap-3 p-4">
          {summary.overallStatus === 'Healthy' ? (
            <CheckCircle2 className="h-6 w-6 text-success" aria-hidden />
          ) : (
            <AlertTriangle className={cn('h-6 w-6', overall.tone === 'destructive' ? 'text-destructive' : 'text-warning')} aria-hidden />
          )}
          <div className="flex min-w-0 flex-1 flex-col">
            <div className="flex items-center gap-2">
              <span className="font-semibold text-foreground">Cloud sync</span>
              <Badge variant={overall.tone}>{overall.label}</Badge>
            </div>
            <span className="text-sm text-muted-foreground">{overall.description}</span>
          </div>
          {summary.oldestPendingAt && (
            <span className="text-sm text-muted-foreground">Oldest waiting change: {formatRelative(summary.oldestPendingAt)}</span>
          )}
        </CardContent>
      </Card>

      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 xl:grid-cols-4">
        <StatusTile icon={cloud.icon} label="Server ↔ cloud" value={cloud.value} detail={cloud.detail} tone={cloud.tone} />
        <StatusTile
          icon={Laptop}
          label="This computer's network"
          value={browserOnline ? 'Online' : 'Offline'}
          detail={browserOnline ? undefined : 'You can keep working: this page talks to the local server.'}
          tone={browserOnline ? 'good' : 'warn'}
        />
        <StatusTile
          icon={Database}
          label="Local database"
          value={summary.localDatabaseAvailable ? 'Available' : 'Not responding'}
          tone={summary.localDatabaseAvailable ? 'good' : 'bad'}
        />
        <StatusTile
          icon={Clock}
          label="Last successful sync"
          value={formatRelative(summary.lastSuccessfulSyncAt)}
          detail={summary.lastSuccessfulSyncAt ? formatDateTime(summary.lastSuccessfulSyncAt) : `Runs every ${summary.intervalInMinutes} min`}
          tone={summary.lastSuccessfulSyncAt ? 'neutral' : 'warn'}
        />
      </div>

      <Card>
        <CardContent className="grid grid-cols-2 gap-2 p-4 sm:grid-cols-3 lg:grid-cols-6">
          <CountTile label="Waiting" value={summary.pendingEvents} tone="neutral" />
          <CountTile label="Sending" value={summary.processingEvents} tone="neutral" />
          <CountTile label="Retrying" value={summary.failedEvents} tone="warn" />
          <CountTile label="Needs attention" value={summary.needsAttentionEvents} tone="bad" />
          <CountTile label="Conflicts" value={summary.conflictEvents} tone="bad" />
          <CountTile label="Synced (24 h)" value={summary.syncedLast24Hours} tone="good" />
        </CardContent>
      </Card>
    </div>
  );
}
