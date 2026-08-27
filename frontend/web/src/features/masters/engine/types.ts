import type { LucideIcon } from 'lucide-react';

/**
 * Masters (Reference Data, see docs/03_Masters_ERD) is a config-driven engine — every
 * entity from the ERD is described by a MasterEntityConfig and rendered through the same
 * generic list/form pages; there is no per-entity page code. The data layer
 * (engine/masterStoreFactory.ts) talks to the real /api/v1/masters/* backend.
 */

export type MasterFieldType = 'text' | 'textarea' | 'number' | 'decimal' | 'boolean' | 'select' | 'reference';

export interface MasterSelectOption {
  value: string;
  label: string;
}

export interface MasterFieldDef {
  /** Property name on the record. */
  key: string;
  label: string;
  type: MasterFieldType;
  required?: boolean;
  /** Show this field as its own column in the list table (default: true for text/number/boolean/select/reference, false for textarea). */
  showInTable?: boolean;
  /**
   * Opts this field out of the automatic "already in use" uniqueness check that otherwise
   * applies to whichever field is config.codeField ?? config.nameField (see MasterForm's
   * makeCodeUniquenessValidator) — for an entity like Consultant, where duplicate values are
   * expected and legitimate (two consultants can share a display name), not a data-entry error.
   */
  skipUniquenessCheck?: boolean;
  /** Static options for type: 'select'. */
  options?: MasterSelectOption[];
  /** Entity key (MasterEntityConfig.key) this field references, for type: 'reference'. */
  referenceEntityKey?: string;
  /**
   * When set, only reference options whose [scopeField] matches the current form's
   * value for [scopeField] are offered — e.g. a storage location's parent must be in
   * the same warehouse.
   */
  referenceScopeField?: string;
  /** Exclude the record currently being edited from reference options (self-referencing parents). */
  excludeSelf?: boolean;
  placeholder?: string;
  defaultValue?: unknown;
  min?: number;
  max?: number;
  step?: number;
  helpText?: string;
}

export interface MasterEntityConfig {
  /** Unique slug — used as the route param, storage key, and registry key. */
  key: string;
  label: string;
  labelPlural: string;
  description: string;
  icon: LucideIcon;
  /** Grouping shown on the Masters hub page — mirrors the ERD legend's table classification. */
  section: string;
  /** The field treated as the record's unique business code, if any (used for search + uniqueness). */
  codeField?: string;
  /** The field treated as the record's primary display name. */
  nameField?: string;
  /** Field whose value scopes codeField's uniqueness check (e.g. storage_location codes are unique per warehouse). */
  uniqueScopeField?: string;
  /**
   * Overrides the table/banner display label for records that have no natural
   * name/code (e.g. unit_conversion, shown as "kg → g" instead of a code).
   */
  getDisplayLabel?: (record: MasterRecord, resolveReference: (entityKey: string, id: string | undefined) => string) => string;
  /** Cross-field rule beyond single-field validation (e.g. unit_conversion's from != to). Return an error message, or undefined if valid. */
  validateForm?: (values: Record<string, unknown>) => string | undefined;
  fields: MasterFieldDef[];
}

/** Every Masters record shares these columns regardless of entity — mirrors HMS.Shared.Kernel.Entity's audit/soft-delete columns, minus the actor/version columns which aren't user-editable. */
export interface MasterRecord {
  id: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  [key: string]: unknown;
}

export interface MasterListQuery {
  page?: number;
  pageSize?: number;
  sort?: string;
  search?: string;
  isActive?: boolean;
}

export interface PaginationMeta {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface PagedMasters {
  items: MasterRecord[];
  meta: PaginationMeta;
}
