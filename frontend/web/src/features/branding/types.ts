/**
 * Shape shared by the mock data layer (mockBrandingStore.ts) today and a future
 * real backend API — see the "Deferred: future backend phase" section of the
 * Theme & Branding plan. Keeping this shape stable means the admin form, the
 * live preview, and applyBrandingTokens() never need to change when a real
 * API replaces the mock store; only the repository implementation swaps.
 */
export interface BrandingConfig {
  hospitalName: string;
  appTitle: string;
  /** Shown alongside hospitalName on printed/exported clinical documents (e.g. the OPD
   * Consultation report) — optional, unlike hospitalName/appTitle. */
  address: string;
  phoneNumber: string;
  /** The Primary logo (application header, and the fallback for every other slot — see
   * logoDisplay.usePrimaryAsFallback). data: URI in the mock store; an absolute API URL
   * otherwise. Null = no custom logo uploaded. */
  logoUrl: string | null;
  /** Per-surface logos (see LOGO_SLOTS) — null = not configured, falls back per resolveBrandLogoUrl. */
  compactLogoUrl: string | null;
  loginLogoUrl: string | null;
  printLogoUrl: string | null;
  faviconUrl: string | null;
  logoDisplay: LogoDisplaySettings;
  /** Paper size, orientation and margin for printouts — see resolvePrintPageSetup. */
  printSettings: PrintSettings;
  fontFamily: FontFamily;
  fontSizeScale: FontSizeScale;
  iconSizeScale: IconSizeScale;
  /** Flat "--css-var-name" -> "H S% L%" maps, written directly onto :root by applyBrandingTokens(). */
  tokensLight: Record<string, string>;
  tokensDark: Record<string, string>;
}

/** Mirrors HMS.Modules.Branding.Contracts.BrandingLogoSlots — the `slot` of POST/DELETE
 * api/v1/branding/logo. */
export const LOGO_SLOTS = ['primary', 'compact', 'login', 'print', 'favicon'] as const;
export type LogoSlot = (typeof LOGO_SLOTS)[number];

/** CSS object-fit modes offered per logo. Only `contain` guarantees the image is never
 * distorted or cropped; `cover` crops to fill the box, `fill` stretches to it. */
export const LOGO_FITS = ['contain', 'cover', 'fill'] as const;
export type LogoFit = (typeof LOGO_FITS)[number];
export const LOGO_FIT_LABELS: Record<LogoFit, string> = { contain: 'Contain', cover: 'Cover', fill: 'Fill' };

/** Bounds for the Width/Height inputs — mirror BrandingService's LogoDisplay validation. */
export const LOGO_DIMENSION_LIMITS = { min: 16, maxWidth: 800, maxHeight: 400 } as const;

export interface LogoSlotDisplay {
  /** Logo box width/height in CSS px — independent, no aspect lock. */
  width: number;
  height: number;
  fit: LogoFit;
}

export interface LogoDisplaySettings {
  /** When on, a slot with no logo of its own shows the Primary logo before the bundled default. */
  usePrimaryAsFallback: boolean;
  slots: Record<LogoSlot, LogoSlotDisplay>;
}

/** Where each slot's logo shows up. Surfaces with a fixed size (the 64px top bar) still clamp
 * a configured width/height to their own box, so no setting can change the header layout. */
export const LOGO_SLOT_META: Record<LogoSlot, { label: string; usage: string; configKey: LogoUrlKey }> = {
  primary: { label: 'Primary Logo', usage: 'Used in application header', configKey: 'logoUrl' },
  compact: { label: 'Compact Logo', usage: 'Used in collapsed sidebar / mobile', configKey: 'compactLogoUrl' },
  login: { label: 'Login Logo', usage: 'Used in login screen', configKey: 'loginLogoUrl' },
  print: { label: 'Print Logo', usage: 'Used in invoices, reports, prescriptions and certificates', configKey: 'printLogoUrl' },
  favicon: { label: 'Favicon', usage: 'Used in browser tab', configKey: 'faviconUrl' },
};

export type LogoUrlKey = 'logoUrl' | 'compactLogoUrl' | 'loginLogoUrl' | 'printLogoUrl' | 'faviconUrl';

export const DEFAULT_LOGO_DISPLAY: LogoDisplaySettings = {
  usePrimaryAsFallback: true,
  slots: {
    primary: { width: 180, height: 40, fit: 'contain' },
    compact: { width: 48, height: 48, fit: 'contain' },
    login: { width: 220, height: 60, fit: 'contain' },
    print: { width: 220, height: 60, fit: 'contain' },
    favicon: { width: 32, height: 32, fit: 'contain' },
  },
};

/** Paper sizes offered for printouts, as CSS `@page size` keywords — mirror
 * HMS.Modules.Branding.Contracts.PrintPaperSizes. */
export const PAPER_SIZES = ['A4', 'A3', 'A5', 'Letter', 'Legal'] as const;
export type PaperSize = (typeof PAPER_SIZES)[number];
export const PAPER_SIZE_LABELS: Record<PaperSize, string> = {
  A4: 'A4 (210 × 297 mm)',
  A3: 'A3 (297 × 420 mm)',
  A5: 'A5 (148 × 210 mm)',
  Letter: 'Letter (8.5 × 11 in)',
  Legal: 'Legal (8.5 × 14 in)',
};

export const PRINT_ORIENTATIONS = ['portrait', 'landscape'] as const;
export type PrintOrientation = (typeof PRINT_ORIENTATIONS)[number];
export const PRINT_ORIENTATION_LABELS: Record<PrintOrientation, string> = { portrait: 'Portrait', landscape: 'Landscape' };

/** Bounds for the page margin input — mirror BrandingService's print settings validation. */
export const PRINT_MARGIN_LIMITS = { min: 0, max: 40 } as const;

/** Printouts that can use their own paper size/orientation instead of the default — mirror
 * HMS.Modules.Branding.Contracts.PrintDocumentTypes. A page passes its key to PrintPageStyle. */
export const PRINT_DOCUMENTS = ['patient', 'invoice', 'labReport', 'opdConsultation', 'report'] as const;
export type PrintDocument = (typeof PRINT_DOCUMENTS)[number];
export const PRINT_DOCUMENT_META: Record<PrintDocument, { label: string; usage: string }> = {
  patient: { label: 'Patient sheet', usage: 'Print on a patient’s page' },
  invoice: { label: 'Invoices', usage: 'Hospital invoices and receipts' },
  labReport: { label: 'Lab reports', usage: 'Laboratory test reports' },
  opdConsultation: { label: 'OPD consultation', usage: 'Consultation notes and prescriptions' },
  report: {
    label: 'Reports & registers',
    usage: 'Patient reports, pharmacy reports and registers, stock counts, supplier statements, purchase orders and receipts',
  },
};

export interface PrintPageSetup {
  paperSize: PaperSize;
  orientation: PrintOrientation;
  /** Page margin on every side, in mm. */
  marginMm: number;
}

/** A per-document override — null means "same as the default page". */
export interface PrintDocumentSetup {
  paperSize: PaperSize | null;
  orientation: PrintOrientation | null;
}

export interface PrintSettings {
  default: PrintPageSetup;
  documents: Partial<Record<PrintDocument, PrintDocumentSetup>>;
}

/** What every printout used before print settings existed (index.css's 12mm @page margin, and
 * the browser's own A4 portrait default). */
export const DEFAULT_PRINT_SETTINGS: PrintSettings = {
  default: { paperSize: 'A4', orientation: 'portrait', marginMm: 12 },
  documents: {},
};

export const FONT_FAMILIES = ['Inter', 'Roboto', 'OpenSans', 'Lato', 'Poppins'] as const;
export type FontFamily = (typeof FONT_FAMILIES)[number];

export const FONT_FAMILY_LABELS: Record<FontFamily, string> = {
  Inter: 'Inter',
  Roboto: 'Roboto',
  OpenSans: 'Open Sans',
  Lato: 'Lato',
  Poppins: 'Poppins',
};

/** CSS font-family stacks for each curated option — all fall back to system-ui/sans-serif. */
export const FONT_FAMILY_STACKS: Record<FontFamily, string> = {
  Inter: "Inter, 'Segoe UI', system-ui, -apple-system, sans-serif",
  Roboto: "Roboto, 'Segoe UI', system-ui, -apple-system, sans-serif",
  OpenSans: "'Open Sans', 'Segoe UI', system-ui, -apple-system, sans-serif",
  Lato: "Lato, 'Segoe UI', system-ui, -apple-system, sans-serif",
  Poppins: "Poppins, 'Segoe UI', system-ui, -apple-system, sans-serif",
};

export const FONT_SIZE_SCALES = ['sm', 'md', 'lg'] as const;
export type FontSizeScale = (typeof FONT_SIZE_SCALES)[number];

export const FONT_SIZE_SCALE_LABELS: Record<FontSizeScale, string> = {
  sm: 'Small (14px)',
  md: 'Medium (16px, default)',
  lg: 'Large (18px)',
};

export const FONT_SIZE_SCALE_PX: Record<FontSizeScale, string> = {
  sm: '14px',
  md: '16px',
  lg: '18px',
};

/** Only the top bar's own icon-only action buttons (Language, Notifications, Calendar,
 * Calculator, Tasks, Expenses Tracking, Profile) — not every icon app-wide, which would need
 * a much bigger, riskier sweep across compact tables/cards that assume a fixed icon size. */
export const ICON_SIZE_SCALES = ['sm', 'md', 'lg', 'xl'] as const;
export type IconSizeScale = (typeof ICON_SIZE_SCALES)[number];

export const ICON_SIZE_SCALE_LABELS: Record<IconSizeScale, string> = {
  sm: 'Small (18px)',
  md: 'Medium (20px, default)',
  lg: 'Large (24px)',
  xl: 'Extra Large (28px)',
};

export const ICON_SIZE_SCALE_PX: Record<IconSizeScale, string> = {
  sm: '18px',
  md: '20px',
  lg: '24px',
  xl: '28px',
};

/** Every token key the admin UI edits, grouped by section. Values are seeded/derived elsewhere (mockBrandingStore.ts). */
export const TOKEN_GROUPS = {
  core: [
    { key: '--background', label: 'Page background', pairedForeground: '--foreground' },
    { key: '--primary', label: 'Primary color', pairedForeground: '--primary-foreground' },
    { key: '--secondary', label: 'Secondary color', pairedForeground: '--secondary-foreground' },
    { key: '--border', label: 'Border color' },
    { key: '--accent', label: 'Hover / tint surface', pairedForeground: '--accent-foreground' },
    { key: '--link', label: 'Link color' },
    { key: '--icon', label: 'Icon color (neutral chrome icons)' },
  ],
  topBar: [{ key: '--header-bg', label: 'Top bar background', pairedForeground: '--header-foreground' }],
  // Flat list (no pairedForeground nesting) — every row stands on its own per the Left nav
  // tab's own redesigned layout (BrandingForm.tsx), unlike every other group here.
  leftNav: [
    // The whole panel (AppSidebar's bg-sidebar, incl. the empty area below the menu). Changing
    // it also sets the odd/even item colors below to match (BrandingForm's handleTokenChange),
    // so one pick recolors the full navbar; those two can still be set afterwards for stripes.
    { key: '--sidebar', label: 'Left nav background (full panel)' },
    { key: '--sidebar-odd-bg', label: 'Left nav background (odd items)' },
    { key: '--sidebar-even-bg', label: 'Left nav background (even items)' },
    { key: '--sidebar-foreground', label: 'Left nav text (default)' },
    { key: '--sidebar-border', label: 'Left nav border' },
    { key: '--sidebar-active-bg', label: 'Left nav active item background' },
    { key: '--sidebar-active-fg', label: 'Left nav active item text' },
    { key: '--sidebar-accent', label: 'Left nav hover background' },
  ],
  sectionHeaders: [
    { key: '--card-header-bg', label: 'Section/card header background', pairedForeground: '--card-header-foreground' },
    { key: '--page-banner-bg', label: 'Page banner (e.g. Reception & Registration)', pairedForeground: '--page-banner-foreground' },
  ],
  buttons: [
    { key: '--primary', label: 'Button — Primary', pairedForeground: '--primary-foreground' },
    { key: '--secondary', label: 'Button — Secondary', pairedForeground: '--secondary-foreground' },
    { key: '--success', label: 'Button — Success', pairedForeground: '--success-foreground' },
    { key: '--warning', label: 'Button — Warning', pairedForeground: '--warning-foreground' },
    { key: '--destructive', label: 'Button — Danger', pairedForeground: '--destructive-foreground' },
  ],
} as const;
