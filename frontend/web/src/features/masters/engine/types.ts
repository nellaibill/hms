import type { LucideIcon } from 'lucide-react';
import type { ReactNode } from 'react';

/**
 * Masters (Reference Data, see docs/03_Masters_ERD) is a config-driven engine — every
 * entity from the ERD is described by a MasterEntityConfig and rendered through the same
 * generic list/form pages; there is no per-entity page code. The data layer
 * (engine/masterStoreFactory.ts) talks to the real /api/v1/masters/* backend.
 */

export type MasterFieldType =
  | 'text'
  | 'textarea'
  | 'number'
  | 'decimal'
  | 'boolean'
  | 'select'
  | 'reference'
  | 'time'
  | 'radio-card'
  | 'day-checkboxes';

export interface MasterSelectOption {
  value: string;
  label: string;
  /** Shown on a 'radio-card' option's card, under its label. */
  description?: string;
  /** Shown on a 'radio-card' option's card, in a small icon chip above/beside the label. */
  icon?: LucideIcon;
}

/** A small colored callout box — used beside a 'radio-card' field's options
 * (MasterFieldDef.infoBox) and at the bottom of a MasterFieldGroup's card (MasterFieldGroup.infoText). */
export interface MasterInfoBox {
  title?: string;
  text: string;
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
  /** 'radio-card' only — a callout shown beside the option cards (see MasterInfoBox). */
  infoBox?: MasterInfoBox;
  /**
   * 'time' only — when set AND the next field declared in the same MasterFieldGroup.fieldKeys
   * is also type 'time', the two render as one connected "label + start + to + end" control
   * (e.g. "Visiting Hours") instead of two separate fields. Data-wise each field still submits
   * independently — this is presentation-only.
   */
  rangeLabel?: string;
}

/**
 * Groups a subset of `fields` into their own titled/iconed Card instead of the default single
 * flat card — opt-in via MasterEntityConfig.fieldGroups. Every field key must appear in exactly
 * one group when a config declares any groups at all (MasterForm falls back to the classic
 * single-card layout when fieldGroups is omitted, so every entity that doesn't need this keeps
 * rendering exactly as before).
 */
export interface MasterFieldGroup {
  key: string;
  label: string;
  icon?: LucideIcon;
  fieldKeys: string[];
  /** Callout shown at the bottom of this group's card, below its fields. */
  infoText?: string;
  /** Renders this group's fields as columns separated by a vertical rule, instead of the
   * default plain flex-wrap row — for a form section that reads as genuinely side-by-side
   * comparisons (e.g. "Available Days" beside "Visiting Hours"). */
  dividedColumns?: boolean;
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
  /**
   * Read-only columns derived from more than one field (e.g. profit margin = price − cost)
   * that don't correspond to a real, editable form input — shown in the list table only,
   * appended after the regular fields and never rendered by MasterForm. Kept entirely separate
   * from `fields` rather than a new field type so a computed value can never accidentally end
   * up editable or included in a create/update request body.
   */
  extraColumns?: MasterExtraColumn[];
  /** Opt-in sectioned-card layout (see MasterFieldGroup) — omit to keep the default single
   * flat-card layout every other Masters entity already uses. */
  fieldGroups?: MasterFieldGroup[];
}

export interface MasterExtraColumn {
  key: string;
  label: string;
  render: (record: MasterRecord) => ReactNode;
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
