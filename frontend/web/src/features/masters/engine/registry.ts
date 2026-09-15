import { appointmentTypeConfig } from '../configs/appointmentType';
import { brandConfig } from '../configs/brand';
import { consultantConfig } from '../configs/consultant';
import { consultationTypeConfig } from '../configs/consultationType';
import { customerConfig } from '../configs/customer';
import { currencyConfig } from '../configs/currency';
import { departmentConfig } from '../configs/department';
import { designationConfig } from '../configs/designation';
import { diagnosisConfig } from '../configs/diagnosis';
import { diagnosticTestConfig } from '../configs/diagnosticTest';
import { manufacturerConfig } from '../configs/manufacturer';
import { paymentMethodConfig } from '../configs/paymentMethod';
import { paymentTermsConfig } from '../configs/paymentTerms';
import { productCategoryConfig } from '../configs/productCategory';
import { productGroupConfig } from '../configs/productGroup';
import { productSubCategoryConfig } from '../configs/productSubCategory';
import { stockAdjustmentReasonConfig } from '../configs/stockAdjustmentReason';
import { storageLocationConfig } from '../configs/storageLocation';
import { supplierConfig } from '../configs/supplier';
import { taxConfig } from '../configs/tax';
import { unitConversionConfig } from '../configs/unitConversion';
import { unitOfMeasureConfig } from '../configs/unitOfMeasure';
import { warehouseConfig } from '../configs/warehouse';
import { createMasterStore, type MasterStore } from './masterStoreFactory';
import type { MasterEntityConfig, MasterRecord } from './types';

/**
 * Every Masters entity from docs/03_Masters_ERD, in ERD legend order — this list (plus
 * each config's `section`) is the single source of truth the hub page, routes, and
 * stores are all generated from. There is no per-entity page/route/store code.
 */
export const MASTER_CONFIGS: MasterEntityConfig[] = [
  departmentConfig,
  designationConfig,
  consultantConfig,
  appointmentTypeConfig,
  consultationTypeConfig,
  diagnosisConfig,
  diagnosticTestConfig,
  productCategoryConfig,
  productSubCategoryConfig,
  productGroupConfig,
  brandConfig,
  manufacturerConfig,
  unitOfMeasureConfig,
  unitConversionConfig,
  taxConfig,
  warehouseConfig,
  storageLocationConfig,
  supplierConfig,
  customerConfig,
  currencyConfig,
  paymentTermsConfig,
  paymentMethodConfig,
  stockAdjustmentReasonConfig,
];

/** Section order for the hub page's tabs — grouped by the hospital's actual functional
 * modules (Hospital/Clinical, HR, Pharmacy & Inventory, Finance) rather than the ERD's more
 * abstract table-classification legend, so each tab reads as "the reference data that module
 * owns" instead of a generic schema grouping. */
export const MASTER_SECTIONS = ['Hospital Reference Data', 'HR', 'Pharmacy & Inventory', 'Finance'];

const configByKey = new Map(MASTER_CONFIGS.map((config) => [config.key, config]));
const storeByKey = new Map<string, MasterStore>(MASTER_CONFIGS.map((config) => [config.key, createMasterStore(config)]));

export function getMasterConfig(key: string | undefined): MasterEntityConfig | undefined {
  return key ? configByKey.get(key) : undefined;
}

export function getMasterStore(key: string | undefined): MasterStore | undefined {
  return key ? storeByKey.get(key) : undefined;
}

export function getAllMasterConfigs(): MasterEntityConfig[] {
  return MASTER_CONFIGS;
}

export function getDisplayLabel(config: MasterEntityConfig, record: MasterRecord): string {
  if (config.getDisplayLabel) return config.getDisplayLabel(record, resolveRecordLabel);
  if (config.nameField && record[config.nameField]) return String(record[config.nameField]);
  if (config.codeField && record[config.codeField]) return String(record[config.codeField]);
  return record.id;
}

/**
 * In-memory cache of records-by-id per entity, primed by useMasterOptionsQuery/
 * useMasterReferenceOptions as their react-query fetches resolve. resolveRecordLabel reads
 * from this cache rather than the (now async, HTTP-backed) store directly, since it's called
 * synchronously during render (table columns, reference <Select> option labels).
 */
const referenceCache = new Map<string, Map<string, MasterRecord>>();

export function primeReferenceCache(entityKey: string, records: MasterRecord[]): void {
  referenceCache.set(entityKey, new Map(records.map((record) => [record.id, record])));
}

/**
 * Resolves a reference field's id to a human-readable label, for table columns and form
 * context. Falls back to the raw id if the referenced entity's options haven't been fetched
 * yet — self-corrects on the next render once the priming query resolves.
 */
export function resolveRecordLabel(entityKey: string, id: string | undefined | null): string {
  if (!id) return '—';
  const config = configByKey.get(entityKey);
  const record = referenceCache.get(entityKey)?.get(id);
  if (!config || !record) return String(id);
  return getDisplayLabel(config, record);
}

/**
 * Resolves a reference field's id to its CostPrice, for margin/profit calculations (see
 * features/billing/billingCalculations.ts's resolveItemCostPrice) — reads from the same
 * synchronous reference cache resolveRecordLabel uses. Returns null (not 0) both when the
 * record hasn't been resolved yet AND when its CostPrice is genuinely 0 ("not yet costed",
 * per DiagnosticTest/ConsultationType.CostPrice's own doc comments) — a caller must treat
 * either case as "cost unknown," never as "free," so collapsing them to the same null return
 * keeps that distinction from being lost at the call site.
 */
export function resolveRecordCostPrice(entityKey: string, id: string | undefined | null): number | null {
  if (!id) return null;
  const record = referenceCache.get(entityKey)?.get(id);
  const cost = record?.costPrice;
  return typeof cost === 'number' && cost > 0 ? cost : null;
}

/**
 * Resolves what a specific consultant charges for a specific consultation type, for
 * Consultation-line profit reporting (see billingCalculations.ts's resolveItemCostPrice) —
 * reads the same synchronous `consultant` reference cache resolveRecordLabel uses, primed by
 * Masters config's `arrayItemKeys: { id: 'consultationTypeId', amount: 'consultantCharge' }`
 * (see configs/consultant.ts). Unlike resolveRecordCostPrice, 0 is a genuine, real charge here
 * (a consultant who takes none of the fee) rather than "not yet costed" — so only a missing
 * consultant/id, an unresolved cache, or a consultation type the consultant doesn't actually
 * offer collapse to null; an explicit 0 is returned as-is.
 */
export function resolveConsultantCharge(consultantId: string | undefined | null, consultationTypeId: string | undefined | null): number | null {
  if (!consultantId || !consultationTypeId) return null;
  const consultant = referenceCache.get('consultant')?.get(consultantId);
  const charges = consultant?.consultationTypeCharges;
  if (!Array.isArray(charges)) return null;
  const entry = charges.find((charge) => (charge as { consultationTypeId?: string }).consultationTypeId === consultationTypeId);
  const charge = (entry as { consultantCharge?: unknown } | undefined)?.consultantCharge;
  return typeof charge === 'number' ? charge : null;
}
