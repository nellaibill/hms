import defaultLogoUrl from '@/assets/logo.png';
import { useBrandingQuery } from './hooks/useBrandingQuery';
import { DEFAULT_LOGO_DISPLAY, LOGO_SLOT_META, type BrandingConfig, type LogoSlot, type LogoSlotDisplay } from './types';

/**
 * The URL a logo slot should render: its own upload, else the Primary logo (when "Use as
 * primary logo (fallback)" is on), else `fallback` — the bundled default artwork for image
 * slots. The favicon passes `null` as its fallback so index.html's own icon stays in place.
 */
export function resolveBrandLogoUrl(config: BrandingConfig | undefined, slot: LogoSlot, fallback: string | null = defaultLogoUrl): string | null {
  if (!config) return fallback;
  const own = config[LOGO_SLOT_META[slot].configKey];
  if (own) return own;
  if (slot !== 'primary' && config.logoDisplay.usePrimaryAsFallback && config.logoUrl) return config.logoUrl;
  return fallback;
}

/** A slot's resolved URL plus its display box height/fit, from the cached branding config. */
export function useBrandLogo(slot: LogoSlot): { url: string; display: LogoSlotDisplay } {
  const { data: config } = useBrandingQuery();
  return {
    url: resolveBrandLogoUrl(config, slot) ?? defaultLogoUrl,
    display: config?.logoDisplay.slots[slot] ?? DEFAULT_LOGO_DISPLAY.slots[slot],
  };
}
