import type { DiagnosticPackage, DiagnosticService } from '@hms/shared';
import type { MasterRecord } from '@/features/masters';
import { computeProfit, isLowProfit, packageBreakdown, packageProfitMode, testsRunningCostTotal, type ProfitBreakdown } from '@/features/diagnostics/tariff';

/**
 * The Price List's rows (ADR-140) — every billable service with what the patient pays and what
 * the hospital keeps, built per tab the way the hospital's tariff sheet lays each kind out.
 * Pure functions, so the page only fetches and renders.
 */

export type PriceListTab = 'lab' | 'outsourced' | 'packages' | 'radiology' | 'procedures' | 'injections' | 'consultations';

export const PRICE_LIST_TABS: PriceListTab[] = ['lab', 'outsourced', 'packages', 'radiology', 'procedures', 'injections', 'consultations'];

/** The Price List, opened on one tab — where the lab/radiology screens' Back links and saves go. */
export function priceListPath(tab: PriceListTab = 'lab'): string {
  return `/diagnostics/price-list?tab=${tab}`;
}

/** The Price List tab a lab test or radiology service is listed on. */
export function priceListTabFor(service: { serviceType: string; isOutsourced: boolean }): PriceListTab {
  if (service.serviceType === 'Radiology') return 'radiology';
  return service.isOutsourced ? 'outsourced' : 'lab';
}

export interface PriceRow {
  id: string;
  name: string;
  /** False for a deactivated item — shown only with "Show inactive". */
  isActive: boolean;
  /** The sheet's section (Haematology, Biochemistry, Ultrasound…), for grouping and filtering. */
  section: string;
  /** Where Edit goes; undefined when the row can't be edited from here. */
  editPath?: string;
  breakdown: ProfitBreakdown;
  /** Lab test: the packages it's part of. Package: its tests. */
  related?: string[];
  /** Outsourced test: the outside lab. */
  outsideLab?: string;
  /** Radiology: its rate option names and bedside add-on, shown as chips. */
  tags?: string[];
  /** Package: whether the running cost is its own figure or its tests' total. */
  costSource?: 'own' | 'tests';
}

export type QuickFilter = 'all' | 'notCosted' | 'lowProfit';

export interface PriceListFilters {
  search: string;
  section: string;
  quick: QuickFilter;
}

const NO_SECTION = 'Other';

function bySectionThenName(a: PriceRow, b: PriceRow): number {
  return a.section.localeCompare(b.section) || a.name.localeCompare(b.name);
}

/** In-house lab tests, with the active packages each one is part of. */
export function buildLabRows(services: DiagnosticService[], categoryNames: Map<string, string>, packages: DiagnosticPackage[], includeInactive = false): PriceRow[] {
  const packagesByService = new Map<string, string[]>();
  for (const pkg of packages) {
    if (!pkg.isActive) continue;
    for (const item of pkg.items) {
      const names = packagesByService.get(item.serviceId) ?? [];
      names.push(pkg.name);
      packagesByService.set(item.serviceId, names);
    }
  }
  return services
    .filter((service) => (includeInactive || service.isActive) && service.serviceType === 'Laboratory' && !service.isOutsourced)
    .map((service) => ({
      id: service.id,
      name: service.name,
      isActive: service.isActive,
      section: categoryNames.get(service.categoryId) ?? NO_SECTION,
      editPath: `/diagnostics/lab/services/${service.id}/edit`,
      breakdown: computeProfit(service.price, service.costPrice),
      related: (packagesByService.get(service.id) ?? []).sort((a, b) => a.localeCompare(b)),
    }))
    .sort(bySectionThenName);
}

/** Outsourced lab tests — the running cost is what the outside lab charges us. */
export function buildOutsourcedRows(
  services: DiagnosticService[],
  categoryNames: Map<string, string>,
  providerNames: Map<string, string>,
  includeInactive = false,
): PriceRow[] {
  return services
    .filter((service) => (includeInactive || service.isActive) && service.serviceType === 'Laboratory' && service.isOutsourced)
    .map((service) => ({
      id: service.id,
      name: service.name,
      isActive: service.isActive,
      section: categoryNames.get(service.categoryId) ?? NO_SECTION,
      editPath: `/diagnostics/lab/services/${service.id}/edit`,
      breakdown: computeProfit(service.price, service.costPrice),
      outsideLab: service.providerId ? providerNames.get(service.providerId) : undefined,
    }))
    .sort(bySectionThenName);
}

/** Lab packages — the running cost is the package's own figure, or its tests' total. */
export function buildPackageRows(packages: DiagnosticPackage[], servicesById: Map<string, DiagnosticService>, includeInactive = false): PriceRow[] {
  return packages
    .filter((pkg) => includeInactive || pkg.isActive)
    .map((pkg) => {
      const tests = pkg.items.map((item) => servicesById.get(item.serviceId)).filter((service): service is DiagnosticService => Boolean(service));
      const testsCost = tests.length === pkg.items.length ? testsRunningCostTotal(tests) : null;
      const mode = packageProfitMode(pkg);
      return {
        id: pkg.id,
        name: pkg.name,
        isActive: pkg.isActive,
        section: 'Lab packages',
        editPath: `/diagnostics/lab/packages/${pkg.id}`,
        breakdown: packageBreakdown(pkg.totalPrice, mode, pkg.totalPrice - pkg.costPrice, testsCost),
        related: tests.map((test) => test.name),
        costSource: mode === 'profit' ? 'own' : 'tests',
      } satisfies PriceRow;
    })
    .sort((a, b) => a.name.localeCompare(b.name));
}

/** Radiology — price − doctor's share − running cost, with rate options and bedside as chips. */
export function buildRadiologyRows(services: DiagnosticService[], categoryNames: Map<string, string>, includeInactive = false): PriceRow[] {
  return services
    .filter((service) => (includeInactive || service.isActive) && service.serviceType === 'Radiology')
    .map((service) => {
      const tags = (service.rateOptions ?? []).map((option) => `${option.name} ₹${option.price.toLocaleString('en-IN')}`);
      if (service.bedsidePrice != null) tags.push(`Bedside +₹${service.bedsidePrice.toLocaleString('en-IN')}`);
      return {
        id: service.id,
        name: service.name,
        isActive: service.isActive,
        section: categoryNames.get(service.categoryId) ?? NO_SECTION,
        editPath: `/diagnostics/lab/services/${service.id}/edit`,
        breakdown: computeProfit(service.price, service.costPrice, service.doctorCharge ?? 0),
        tags,
      };
    })
    .sort(bySectionThenName);
}

/**
 * Procedures, injections and files from the Diagnostic Test master. Their billing and costs are
 * unchanged (ADR-140): the cost shown is the master's own Running Cost, as reports use it.
 */
export function buildMasterTestRows(records: MasterRecord[], serviceTypes: string[], canEdit: boolean, includeInactive = false): PriceRow[] {
  return records
    .filter((record) => (includeInactive || record.isActive) && serviceTypes.includes(String(record.serviceType)))
    .map((record) => ({
      id: record.id,
      name: String(record.name ?? ''),
      isActive: Boolean(record.isActive),
      section: String(record.category || record.serviceType || NO_SECTION),
      editPath: canEdit ? `/admin/masters/diagnosticTest/${record.id}/edit` : undefined,
      breakdown: computeProfit(record.price, record.costPrice),
    }))
    .sort(bySectionThenName);
}

/** The rows a tab shows after its search, section and quick filters. */
export function filterRows(rows: PriceRow[], filters: PriceListFilters): PriceRow[] {
  const term = filters.search.trim().toLowerCase();
  return rows.filter((row) => {
    if (filters.section && row.section !== filters.section) return false;
    if (filters.quick === 'notCosted' && row.breakdown.isCosted) return false;
    if (filters.quick === 'lowProfit' && !isLowProfit(row.breakdown)) return false;
    if (!term) return true;
    return row.name.toLowerCase().includes(term) || (row.related ?? []).some((name) => name.toLowerCase().includes(term));
  });
}

export function countNotCosted(rows: PriceRow[]): number {
  return rows.filter((row) => !row.breakdown.isCosted).length;
}

export function countLowProfit(rows: PriceRow[]): number {
  return rows.filter((row) => isLowProfit(row.breakdown)).length;
}

/** Rows in order, split into their sections. */
export function groupBySection(rows: PriceRow[]): { section: string; rows: PriceRow[] }[] {
  const groups: { section: string; rows: PriceRow[] }[] = [];
  for (const row of rows) {
    const last = groups[groups.length - 1];
    if (last && last.section === row.section) last.rows.push(row);
    else groups.push({ section: row.section, rows: [row] });
  }
  return groups;
}

export function sectionsOf(rows: PriceRow[]): string[] {
  return Array.from(new Set(rows.map((row) => row.section))).sort((a, b) => a.localeCompare(b));
}

// --- Consultations -------------------------------------------------------------------------

export interface ConsultationTypeColumn {
  id: string;
  name: string;
  /** What the patient pays (the type's hospital charge); null for a type with no fixed rate. */
  amount: number | null;
}

export interface ConsultationCell {
  typeId: string;
  offered: boolean;
  /** The consultant's charge; null when offered but not set yet. */
  doctorCharge: number | null;
  /** Hospital charge − doctor's charge; null when either isn't known. */
  hospitalKeeps: number | null;
  /** hospitalKeeps ÷ hospital charge, as a whole-number percent. */
  percent: number | null;
}

export interface ConsultantRow {
  id: string;
  name: string;
  group: 'In-house' | 'Visiting';
  cells: ConsultationCell[];
}

function asNumber(value: unknown): number | null {
  return typeof value === 'number' && Number.isFinite(value) ? value : null;
}

/**
 * The consultant × consultation type grid. A consultant counts as Visiting when they offer a
 * "(Visiting)" type, In-house otherwise — the app has no separate in-house/visiting field.
 * Read-only: consultation billing and costs are unchanged (ADR-140).
 */
export function buildConsultationMatrix(consultants: MasterRecord[], consultationTypes: MasterRecord[]): { columns: ConsultationTypeColumn[]; rows: ConsultantRow[] } {
  const columns = consultationTypes
    .filter((type) => type.isActive)
    .map((type) => ({ id: type.id, name: String(type.name ?? ''), amount: asNumber(type.amount) }))
    .sort((a, b) => a.name.localeCompare(b.name));
  const visitingTypeIds = new Set(columns.filter((column) => /\(visiting\)/i.test(column.name)).map((column) => column.id));

  const rows = consultants
    .filter((consultant) => consultant.isActive)
    .map((consultant) => {
      const charges = Array.isArray(consultant.consultationTypeCharges)
        ? (consultant.consultationTypeCharges as { consultationTypeId?: string; consultantCharge?: unknown }[])
        : [];
      const chargeByType = new Map(charges.map((charge) => [charge.consultationTypeId, asNumber(charge.consultantCharge)]));
      const cells = columns.map((column) => {
        const offered = chargeByType.has(column.id);
        const doctorCharge = offered ? (chargeByType.get(column.id) ?? null) : null;
        const hospitalKeeps = doctorCharge !== null && column.amount !== null ? column.amount - doctorCharge : null;
        const percent = hospitalKeeps !== null && column.amount ? Math.round((hospitalKeeps / column.amount) * 100) : null;
        return { typeId: column.id, offered, doctorCharge, hospitalKeeps, percent };
      });
      const isVisiting = charges.some((charge) => charge.consultationTypeId && visitingTypeIds.has(charge.consultationTypeId));
      return { id: consultant.id, name: String(consultant.name ?? ''), group: isVisiting ? 'Visiting' : 'In-house', cells } satisfies ConsultantRow;
    })
    .sort((a, b) => (a.group === b.group ? a.name.localeCompare(b.name) : a.group === 'In-house' ? -1 : 1));

  return { columns, rows };
}
