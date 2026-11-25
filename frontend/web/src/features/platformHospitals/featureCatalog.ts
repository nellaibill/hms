import type { SubFeatureResponse } from '@hms/shared';
import {
  Ambulance,
  Award,
  BarChart3,
  BedDouble,
  Briefcase,
  CalendarDays,
  ClipboardList,
  CloudCog,
  Database,
  Droplets,
  FileCheck,
  FileText,
  FlaskConical,
  FolderOpen,
  HeartPulse,
  History,
  LayoutGrid,
  type LucideIcon,
  MessageSquare,
  Microscope,
  Package,
  Palette,
  Pill,
  Receipt,
  ScanLine,
  Scissors,
  Settings,
  Sparkles,
  Stethoscope,
  Users,
  Wallet,
  Wrench,
} from 'lucide-react';

export type FeatureCategoryId = 'core' | 'clinical' | 'diagnostics' | 'operations' | 'billing' | 'support' | 'ai' | 'other';

interface FeatureCategory {
  id: FeatureCategoryId;
  label: string;
  icon: LucideIcon;
  /** Tile colour for this category's feature icons — full class strings so Tailwind's JIT sees them. */
  tint: string;
}

/** Display order of the category rail/sections. "other" catches any key the backend adds before this map is updated. */
export const FEATURE_CATEGORIES: readonly FeatureCategory[] = [
  { id: 'core', label: 'Core', icon: LayoutGrid, tint: 'bg-blue-500/10 text-blue-600 dark:text-blue-400' },
  { id: 'clinical', label: 'Clinical', icon: HeartPulse, tint: 'bg-emerald-500/10 text-emerald-600 dark:text-emerald-400' },
  { id: 'diagnostics', label: 'Diagnostics & Pharmacy', icon: FlaskConical, tint: 'bg-violet-500/10 text-violet-600 dark:text-violet-400' },
  { id: 'operations', label: 'Operations', icon: Wrench, tint: 'bg-amber-500/10 text-amber-600 dark:text-amber-400' },
  { id: 'billing', label: 'Billing & Finance', icon: Receipt, tint: 'bg-rose-500/10 text-rose-600 dark:text-rose-400' },
  { id: 'support', label: 'Support & Communication', icon: MessageSquare, tint: 'bg-sky-500/10 text-sky-600 dark:text-sky-400' },
  { id: 'ai', label: 'AI & Advanced', icon: Sparkles, tint: 'bg-fuchsia-500/10 text-fuchsia-600 dark:text-fuchsia-400' },
  { id: 'other', label: 'Other', icon: Package, tint: 'bg-muted text-muted-foreground' },
];

interface FeatureMeta {
  label: string;
  description: string;
  icon: LucideIcon;
  category: FeatureCategoryId;
}

/**
 * Display metadata for FeatureCatalog keys (HMS.Shared.Kernel.FeatureCatalog) — the "which
 * modules does this hospital have" set, distinct from ROLE_MODULES (the RBAC permission-category
 * set). The actual key list, mandatory subset and dependencies always come from the API response
 * (TenantFeaturesResponse); this map only supplies label, description, icon and category.
 * A key missing here still renders (raw key, "Other" category), so a new backend key never
 * disappears from the dialog.
 */
export const FEATURE_META: Record<string, FeatureMeta> = {
  identity: { label: 'Identity & Users', description: 'User management, authentication', icon: Users, category: 'core' },
  masters: { label: 'Master Data', description: 'Departments, designations, references', icon: Database, category: 'core' },
  patients: { label: 'Patients', description: 'Patient registration and records', icon: Users, category: 'core' },
  documents: { label: 'Documents', description: 'Document templates and storage', icon: FileText, category: 'core' },
  branding: { label: 'Branding', description: 'Logo, theme, hospital settings', icon: Palette, category: 'core' },
  settings: { label: 'Settings', description: 'Hospital settings pages — Roles & Permissions always stays on', icon: Settings, category: 'core' },

  opd: { label: 'Out Patient Department (OPD)', description: 'Outpatient queue and scheduling', icon: Stethoscope, category: 'clinical' },
  'opd-consultation': { label: 'OPD Consultation', description: 'Consultation notes, diagnosis, plan', icon: ClipboardList, category: 'clinical' },
  ipd: { label: 'IPD (In-Patient Department)', description: 'Admissions, beds, nursing, orders', icon: BedDouble, category: 'clinical' },
  'discharge-summary': { label: 'Discharge Summary', description: 'Clinical discharge documents', icon: FileCheck, category: 'clinical' },
  ot: { label: 'Operation Theatre (OT)', description: 'OT scheduling and records', icon: Scissors, category: 'clinical' },
  'e-mrd': { label: 'E-MRD', description: 'Electronic medical records department', icon: FolderOpen, category: 'clinical' },

  laboratory: { label: 'Laboratory Workflow', description: 'Lab orders, samples and results', icon: FlaskConical, category: 'diagnostics' },
  'central-laboratory': { label: 'Central Laboratory', description: 'Test, package and tariff catalog', icon: Microscope, category: 'diagnostics' },
  radiology: { label: 'Radiology', description: 'Radiology orders and reports', icon: ScanLine, category: 'diagnostics' },
  'blood-bank': { label: 'Blood Bank', description: 'Blood inventory and requests', icon: Droplets, category: 'diagnostics' },
  pharmacy: { label: 'Pharmacy', description: 'Dispensing and stock receipts', icon: Pill, category: 'diagnostics' },
  products: { label: 'Products & Inventory', description: 'Product catalog and batches', icon: Package, category: 'diagnostics' },

  hr: { label: 'HR & Staffing', description: 'Staff, shifts and rosters', icon: Briefcase, category: 'operations' },
  calendar: { label: 'Calendar', description: 'Hospital calendar and events', icon: CalendarDays, category: 'operations' },
  ambulance: { label: 'Ambulance', description: 'Ambulance dispatch and transport', icon: Ambulance, category: 'operations' },
  'activity-log': { label: 'Activity Log', description: 'Audit trail of user actions', icon: History, category: 'operations' },
  'cloud-sync': { label: 'Cloud Sync', description: 'Send patient changes from this server to the cloud', icon: CloudCog, category: 'operations' },

  billing: { label: 'Billing', description: 'Invoices and payments', icon: Receipt, category: 'billing' },
  finance: { label: 'Accounts and Finance', description: 'Ledger and finance reports', icon: Wallet, category: 'billing' },
  reports: { label: 'Reports', description: 'Operational and revenue reports', icon: BarChart3, category: 'billing' },
  'records-and-certificates': { label: 'Records and Certificates', description: 'Medical records and certificates', icon: Award, category: 'billing' },

  'messages-and-notifications': { label: 'Messages and Notifications', description: 'Messaging, email and SMS alerts', icon: MessageSquare, category: 'support' },

  'opd-ambient-notes': { label: 'OPD Ambient Note Generation (AI)', description: 'Dictate or paste a consultation, AI drafts the note', icon: Sparkles, category: 'ai' },
  'discharge-summary-ai-draft': { label: 'Discharge Summary AI Draft', description: 'AI drafts a summary from IPD records', icon: Sparkles, category: 'ai' },
};

export const FEATURE_LABELS: Record<string, string> = Object.fromEntries(Object.entries(FEATURE_META).map(([key, meta]) => [key, meta.label]));

interface SubFeatureMeta {
  label: string;
  description: string;
  /** Heading the part is listed under inside its module, e.g. HR's "Duty Roster" — mirrors how
   * the hospital app's own hub page sections them. */
  group?: string;
}

/**
 * Display metadata for sub-features (HMS.Shared.Kernel.FeatureCatalog.SubFeatures, ADR-145) —
 * the parts of a module a platform admin can switch off on their own. Keys, parents and
 * dependencies come from the API; a key missing here still renders under its raw key.
 */
export const SUB_FEATURE_META: Record<string, SubFeatureMeta> = {
  'hr.dashboard': { label: 'HR Dashboard', description: 'Headcount, attendance and leave at a glance', group: 'Employee Management' },
  'hr.employees': { label: 'Employees', description: 'Employee directory — profiles, department and status', group: 'Employee Management' },
  'hr.attendance': { label: 'Attendance', description: 'Daily check-in/check-out records', group: 'Employee Management' },
  'hr.leave-requests': { label: 'Leave Requests', description: 'Submit, approve and reject leave', group: 'Employee Management' },
  'hr.leave-types': { label: 'Leave Types', description: 'Leave types, paid status and yearly limits', group: 'Employee Management' },
  'hr.shifts': { label: 'Shift Management', description: 'Shift codes, timings, breaks and grace periods', group: 'Duty Roster' },
  'hr.staff-availability': { label: 'Staff Availability', description: 'When staff are available or away, and why', group: 'Duty Roster' },
  'hr.weekly-rosters': { label: 'Weekly Roster', description: 'Per-department weekly rosters and publishing', group: 'Duty Roster' },
  'hr.shift-assignments': { label: 'Shift Assignments', description: 'Assign staff to shifts on roster dates', group: 'Duty Roster' },
  'hr.shift-swap-requests': { label: 'Shift Swap Requests', description: 'Staff requests to swap shifts', group: 'Duty Roster' },
  'hr.monthly-calendar': { label: 'Monthly Calendar', description: 'A month of weekly rosters at a glance', group: 'Duty Roster' },

  'settings.reference-data': { label: 'Hospital Reference Data', description: 'The masters hub — departments, designations, lab catalog, taxes and more' },
  'settings.branding': { label: 'Theme & Branding', description: 'Hospital identity, logos, colors, fonts and print layout' },
  'settings.reports-visibility': { label: 'Reports Visibility', description: 'Choose which Accounts & Finance reports show' },
  'settings.patient-fields': { label: 'Patient Registration Fields', description: 'Choose which registration fields are mandatory' },
  'settings.billing-approvals': { label: 'Billing Approvals', description: 'Discount and bill-cancellation approval rules' },
  'settings.backup': { label: 'Backup', description: 'Download the daily database backup and files zip' },

  'settings.branding.hospital-info': { label: 'Hospital information', description: 'Name, application title, address and phone' },
  'settings.branding.logo': { label: 'Logo configuration', description: 'Upload and size the header, compact, login and print logos' },
  'settings.branding.core-colors': { label: 'Core colors', description: 'Page, primary, secondary, border and link colors' },
  'settings.branding.top-bar': { label: 'Top bar', description: 'Top bar background and text colors' },
  'settings.branding.left-nav': { label: 'Left nav', description: 'Left navigation menu colors' },
  'settings.branding.section-headers': { label: 'Section headers', description: 'Card header and page banner colors' },
  'settings.branding.buttons': { label: 'Buttons', description: 'Primary, success, warning and danger button colors' },
  'settings.branding.typography': { label: 'Typography', description: 'Font family, base font size and top bar icon size' },
  'settings.branding.print': { label: 'Print', description: 'Paper size and margins for printouts' },

  'patients.bulk-import': { label: 'Bulk Patient Import', description: 'Register many patients from an Excel file, plus Import History' },

  'cloud-sync.controls': { label: 'Sync controls', description: 'Sync now, Pause/Resume, and retry or resolve failed changes' },
  'cloud-sync.job-dashboard': { label: 'Job dashboard', description: 'Open the background job dashboard from Cloud Sync' },
};

export function featureLabel(key: string): string {
  return FEATURE_META[key]?.label ?? SUB_FEATURE_META[key]?.label ?? key;
}

/** The direct children of a module or sub-feature, in catalog order. */
export function childrenOf(parent: string, subFeatures: readonly SubFeatureResponse[]): string[] {
  return subFeatures.filter((sub) => sub.parent === parent).map((sub) => sub.key);
}

/** Every sub-feature under a module or sub-feature, at any depth. */
export function descendantsOf(parent: string, subFeatures: readonly SubFeatureResponse[]): string[] {
  return childrenOf(parent, subFeatures).flatMap((child) => [child, ...descendantsOf(child, subFeatures)]);
}

/** Children grouped under their SUB_FEATURE_META heading, keeping catalog order; ungrouped
 * children come first under an empty heading. */
export function groupChildren(children: readonly string[]): { group: string; keys: string[] }[] {
  const groups: { group: string; keys: string[] }[] = [];
  for (const key of children) {
    const group = SUB_FEATURE_META[key]?.group ?? '';
    const existing = groups.find((g) => g.group === group);
    if (existing) existing.keys.push(key);
    else groups.push({ group, keys: [key] });
  }
  return groups;
}

export function featureCategoryOf(key: string): FeatureCategoryId {
  return FEATURE_META[key]?.category ?? 'other';
}

/** Non-empty categories, in display order, each with its feature keys (keeping the input order). */
export function groupByCategory(keys: readonly string[]): { category: FeatureCategory; keys: string[] }[] {
  return FEATURE_CATEGORIES.map((category) => ({ category, keys: keys.filter((key) => featureCategoryOf(key) === category.id) })).filter(
    (group) => group.keys.length > 0,
  );
}

/** Keys the given key requires, directly or through another requirement. */
function requirementsOf(key: string, dependencies: Record<string, string[]>): Set<string> {
  const found = new Set<string>();
  const pending = [...(dependencies[key] ?? [])];
  while (pending.length > 0) {
    const next = pending.pop()!;
    if (found.has(next)) continue;
    found.add(next);
    pending.push(...(dependencies[next] ?? []));
  }
  return found;
}

/**
 * Turns a feature on or off, keeping the set consistent with the dependency rules the backend
 * enforces: enabling also enables everything the feature requires; disabling also disables every
 * feature that requires it. Mandatory keys are never removed.
 */
export function withFeatureToggled(
  current: ReadonlySet<string>,
  key: string,
  enabled: boolean,
  dependencies: Record<string, string[]>,
  mandatory: readonly string[],
): Set<string> {
  const next = new Set(current);
  if (enabled) {
    next.add(key);
    for (const required of requirementsOf(key, dependencies)) next.add(required);
  } else {
    next.delete(key);
    for (const dependent of Object.keys(dependencies)) {
      if (requirementsOf(dependent, dependencies).has(key)) next.delete(dependent);
    }
  }
  for (const key of mandatory) next.add(key);
  return next;
}

/** Every feature except the AI ones, which send data to a third-party provider and so stay opt-in. */
export function recommendedFeatures(all: readonly string[], mandatory: readonly string[]): Set<string> {
  return new Set([...all.filter((key) => featureCategoryOf(key) !== 'ai'), ...mandatory]);
}

/** Mirrors HMS.Shared.Kernel.FeatureCatalog.Optional — the only keys a platform admin can
 * choose at hospital-creation time (mandatory ones are always included server-side).
 *
 * IMPORTANT — keep this in sync with FeatureCatalog.Optional by hand: this is a hardcoded
 * local list, not fetched from the API (unlike HospitalFeaturesEditor.tsx's post-creation
 * "Manage Features" screen, which reads live from TenantFeaturesResponse.allFeatures). It
 * already drifted out of sync once — "laboratory" was added to the backend catalog without a
 * matching update here, so newly-registered hospitals had no way to enable it at creation
 * time even though the backend would have happily accepted it. Add every new
 * FeatureCatalog.Optional key here the same day it's added on the backend. */
export const OPTIONAL_FEATURE_KEYS = [
  'hr',
  'calendar',
  'products',
  'ipd',
  'opd',
  'ot',
  'pharmacy',
  'central-laboratory',
  'laboratory',
  'radiology',
  'blood-bank',
  'ambulance',
  'finance',
  'records-and-certificates',
  'activity-log',
  'messages-and-notifications',
  'reports',
  'e-mrd',
  'discharge-summary',
  'opd-ambient-notes',
  'discharge-summary-ai-draft',
  'cloud-sync',
];
