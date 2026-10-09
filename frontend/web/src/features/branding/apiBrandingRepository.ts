import type { BrandingConfigDto, LogoDisplaySettingsDto } from '@hms/shared';
import { env } from '@/config/env';
import { brandingApi } from '@/services/apiClient';
import { mockBrandingStore, readRememberedLogos, rememberLogos } from './mockBrandingStore';
import {
  DEFAULT_LOGO_DISPLAY,
  FONT_FAMILIES,
  LOGO_FITS,
  LOGO_SLOTS,
  FONT_SIZE_SCALES,
  ICON_SIZE_SCALES,
  type BrandingConfig,
  type FontFamily,
  type FontSizeScale,
  type IconSizeScale,
  type LogoDisplaySettings,
  type LogoSlot,
} from './types';

function toFontFamily(value: string): FontFamily {
  return (FONT_FAMILIES as readonly string[]).includes(value) ? (value as FontFamily) : 'Inter';
}

function toFontSizeScale(value: string): FontSizeScale {
  return (FONT_SIZE_SCALES as readonly string[]).includes(value) ? (value as FontSizeScale) : 'md';
}

function toIconSizeScale(value: string): IconSizeScale {
  return (ICON_SIZE_SCALES as readonly string[]).includes(value) ? (value as IconSizeScale) : 'md';
}

// The backend returns the logo as a server-relative path (e.g. "uploads/Tenant/{tenantId}/branding/primary/xxx.png"),
// same as patient photos elsewhere in the app (see PatientSummaryCard) — it has to be resolved
// against the API's own origin, not the frontend's, or every <img> using it 404s. Left alone when
// it's already absolute (http(s):/data:/blob:) — e.g. the mock store's data: URI fallback below.
function resolveLogoUrl(logoUrl: string | null): string | null {
  if (!logoUrl) return null;
  if (/^(https?:|data:|blob:)/i.test(logoUrl)) return logoUrl;
  return `${env.apiBaseUrl}/${logoUrl.replace(/^\/+/, '')}`;
}

/** Fills any slot the server hasn't stored settings for yet (a row saved before this feature,
 * or a partial map) from DEFAULT_LOGO_DISPLAY, and drops values the UI doesn't know. */
function toLogoDisplay(dto: LogoDisplaySettingsDto | null | undefined): LogoDisplaySettings {
  const slots = { ...DEFAULT_LOGO_DISPLAY.slots };
  for (const slot of LOGO_SLOTS) {
    const stored = dto?.slots?.[slot];
    if (stored && stored.height > 0 && (LOGO_FITS as readonly string[]).includes(stored.fit)) {
      slots[slot] = { width: stored.width > 0 ? stored.width : slots[slot].width, height: stored.height, fit: stored.fit };
    }
  }
  return { usePrimaryAsFallback: dto?.usePrimaryAsFallback ?? DEFAULT_LOGO_DISPLAY.usePrimaryAsFallback, slots };
}

function fromDto(dto: BrandingConfigDto): BrandingConfig {
  return {
    hospitalName: dto.hospitalName,
    appTitle: dto.appTitle,
    address: dto.address ?? '',
    phoneNumber: dto.phoneNumber ?? '',
    logoUrl: resolveLogoUrl(dto.logoUrl),
    compactLogoUrl: resolveLogoUrl(dto.compactLogoUrl),
    loginLogoUrl: resolveLogoUrl(dto.loginLogoUrl),
    printLogoUrl: resolveLogoUrl(dto.printLogoUrl),
    faviconUrl: resolveLogoUrl(dto.faviconUrl),
    logoDisplay: toLogoDisplay(dto.logoDisplay),
    fontFamily: toFontFamily(dto.fontFamily),
    fontSizeScale: toFontSizeScale(dto.fontSizeScale),
    iconSizeScale: toIconSizeScale(dto.iconSizeScale),
    tokensLight: dto.tokensLight,
    tokensDark: dto.tokensDark,
  };
}

/**
 * Real, database-backed repository (HMS.Modules.Branding) — implements the same shape as
 * mockBrandingStore.ts, per the Theme & Branding plan's "swap the implementation, not the
 * UI" design. Every hook/component built against mockBrandingStore keeps working unchanged.
 */
export const apiBrandingRepository = {
  async getBranding(): Promise<BrandingConfig> {
    try {
      const dto = await brandingApi.getBranding();
      const config = fromDto(dto);
      rememberLogos(config);
      return config;
    } catch {
      // Backend unreachable (down, or the frontend is being run standalone), or — the common
      // case — the pre-login screen, which has no tenant/JWT yet and always gets a 401 here.
      // Falls back to the local mock/localStorage store so the app never renders unstyled,
      // with the logos last seen on a signed-in session in this browser layered on top, so
      // the login screen and favicon can still show the hospital's configured Login logo.
      const fallback = await mockBrandingStore.getBranding();
      return { ...fallback, ...readRememberedLogos() };
    }
  },

  async updateBranding(patch: Partial<BrandingConfig>): Promise<BrandingConfig> {
    // The backend's PUT replaces the identity/typography/token fields wholesale (no partial
    // merge server-side), so a true partial patch is merged onto the current config here
    // first. In practice BrandingForm always submits the complete draft, but this keeps the
    // repository correct for the interface's Partial<BrandingConfig> contract regardless.
    const current = await apiBrandingRepository.getBranding();
    const merged: BrandingConfig = {
      ...current,
      ...patch,
      tokensLight: { ...current.tokensLight, ...patch.tokensLight },
      tokensDark: { ...current.tokensDark, ...patch.tokensDark },
    };

    const dto = await brandingApi.updateBranding({
      hospitalName: merged.hospitalName,
      appTitle: merged.appTitle,
      address: merged.address || null,
      phoneNumber: merged.phoneNumber || null,
      fontFamily: merged.fontFamily,
      fontSizeScale: merged.fontSizeScale,
      iconSizeScale: merged.iconSizeScale,
      tokensLight: merged.tokensLight,
      tokensDark: merged.tokensDark,
      logoDisplay: merged.logoDisplay,
    });
    return fromDto(dto);
  },

  async uploadLogo(file: File, slot: LogoSlot = 'primary'): Promise<BrandingConfig> {
    return fromDto(await brandingApi.uploadLogo(file, slot));
  },

  async removeLogo(slot: LogoSlot): Promise<BrandingConfig> {
    return fromDto(await brandingApi.removeLogo(slot));
  },

  async resetToDefaults(): Promise<BrandingConfig> {
    // No backend "reset" endpoint (kept out of scope) — resetting means restoring the app's
    // original static defaults, so this reuses the mock store's default snapshot and writes
    // it back through the normal update path, persisting it for real.
    const defaults = await mockBrandingStore.resetToDefaults();
    return apiBrandingRepository.updateBranding(defaults);
  },
};
