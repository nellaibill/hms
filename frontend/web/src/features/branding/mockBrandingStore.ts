import { branding } from '@/config/branding';
import { contrastForeground } from '@/lib/color';
import type { BrandingConfig } from './types';

const STORAGE_KEY = 'hms-branding-config';

/**
 * Default token set — a hand-copied snapshot of index.css's :root/.dark blocks
 * plus the primary-derived overrides applyBrandingTokens() used to compute at
 * runtime. This is both the seed for a first-ever load and the target of
 * "Reset to default theme", so the app looks byte-identical to pre-feature
 * HMS until an admin actually changes something.
 */
const DEFAULT_TOKENS_LIGHT: Record<string, string> = {
  '--background': '0 0% 100%',
  '--foreground': '222 47% 11%',
  '--card': '0 0% 100%',
  '--card-foreground': '222 47% 11%',
  '--card-header-bg': '0 0% 100%',
  '--card-header-foreground': '222 47% 11%',
  '--page-banner-bg': '196 86% 58%',
  '--page-banner-foreground': '0 0% 100%',
  '--popover': '0 0% 100%',
  '--popover-foreground': '222 47% 11%',
  '--primary': branding.primaryColor.light,
  '--primary-foreground': contrastForeground(branding.primaryColor.light, '222 47% 11%', '0 0% 100%'),
  '--secondary': '210 30% 97%',
  '--secondary-foreground': '215 25% 27%',
  '--muted': '210 30% 97%',
  '--muted-foreground': '215 16% 47%',
  '--accent': '210 82% 95%',
  '--accent-foreground': '210 82% 32%',
  '--link': branding.primaryColor.light,
  '--icon': '215 16% 47%',
  '--destructive': '356 75% 49%',
  '--destructive-foreground': '0 0% 100%',
  '--success': '152 56% 34%',
  '--success-foreground': '0 0% 100%',
  '--warning': '38 92% 50%',
  '--warning-foreground': '222 47% 11%',
  '--info': '199 89% 42%',
  '--info-foreground': '0 0% 100%',
  '--border': '214 28% 91%',
  '--input': '214 28% 91%',
  '--ring': branding.primaryColor.light,
  '--sidebar': '210 33% 99%',
  '--sidebar-foreground': '222 40% 18%',
  '--sidebar-border': '214 28% 91%',
  '--sidebar-accent': '210 40% 95%',
  '--sidebar-active-bg': '210 82% 95%',
  '--sidebar-active-fg': '210 82% 40%',
  '--header-bg': branding.primaryColor.light,
  '--header-foreground': contrastForeground(branding.primaryColor.light, '222 47% 11%', '0 0% 100%'),
};

const DEFAULT_TOKENS_DARK: Record<string, string> = {
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
  '--primary': branding.primaryColor.dark,
  '--primary-foreground': contrastForeground(branding.primaryColor.dark, '222 47% 9%', '0 0% 100%'),
  '--secondary': '217 27% 17%',
  '--secondary-foreground': '210 30% 92%',
  '--muted': '217 27% 17%',
  '--muted-foreground': '215 18% 68%',
  '--accent': '210 60% 20%',
  '--accent-foreground': '210 90% 85%',
  '--link': branding.primaryColor.dark,
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
  '--ring': branding.primaryColor.dark,
  '--sidebar': '222 44% 8%',
  '--sidebar-foreground': '210 30% 92%',
  '--sidebar-border': '217 24% 20%',
  '--sidebar-accent': '217 30% 16%',
  '--sidebar-active-bg': '210 60% 20%',
  '--sidebar-active-fg': '210 90% 72%',
  '--header-bg': branding.primaryColor.light,
  '--header-foreground': contrastForeground(branding.primaryColor.light, '222 47% 11%', '0 0% 100%'),
};

function defaultConfig(): BrandingConfig {
  return {
    hospitalName: branding.hospitalName,
    appTitle: branding.systemName,
    logoUrl: null,
    fontFamily: 'Inter',
    fontSizeScale: 'md',
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
