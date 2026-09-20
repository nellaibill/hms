import type { ActivityLogEntry } from '@hms/shared';

/** Module/Action values the backend writes today (HMS.Modules.ActivityLog.Contracts.
 * ActivityLogModules / ActivityLogActions) — the API has no "list distinct values" endpoint,
 * so the filter dropdowns mirror those contract constants. Add a value here when a new module
 * or action starts logging. */
export const ACTIVITY_MODULES = ['Patients', 'Identity', 'Billing'] as const;
export const ACTIVITY_ACTIONS = ['Create', 'Update', 'Delete', 'Payment', 'Void'] as const;

const ACTION_LABELS: Record<string, string> = {
  Create: 'Created',
  Update: 'Updated',
  Delete: 'Deleted',
  Payment: 'Payment',
  Void: 'Voided',
};

export function actionLabel(action: string): string {
  return ACTION_LABELS[action] ?? action;
}

export function formatDateTime(iso: string): string {
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) {
    return iso;
  }
  const day = date.toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' });
  const time = date.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit', hour12: true });
  return `${day}, ${time}`;
}

/** "Patient" + "abc12345-…" -> "Patient · abc12345" — the API only carries a GUID, so the
 * table shows a short prefix and the drawer shows the full id. */
export function entityLabel(entry: Pick<ActivityLogEntry, 'entityType' | 'entityId'>): string {
  if (!entry.entityType && !entry.entityId) {
    return '—';
  }
  const shortId = entry.entityId ? entry.entityId.slice(0, 8) : '';
  return [entry.entityType, shortId].filter(Boolean).join(' · ');
}

// Bookkeeping fields that change on every save and would drown out the real edits.
const NOISE_FIELDS = new Set(['rowVersion', 'updatedAt', 'updatedBy']);

export interface ChangeRow {
  field: string;
  before: string;
  after: string;
}

function humanize(path: string): string {
  return path
    .split('.')
    .map((part) => part.replace(/([a-z0-9])([A-Z])/g, '$1 $2').replace(/^./, (c) => c.toUpperCase()))
    .join(' › ');
}

function formatLeaf(value: unknown): string {
  if (value === null || value === undefined || value === '') {
    return '—';
  }
  if (typeof value === 'boolean') {
    return value ? 'Yes' : 'No';
  }
  if (Array.isArray(value)) {
    if (value.length === 0) {
      return '—';
    }
    return value.every((v) => v === null || typeof v !== 'object') ? value.map(String).join(', ') : JSON.stringify(value);
  }
  return String(value);
}

function flatten(value: unknown, prefix: string, out: Map<string, string>) {
  if (value !== null && typeof value === 'object' && !Array.isArray(value)) {
    for (const [key, child] of Object.entries(value as Record<string, unknown>)) {
      if (NOISE_FIELDS.has(key)) {
        continue;
      }
      flatten(child, prefix ? `${prefix}.${key}` : key, out);
    }
    return;
  }
  out.set(prefix, formatLeaf(value));
}

/**
 * Field-by-field before/after rows. When both snapshots exist only the fields that actually
 * changed are returned; for a create (no old values) or delete (no new values) every field of
 * the one snapshot is listed against an em-dash.
 */
export function buildChangeRows(oldValues: unknown, newValues: unknown): ChangeRow[] {
  const before = new Map<string, string>();
  const after = new Map<string, string>();
  if (oldValues !== null && oldValues !== undefined) {
    flatten(oldValues, '', before);
  }
  if (newValues !== null && newValues !== undefined) {
    flatten(newValues, '', after);
  }

  const hasBoth = before.size > 0 && after.size > 0;
  const fields = [...new Set([...before.keys(), ...after.keys()])].filter((f) => f !== '');

  return fields
    .map((field) => ({ field, before: before.get(field) ?? '—', after: after.get(field) ?? '—' }))
    .filter((row) => !hasBoth || row.before !== row.after)
    .map((row) => ({ ...row, field: humanize(row.field) }));
}

export interface ActivityLogFilterValues {
  from: string;
  to: string;
  userId: string;
  module: string;
  action: string;
  entity: string;
}

export const EMPTY_FILTERS: ActivityLogFilterValues = { from: '', to: '', userId: '', module: '', action: '', entity: '' };

export type ExportFormat = 'pdf' | 'excel' | 'csv';
