import { API_ROUTES } from '../../constants';
import type { BrandingConfigDto, BrandingLogoSlotDto, UpdateBrandingRequest } from '../../dtos';
import type { HttpClient } from '../httpClient';

/**
 * Typed API service for the Branding module, built on the shared HTTP client. Feature code
 * (web/mobile) calls this, never the HTTP client directly — docs/FrontendArchitecture.md §6.
 */
export class BrandingApi {
  constructor(private readonly client: HttpClient) {}

  async getBranding(): Promise<BrandingConfigDto> {
    const response = await this.client.get<BrandingConfigDto>(API_ROUTES.branding.base);
    return response.data;
  }

  async updateBranding(request: UpdateBrandingRequest): Promise<BrandingConfigDto> {
    const response = await this.client.put<BrandingConfigDto>(API_ROUTES.branding.base, request);
    return response.data;
  }

  /** Uploads/replaces one logo slot — `primary` (the original hospital logo) when omitted. */
  async uploadLogo(file: File, slot: BrandingLogoSlotDto = 'primary'): Promise<BrandingConfigDto> {
    const formData = new FormData();
    formData.append('file', file);
    const response = await this.client.postFormData<BrandingConfigDto>(API_ROUTES.branding.logo, formData, { query: { slot } });
    return response.data;
  }

  /** Clears one logo slot; the app then falls back per the logo display settings. */
  async removeLogo(slot: BrandingLogoSlotDto): Promise<BrandingConfigDto> {
    const response = await this.client.delete<BrandingConfigDto>(API_ROUTES.branding.logo, { query: { slot } });
    return response.data;
  }
}
