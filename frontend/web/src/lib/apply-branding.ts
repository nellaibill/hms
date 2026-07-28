import { branding } from '@/config/branding';
import { adjustLightness, contrastForeground, setLightness } from '@/lib/color';

type Theme = 'light' | 'dark';

/**
 * Derives every brand-dependent CSS variable from branding.primaryColor and
 * writes them onto :root. This is the whole mechanism that makes "change the
 * brand color" a one-line config edit (config/branding.ts) instead of a
 * hunt-and-replace across components: every component consumes `--primary`,
 * `--ring`, `--sidebar-active-*`, etc., never a hardcoded hex/hsl value.
 *
 * Text-bearing pairs (sidebar active row, accent tint) use *absolute*
 * lightness targets, not relative offsets from the configured primary —
 * a saturated hue needs a much bigger lightness gap than grayscale to clear
 * WCAG AA's 4.5:1 for normal text, and a relative offset from an arbitrary
 * brand primary can't guarantee that regardless of what color a future
 * brand config supplies.
 */
export function applyBrandingTokens(theme: Theme) {
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

  // Light, translucent brand tint used for hover/active surfaces (sidebar
  // hover, hoverable cards) — derived, not hand-picked, so it always
  // harmonizes with whatever primary color branding.ts specifies.
  const accentBg = setLightness(primary, theme === 'dark' ? 20 : 95);
  const accentFg = setLightness(primary, theme === 'dark' ? 82 : 32);
  root.setProperty('--accent', accentBg);
  root.setProperty('--accent-foreground', accentFg);

  const sidebarActiveBg = setLightness(primary, theme === 'dark' ? 18 : 93);
  const sidebarActiveFg = setLightness(primary, theme === 'dark' ? 82 : 36);
  root.setProperty('--sidebar-active-bg', sidebarActiveBg);
  root.setProperty('--sidebar-active-fg', sidebarActiveFg);

  // Sidebar hover (not active) uses a lighter touch than the active state —
  // a small relative lift off the sidebar surface itself, not the brand hue.
  root.setProperty('--sidebar-accent', theme === 'dark' ? adjustLightness(primary, -32) : adjustLightness(primary, 40));
}
