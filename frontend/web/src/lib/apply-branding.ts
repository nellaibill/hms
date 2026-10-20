import {
  DEFAULT_FONT_FAMILY,
  DEFAULT_FONT_SIZE_SCALE,
  DEFAULT_ICON_SIZE_SCALE,
  DEFAULT_TOKENS_DARK,
  DEFAULT_TOKENS_LIGHT,
  readRememberedTheme,
} from '@/features/branding/mockBrandingStore';
import { typographyCssVars, type BrandingConfig, type BrandingTheme } from '@/features/branding/types';

type Theme = 'light' | 'dark';

/** The built-in (LHS) theme — the same snapshot index.css paints before JS runs. */
const BUILT_IN_THEME: BrandingTheme = {
  tokensLight: DEFAULT_TOKENS_LIGHT,
  tokensDark: DEFAULT_TOKENS_DARK,
  fontFamily: DEFAULT_FONT_FAMILY,
  fontSizeScale: DEFAULT_FONT_SIZE_SCALE,
  iconSizeScale: DEFAULT_ICON_SIZE_SCALE,
};

function writeThemeVars(theme: Theme, settings: BrandingTheme) {
  const root = document.documentElement.style;
  const vars = { ...(theme === 'dark' ? settings.tokensDark : settings.tokensLight), ...typographyCssVars(settings) };
  for (const [key, value] of Object.entries(vars)) {
    root.setProperty(key, value);
  }
}

/**
 * Applies the hospital's admin-saved BrandingConfig onto :root. While that config is still
 * loading (`config` undefined), the theme this browser last saw (mockBrandingStore.ts's
 * rememberTheme()) stands in for it — the same one index.html painted before React loaded —
 * and only on a first visit does the built-in theme show. Either way the built-in theme is
 * written first, so a token the applied theme doesn't have (e.g. a config saved before a newly
 * introduced token existed) still resolves to a sane value instead of an unset CSS var.
 */
export function applyBrandingTokens(theme: Theme, config?: BrandingConfig | null) {
  writeThemeVars(theme, BUILT_IN_THEME);
  const settings = config ?? readRememberedTheme();
  if (settings) writeThemeVars(theme, settings);
}
