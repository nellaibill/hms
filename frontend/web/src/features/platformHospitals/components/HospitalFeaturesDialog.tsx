import { useEffect, useMemo, useRef, useState } from 'react';
import { ApiError, type TenantListItemResponse } from '@hms/shared';
import {
  Building2,
  Check,
  ChevronDown,
  ChevronRight,
  LayoutGrid,
  Link2,
  Lock,
  Search,
} from 'lucide-react';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { cn } from '@/lib/utils';
import {
  FEATURE_CATEGORIES,
  FEATURE_META,
  featureCategoryOf,
  featureLabel,
  groupByCategory,
  recommendedFeatures,
  withFeatureToggled,
} from '../featureCatalog';
import { useHospitalFeaturesQuery } from '../hooks/useHospitalFeaturesQuery';
import { useUpdateHospitalFeaturesMutation } from '../hooks/useUpdateHospitalFeaturesMutation';

interface HospitalFeaturesDialogProps {
  hospital: TenantListItemResponse;
  onClose: () => void;
}

const checkboxClassName =
  'h-4 w-4 shrink-0 rounded border-input accent-primary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-60';

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

function FeatureCard({
  featureKey,
  checked,
  mandatory,
  requires,
  pending,
  onToggle,
}: {
  featureKey: string;
  checked: boolean;
  mandatory: boolean;
  requires: string[];
  pending: PendingChange;
  onToggle: () => void;
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
          {pending && (
            <span
              className={cn(
                'rounded-full px-1.5 py-px text-[10px] font-semibold uppercase tracking-wide',
                pending === 'on'
                  ? 'bg-success/15 text-success'
                  : 'bg-destructive/10 text-destructive',
              )}
            >
              {pending === 'on' ? 'Turning on' : 'Turning off'}
            </span>
          )}
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
      <div className="flex h-full items-start gap-3 rounded-lg border border-dashed border-border bg-muted/30 p-3">
        {body}
      </div>
    );
  }

  return (
    <label
      className={cn(
        'flex h-full cursor-pointer items-start gap-3 rounded-lg border p-3 transition-colors',
        checked
          ? 'border-primary/60 bg-primary/[0.04] shadow-sm'
          : 'border-border bg-card hover:border-primary/40 hover:bg-accent/40',
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

export function HospitalFeaturesDialog({ hospital, onClose }: HospitalFeaturesDialogProps) {
  const featuresQuery = useHospitalFeaturesQuery(hospital.id);
  const updateMutation = useUpdateHospitalFeaturesMutation();

  const [enabledFeatures, setEnabledFeatures] = useState<Set<string>>(new Set());
  const [search, setSearch] = useState('');
  const [activeCategory, setActiveCategory] = useState<string>('all');
  const [collapsed, setCollapsed] = useState<Set<string>>(new Set());

  const data = featuresQuery.data;
  const mandatory = useMemo(() => data?.mandatoryFeatures ?? [], [data]);
  const dependencies = useMemo(() => data?.dependencies ?? {}, [data]);

  // The tenant's own enabled set plus every mandatory key: a tenant provisioned before a key
  // became Mandatory has no row for it, but it is shown as required-and-on, so the submitted
  // payload must contain it or the backend's "mandatory features cannot be disabled" check rejects the save.
  const initialFeatures = useMemo(
    () => new Set([...(data?.enabledFeatures ?? []), ...mandatory]),
    [data, mandatory],
  );

  useEffect(() => {
    if (data) setEnabledFeatures(new Set(initialFeatures));
  }, [data, initialFeatures]);

  const turningOn = [...enabledFeatures].filter((key) => !initialFeatures.has(key));
  const turningOff = [...initialFeatures].filter((key) => !enabledFeatures.has(key));
  const isDirty = turningOn.length > 0 || turningOff.length > 0;

  const visibleKeys = useMemo(() => {
    const term = search.trim().toLowerCase();
    return (data?.allFeatures ?? []).filter((key) => {
      if (!term) return true;
      return `${key} ${featureLabel(key)} ${FEATURE_META[key]?.description ?? ''}`
        .toLowerCase()
        .includes(term);
    });
  }, [data, search]);

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

  function handleSave() {
    updateMutation.mutate(
      { id: hospital.id, request: { enabledFeatures: Array.from(enabledFeatures) } },
      { onSuccess: onClose },
    );
  }

  const apiError = updateMutation.error instanceof ApiError ? updateMutation.error.message : null;
  const registeredOn = new Date(hospital.createdAt).toLocaleDateString('en-IN', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
  });
  const railItems = [
    { id: 'all', label: 'All features', icon: LayoutGrid, keys: visibleKeys },
    ...groups.map((g) => ({
      id: g.category.id,
      label: g.category.label,
      icon: g.category.icon,
      keys: g.keys,
    })),
  ];

  return (
    <Dialog open onOpenChange={(open) => !open && onClose()}>
      <DialogContent
        aria-labelledby="hospital-features-title"
        className="flex h-[90vh] w-[calc(100%-2rem)] max-w-5xl flex-col gap-0 overflow-hidden p-0"
      >
        <DialogHeader className="flex-row items-start gap-4 space-y-0 border-b border-border px-6 py-5 pr-12 text-left">
          <span className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-primary/10 text-primary">
            <Building2 className="h-5 w-5" />
          </span>
          <div className="flex min-w-0 flex-col gap-1">
            <DialogTitle
              id="hospital-features-title"
              className="flex flex-wrap items-center gap-2 text-lg"
            >
              Manage features
              <span className="text-muted-foreground">·</span>
              <span className="truncate">{hospital.hospitalName}</span>
              <Badge variant={hospital.status === 'Active' ? 'success' : 'secondary'}>
                {hospital.status}
              </Badge>
            </DialogTitle>
            <p className="flex flex-wrap gap-x-3 gap-y-0.5 text-xs text-muted-foreground">
              <span>
                Code <span className="font-mono text-foreground">{hospital.hospitalCode}</span>
              </span>
              <span>{hospital.subscriptionTier} plan</span>
              <span>Registered {registeredOn}</span>
            </p>
            <DialogDescription className="hidden text-xs sm:block">
              Choose the modules this hospital can use. Turning one on sets up its data straight
              away; turning one off only hides it and never deletes data. Only this hospital is
              affected.
            </DialogDescription>
          </div>
        </DialogHeader>

        {featuresQuery.isPending && (
          <p className="px-6 py-6 text-sm text-muted-foreground">Loading features…</p>
        )}

        {data && (
          <div className="flex min-h-0 flex-1 flex-col md:flex-row">
            <nav
              aria-label="Feature categories"
              className="flex shrink-0 flex-col gap-3 border-b border-border bg-muted/20 p-4 md:w-64 md:border-b-0 md:border-r"
            >
              <div className="relative">
                <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                <Input
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                  placeholder="Search features…"
                  aria-label="Search features"
                  className="h-9 bg-background pl-9"
                />
              </div>
              <ul className="flex gap-1 overflow-x-auto md:flex-col md:overflow-visible">
                {railItems.map((item) => {
                  const active = activeCategory === item.id;
                  return (
                    <li key={item.id}>
                      <button
                        type="button"
                        onClick={() => setActiveCategory(item.id)}
                        aria-current={active}
                        className={cn(
                          'flex w-full items-center gap-2.5 whitespace-nowrap rounded-md px-3 py-2 text-left text-sm transition-colors',
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

            <div className="flex min-h-0 min-w-0 flex-1 flex-col">
              <div className="flex flex-wrap items-center justify-between gap-3 px-5 py-3">
                <p className="text-sm text-muted-foreground">
                  <span className="font-semibold text-foreground">{enabledFeatures.size}</span> of{' '}
                  {data.allFeatures.length} features on · {mandatory.length} always on
                </p>
                <div className="flex items-center gap-2">
                  <span className="hidden text-xs text-muted-foreground sm:inline">
                    Quick select
                  </span>
                  <div className="inline-flex overflow-hidden rounded-md border border-border">
                    {[
                      {
                        label: 'All',
                        onClick: () => setEnabledFeatures(new Set(data.allFeatures)),
                      },
                      {
                        label: 'Recommended',
                        onClick: () =>
                          setEnabledFeatures(recommendedFeatures(data.allFeatures, mandatory)),
                      },
                      {
                        label: 'Required only',
                        onClick: () => setEnabledFeatures(new Set(mandatory)),
                      },
                    ].map((option, index) => (
                      <button
                        key={option.label}
                        type="button"
                        onClick={option.onClick}
                        className={cn(
                          'whitespace-nowrap px-3 py-1.5 text-xs font-medium text-foreground transition-colors hover:bg-accent',
                          index > 0 && 'border-l border-border',
                        )}
                      >
                        {option.label}
                      </button>
                    ))}
                  </div>
                </div>
              </div>

              <div className="flex min-h-0 flex-1 flex-col gap-3 overflow-y-auto px-5 pb-5">
                {shownGroups.length === 0 && (
                  <p className="rounded-lg border border-dashed border-border py-10 text-center text-sm text-muted-foreground">
                    No features match &ldquo;{search}&rdquo;.
                  </p>
                )}
                {shownGroups.map(({ category, keys }) => {
                  const optionalKeys = keys.filter((key) => !mandatory.includes(key));
                  const enabledOptional = enabledIn(optionalKeys);
                  const isCollapsed = collapsed.has(category.id);
                  const Chevron = isCollapsed ? ChevronRight : ChevronDown;

                  return (
                    <section key={category.id} className="rounded-xl border border-border bg-card">
                      <div
                        className={cn(
                          'flex items-center justify-between gap-2 px-4 py-2.5',
                          !isCollapsed && 'border-b border-border',
                        )}
                      >
                        <button
                          type="button"
                          onClick={() => toggleCollapsed(category.id)}
                          aria-expanded={!isCollapsed}
                          className="flex items-center gap-2 text-sm font-semibold text-foreground"
                        >
                          <Chevron className="h-4 w-4 text-muted-foreground" />
                          <span
                            className={cn(
                              'flex h-6 w-6 items-center justify-center rounded-md',
                              category.tint,
                            )}
                          >
                            <category.icon className="h-3.5 w-3.5" />
                          </span>
                          {category.label}
                          <span className="text-xs font-normal text-muted-foreground">
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
                        <div className="grid grid-cols-1 gap-2.5 p-3 sm:grid-cols-2">
                          {keys.map((key) => (
                            <FeatureCard
                              key={key}
                              featureKey={key}
                              checked={enabledFeatures.has(key)}
                              mandatory={mandatory.includes(key)}
                              requires={dependencies[key] ?? []}
                              pending={
                                turningOn.includes(key)
                                  ? 'on'
                                  : turningOff.includes(key)
                                    ? 'off'
                                    : null
                              }
                              onToggle={() => setFeature(key, !enabledFeatures.has(key))}
                            />
                          ))}
                        </div>
                      )}
                    </section>
                  );
                })}
              </div>
            </div>
          </div>
        )}

        <div className="flex flex-col gap-3 border-t border-border bg-muted/20 px-6 py-3.5">
          {apiError && (
            <p
              role="alert"
              className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive"
            >
              {apiError}
            </p>
          )}
          <div className="flex flex-wrap items-center justify-between gap-3">
            <p className="flex items-center gap-2 text-sm text-muted-foreground" aria-live="polite">
              {isDirty ? (
                <>
                  <span className="h-2 w-2 rounded-full bg-warning" />
                  <span>
                    <span className="font-medium text-foreground">
                      {turningOn.length + turningOff.length} unsaved change
                      {turningOn.length + turningOff.length === 1 ? '' : 's'}
                    </span>
                    {turningOn.length > 0 && <> · {turningOn.length} turning on</>}
                    {turningOff.length > 0 && <> · {turningOff.length} turning off</>}
                  </span>
                </>
              ) : (
                <>
                  <Check className="h-4 w-4 text-success" />
                  No unsaved changes
                </>
              )}
            </p>
            <div className="flex gap-2">
              <Button variant="outline" onClick={onClose} disabled={updateMutation.isPending}>
                Cancel
              </Button>
              <Button onClick={handleSave} disabled={!data || !isDirty || updateMutation.isPending}>
                {updateMutation.isPending ? 'Saving…' : 'Save changes'}
              </Button>
            </div>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}
