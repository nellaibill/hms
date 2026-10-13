import { useEffect, useRef, useState } from 'react';
import { useAuth } from '@/features/auth/AuthContext';
import { defaultReportDateRange } from '@/lib/reportDateRange';
import type { ProfitReportFilterState } from './profitReport';
import type { ReportDateRange } from './types';

const STORAGE_PREFIX = 'hms-report-filters';

/** How long typing (Search box, a date being keyed in) has to pause before the report recomputes
 * — same delay useDebouncedValue defaults to. A native date input reports a complete-but-wrong
 * value after every keystroke of the year ("0002-…", "0020-…"), so applying each one would
 * briefly recompute the report over the hospital's entire history. */
const TYPING_APPLY_DELAY_MS = 300;

export interface ReportFilterValues {
  range: ReportDateRange;
  filters: ProfitReportFilterState;
}

function defaultReportFilterValues(): ReportFilterValues {
  return { range: defaultReportDateRange(), filters: {} };
}

function isSameRange(a: ReportDateRange, b: ReportDateRange): boolean {
  return a.from === b.from && a.to === b.to;
}

function readStoredFilters(storageKey: string): ReportFilterValues | null {
  try {
    const raw = sessionStorage.getItem(storageKey);
    if (!raw) return null;
    const parsed = JSON.parse(raw) as { range?: Partial<ReportDateRange>; filters?: Record<string, unknown> };
    const range =
      typeof parsed.range?.from === 'string' && typeof parsed.range.to === 'string'
        ? { from: parsed.range.from, to: parsed.range.to }
        : defaultReportDateRange();
    const text = (key: keyof ProfitReportFilterState) => {
      const value = parsed.filters?.[key];
      return typeof value === 'string' && value ? value : undefined;
    };
    return {
      range,
      filters: {
        billingType: text('billingType') as ProfitReportFilterState['billingType'],
        departmentId: text('departmentId'),
        consultantId: text('consultantId'),
        search: text('search'),
      },
    };
  } catch {
    // Unreadable or blocked storage — fall back to the defaults, same as a first visit.
    return null;
  }
}

function writeStoredFilters(storageKey: string, values: ReportFilterValues) {
  try {
    // A range still at the default is left out rather than stored as two fixed dates, so a tab
    // kept open overnight comes back to the new day's default range, not yesterday's.
    const range = isSameRange(values.range, defaultReportDateRange()) ? undefined : values.range;
    sessionStorage.setItem(storageKey, JSON.stringify({ range, filters: values.filters }));
  } catch {
    // Best-effort only — worst case the filters just don't survive leaving the page.
  }
}

function clearStoredFilters(storageKey: string) {
  try {
    sessionStorage.removeItem(storageKey);
  } catch {
    // Best-effort only — see writeStoredFilters.
  }
}

/**
 * Filter state for one Finance report's ReportFilterBar — live, not a draft committed by a
 * Search click: `range`/`filters` are what the bar's inputs show, `appliedRange`/`appliedFilters`
 * are what the report computes from. A dropdown choice or Reset applies immediately; typing
 * applies once it pauses (see TYPING_APPLY_DELAY_MS), which is the only reason the two pairs
 * are ever briefly different.
 *
 * The inputs' values are also kept in sessionStorage under `reportKey`, so switching to another
 * Accounts & Finance tab and back (each tab is its own route, so the page unmounts) restores them
 * instead of silently returning to the defaults. Keyed per signed-in user as well: sessionStorage
 * outlives a logout, and one user's department/consultant ids or search text mean nothing — or
 * shouldn't be shown — to the next person signing in on the same browser tab. Reset clears the
 * stored copy along with the fields.
 *
 * `onApply` runs each time the applied values change — pages use it to return to page 1.
 */
export function useReportFilters(reportKey: string, onApply?: () => void) {
  const { user } = useAuth();
  const storageKey = `${STORAGE_PREFIX}:${user?.id ?? 'anonymous'}:${reportKey}`;

  const [values, setValues] = useState<ReportFilterValues>(() => readStoredFilters(storageKey) ?? defaultReportFilterValues());
  const [applied, setApplied] = useState(values);
  const pendingApply = useRef<ReturnType<typeof setTimeout> | undefined>(undefined);

  useEffect(() => () => clearTimeout(pendingApply.current), []);

  function commit(next: ReportFilterValues, delayMs: number) {
    setValues(next);
    clearTimeout(pendingApply.current);
    const apply = () => {
      setApplied(next);
      onApply?.();
    };
    if (delayMs > 0) pendingApply.current = setTimeout(apply, delayMs);
    else apply();
  }

  function setRange(range: ReportDateRange) {
    const next = { ...values, range };
    writeStoredFilters(storageKey, next);
    commit(next, TYPING_APPLY_DELAY_MS);
  }

  function setFilters(filters: ProfitReportFilterState) {
    const next = { ...values, filters };
    writeStoredFilters(storageKey, next);
    commit(next, filters.search === values.filters.search ? 0 : TYPING_APPLY_DELAY_MS);
  }

  function reset() {
    clearStoredFilters(storageKey);
    commit(defaultReportFilterValues(), 0);
  }

  return {
    range: values.range,
    filters: values.filters,
    appliedRange: applied.range,
    appliedFilters: applied.filters,
    setRange,
    setFilters,
    reset,
  };
}
