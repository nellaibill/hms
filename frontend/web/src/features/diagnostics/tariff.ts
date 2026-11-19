import type { CreateDiagnosticServiceRequest, DiagnosticService, DiagnosticServiceFormValues } from '@hms/shared';

/**
 * The hospital tariff sheet's profit rules for Laboratory and Radiology (ADR-140). Staff enter
 * and see the price the patient pays; profit is worked out from it:
 *   - Lab test:            Profit = Price − Running cost (an outsourced test's running cost is
 *                          what the outside lab charges us)
 *   - Radiology:           Profit = Price − Doctor's share − Running cost
 *   - Lab package:         Profit = Package price − Running cost (the package's own figure,
 *                          or its tests' total)
 * Profit % is always profit ÷ price, the sheet's own convention ("CBC – 300, Profit – 120 (40%)").
 */

/** Below this profit %, a test or package is flagged in the Price List. */
export const LOW_PROFIT_PERCENT = 25;

export interface ProfitBreakdown {
  price: number;
  doctorShare: number;
  runningCost: number;
  /** Price − doctor's share: what the hospital keeps before its own running cost. */
  hospitalIncome: number;
  /** Price − doctor's share − running cost. */
  profit: number;
  /** Profit ÷ price as a whole-number percent; null for a price of 0. */
  profitPercent: number | null;
  /** False while neither a doctor's share nor a running cost is set ("not yet costed") — the
   * profit then can't be told, so it isn't shown as 100%. */
  isCosted: boolean;
}

function amount(value: unknown): number {
  const number = typeof value === 'number' ? value : Number(value);
  return Number.isFinite(number) ? number : 0;
}

export function computeProfit(price: unknown, runningCost: unknown, doctorShare: unknown = 0): ProfitBreakdown {
  const p = amount(price);
  const running = amount(runningCost);
  const doctor = amount(doctorShare);
  const hospitalIncome = p - doctor;
  const profit = hospitalIncome - running;
  return {
    price: p,
    doctorShare: doctor,
    runningCost: running,
    hospitalIncome,
    profit,
    profitPercent: p > 0 ? Math.round((profit / p) * 100) : null,
    isCosted: doctor > 0 || running > 0,
  };
}

/** True when a costed item makes less than LOW_PROFIT_PERCENT (a loss counts too). */
export function isLowProfit(breakdown: ProfitBreakdown): boolean {
  return breakdown.isCosted && breakdown.profitPercent !== null && breakdown.profitPercent < LOW_PROFIT_PERCENT;
}

/** "₹1,300" — whole rupees unless the amount has paise. */
export function formatRupees(value: number): string {
  return `₹${value.toLocaleString('en-IN', { maximumFractionDigits: 2 })}`;
}

/** A DiagnosticService's standard-rate breakdown. */
export function serviceProfit(service: Pick<DiagnosticService, 'price' | 'costPrice' | 'doctorCharge'>): ProfitBreakdown {
  return computeProfit(service.price, service.costPrice, service.doctorCharge ?? 0);
}

/** The service form's values as the create/update request body. A lab test sends no doctor's
 * share, bedside add-on or rate options whatever the form holds; a radiology scan is never
 * outsourced in this form, so it sends no provider. */
export function toDiagnosticServiceRequest(values: DiagnosticServiceFormValues): CreateDiagnosticServiceRequest {
  const isRadiology = values.serviceType === 'Radiology';
  const isOutsourced = !isRadiology && values.isOutsourced;
  return {
    code: values.code,
    name: values.name,
    categoryId: values.categoryId,
    serviceType: values.serviceType,
    isOutsourced,
    providerId: isOutsourced ? values.providerId || undefined : undefined,
    price: values.price,
    costPrice: values.costPrice,
    isActive: values.isActive,
    doctorCharge: isRadiology ? values.doctorCharge : 0,
    bedsidePrice: isRadiology && values.bedsideEnabled ? values.bedsidePrice : null,
    bedsideDoctorCharge: isRadiology && values.bedsideEnabled ? values.bedsideDoctorCharge : null,
    rateOptions: isRadiology
      ? values.rateOptions.map((option) => ({
          id: option.id || null,
          name: option.name.trim(),
          price: option.price,
          doctorCharge: option.doctorCharge,
          costPrice: option.costPrice,
        }))
      : [],
  };
}

/** A saved service as the form's starting values. */
export function toDiagnosticServiceFormValues(service: DiagnosticService): DiagnosticServiceFormValues {
  return {
    code: service.code,
    name: service.name,
    categoryId: service.categoryId,
    serviceType: service.serviceType,
    isOutsourced: service.isOutsourced,
    providerId: service.providerId ?? '',
    price: service.price,
    costPrice: service.costPrice,
    isActive: service.isActive,
    doctorCharge: service.doctorCharge ?? 0,
    bedsideEnabled: service.bedsidePrice != null,
    bedsidePrice: service.bedsidePrice ?? 0,
    bedsideDoctorCharge: service.bedsideDoctorCharge ?? 0,
    rateOptions: (service.rateOptions ?? []).map((option) => ({
      id: option.id ?? undefined,
      name: option.name,
      price: option.price,
      doctorCharge: option.doctorCharge,
      costPrice: option.costPrice,
    })),
  };
}

/**
 * How a lab package's running cost is set (ADR-140): its own figure, entered the way the tariff
 * sheet writes it — the package price and the profit ("CBC – 300, Profit – 120"), so the running
 * cost saved is price − profit — or the total of its tests' running costs (the package's
 * CostPrice is then saved as 0, which reports read as "use the tests' total").
 */
export type PackageProfitMode = 'profit' | 'tests';

export function packageProfitMode(pkg: { costPrice: number }): PackageProfitMode {
  return pkg.costPrice > 0 ? 'profit' : 'tests';
}

/** Total running cost of a package's tests, or null when any test isn't costed yet — a partial
 * total would understate the cost (same rule as referenceCache's resolveDiagnosticPackageCostPrice). */
export function testsRunningCostTotal(tests: { costPrice: number }[]): number | null {
  if (tests.length === 0) return null;
  let total = 0;
  for (const test of tests) {
    if (!(test.costPrice > 0)) return null;
    total += test.costPrice;
  }
  return total;
}

/** A package's price-and-profit breakdown for the chosen mode. */
export function packageBreakdown(price: unknown, mode: PackageProfitMode, profit: unknown, testsRunningCost: number | null): ProfitBreakdown {
  const p = amount(price);
  if (mode === 'profit') return computeProfit(p, p - amount(profit));
  return computeProfit(p, testsRunningCost ?? 0);
}

/** The CostPrice to save for a package: price − profit, or 0 for "use the tests' total". */
export function packageCostPrice(price: unknown, mode: PackageProfitMode, profit: unknown): number {
  return mode === 'profit' ? Math.max(0, amount(price) - amount(profit)) : 0;
}

/** Why an entered package profit can't be saved, or null when it's fine. */
export function validatePackageProfit(price: unknown, profit: unknown): string | null {
  const p = amount(price);
  const pr = amount(profit);
  if (pr < 0) return "Profit can't be negative.";
  if (pr >= p) return 'Profit must be less than the package price.';
  return null;
}
