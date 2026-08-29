import { Scan } from 'lucide-react';
import { useDiagnosticServices } from '@/features/diagnostics';
import { useAllActiveConsultants } from '../hooks/useAllActiveConsultants';
import { ServiceBillingCard } from './ServiceBillingCard';

interface RadiologyBillingCardProps {
  expanded: boolean;
  onToggle: () => void;
  hasError: boolean;
}

/** Data source swapped from the old untyped DiagnosticTest master to the new typed
 * DiagnosticService catalog (useDiagnosticServices) — UI/schema completely unchanged, since
 * useDiagnosticServices maps to the same {id, name, price} shape useDiagnosticTestServices
 * already returned. Laboratory forked off entirely (see LaboratoryBillingCard.tsx); Procedure
 * stays on useDiagnosticTestServices, untouched. */
export function RadiologyBillingCard(props: RadiologyBillingCardProps) {
  const { services, isLoading } = useDiagnosticServices('Radiology');
  const { consultants } = useAllActiveConsultants();

  return (
    <ServiceBillingCard
      category="radiology"
      title="Radiology Billing"
      description="Imaging services (X-Ray, CT, MRI, Ultrasound…) for this visit."
      icon={<Scan className="h-5 w-5" />}
      services={services}
      consultants={consultants}
      isLoadingServices={isLoading}
      {...props}
    />
  );
}
