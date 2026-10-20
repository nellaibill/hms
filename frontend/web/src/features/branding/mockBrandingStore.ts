import { branding } from '@/config/branding';
import {
  DEFAULT_LOGO_DISPLAY,
  DEFAULT_PRINT_SETTINGS,
  type BrandingConfig,
  type FontFamily,
  type FontSizeScale,
  type IconSizeScale,
} from './types';

const STORAGE_KEY = 'hms-branding-config';

/**
 * Default token set — a hand-copied snapshot of the LHS (Lakshmi Hospitals) tenant's saved
 * theme, taken from production on 2026-10-20. It is the app's built-in theme: index.css's
 * :root/.dark blocks and applyBrandingTokens()'s loading-state fallback paint exactly these
 * values, so LHS never sees a different theme flash in while its saved branding is still
 * being fetched. It is also the seed for a first-ever load and the target of "Reset to
 * default theme". Kept in exact sync with index.css and the backend's BrandingDefaults.cs —
 * if one changes, the others should too.
 */
export const DEFAULT_TOKENS_LIGHT: Record<string, string> = {
  '--background': '0 0% 100%',
  '--foreground': '0 3% 14%',
  '--card': '0 0% 100%',
  '--card-foreground': '222 47% 11%',
  '--card-header-bg': '0 0% 100%',
  '--card-header-foreground': '222 47% 11%',
  '--page-banner-bg': '38 100% 50%',
  '--page-banner-foreground': '0 0% 100%',
  '--popover': '0 0% 100%',
  '--popover-foreground': '222 47% 11%',
  '--primary': '38 93% 57%',
  '--primary-foreground': '0 0% 100%',
  '--secondary': '214 31% 87%',
  '--secondary-foreground': '220 3% 17%',
  '--muted': '210 30% 97%',
  '--muted-foreground': '215 16% 47%',
  '--accent': '0 0% 90%',
  '--accent-foreground': '228 7% 59%',
  '--link': '209 69% 65%',
  '--icon': '215 16% 47%',
  '--destructive': '356 100% 47%',
  '--destructive-foreground': '0 0% 100%',
  '--success': '152 100% 38%',
  '--success-foreground': '0 0% 100%',
  '--warning': '0 76% 77%',
  '--warning-foreground': '222 47% 11%',
  '--info': '199 89% 42%',
  '--info-foreground': '0 0% 100%',
  '--border': '0 0% 83%',
  '--input': '214 28% 91%',
  '--ring': '210 82% 45%',
  '--sidebar': '72 100% 99%',
  '--sidebar-foreground': '222 39% 18%',
  '--sidebar-border': '0 14% 67%',
  '--sidebar-accent': '210 40% 95%',
  '--sidebar-odd-bg': '72 100% 99%',
  '--sidebar-even-bg': '72 100% 99%',
  '--sidebar-active-bg': '41 100% 86%',
  '--sidebar-active-fg': '210 82% 40%',
  '--header-bg': '0 0% 44%',
  '--header-foreground': '0 0% 100%',
};

export const DEFAULT_TOKENS_DARK: Record<string, string> = {
  '--background': '222 47% 7%',
  '--foreground': '210 40% 98%',
  '--card': '222 40% 10%',
  '--card-foreground': '210 40% 98%',
  '--card-header-bg': '222 40% 10%',
  '--card-header-foreground': '210 40% 98%',
  '--page-banner-bg': '196 86% 58%',
  '--page-banner-foreground': '0 0% 100%',
  '--popover': '222 40% 10%',
  '--popover-foreground': '210 40% 98%',
  '--primary': '210 82% 45%',
  '--primary-foreground': '0 0% 100%',
  '--secondary': '217 27% 17%',
  '--secondary-foreground': '210 30% 92%',
  '--muted': '217 27% 17%',
  '--muted-foreground': '215 18% 68%',
  '--accent': '210 60% 20%',
  '--accent-foreground': '210 90% 85%',
  '--link': '210 82% 45%',
  '--icon': '215 18% 68%',
  '--destructive': '356 85% 64%',
  '--destructive-foreground': '222 47% 9%',
  '--success': '152 48% 52%',
  '--success-foreground': '222 47% 9%',
  '--warning': '42 96% 62%',
  '--warning-foreground': '222 47% 9%',
  '--info': '199 89% 62%',
  '--info-foreground': '222 47% 9%',
  '--border': '217 24% 20%',
  '--input': '217 24% 20%',
  '--ring': '210 82% 45%',
  '--sidebar': '222 44% 8%',
  '--sidebar-foreground': '210 30% 92%',
  '--sidebar-border': '217 24% 20%',
  '--sidebar-accent': '217 30% 16%',
  '--sidebar-odd-bg': '222 44% 8%',
  '--sidebar-even-bg': '222 40% 10%',
  '--sidebar-active-bg': '210 60% 20%',
  '--sidebar-active-fg': '210 90% 72%',
  '--header-bg': '210 82% 45%',
  '--header-foreground': '0 0% 100%',
};

/** LHS's saved typography — Medium text, Large top-bar icons. */
export const DEFAULT_FONT_FAMILY = 'Inter' satisfies FontFamily;
export const DEFAULT_FONT_SIZE_SCALE = 'md' satisfies FontSizeScale;
export const DEFAULT_ICON_SIZE_SCALE = 'lg' satisfies IconSizeScale;

function defaultConfig(): BrandingConfig {
  return {
    hospitalName: branding.hospitalName,
    appTitle: branding.systemName,
    address: '',
    phoneNumber: '',
    logoUrl: null,
    compactLogoUrl: null,
    loginLogoUrl: null,
    printLogoUrl: null,
    faviconUrl: null,
    logoDisplay: DEFAULT_LOGO_DISPLAY,
    printSettings: DEFAULT_PRINT_SETTINGS,
    fontFamily: DEFAULT_FONT_FAMILY,
    fontSizeScale: DEFAULT_FONT_SIZE_SCALE,
    iconSizeScale: DEFAULT_ICON_SIZE_SCALE,
    tokensLight: { ...DEFAULT_TOKENS_LIGHT },
    tokensDark: { ...DEFAULT_TOKENS_DARK },
  };
}

function readStored(): BrandingConfig | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return null;
    const parsed = JSON.parse(raw) as Partial<BrandingConfig>;
    // Merge over defaults so a config saved before a new token/field existed
    // never ends up missing a key the UI expects.
    const defaults = defaultConfig();
    return {
      ...defaults,
      ...parsed,
      tokensLight: { ...defaults.tokensLight, ...parsed.tokensLight },
      tokensDark: { ...defaults.tokensDark, ...parsed.tokensDark },
    };
  } catch {
    return null;
  }
}

function writeStored(config: BrandingConfig): void {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(config));
  } catch {
    // localStorage unavailable/quota exceeded — config still lives in memory
    // for the rest of this session via the in-flight promise chain/query cache.
  }
}

const REMEMBERED_LOGOS_KEY = 'hms-branding-logos';

type RememberedLogos = Pick<BrandingConfig, 'logoUrl' | 'loginLogoUrl' | 'faviconUrl' | 'logoDisplay'>;

/** Remembers the logos the pre-login screen needs (Login logo, its Primary fallback, the
 * favicon) from the last signed-in branding fetch in this browser — the branding API needs a
 * tenant JWT, so the login page can't fetch them itself. Only public static-file URLs. */
export function rememberLogos(config: BrandingConfig): void {
  const remembered: RememberedLogos = {
    logoUrl: config.logoUrl,
    loginLogoUrl: config.loginLogoUrl,
    faviconUrl: config.faviconUrl,
    logoDisplay: config.logoDisplay,
  };
  try {
    localStorage.setItem(REMEMBERED_LOGOS_KEY, JSON.stringify(remembered));
  } catch {
    // localStorage unavailable — the login screen just shows the bundled default logo.
  }
}

export function readRememberedLogos(): Partial<RememberedLogos> {
  try {
    const raw = localStorage.getItem(REMEMBERED_LOGOS_KEY);
    return raw ? (JSON.parse(raw) as Partial<RememberedLogos>) : {};
  } catch {
    return {};
  }
}

const MAX_LOGO_BYTES = 500 * 1024;

export class LogoTooLargeError extends Error {
  constructor() {
    super('Logo image must be 500KB or smaller.');
    this.name = 'LogoTooLargeError';
  }
}

/**
 * Async interface mirrors what a real API client (BrandingApi) would expose,
 * so a future backend swap only replaces this module's implementation — every
 * hook/component consuming it is unaffected.
 */
export const mockBrandingStore = {
  async getBranding(): Promise<BrandingConfig> {
    return readStored() ?? defaultConfig();
  },

  async updateBranding(patch: Partial<BrandingConfig>): Promise<BrandingConfig> {
    const current = readStored() ?? defaultConfig();
    const next: BrandingConfig = {
      ...current,
      ...patch,
      tokensLight: { ...current.tokensLight, ...patch.tokensLight },
      tokensDark: { ...current.tokensDark, ...patch.tokensDark },
    };
    writeStored(next);
    return next;
  },

  async uploadLogo(file: File): Promise<{ logoUrl: string }> {
    if (file.size > MAX_LOGO_BYTES) {
      throw new LogoTooLargeError();
    }
    const logoUrl = await new Promise<string>((resolve, reject) => {
      const reader = new FileReader();
      reader.onload = () => resolve(reader.result as string);
      reader.onerror = () => reject(reader.error ?? new Error('Failed to read logo file.'));
      reader.readAsDataURL(file);
    });
    const current = readStored() ?? defaultConfig();
    writeStored({ ...current, logoUrl });
    return { logoUrl };
  },

  async resetToDefaults(): Promise<BrandingConfig> {
    try {
      localStorage.removeItem(STORAGE_KEY);
    } catch {
      // ignore
    }
    return defaultConfig();
  },
};
