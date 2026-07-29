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
  /** data: URI in the mock store; a server-relative URL once a backend exists. Null = no custom logo uploaded. */
  logoUrl: string | null;
  fontFamily: FontFamily;
  fontSizeScale: FontSizeScale;
  /** Flat "--css-var-name" -> "H S% L%" maps, written directly onto :root by applyBrandingTokens(). */
  tokensLight: Record<string, string>;
  tokensDark: Record<string, string>;
}

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
  leftNav: [
    { key: '--sidebar', label: 'Left nav background', pairedForeground: '--sidebar-foreground' },
    { key: '--sidebar-border', label: 'Left nav border' },
    { key: '--sidebar-active-bg', label: 'Left nav active item background', pairedForeground: '--sidebar-active-fg' },
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
