import {
  DEFAULT_FONT_FAMILY,
  DEFAULT_FONT_SIZE_SCALE,
  DEFAULT_ICON_SIZE_SCALE,
  DEFAULT_TOKENS_DARK,
  DEFAULT_TOKENS_LIGHT,
} from '@/features/branding/mockBrandingStore';
import { FONT_FAMILY_STACKS, FONT_SIZE_SCALE_PX, ICON_SIZE_SCALE_PX, type BrandingConfig } from '@/features/branding/types';

type Theme = 'light' | 'dark';

/**
 * Applies the built-in (LHS) theme — used while the hospital's saved BrandingConfig is
 * still loading, or when none exists (first-ever load, or the mock store is unreadable).
 * It is the same snapshot index.css paints before JS runs, so the hand-off from CSS to
 * this fallback to LHS's own saved branding never shows a different theme in between.
 */
function applyStaticBrandingDefaults(theme: Theme) {
  const root = document.documentElement.style;
  const tokens = theme === 'dark' ? DEFAULT_TOKENS_DARK : DEFAULT_TOKENS_LIGHT;
  for (const [key, value] of Object.entries(tokens)) {
    root.setProperty(key, value);
  }

  root.setProperty('--font-sans', FONT_FAMILY_STACKS[DEFAULT_FONT_FAMILY]);
  root.setProperty('--font-size-base', FONT_SIZE_SCALE_PX[DEFAULT_FONT_SIZE_SCALE]);
  root.setProperty('--header-icon-size', ICON_SIZE_SCALE_PX[DEFAULT_ICON_SIZE_SCALE]);
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
  root.setProperty('--header-icon-size', ICON_SIZE_SCALE_PX[config.iconSizeScale]);
}
