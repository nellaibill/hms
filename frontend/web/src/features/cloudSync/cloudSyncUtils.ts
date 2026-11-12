import type { SyncEventStatus, SyncOverallStatus, SyncSummary } from '@hms/shared';

export type BadgeTone = 'default' | 'secondary' | 'destructive' | 'success' | 'warning' | 'outline';

/** Page-level permissions: reading needs .view, every action .edit (mirrors SyncMonitorController). */
export const CLOUD_SYNC_VIEW_PERMISSION = 'identity-administration.view';
export const CLOUD_SYNC_EDIT_PERMISSION = 'identity-administration.edit';

/** The page refreshes itself this often (ms). */
export const CLOUD_SYNC_REFRESH_MS = 15_000;

export const EVENT_STATUS_LABELS: Record<SyncEventStatus, string> = {
  Pending: 'Waiting',
  Processing: 'Sending',
  Synced: 'Synced',
  Failed: 'Retrying',
  NeedsAttention: 'Needs attention',
  Conflict: 'Conflict',
};

export const EVENT_STATUS_TONES: Record<SyncEventStatus, BadgeTone> = {
  Pending: 'secondary',
  Processing: 'default',
  Synced: 'success',
  Failed: 'warning',
  NeedsAttention: 'destructive',
  Conflict: 'destructive',
};

export const OPERATION_LABELS: Record<string, string> = {
  Insert: 'Registered',
  Update: 'Edited',
  Delete: 'Deleted',
  Snapshot: 'Full copy',
};

export const RESOLUTION_LABELS: Record<string, string> = {
  LocalWins: 'Sent local version',
  CloudWins: 'Kept cloud version',
  Skip: 'Skipped',
};

interface OverallStatusView {
  label: string;
  tone: BadgeTone;
  description: string;
}

export function overallStatusView(summary: SyncSummary): OverallStatusView {
  const views: Record<SyncOverallStatus, OverallStatusView> = {
    NotEnabled: {
      label: 'Not enabled',
      tone: 'outline',
      description:
        summary.role === 'Cloud'
          ? 'This server is the cloud. Local hospital servers send their changes here.'
          : 'Cloud sync is not switched on for this server.',
    },
    NotConfigured: {
      label: 'Not configured',
      tone: 'warning',
      description: 'No cloud address or key is set up for this hospital on this server yet. Every patient change is saved and queued here, and goes out once they are.',
    },
    Disabled: {
      label: 'Sending off',
      tone: 'warning',
      description: 'Sending is switched off in the server settings. Changes are kept here until it is switched on.',
    },
    Paused: {
      label: 'Paused',
      tone: 'warning',
      description: 'An administrator paused sync. Changes keep being saved here and go out after resuming.',
    },
    AuthFailed: {
      label: 'Key rejected',
      tone: 'destructive',
      description: "The cloud refused this server's key. Ask IT to check the cloud key. Sending resumes on its own once it is fixed.",
    },
    Offline: {
      label: 'Offline',
      tone: 'warning',
      description: 'The cloud cannot be reached. Everyone can keep working; changes are queued and sent when the connection returns.',
    },
    Attention: {
      label: 'Needs attention',
      tone: 'destructive',
      description: 'Some patients are held back until an administrator retries, skips or resolves them. Others keep syncing.',
    },
    Syncing: {
      label: 'Syncing',
      tone: 'default',
      description: 'Changes are waiting or being sent to the cloud.',
    },
    Healthy: {
      label: 'Up to date',
      tone: 'success',
      description: 'Every change saved here has reached the cloud.',
    },
  };

  return views[summary.overallStatus] ?? views.Syncing;
}

export function formatDateTime(iso?: string | null): string {
  if (!iso) {
    return '—';
  }
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) {
    return iso;
  }
  const day = date.toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' });
  const time = date.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: true });
  return `${day}, ${time}`;
}

/** "just now", "5 min ago", "3 h ago", "2 days ago"; future times as "in 4 min". */
export function formatRelative(iso?: string | null, now: number = Date.now()): string {
  if (!iso) {
    return '—';
  }
  const time = new Date(iso).getTime();
  if (Number.isNaN(time)) {
    return '—';
  }
  const seconds = Math.round((time - now) / 1000);
  const abs = Math.abs(seconds);
  const unit = abs < 45 ? null : abs < 3600 ? [Math.round(abs / 60), 'min'] : abs < 86_400 ? [Math.round(abs / 3600), 'h'] : [Math.round(abs / 86_400), abs < 172_800 ? 'day' : 'days'];
  if (!unit) {
    return seconds > 0 ? 'in a moment' : 'just now';
  }
  return seconds > 0 ? `in ${unit[0]} ${unit[1]}` : `${unit[0]} ${unit[1]} ago`;
}

/** Shows a patient by name/UHID when the API returned them, else by a short id. */
export function patientLabel(row: { patientId: string; patientName?: string | null; uhid?: string | null }): string {
  if (row.patientName) {
    return row.uhid ? `${row.patientName} (${row.uhid})` : row.patientName;
  }
  return `Patient ${row.patientId.slice(0, 8)}…`;
}
