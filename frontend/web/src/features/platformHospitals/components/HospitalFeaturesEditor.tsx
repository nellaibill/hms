import { useEffect, useMemo, useRef, useState, type ReactNode } from 'react';
import { ApiError } from '@hms/shared';
import {
  Check,
  ChevronDown,
  ChevronRight,
  Info,
  LayoutGrid,
  Link2,
  ListChecks,
  Loader2,
  Lock,
  Save,
  Search,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { useToast } from '@/components/ui/toast-context';
import { UnsavedChangesDialog } from '@/components/UnsavedChangesDialog';
import { PLATFORM_CONTAINER } from '@/features/platformAuth/platformLayout';
import { useUnsavedChangesGuard } from '@/hooks/useUnsavedChangesGuard';
import { cn } from '@/lib/utils';
import {
  FEATURE_CATEGORIES,
  FEATURE_META,
  SUB_FEATURE_META,
  childrenOf,
  descendantsOf,
  featureCategoryOf,
  featureLabel,
  groupByCategory,
  groupChildren,
  recommendedFeatures,
  withFeatureToggled,
} from '../featureCatalog';
import { useHospitalFeaturesQuery } from '../hooks/useHospitalFeaturesQuery';
import { useUpdateHospitalFeaturesMutation } from '../hooks/useUpdateHospitalFeaturesMutation';

interface HospitalFeaturesEditorProps {
  hospitalId: string;
  /** Used in the save toast and the leave-without-saving prompt; undefined when the page was
   * opened from a bare link and the hospital list isn't cached. */
  hospitalName?: string;
}

const checkboxClassName =
  'h-4 w-4 shrink-0 rounded border-input accent-primary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-60';

function sameSet(a: Set<string>, b: Set<string>) {
  return a.size === b.size && [...a].every((key) => b.has(key));
}

function IndeterminateCheckbox({
  checked,
  indeterminate,
  onChange,
  label,
}: {
  checked: boolean;
  indeterminate: boolean;
  onChange: () => void;
  label: string;
}) {
  const ref = useRef<HTMLInputElement>(null);
  useEffect(() => {
    if (ref.current) ref.current.indeterminate = indeterminate;
  }, [indeterminate]);

  return (
    <label className="flex cursor-pointer items-center gap-2 text-xs font-medium text-muted-foreground hover:text-foreground">
      <input
        ref={ref}
        type="checkbox"
        checked={checked}
        onChange={onChange}
        className={checkboxClassName}
      />
      {label}
    </label>
  );
}

/** What Save will do to a feature, so a pending change is visible on its card before saving. */
type PendingChange = 'on' | 'off' | null;

function PendingBadge({ pending }: { pending: PendingChange }) {
  if (!pending) return null;
  return (
    <span
      className={cn(
        'rounded-full px-1.5 py-px text-[10px] font-semibold uppercase tracking-wide',
        pending === 'on' ? 'bg-success/15 text-success' : 'bg-destructive/10 text-destructive',
      )}
    >
      {pending === 'on' ? 'Turning on' : 'Turning off'}
    </span>
  );
}

/** A module's switchable parts (ADR-145) — shown under its card, collapsed to a count. */
interface FeatureParts {
  total: number;
  on: number;
  expanded: boolean;
  onToggleExpanded: () => void;
  content: ReactNode;
}

/** A module card. One with parts (e.g. HR, Settings) carries a "Parts" disclosure under the
 * module's own switch and, once opened, spans the whole row so its checklist has room. */
function FeatureCard({
  featureKey,
  checked,
  mandatory,
  requires,
  pending,
  onToggle,
  parts,
}: {
  featureKey: string;
  checked: boolean;
  mandatory: boolean;
  requires: string[];
  pending: PendingChange;
  onToggle: () => void;
  parts?: FeatureParts;
}) {
  if (parts) {
    const Chevron = parts.expanded ? ChevronDown : ChevronRight;
    return (
      <div
        className={cn(
          'flex flex-col rounded-lg border transition-colors',
          parts.expanded && 'sm:col-span-2 2xl:col-span-3',
          mandatory
            ? 'border-dashed border-border bg-muted/30'
            : checked
              ? 'border-primary/60 bg-primary/[0.04] shadow-sm'
              : 'border-border bg-card',
        )}
      >
        <FeatureCardBody
          featureKey={featureKey}
          checked={checked}
          mandatory={mandatory}
          requires={requires}
          pending={pending}
          onToggle={onToggle}
          bare
        />
        <button
          type="button"
          onClick={parts.onToggleExpanded}
          aria-expanded={parts.expanded}
          className="flex items-center gap-2 border-t border-border/70 px-3 py-2 text-left text-xs font-medium text-muted-foreground transition-colors hover:bg-accent/40 hover:text-foreground"
        >
          <ListChecks className="h-3.5 w-3.5 shrink-0" />
          <span className="flex-1">
            Parts · {parts.on} of {parts.total} on
          </span>
          <span className="text-primary">{parts.expanded ? 'Hide' : 'Choose parts'}</span>
          <Chevron className="h-3.5 w-3.5" />
        </button>
        {parts.expanded && <div className="border-t border-border/70 p-3">{parts.content}</div>}
      </div>
    );
  }

  return <FeatureCardBody featureKey={featureKey} checked={checked} mandatory={mandatory} requires={requires} pending={pending} onToggle={onToggle} />;
}

function FeatureCardBody({
  featureKey,
  checked,
  mandatory,
  requires,
  pending,
  onToggle,
  bare = false,
}: {
  featureKey: string;
  checked: boolean;
  mandatory: boolean;
  requires: string[];
  pending: PendingChange;
  onToggle: () => void;
  /** Inside a card with parts: no border/background of its own (the outer card has them). */
  bare?: boolean;
}) {
  const meta = FEATURE_META[featureKey];
  const category = FEATURE_CATEGORIES.find((c) => c.id === featureCategoryOf(featureKey))!;
  const Icon = meta?.icon ?? category.icon;

  const body = (
    <>
      <span
        className={cn(
          'flex h-9 w-9 shrink-0 items-center justify-center rounded-lg',
          category.tint,
        )}
      >
        <Icon className="h-4 w-4" />
      </span>
      <span className="flex min-w-0 flex-1 flex-col gap-0.5">
        <span className="flex flex-wrap items-center gap-x-2 gap-y-1 text-sm font-medium leading-tight text-foreground">
          {featureLabel(featureKey)}
          {mandatory && (
            <span
              className="inline-flex items-center gap-1 rounded-full bg-muted px-1.5 py-px text-[10px] font-medium text-muted-foreground"
              title="Required for every hospital"
            >
              <Lock className="h-2.5 w-2.5" />
              Always on
            </span>
          )}
          <PendingBadge pending={pending} />
        </span>
        {meta && (
          <span className="text-xs leading-snug text-muted-foreground">{meta.description}</span>
        )}
        {requires.length > 0 && (
          <span className="mt-0.5 flex items-center gap-1 text-[11px] text-muted-foreground">
            <Link2 className="h-3 w-3 shrink-0" />
            Needs {requires.map(featureLabel).join(', ')}
          </span>
        )}
      </span>
    </>
  );

  // A required feature can't be switched off, so it gets a lock and no checkbox. A disabled
  // checkbox read as broken rather than "always on".
  if (mandatory) {
    return (
      <div
        className={cn(
          'flex items-start gap-3 p-3',
          !bare && 'h-full rounded-lg border border-dashed border-border bg-muted/30',
        )}
      >
        {body}
      </div>
    );
  }

  return (
    <label
      className={cn(
        'flex cursor-pointer items-start gap-3 p-3 transition-colors',
        bare
          ? 'rounded-t-lg hover:bg-accent/40'
          : cn(
              'h-full rounded-lg border',
              checked
                ? 'border-primary/60 bg-primary/[0.04] shadow-sm'
                : 'border-border bg-card hover:border-primary/40 hover:bg-accent/40',
            ),
      )}
    >
      {body}
      <input
        type="checkbox"
        checked={checked}
        onChange={onToggle}
        className={cn(checkboxClassName, 'mt-0.5')}
        aria-label={featureLabel(featureKey)}
      />
    </label>
  );
}

/** One part (sub-feature) of a module in the "Parts" checklist. */
function PartRow({
  partKey,
  checked,
  disabled,
  requires,
  pending,
  onToggle,
}: {
  partKey: string;
  checked: boolean;
  /** Its module (or parent part) is off: shown with its own choice, but not changeable. */
  disabled: boolean;
  requires: string[];
  pending: PendingChange;
  onToggle: () => void;
}) {
  const meta = SUB_FEATURE_META[partKey];
  return (
    <label
      className={cn(
        'flex items-start gap-2.5 rounded-md border px-2.5 py-2 transition-colors',
        disabled
          ? 'cursor-not-allowed border-border/60 bg-muted/20 opacity-60'
          : checked
            ? 'cursor-pointer border-primary/40 bg-primary/[0.03] hover:bg-accent/40'
            : 'cursor-pointer border-border bg-card hover:border-primary/40 hover:bg-accent/40',
      )}
    >
      <input
        type="checkbox"
        checked={checked}
        disabled={disabled}
        onChange={onToggle}
        className={cn(checkboxClassName, 'mt-0.5')}
        aria-label={featureLabel(partKey)}
      />
      <span className="flex min-w-0 flex-1 flex-col gap-0.5">
        <span className="flex flex-wrap items-center gap-x-2 gap-y-1 text-[13px] font-medium leading-tight text-foreground">
          {featureLabel(partKey)}
          <PendingBadge pending={pending} />
        </span>
        {meta && <span className="text-[11px] leading-snug text-muted-foreground">{meta.description}</span>}
        {requires.length > 0 && (
          <span className="flex items-center gap-1 text-[11px] text-muted-foreground">
            <Link2 className="h-3 w-3 shrink-0" />
            Needs {requires.map(featureLabel).join(', ')}
          </span>
        )}
      </span>
    </label>
  );
}

/**
 * The body of the hospital Features page: a sticky left column (totals, search, categories,
 * quick select) beside the feature cards, and a save bar pinned to the bottom of the screen.
 * Leaving with unsaved changes asks first.
 */
export function HospitalFeaturesEditor({ hospitalId, hospitalName }: HospitalFeaturesEditorProps) {
  const featuresQuery = useHospitalFeaturesQuery(hospitalId);
  const updateMutation = useUpdateHospitalFeaturesMutation();
  const { toast } = useToast();

  // Modules and sub-features (ADR-145) in one set, so the dependency rules can run over both;
  // split apart again on save.
  const [enabledFeatures, setEnabledFeatures] = useState<Set<string>>(new Set());
  const [search, setSearch] = useState('');
  const [activeCategory, setActiveCategory] = useState<string>('all');
  const [collapsed, setCollapsed] = useState<Set<string>>(new Set());
  const [expandedParts, setExpandedParts] = useState<Set<string>>(new Set());

  const data = featuresQuery.data;
  const mandatory = useMemo(() => data?.mandatoryFeatures ?? [], [data]);
  const subFeatures = useMemo(() => data?.subFeatures ?? [], [data]);
  const subFeatureKeys = useMemo(() => subFeatures.map((sub) => sub.key), [subFeatures]);
  const dependencies = useMemo(
    () => ({ ...(data?.dependencies ?? {}), ...(data?.subFeatureDependencies ?? {}) }),
    [data],
  );

  // The tenant's own enabled set plus every mandatory key: a tenant provisioned before a key
  // became Mandatory has no row for it, but it is shown as required-and-on, so the submitted
  // payload must contain it or the backend's "mandatory features cannot be disabled" check rejects the save.
  // Sub-features are each part's own choice, kept even while its module is off.
  const initialFeatures = useMemo(
    () => new Set([...(data?.enabledFeatures ?? []), ...mandatory, ...(data?.enabledSubFeatures ?? [])]),
    [data, mandatory],
  );

  useEffect(() => {
    if (data) setEnabledFeatures(new Set(initialFeatures));
  }, [data, initialFeatures]);

  const turningOn = [...enabledFeatures].filter((key) => !initialFeatures.has(key));
  const turningOff = [...initialFeatures].filter((key) => !enabledFeatures.has(key));
  const changeCount = turningOn.length + turningOff.length;
  const isDirty = data !== undefined && changeCount > 0;
  const pendingOf = (key: string): PendingChange =>
    turningOn.includes(key) ? 'on' : turningOff.includes(key) ? 'off' : null;

  const guard = useUnsavedChangesGuard(isDirty);

  const term = search.trim().toLowerCase();
  const matchesSearch = (key: string) =>
    `${key} ${featureLabel(key)} ${FEATURE_META[key]?.description ?? SUB_FEATURE_META[key]?.description ?? ''}`
      .toLowerCase()
      .includes(term);
  /** A module whose own name doesn't match but one of its parts does still shows — opened, so
   * the matching part is in view. */
  const partMatchesSearch = (key: string) =>
    term !== '' && descendantsOf(key, subFeatures).some(matchesSearch);

  const visibleKeys = (data?.allFeatures ?? []).filter(
    (key) => !term || matchesSearch(key) || partMatchesSearch(key),
  );

  // Searching for a part opens its module's parts, so the match is in view; the operator can
  // still close it again.
  const partSearchMatches = visibleKeys.filter(partMatchesSearch).join('|');
  useEffect(() => {
    if (!partSearchMatches) return;
    setExpandedParts((current) => new Set([...current, ...partSearchMatches.split('|')]));
  }, [partSearchMatches]);

  const groups = groupByCategory(visibleKeys);
  const shownGroups =
    activeCategory === 'all'
      ? groups
      : groups.filter((group) => group.category.id === activeCategory);
  const enabledIn = (keys: string[]) => keys.filter((key) => enabledFeatures.has(key)).length;

  function setFeature(key: string, on: boolean) {
    setEnabledFeatures((current) => withFeatureToggled(current, key, on, dependencies, mandatory));
  }

  function setMany(keys: string[], on: boolean) {
    setEnabledFeatures((current) =>
      keys.reduce(
        (next, key) => withFeatureToggled(next, key, on, dependencies, mandatory),
        current,
      ),
    );
  }

  function toggleCollapsed(id: string) {
    setCollapsed((current) => {
      const next = new Set(current);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  }

  function toggleExpandedParts(key: string) {
    setExpandedParts((current) => {
      const next = new Set(current);
      if (next.has(key)) next.delete(key);
      else next.add(key);
      return next;
    });
  }

  /** The checklist of a module's (or a part's) own parts, grouped under their headings. A
   * part whose parent is off can't be changed, but keeps — and shows — its own choice. */
  function renderParts(parent: string, parentOn: boolean): ReactNode {
    return (
      <div className="flex flex-col gap-3">
        {groupChildren(childrenOf(parent, subFeatures)).map(({ group, keys }) => (
          <div key={group || 'parts'} className="flex flex-col gap-2">
            {group && (
              <h4 className="text-[11px] font-semibold uppercase tracking-wide text-muted-foreground">
                {group}
              </h4>
            )}
            <div className="grid grid-cols-1 gap-2 sm:grid-cols-2 xl:grid-cols-3">
              {keys.map((key) => {
                const hasChildren = childrenOf(key, subFeatures).length > 0;
                return (
                  <div
                    key={key}
                    className={cn('flex flex-col gap-2', hasChildren && 'sm:col-span-2 xl:col-span-3')}
                  >
                    <PartRow
                      partKey={key}
                      checked={enabledFeatures.has(key)}
                      disabled={!parentOn}
                      requires={dependencies[key] ?? []}
                      pending={pendingOf(key)}
                      onToggle={() => setFeature(key, !enabledFeatures.has(key))}
                    />
                    {hasChildren && (
                      <div className="ml-4 border-l-2 border-border pl-3">
                        {renderParts(key, parentOn && enabledFeatures.has(key))}
                      </div>
                    )}
                  </div>
                );
              })}
            </div>
          </div>
        ))}
      </div>
    );
  }

  function partsOf(key: string): FeatureParts | undefined {
    const descendants = descendantsOf(key, subFeatures);
    if (descendants.length === 0) return undefined;
    const moduleOn = enabledFeatures.has(key);
    return {
      total: descendants.length,
      on: descendants.filter((part) => enabledFeatures.has(part)).length,
      expanded: expandedParts.has(key),
      onToggleExpanded: () => toggleExpandedParts(key),
      content: (
        <div className="flex flex-col gap-3">
          {!moduleOn && (
            <p className="flex items-center gap-2 rounded-md bg-muted px-3 py-2 text-xs text-muted-foreground">
              <Info className="h-3.5 w-3.5 shrink-0" />
              Turn on {featureLabel(key)} to use these parts. The choices below are kept.
            </p>
          )}
          {renderParts(key, moduleOn)}
          {moduleOn && (
            <div className="flex flex-wrap gap-2 border-t border-border/70 pt-3">
              <Button type="button" variant="outline" size="sm" onClick={() => setMany(descendants, true)}>
                All parts on
              </Button>
              <Button type="button" variant="ghost" size="sm" onClick={() => setMany(descendants, false)}>
                All parts off
              </Button>
            </div>
          )}
        </div>
      ),
    };
  }

  function handleSave() {
    const summary = [
      turningOn.length > 0 && `${turningOn.length} turned on`,
      turningOff.length > 0 && `${turningOff.length} turned off`,
    ]
      .filter(Boolean)
      .join(', ');
    const moduleKeys = new Set(data?.allFeatures ?? []);
    const partKeys = new Set(subFeatureKeys);
    const request = {
      enabledFeatures: [...enabledFeatures].filter((key) => moduleKeys.has(key)),
      enabledSubFeatures: [...enabledFeatures].filter((key) => partKeys.has(key)),
    };
    updateMutation.mutate(
      { id: hospitalId, request },
      {
        onSuccess: () =>
          toast({
            title: 'Features saved',
            description: `${hospitalName ?? 'This hospital'}: ${summary}.`,
            variant: 'success',
          }),
      },
    );
  }

  const saveError =
    updateMutation.error instanceof ApiError
      ? updateMutation.error.message
      : updateMutation.error
        ? 'The features could not be saved. Check your connection and try again.'
        : null;

  // Totals count modules; parts switched off are noted separately below them.
  const total = data?.allFeatures.length ?? 0;
  const modulesOn = (data?.allFeatures ?? []).filter((key) => enabledFeatures.has(key)).length;
  const optionalTotal = total - mandatory.length;
  const optionalOn = modulesOn - mandatory.length;
  const onShare = total > 0 ? Math.round((modulesOn / total) * 100) : 0;
  const partsOff = subFeatureKeys.filter((key) => !enabledFeatures.has(key)).length;

  const railItems = [
    { id: 'all', label: 'All features', icon: LayoutGrid, keys: visibleKeys },
    ...groups.map((g) => ({
      id: g.category.id,
      label: g.category.label,
      icon: g.category.icon,
      keys: g.keys,
    })),
  ];

  // Each preset picks modules and leaves every part on — a part only matters while its module is on.
  const quickSelect = data
    ? [
        { label: 'All', hint: 'Every feature on', set: new Set([...data.allFeatures, ...subFeatureKeys]) },
        {
          label: 'Recommended',
          hint: 'A sensible starting set',
          set: new Set([...recommendedFeatures(data.allFeatures, mandatory), ...subFeatureKeys]),
        },
        { label: 'Required only', hint: 'Just the always-on core', set: new Set([...mandatory, ...subFeatureKeys]) },
      ]
    : [];

  return (
    <div className="flex flex-1 flex-col">
      <div className={cn(PLATFORM_CONTAINER, 'flex-1 py-6')}>
        {featuresQuery.isPending && (
          <Card className="flex items-center justify-center gap-2 py-20 text-sm text-muted-foreground shadow-sm">
            <Loader2 className="h-4 w-4 animate-spin" />
            Loading features…
          </Card>
        )}

        {featuresQuery.isError && (
          <Card className="flex flex-col items-center gap-3 py-16 text-center shadow-sm">
            <p role="alert" className="text-sm text-destructive">
              This hospital&rsquo;s features could not be loaded.
            </p>
            <Button variant="outline" size="sm" onClick={() => featuresQuery.refetch()}>
              Try again
            </Button>
          </Card>
        )}

        {data && (
          <div className="grid grid-cols-[minmax(0,1fr)] items-start gap-6 lg:grid-cols-[18rem_minmax(0,1fr)]">
            {/* minmax(0,…) on both layouts: the category strip scrolls sideways on a phone, and
                an auto column would grow to its full width and push the page off screen. */}
            <aside className="flex min-w-0 flex-col gap-3 lg:sticky lg:top-20 lg:max-h-[calc(100vh-10rem)] lg:overflow-y-auto">
              <Card className="flex flex-col divide-y divide-border shadow-sm">
                <div className="flex flex-col gap-2.5 p-4">
                  <div className="flex items-baseline justify-between gap-2">
                    <span className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
                      Features on
                    </span>
                    <span className="text-2xl font-semibold leading-none tabular-nums text-foreground">
                      {modulesOn}
                      <span className="text-sm font-normal text-muted-foreground"> / {total}</span>
                    </span>
                  </div>
                  <div className="h-1.5 w-full overflow-hidden rounded-full bg-muted">
                    <div
                      className="h-full rounded-full bg-primary transition-all"
                      style={{ width: `${onShare}%` }}
                    />
                  </div>
                  <span className="text-xs text-muted-foreground">
                    {mandatory.length} always on · {optionalOn} of {optionalTotal} optional
                    {partsOff > 0 && (
                      <>
                        {' '}
                        · {partsOff} part{partsOff === 1 ? '' : 's'} off
                      </>
                    )}
                  </span>
                </div>

                <div className="flex flex-col gap-2 p-3">
                  <div className="relative">
                    <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                    <Input
                      value={search}
                      onChange={(e) => setSearch(e.target.value)}
                      placeholder="Search features…"
                      aria-label="Search features"
                      className="h-9 pl-9"
                    />
                  </div>
                  <nav aria-label="Feature categories">
                    <ul className="flex gap-1 overflow-x-auto lg:flex-col lg:gap-0.5 lg:overflow-visible">
                      {railItems.map((item) => {
                        const active = activeCategory === item.id;
                        return (
                          <li key={item.id}>
                            <button
                              type="button"
                              onClick={() => setActiveCategory(item.id)}
                              aria-current={active}
                              className={cn(
                                'flex w-full items-center gap-2.5 whitespace-nowrap rounded-md px-3 py-1.5 text-left text-sm transition-colors',
                                active
                                  ? 'bg-primary font-medium text-primary-foreground shadow-sm'
                                  : 'text-foreground hover:bg-accent',
                              )}
                            >
                              <item.icon className="h-4 w-4 shrink-0" />
                              <span className="flex-1">{item.label}</span>
                              <span
                                className={cn(
                                  'text-xs tabular-nums',
                                  active ? 'text-primary-foreground/80' : 'text-muted-foreground',
                                )}
                              >
                                {enabledIn(item.keys)}/{item.keys.length}
                              </span>
                            </button>
                          </li>
                        );
                      })}
                    </ul>
                  </nav>
                </div>

                <div className="flex flex-col gap-2 p-3">
                  <span className="px-1 text-xs font-medium uppercase tracking-wide text-muted-foreground">
                    Quick select
                  </span>
                  <div className="grid grid-cols-3 gap-1.5 lg:grid-cols-1">
                    {quickSelect.map((option) => {
                      const current = sameSet(enabledFeatures, option.set);
                      return (
                        <button
                          key={option.label}
                          type="button"
                          title={option.hint}
                          onClick={() => setEnabledFeatures(new Set(option.set))}
                          aria-pressed={current}
                          className={cn(
                            'flex items-center justify-center gap-2 rounded-md border px-2 py-1.5 text-xs font-medium transition-colors lg:justify-between lg:px-3',
                            current
                              ? 'border-primary bg-primary/10 text-primary'
                              : 'border-border text-foreground hover:border-primary/40 hover:bg-accent/40',
                          )}
                        >
                          {option.label}
                          <span className="hidden font-normal text-muted-foreground lg:inline">
                            {option.hint}
                          </span>
                        </button>
                      );
                    })}
                  </div>
                </div>
              </Card>

              <p className="flex gap-2 px-1 text-xs leading-relaxed text-muted-foreground">
                <Info className="mt-0.5 h-3.5 w-3.5 shrink-0" />
                Turning a feature on sets up its data straight away. Turning a feature — or one of
                its parts — off only hides it; nothing is deleted. Only this hospital is affected,
                and its signed-in users see the change within a minute.
              </p>
            </aside>

            <div className="flex min-w-0 flex-col gap-4">
              {shownGroups.length === 0 && (
                <Card className="border-dashed py-16 text-center text-sm text-muted-foreground shadow-none">
                  No features match &ldquo;{search}&rdquo;.
                </Card>
              )}
              {shownGroups.map(({ category, keys }) => {
                const optionalKeys = keys.filter((key) => !mandatory.includes(key));
                const enabledOptional = enabledIn(optionalKeys);
                const isCollapsed = collapsed.has(category.id);
                const Chevron = isCollapsed ? ChevronRight : ChevronDown;

                return (
                  <Card key={category.id} className="overflow-hidden shadow-sm">
                    <div
                      className={cn(
                        'flex items-center justify-between gap-2 px-4 py-3',
                        !isCollapsed && 'border-b border-border',
                      )}
                    >
                      <button
                        type="button"
                        onClick={() => toggleCollapsed(category.id)}
                        aria-expanded={!isCollapsed}
                        className="flex items-center gap-2.5 text-sm font-semibold text-foreground"
                      >
                        <Chevron className="h-4 w-4 text-muted-foreground" />
                        <span
                          className={cn(
                            'flex h-7 w-7 items-center justify-center rounded-md',
                            category.tint,
                          )}
                        >
                          <category.icon className="h-4 w-4" />
                        </span>
                        {category.label}
                        <span className="rounded-full bg-muted px-2 py-0.5 text-xs font-medium tabular-nums text-muted-foreground">
                          {enabledIn(keys)} of {keys.length} on
                        </span>
                      </button>
                      {optionalKeys.length > 0 ? (
                        <IndeterminateCheckbox
                          label="Select all"
                          checked={enabledOptional === optionalKeys.length}
                          indeterminate={
                            enabledOptional > 0 && enabledOptional < optionalKeys.length
                          }
                          onChange={() =>
                            setMany(optionalKeys, enabledOptional !== optionalKeys.length)
                          }
                        />
                      ) : (
                        <span className="flex items-center gap-1 text-xs text-muted-foreground">
                          <Lock className="h-3.5 w-3.5" />
                          All required
                        </span>
                      )}
                    </div>
                    {!isCollapsed && (
                      <div className="grid grid-cols-1 gap-3 p-4 sm:grid-cols-2 2xl:grid-cols-3">
                        {keys.map((key) => (
                          <FeatureCard
                            key={key}
                            featureKey={key}
                            checked={enabledFeatures.has(key)}
                            mandatory={mandatory.includes(key)}
                            requires={dependencies[key] ?? []}
                            pending={pendingOf(key)}
                            onToggle={() => setFeature(key, !enabledFeatures.has(key))}
                            parts={partsOf(key)}
                          />
                        ))}
                      </div>
                    )}
                  </Card>
                );
              })}
            </div>
          </div>
        )}
      </div>

      {/* Pinned to the bottom of the screen so Save is always in reach, however far down the
          list the operator has scrolled. */}
      <div
        className={cn(
          'sticky bottom-0 z-10 border-t bg-background/95 backdrop-blur transition-shadow',
          isDirty
            ? 'border-warning/50 shadow-[0_-6px_16px_-8px_rgba(0,0,0,0.18)]'
            : 'border-border',
        )}
      >
        <div className={cn(PLATFORM_CONTAINER, 'flex flex-col gap-2 py-3')}>
          {saveError && (
            <p
              role="alert"
              className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive"
            >
              {saveError}
            </p>
          )}
          <div className="flex flex-wrap items-center justify-between gap-3">
            <p className="flex items-center gap-2 text-sm text-muted-foreground" aria-live="polite">
              {isDirty ? (
                <>
                  <span className="h-2 w-2 rounded-full bg-warning" />
                  <span>
                    <span className="font-medium text-foreground">
                      {changeCount} unsaved change{changeCount === 1 ? '' : 's'}
                    </span>
                    {turningOn.length > 0 && <> · {turningOn.length} turning on</>}
                    {turningOff.length > 0 && <> · {turningOff.length} turning off</>}
                  </span>
                </>
              ) : (
                <>
                  <Check className="h-4 w-4 text-success" />
                  All changes saved
                </>
              )}
            </p>
            <div className="flex gap-2">
              <Button
                variant="outline"
                onClick={() => setEnabledFeatures(new Set(initialFeatures))}
                disabled={!isDirty || updateMutation.isPending}
              >
                Discard changes
              </Button>
              <Button
                className="gap-1.5"
                onClick={handleSave}
                disabled={!isDirty || updateMutation.isPending}
              >
                {updateMutation.isPending ? (
                  <Loader2 className="h-4 w-4 animate-spin" />
                ) : (
                  <Save className="h-4 w-4" />
                )}
                {updateMutation.isPending ? 'Saving…' : 'Save changes'}
              </Button>
            </div>
          </div>
        </div>
      </div>

      <UnsavedChangesDialog
        open={guard.showUnsavedDialog}
        onConfirmDiscard={guard.confirmDiscard}
        onCancel={guard.cancelDiscard}
        title="Leave without saving?"
        description={`The feature changes for ${hospitalName ?? 'this hospital'} haven't been saved. Leaving now discards them.`}
      />
    </div>
  );
}
