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
}

export interface ServiceConsultant {
  id: string;
  name: string;
}

export function getServicePrice(services: BillingService[], serviceId: string): number {
  return services.find((s) => s.id === serviceId)?.price ?? 0;
}
