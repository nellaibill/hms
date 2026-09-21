import { useEffect, useMemo, useRef, useState } from 'react';
import { ApiError, type TenantListItemResponse } from '@hms/shared';
import { Building2, ChevronDown, ChevronRight, Info, LayoutGrid, Search } from 'lucide-react';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from '@/components/ui/dialog';
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
  'h-4 w-4 shrink-0 rounded border-input text-primary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-60';

function IndeterminateCheckbox({ checked, indeterminate, disabled, onChange, label }: { checked: boolean; indeterminate: boolean; disabled: boolean; onChange: () => void; label: string }) {
  const ref = useRef<HTMLInputElement>(null);
  useEffect(() => {
    if (ref.current) ref.current.indeterminate = indeterminate;
  }, [indeterminate]);

  return (
    <label className="flex items-center gap-2 text-sm text-muted-foreground">
      <input ref={ref} type="checkbox" checked={checked} disabled={disabled} onChange={onChange} className={checkboxClassName} />
      {label}
    </label>
  );
}

function FeatureCard({
  featureKey,
  checked,
  mandatory,
  requires,
  onToggle,
}: {
  featureKey: string;
  checked: boolean;
  mandatory: boolean;
  requires: string[];
  onToggle: () => void;
}) {
  const meta = FEATURE_META[featureKey];
  const category = FEATURE_CATEGORIES.find((c) => c.id === featureCategoryOf(featureKey))!;
  const Icon = meta?.icon ?? category.icon;

  return (
    <label
      className={cn(
        'flex items-start gap-3 rounded-lg border border-border bg-card p-3 transition-colors',
        mandatory ? 'cursor-not-allowed' : 'cursor-pointer hover:border-primary/40 hover:bg-accent/40',
        checked && !mandatory && 'border-primary/30',
      )}
    >
      <input type="checkbox" checked={checked} disabled={mandatory} onChange={onToggle} className={cn(checkboxClassName, 'mt-1')} />
      <span className={cn('flex h-9 w-9 shrink-0 items-center justify-center rounded-md', category.tint)}>
        <Icon className="h-4 w-4" />
      </span>
      <span className="flex min-w-0 flex-col gap-0.5">
        <span className="flex flex-wrap items-center gap-x-2 text-sm font-medium text-foreground">
          {featureLabel(featureKey)}
          {mandatory && (
            <Badge variant="secondary" className="px-1.5 py-0 text-[10px] font-medium">
              Required
            </Badge>
          )}
        </span>
        {meta && <span className="text-xs text-muted-foreground">{meta.description}</span>}
        {requires.length > 0 && <span className="text-xs text-muted-foreground">Requires {requires.map(featureLabel).join(', ')}</span>}
      </span>
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
  const initialFeatures = useMemo(() => new Set([...(data?.enabledFeatures ?? []), ...mandatory]), [data, mandatory]);

  useEffect(() => {
    if (data) setEnabledFeatures(new Set(initialFeatures));
  }, [data, initialFeatures]);

  const isDirty = enabledFeatures.size !== initialFeatures.size || [...enabledFeatures].some((key) => !initialFeatures.has(key));

  const visibleKeys = useMemo(() => {
    const term = search.trim().toLowerCase();
    return (data?.allFeatures ?? []).filter((key) => {
      if (!term) return true;
      return `${key} ${featureLabel(key)} ${FEATURE_META[key]?.description ?? ''}`.toLowerCase().includes(term);
    });
  }, [data, search]);

  const groups = groupByCategory(visibleKeys);
  const shownGroups = activeCategory === 'all' ? groups : groups.filter((group) => group.category.id === activeCategory);
  const shownCount = shownGroups.reduce((total, group) => total + group.keys.length, 0);

  function setFeature(key: string, on: boolean) {
    setEnabledFeatures((current) => withFeatureToggled(current, key, on, dependencies, mandatory));
  }

  function setMany(keys: string[], on: boolean) {
    setEnabledFeatures((current) => keys.reduce((next, key) => withFeatureToggled(next, key, on, dependencies, mandatory), current));
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
    updateMutation.mutate({ id: hospital.id, request: { enabledFeatures: Array.from(enabledFeatures) } }, { onSuccess: onClose });
  }

  const apiError = updateMutation.error instanceof ApiError ? updateMutation.error.message : null;
  const registeredOn = new Date(hospital.createdAt).toLocaleDateString(undefined, { day: 'numeric', month: 'short', year: 'numeric' });

  return (
    <Dialog open onOpenChange={(open) => !open && onClose()}>
      <DialogContent aria-labelledby="hospital-features-title" className="flex max-h-[90vh] w-[calc(100%-2rem)] max-w-5xl flex-col gap-0 overflow-hidden p-0">
        <DialogHeader className="px-6 pb-4 pt-6 text-left">
          <DialogTitle id="hospital-features-title">Manage Features — {hospital.hospitalName}</DialogTitle>
          <DialogDescription>
            Select the modules and features available for this hospital. Enabling a new module provisions its schema immediately; disabling one only
            revokes access, it never deletes data. Changes apply only to this tenant.
          </DialogDescription>
        </DialogHeader>

        <div className="px-6 pb-4">
          <div className="flex flex-wrap items-center gap-4 rounded-lg border border-border bg-muted/40 p-4">
            <span className="flex h-11 w-11 items-center justify-center rounded-full bg-primary/10 text-primary">
              <Building2 className="h-5 w-5" />
            </span>
            <div className="flex flex-col gap-1">
              <div className="flex items-center gap-2">
                <span className="font-semibold text-foreground">{hospital.hospitalName}</span>
                <Badge variant={hospital.status === 'Active' ? 'success' : 'secondary'}>{hospital.status}</Badge>
              </div>
              <div className="flex flex-wrap gap-x-4 gap-y-0.5 text-xs text-muted-foreground">
                <span>Code: {hospital.hospitalCode}</span>
                <span>Registered on: {registeredOn}</span>
                <span>Plan: {hospital.subscriptionTier}</span>
              </div>
            </div>
          </div>
        </div>

        {featuresQuery.isPending && <p className="px-6 pb-6 text-sm text-muted-foreground">Loading…</p>}

        {data && (
          <div className="flex min-h-0 flex-1 flex-col gap-4 border-t border-border px-6 py-4 md:flex-row">
            <nav aria-label="Feature categories" className="flex shrink-0 flex-col gap-3 md:w-60">
              <div className="relative">
                <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                <Input value={search} onChange={(e) => setSearch(e.target.value)} placeholder="Search features…" aria-label="Search features" className="pl-9" />
              </div>
              <ul className="flex gap-1 overflow-x-auto md:flex-col md:overflow-visible">
                {[{ id: 'all', label: 'All Features', icon: LayoutGrid, count: visibleKeys.length }, ...groups.map((g) => ({ id: g.category.id, label: g.category.label, icon: g.category.icon, count: g.keys.length }))].map(
                  (item) => (
                    <li key={item.id}>
                      <button
                        type="button"
                        onClick={() => setActiveCategory(item.id)}
                        aria-current={activeCategory === item.id}
                        className={cn(
                          'flex w-full items-center gap-2 whitespace-nowrap rounded-md px-3 py-2 text-left text-sm transition-colors',
                          activeCategory === item.id ? 'bg-primary font-medium text-primary-foreground' : 'text-foreground hover:bg-accent',
                        )}
                      >
                        <item.icon className="h-4 w-4 shrink-0" />
                        <span className="flex-1">{item.label}</span>
                        <span className={cn('rounded-full px-2 py-0.5 text-xs', activeCategory === item.id ? 'bg-primary-foreground/20' : 'bg-muted text-muted-foreground')}>
                          {item.count}
                        </span>
                      </button>
                    </li>
                  ),
                )}
              </ul>
            </nav>

            <div className="flex min-h-0 min-w-0 flex-1 flex-col gap-3">
              <div className="flex flex-wrap items-center justify-between gap-2">
                <h3 className="text-base font-semibold text-foreground">Features ({shownCount})</h3>
                <div className="flex items-center gap-2 text-sm text-muted-foreground">
                  Select:
                  <Button type="button" variant="outline" size="sm" onClick={() => setEnabledFeatures(new Set(data.allFeatures))}>
                    All
                  </Button>
                  <Button type="button" variant="outline" size="sm" onClick={() => setEnabledFeatures(new Set(mandatory))}>
                    None
                  </Button>
                  <Button type="button" variant="outline" size="sm" onClick={() => setEnabledFeatures(recommendedFeatures(data.allFeatures, mandatory))}>
                    Recommended
                  </Button>
                </div>
              </div>

              <div className="flex min-h-0 flex-1 flex-col gap-4 overflow-y-auto pr-1">
                {shownGroups.length === 0 && <p className="text-sm text-muted-foreground">No features match &ldquo;{search}&rdquo;.</p>}
                {shownGroups.map(({ category, keys }) => {
                  const optionalKeys = keys.filter((key) => !mandatory.includes(key));
                  const enabledCount = optionalKeys.filter((key) => enabledFeatures.has(key)).length;
                  const isCollapsed = collapsed.has(category.id);
                  const Chevron = isCollapsed ? ChevronRight : ChevronDown;

                  return (
                    <section key={category.id} className="rounded-lg border border-border bg-muted/20">
                      <div className="flex items-center justify-between gap-2 px-3 py-2">
                        <button type="button" onClick={() => toggleCollapsed(category.id)} aria-expanded={!isCollapsed} className="flex items-center gap-2 text-sm font-semibold text-foreground">
                          <Chevron className="h-4 w-4 text-muted-foreground" />
                          <category.icon className="h-4 w-4 text-primary" />
                          {category.label}
                          <span className="font-normal text-muted-foreground">({keys.length})</span>
                        </button>
                        <IndeterminateCheckbox
                          label="Select all"
                          checked={optionalKeys.length > 0 && enabledCount === optionalKeys.length}
                          indeterminate={enabledCount > 0 && enabledCount < optionalKeys.length}
                          disabled={optionalKeys.length === 0}
                          onChange={() => setMany(optionalKeys, enabledCount !== optionalKeys.length)}
                        />
                      </div>
                      {!isCollapsed && (
                        <div className="grid grid-cols-1 gap-2 px-3 pb-3 sm:grid-cols-2">
                          {keys.map((key) => (
                            <FeatureCard
                              key={key}
                              featureKey={key}
                              checked={enabledFeatures.has(key)}
                              mandatory={mandatory.includes(key)}
                              requires={dependencies[key] ?? []}
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

        <div className="flex flex-col gap-3 border-t border-border px-6 py-4">
          {apiError && (
            <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
              {apiError}
            </p>
          )}
          <div className="flex flex-wrap items-center justify-between gap-3">
            <div className="flex max-w-xl items-start gap-2 rounded-md bg-info/10 px-3 py-2 text-xs text-foreground">
              <Info className="mt-0.5 h-4 w-4 shrink-0 text-info" />
              <span>
                <span className="font-medium">These settings are tenant-specific.</span> Enable only the modules required for this hospital. You can change these
                anytime.
              </span>
            </div>
            <div className="flex gap-2">
              <Button variant="outline" onClick={onClose} disabled={updateMutation.isPending}>
                Cancel
              </Button>
              <Button onClick={handleSave} disabled={!data || !isDirty || updateMutation.isPending}>
                {updateMutation.isPending ? 'Saving…' : 'Save Changes'}
              </Button>
            </div>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}
