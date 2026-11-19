import { Loader2, Plus, Search, Tags } from 'lucide-react';
import { useMemo, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { PageBanner } from '@/components/PageBanner';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Switch } from '@/components/ui/switch';
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { useAuth } from '@/features/auth/AuthContext';
import {
  LOW_PROFIT_PERCENT,
  useAllDiagnosticServicesQuery,
  useDiagnosticCategoriesQuery,
  useDiagnosticProvidersQuery,
  usePrimeDiagnosticPackageCache,
} from '@/features/diagnostics';
import { useMasterOptionsQuery } from '@/features/masters';
import { ConsultantChargesDialog } from '@/features/priceList/components/ConsultantChargesDialog';
import { ConsultationRatesGrid } from '@/features/priceList/components/ConsultationRatesGrid';
import { PriceListTable, type PriceTableVariant } from '@/features/priceList/components/PriceListTable';
import {
  buildConsultationMatrix,
  buildLabRows,
  buildMasterTestRows,
  buildOutsourcedRows,
  buildPackageRows,
  buildRadiologyRows,
  countLowProfit,
  countNotCosted,
  filterRows,
  PRICE_LIST_TABS,
  sectionsOf,
  type PriceListTab,
  type PriceRow,
  type QuickFilter,
} from '@/features/priceList/priceListRows';
import { cn } from '@/lib/utils';

interface TabSpec {
  key: PriceListTab;
  label: string;
  rule: string;
  variant?: PriceTableVariant;
  grouped?: boolean;
  /** "Add" for this tab, and the permission it needs. */
  add?: { label: string; path: string; permission: string };
}

const TABS: TabSpec[] = [
  {
    key: 'lab',
    label: 'Lab tests',
    rule: 'Profit = Price − Running cost. No doctor’s share on lab tests.',
    variant: 'lab',
    grouped: true,
    add: { label: 'Add lab test', path: '/diagnostics/lab/services/new?kind=lab', permission: 'diagnostics.create' },
  },
  {
    key: 'outsourced',
    label: 'Outsourced tests',
    rule: 'Profit = Price − what the outside lab charges us.',
    variant: 'outsourced',
    grouped: true,
    add: { label: 'Add outsourced test', path: '/diagnostics/lab/services/new?kind=outsourced', permission: 'diagnostics.create' },
  },
  {
    key: 'packages',
    label: 'Lab packages',
    rule: 'Profit = Package price − Running cost (the package’s own figure, or its tests’ total).',
    variant: 'packages',
    add: { label: 'Add package', path: '/diagnostics/lab/packages/new', permission: 'diagnostics.create' },
  },
  {
    key: 'radiology',
    label: 'Radiology',
    rule: 'Profit = Price − Doctor’s share − Running cost.',
    variant: 'radiology',
    grouped: true,
    add: { label: 'Add radiology service', path: '/diagnostics/lab/services/new?kind=radiology', permission: 'diagnostics.create' },
  },
  {
    key: 'procedures',
    label: 'Procedures',
    rule: 'Profit = Price − Cost, as the Diagnostic Test master holds it.',
    variant: 'master',
    grouped: true,
    add: { label: 'Add procedure', path: '/admin/masters/diagnosticTest/new', permission: 'identity-administration.create' },
  },
  {
    key: 'injections',
    label: 'Injections & files',
    rule: 'Profit = Price − Cost, as the Diagnostic Test master holds it.',
    variant: 'master',
    grouped: true,
    add: { label: 'Add injection or file', path: '/admin/masters/diagnosticTest/new', permission: 'identity-administration.create' },
  },
  { key: 'consultations', label: 'Consultations', rule: 'Hospital keeps = Hospital charge − Doctor’s charge. No running cost.' },
];

const QUICK_FILTERS: { key: QuickFilter; label: string }[] = [
  { key: 'all', label: 'All' },
  { key: 'notCosted', label: 'Not costed' },
  { key: 'lowProfit', label: `Profit below ${LOW_PROFIT_PERCENT}%` },
];

const ALL_SECTIONS = '__all__';

/**
 * The Price List (ADR-140): every billable service in one place — what the patient pays, what
 * goes to the doctor and to running costs, and the profit the hospital keeps — tabbed by kind
 * and laid out like the hospital's tariff sheet. It replaces the separate Diagnostic Services
 * and Diagnostic Packages lists: items are added from here, edited on their own screens, and a
 * consultant's charges are edited right here. The open tab lives in the URL (?tab=), so links
 * and Back buttons land on the right one.
 */
export default function PriceListPage() {
  const { hasPermission } = useAuth();
  const canSeeConsultations = hasPermission('identity-administration.view');
  const canEditMasters = hasPermission('identity-administration.edit');

  const labQuery = useAllDiagnosticServicesQuery('Laboratory');
  const radiologyQuery = useAllDiagnosticServicesQuery('Radiology');
  const packages = usePrimeDiagnosticPackageCache();
  const categoriesQuery = useDiagnosticCategoriesQuery({ pageSize: 100, sort: 'name' });
  const providersQuery = useDiagnosticProvidersQuery({ pageSize: 100, sort: 'name' });
  const testsQuery = useMasterOptionsQuery('diagnosticTest');
  const consultantsQuery = useMasterOptionsQuery(canSeeConsultations ? 'consultant' : undefined);
  const typesQuery = useMasterOptionsQuery(canSeeConsultations ? 'consultationType' : undefined);

  const [searchParams, setSearchParams] = useSearchParams();
  const requestedTab = searchParams.get('tab') as PriceListTab | null;
  const [search, setSearch] = useState('');
  const [section, setSection] = useState('');
  const [quick, setQuick] = useState<QuickFilter>('all');
  const [showInactive, setShowInactive] = useState(false);
  const [editingConsultantId, setEditingConsultantId] = useState<string | null>(null);

  const rowsByTab = useMemo<Record<Exclude<PriceListTab, 'consultations'>, PriceRow[]>>(() => {
    const services = [...(labQuery.data ?? []), ...(radiologyQuery.data ?? [])];
    const categoryNames = new Map((categoriesQuery.data?.items ?? []).map((category) => [category.id, category.name]));
    const providerNames = new Map((providersQuery.data?.items ?? []).map((provider) => [provider.id, provider.name]));
    const servicesById = new Map(services.map((service) => [service.id, service]));
    const tests = testsQuery.data ?? [];
    return {
      lab: buildLabRows(services, categoryNames, packages ?? [], showInactive),
      outsourced: buildOutsourcedRows(services, categoryNames, providerNames, showInactive),
      packages: buildPackageRows(packages ?? [], servicesById, showInactive),
      radiology: buildRadiologyRows(services, categoryNames, showInactive),
      procedures: buildMasterTestRows(tests, ['Procedure'], canEditMasters, showInactive),
      injections: buildMasterTestRows(tests, ['Injection', 'File'], canEditMasters, showInactive),
    };
  }, [labQuery.data, radiologyQuery.data, packages, categoriesQuery.data, providersQuery.data, testsQuery.data, canEditMasters, showInactive]);

  const consultationMatrix = useMemo(
    () => buildConsultationMatrix(consultantsQuery.data ?? [], typesQuery.data ?? []),
    [consultantsQuery.data, typesQuery.data],
  );

  const tabs = TABS.filter((spec) => spec.key !== 'consultations' || canSeeConsultations);
  const activeTab = tabs.find((spec) => spec.key === requestedTab) ?? tabs[0];
  const isConsultations = activeTab.key === 'consultations';
  const tabRows = isConsultations ? [] : rowsByTab[activeTab.key as Exclude<PriceListTab, 'consultations'>];
  const visibleRows = filterRows(tabRows, { search, section, quick });
  const sections = sectionsOf(tabRows);
  const isLoading = labQuery.isPending || radiologyQuery.isPending || !packages || testsQuery.isPending;
  const addAction = activeTab.add && hasPermission(activeTab.add.permission) ? activeTab.add : undefined;
  const editingConsultant = editingConsultantId ? consultantsQuery.data?.find((record) => record.id === editingConsultantId) : undefined;

  function countFor(key: PriceListTab): number {
    return key === 'consultations' ? consultationMatrix.rows.length : rowsByTab[key].length;
  }

  function selectTab(next: string) {
    if (!PRICE_LIST_TABS.includes(next as PriceListTab)) return;
    setSearchParams({ tab: next }, { replace: true });
    setSection('');
    setQuick('all');
  }

  return (
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={Tags}
        title="Price List"
        subtitle="Every billable service: what the patient pays, the doctor's share, the running cost and the profit the hospital keeps."
      />

      <div className="flex w-full flex-1 flex-col gap-5 p-4 sm:p-6 lg:p-8">
        <Tabs value={activeTab.key} onValueChange={selectTab}>
          <TabsList className="h-auto flex-wrap justify-start">
            {tabs.map((spec) => (
              <TabsTrigger key={spec.key} value={spec.key} className="gap-2">
                {spec.label}
                <span className="rounded-full bg-muted px-2 py-0.5 text-xs tabular-nums text-muted-foreground">{countFor(spec.key)}</span>
              </TabsTrigger>
            ))}
          </TabsList>
        </Tabs>

        {!isConsultations && (
          <div className="flex flex-wrap items-center justify-between gap-3">
            <div className="flex flex-wrap items-center gap-3">
              <label className="flex h-10 min-w-[240px] items-center gap-2 rounded-md border border-input bg-background px-3">
                <Search aria-hidden="true" className="h-4 w-4 text-muted-foreground" />
                <span className="sr-only">Search</span>
                <Input
                  type="search"
                  value={search}
                  onChange={(event) => setSearch(event.target.value)}
                  placeholder={activeTab.key === 'lab' ? 'Search tests or packages' : 'Search'}
                  className="h-8 border-0 px-0 shadow-none focus-visible:ring-0"
                />
              </label>
              {sections.length > 1 && (
                <Select value={section || ALL_SECTIONS} onValueChange={(value) => setSection(value === ALL_SECTIONS ? '' : value)}>
                  <SelectTrigger className="h-10 w-[200px]" aria-label="Section">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value={ALL_SECTIONS}>All sections</SelectItem>
                    {sections.map((name) => (
                      <SelectItem key={name} value={name}>
                        {name}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              )}
              <div className="flex items-center gap-2">
                <Switch id="pl-show-inactive" checked={showInactive} onCheckedChange={setShowInactive} />
                <Label htmlFor="pl-show-inactive" className="cursor-pointer text-sm font-normal">
                  Show inactive
                </Label>
              </div>
            </div>
            <div className="flex flex-wrap items-center gap-2">
              <div role="group" aria-label="Quick filters" className="flex flex-wrap gap-2">
                {QUICK_FILTERS.map((filter) => {
                  const count = filter.key === 'all' ? tabRows.length : filter.key === 'notCosted' ? countNotCosted(tabRows) : countLowProfit(tabRows);
                  const pressed = quick === filter.key;
                  return (
                    <button
                      key={filter.key}
                      type="button"
                      aria-pressed={pressed}
                      onClick={() => setQuick(filter.key)}
                      className={cn(
                        'min-h-9 rounded-full border px-3.5 text-sm transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring',
                        pressed ? 'border-primary bg-primary/10 font-semibold text-foreground' : 'border-border bg-background text-muted-foreground hover:text-foreground',
                      )}
                    >
                      {filter.label} · {count}
                    </button>
                  );
                })}
              </div>
              {addAction && (
                <Button asChild className="gap-1.5">
                  <Link to={addAction.path}>
                    <Plus className="h-4 w-4" />
                    {addAction.label}
                  </Link>
                </Button>
              )}
            </div>
          </div>
        )}

        <Card className="overflow-hidden">
          <div className="border-b border-border px-4 py-3 text-sm">
            <span className="font-semibold">{activeTab.rule}</span>
            {!isConsultations && <span className="text-muted-foreground"> Profit % is profit ÷ price, as in the tariff sheet.</span>}
            {isConsultations && canEditMasters && <span className="text-muted-foreground"> Use Edit to change a doctor&apos;s types and charges.</span>}
          </div>
          {isLoading ? (
            <div className="flex items-center justify-center gap-2 p-10 text-sm text-muted-foreground">
              <Loader2 className="h-4 w-4 animate-spin" />
              Loading the price list…
            </div>
          ) : isConsultations ? (
            <ConsultationRatesGrid
              columns={consultationMatrix.columns}
              rows={consultationMatrix.rows}
              canEdit={canEditMasters}
              onEdit={setEditingConsultantId}
            />
          ) : (
            <PriceListTable
              rows={visibleRows}
              variant={activeTab.variant as PriceTableVariant}
              grouped={activeTab.grouped && !section}
              emptyText={tabRows.length === 0 ? 'Nothing here yet.' : 'Nothing matches these filters.'}
            />
          )}
          {!isConsultations && !isLoading && visibleRows.length > 0 && (
            <p className="border-t border-border px-4 py-3 text-xs text-muted-foreground">
              Showing {visibleRows.length} of {tabRows.length}. Prices and costs are changed on each item's own screen — use Edit.
            </p>
          )}
        </Card>
      </div>

      {editingConsultant && (
        <ConsultantChargesDialog consultant={editingConsultant} columns={consultationMatrix.columns} onClose={() => setEditingConsultantId(null)} />
      )}
    </div>
  );
}
