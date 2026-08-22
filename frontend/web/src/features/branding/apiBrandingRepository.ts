import type { BrandingConfigDto } from '@hms/shared';
import { env } from '@/config/env';
import { brandingApi } from '@/services/apiClient';
import { mockBrandingStore } from './mockBrandingStore';
import { FONT_FAMILIES, FONT_SIZE_SCALES, type BrandingConfig, type FontFamily, type FontSizeScale } from './types';

function toFontFamily(value: string): FontFamily {
  return (FONT_FAMILIES as readonly string[]).includes(value) ? (value as FontFamily) : 'Inter';
}

function toFontSizeScale(value: string): FontSizeScale {
  return (FONT_SIZE_SCALES as readonly string[]).includes(value) ? (value as FontSizeScale) : 'md';
}

// The backend returns the logo as a server-relative path (e.g. "uploads/branding/logo/xxx.png"),
// same as patient photos elsewhere in the app (see PatientSummaryCard) — it has to be resolved
// against the API's own origin, not the frontend's, or every <img> using it 404s. Left alone when
// it's already absolute (http(s):/data:/blob:) — e.g. the mock store's data: URI fallback below.
function resolveLogoUrl(logoUrl: string | null): string | null {
  if (!logoUrl) return null;
  if (/^(https?:|data:|blob:)/i.test(logoUrl)) return logoUrl;
  return `${env.apiBaseUrl}/${logoUrl.replace(/^\/+/, '')}`;
}

function fromDto(dto: BrandingConfigDto): BrandingConfig {
  return {
    hospitalName: dto.hospitalName,
    appTitle: dto.appTitle,
    logoUrl: resolveLogoUrl(dto.logoUrl),
    fontFamily: toFontFamily(dto.fontFamily),
    fontSizeScale: toFontSizeScale(dto.fontSizeScale),
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
      return fromDto(dto);
    } catch {
      // Backend unreachable (down, or the frontend is being run standalone) — fall back to
      // the local mock/localStorage store so the app never renders unstyled.
      return mockBrandingStore.getBranding();
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
      fontFamily: merged.fontFamily,
      fontSizeScale: merged.fontSizeScale,
      tokensLight: merged.tokensLight,
      tokensDark: merged.tokensDark,
    });
    return fromDto(dto);
  },

  async uploadLogo(file: File): Promise<{ logoUrl: string }> {
    const dto = await brandingApi.uploadLogo(file);
    return { logoUrl: resolveLogoUrl(dto.logoUrl) ?? '' };
  },

  async resetToDefaults(): Promise<BrandingConfig> {
    // No backend "reset" endpoint (kept out of scope) — resetting means restoring the app's
    // original static defaults, so this reuses the mock store's default snapshot and writes
    // it back through the normal update path, persisting it for real.
    const defaults = await mockBrandingStore.resetToDefaults();
    return apiBrandingRepository.updateBranding(defaults);
  },
};
