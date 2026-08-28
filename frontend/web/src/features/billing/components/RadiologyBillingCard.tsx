import { Scan } from 'lucide-react';
import { useAllActiveConsultants } from '../hooks/useAllActiveConsultants';
import { useDiagnosticTestServices } from '../hooks/useDiagnosticTestServices';
import { ServiceBillingCard } from './ServiceBillingCard';

interface RadiologyBillingCardProps {
  expanded: boolean;
  onToggle: () => void;
  hasError: boolean;
}

export function RadiologyBillingCard(props: RadiologyBillingCardProps) {
  const { services, isLoading } = useDiagnosticTestServices('Radiology');
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
