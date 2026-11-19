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
  /** Radiology only: the doctor's part of bedsidePrice (ADR-144) — the rest is the hospital's. */
  bedsideDoctorCharge?: number | null;
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
 * radiology services, so for any other row this is just the service's price. The bedside add-on
 * is `bedsideAmount` when the counter changed it (ADR-144), else the service's bedside price.
 */
export function getServiceRowPrice(
  services: BillingService[],
  serviceId: string,
  rateOptionId?: string,
  isBedside?: boolean,
  bedsideAmount?: number,
): number {
  const service = services.find((s) => s.id === serviceId);
  if (!service) return 0;
  const option = rateOptionId ? service.rateOptions?.find((candidate) => candidate.id === rateOptionId) : undefined;
  const base = option ? option.price : service.price;
  if (!isBedside || service.bedsidePrice == null) return base;
  return base + (bedsideAmount ?? service.bedsidePrice);
}

/**
 * The hospital's part of a service's bedside add-on (ADR-144): its bedside price less the
 * doctor's part, never below 0 — the least a bedside scan's add-on can be charged at. Null when
 * the service isn't done at the bedside. Mirrors the backend's Domain/BedsideShare.cs.
 */
export function getBedsideHospitalShare(service: Pick<BillingService, 'bedsidePrice' | 'bedsideDoctorCharge'> | undefined): number | null {
  if (service?.bedsidePrice == null) return null;
  return Math.max(service.bedsidePrice - (service.bedsideDoctorCharge ?? 0), 0);
}
