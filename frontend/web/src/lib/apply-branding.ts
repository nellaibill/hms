import { branding } from '@/config/branding';
import { adjustLightness, contrastForeground, setLightness } from '@/lib/color';
import { FONT_FAMILY_STACKS, FONT_SIZE_SCALE_PX, type BrandingConfig } from '@/features/branding/types';

type Theme = 'light' | 'dark';

/**
 * Derives the static-config fallback token set — used when no admin-saved
 * BrandingConfig exists yet (first-ever load, or the mock store is
 * unreadable). Kept as a pure fallback so the app never renders unstyled.
 */
function applyStaticBrandingDefaults(theme: Theme) {
  const root = document.documentElement.style;
  const primary = branding.primaryColor[theme];
  const primaryForeground = contrastForeground(primary, '222 47% 11%', '0 0% 100%');

  root.setProperty('--primary', primary);
  root.setProperty('--primary-foreground', primaryForeground);
  root.setProperty('--ring', primary);

  // The top app bar always uses the *light-mode* brand color, regardless of
  // the active theme — a banner that pales out in dark mode reads as
  // unbranded/washed-out, unlike buttons and accents which intentionally
  // lighten for contrast against a dark page.
  const headerBg = branding.primaryColor.light;
  root.setProperty('--header-bg', headerBg);
  root.setProperty('--header-foreground', contrastForeground(headerBg, '222 47% 11%', '0 0% 100%'));

  const accentBg = setLightness(primary, theme === 'dark' ? 20 : 95);
  const accentFg = setLightness(primary, theme === 'dark' ? 82 : 32);
  root.setProperty('--accent', accentBg);
  root.setProperty('--accent-foreground', accentFg);

  const sidebarActiveBg = setLightness(primary, theme === 'dark' ? 18 : 93);
  const sidebarActiveFg = setLightness(primary, theme === 'dark' ? 82 : 36);
  root.setProperty('--sidebar-active-bg', sidebarActiveBg);
  root.setProperty('--sidebar-active-fg', sidebarActiveFg);

  root.setProperty('--sidebar-accent', theme === 'dark' ? adjustLightness(primary, -32) : adjustLightness(primary, 40));

  // New tokens introduced for admin theming — default to values matching
  // today's hardcoded appearance so their introduction is a no-op visually.
  root.setProperty('--link', primary);
  root.setProperty('--icon', theme === 'dark' ? '215 18% 68%' : '215 16% 47%');
  root.setProperty('--card-header-bg', theme === 'dark' ? '222 40% 10%' : '0 0% 100%');
  root.setProperty('--card-header-foreground', theme === 'dark' ? '210 40% 98%' : '222 47% 11%');
  root.setProperty('--page-banner-bg', '196 86% 58%');
  root.setProperty('--page-banner-foreground', '0 0% 100%');
  root.setProperty('--font-sans', FONT_FAMILY_STACKS.Inter);
  root.setProperty('--font-size-base', FONT_SIZE_SCALE_PX.md);
}

/**
 * Applies an admin-saved BrandingConfig's full token map onto :root. Any
 * token key the config doesn't have (e.g. a config saved before a newly
 * introduced token existed) falls back to the static-default value for that
 * one key, so the UI is never left with an unset CSS var.
 */
export function applyBrandingTokens(theme: Theme, config?: BrandingConfig | null) {
  if (!config) {
    applyStaticBrandingDefaults(theme);
    return;
  }

  // Establish full static defaults first so any token missing from `config`
  // (old cached config, etc.) still resolves to something sane, then layer
  // the admin's explicit values on top.
  applyStaticBrandingDefaults(theme);

  const root = document.documentElement.style;
  const tokens = theme === 'dark' ? config.tokensDark : config.tokensLight;
  for (const [key, value] of Object.entries(tokens)) {
    root.setProperty(key, value);
  }

  root.setProperty('--font-sans', FONT_FAMILY_STACKS[config.fontFamily]);
  root.setProperty('--font-size-base', FONT_SIZE_SCALE_PX[config.fontSizeScale]);
}
