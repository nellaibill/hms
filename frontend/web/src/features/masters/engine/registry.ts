import { appointmentTypeConfig } from '../configs/appointmentType';
import { brandConfig } from '../configs/brand';
import { consultantConfig } from '../configs/consultant';
import { consultationTypeConfig } from '../configs/consultationType';
import { customerConfig } from '../configs/customer';
import { currencyConfig } from '../configs/currency';
import { departmentConfig } from '../configs/department';
import { designationConfig } from '../configs/designation';
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

/** Section order for the hub page — mirrors the ERD's "Table Classification" legend. */
export const MASTER_SECTIONS = [
  'Hospital Reference Data',
  'Product Classification',
  'Brand & Manufacturer',
  'Units & Tax',
  'Warehouse & Location',
  'Business Partners',
  'Finance & Payment',
  'Inventory Lookup',
];

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
