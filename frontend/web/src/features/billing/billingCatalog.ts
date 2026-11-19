/**
 * Shared type definitions for Billing's Service/Consultant selection — the real data behind
 * them now comes from the Masters API (DiagnosticTest for Radiology/Laboratory/Procedure
 * services, Department/Consultant/ConsultationType for Consultation), not from static arrays
 * here. See `hooks/useDiagnosticTestServices.ts`, `hooks/useAllActiveConsultants.ts`, and
 * `components/ConsultationBillingCard.tsx`.
 */

export interface BillingService {
  id: string;
  name: string;
  price: number;
  /** Radiology only (ADR-140): other rates for the same scan, billed instead of `price`. */
  rateOptions?: { id: string; name: string; price: number }[];
  /** Radiology only: what a bedside scan adds to the price; null/undefined when not offered. */
  bedsidePrice?: number | null;
}

export interface ServiceConsultant {
  id: string;
  name: string;
}

export function getServicePrice(services: BillingService[], serviceId: string): number {
  return services.find((s) => s.id === serviceId)?.price ?? 0;
}

/**
 * A service row's charge (ADR-140): the picked rate option's price, else the service's standard
 * price, plus the bedside add-on for a bedside scan. Rate options and bedside only exist on
 * radiology services, so for any other row this is just the service's price.
 */
export function getServiceRowPrice(services: BillingService[], serviceId: string, rateOptionId?: string, isBedside?: boolean): number {
  const service = services.find((s) => s.id === serviceId);
  if (!service) return 0;
  const option = rateOptionId ? service.rateOptions?.find((candidate) => candidate.id === rateOptionId) : undefined;
  const base = option ? option.price : service.price;
  return base + (isBedside && service.bedsidePrice != null ? service.bedsidePrice : 0);
}
