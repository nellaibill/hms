/** Mirrors HMS.Modules.Branding.Contracts.BrandingResponse. */
export interface BrandingConfigDto {
  hospitalName: string;
  appTitle: string;
  logoUrl: string | null;
  fontFamily: string;
  fontSizeScale: string;
  tokensLight: Record<string, string>;
  tokensDark: Record<string, string>;
}

/** Mirrors HMS.Modules.Branding.Contracts.UpdateBrandingRequest. */
export interface UpdateBrandingRequest {
  hospitalName: string;
  appTitle: string;
  fontFamily: string;
  fontSizeScale: string;
  tokensLight: Record<string, string>;
  tokensDark: Record<string, string>;
}
