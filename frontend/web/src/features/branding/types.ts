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

/** Non-cropping object-fit modes only — no logo is ever stretched or cut off. */
export const LOGO_FITS = ['contain', 'scale-down'] as const;
export type LogoFit = (typeof LOGO_FITS)[number];
export const LOGO_FIT_LABELS: Record<LogoFit, string> = { contain: 'Contain', 'scale-down': 'Scale down (never enlarge)' };

export interface LogoSlotDisplay {
  /** Logo box height in CSS px. */
  height: number;
  fit: LogoFit;
}

export interface LogoDisplaySettings {
  /** When on, a slot with no logo of its own shows the Primary logo before the bundled default. */
  usePrimaryAsFallback: boolean;
  slots: Record<LogoSlot, LogoSlotDisplay>;
}

/** Where each slot's logo shows up, and the display heights offered for it. The Primary
 * logo's heights are capped at the top bar's own 64px height (h-16), so a configured size can
 * never make the header taller. */
export const LOGO_SLOT_META: Record<LogoSlot, { label: string; usage: string; heights: readonly number[]; configKey: LogoUrlKey }> = {
  primary: { label: 'Primary Logo', usage: 'Used in application header', heights: [32, 40, 48, 56, 64], configKey: 'logoUrl' },
  compact: { label: 'Compact Logo', usage: 'Used in collapsed sidebar / mobile', heights: [24, 32, 40], configKey: 'compactLogoUrl' },
  login: { label: 'Login Logo', usage: 'Used in login screen', heights: [40, 48, 64, 80, 96], configKey: 'loginLogoUrl' },
  print: { label: 'Print Logo', usage: 'Used in invoices, reports, prescriptions and certificates', heights: [40, 48, 64, 72, 80], configKey: 'printLogoUrl' },
  favicon: { label: 'Favicon', usage: 'Used in browser tab', heights: [32], configKey: 'faviconUrl' },
};

export type LogoUrlKey = 'logoUrl' | 'compactLogoUrl' | 'loginLogoUrl' | 'printLogoUrl' | 'faviconUrl';

export const DEFAULT_LOGO_DISPLAY: LogoDisplaySettings = {
  usePrimaryAsFallback: true,
  slots: {
    primary: { height: 64, fit: 'contain' },
    compact: { height: 40, fit: 'contain' },
    login: { height: 40, fit: 'contain' },
    print: { height: 64, fit: 'contain' },
    favicon: { height: 32, fit: 'contain' },
  },
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
