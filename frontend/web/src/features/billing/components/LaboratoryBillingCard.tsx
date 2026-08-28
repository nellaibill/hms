import { FlaskConical } from 'lucide-react';
import { LABORATORY_CONSULTANTS } from '../billingCatalog';
import { useDiagnosticTestServices } from '../hooks/useDiagnosticTestServices';
import { ServiceBillingCard } from './ServiceBillingCard';

interface LaboratoryBillingCardProps {
  expanded: boolean;
  onToggle: () => void;
  hasError: boolean;
}

export function LaboratoryBillingCard(props: LaboratoryBillingCardProps) {
  const { services, isLoading } = useDiagnosticTestServices('Laboratory');

  return (
    <ServiceBillingCard
      category="laboratory"
      title="Laboratory Billing"
      description="Pathology and diagnostic lab tests for this visit."
      icon={<FlaskConical className="h-5 w-5" />}
      services={services}
      consultants={LABORATORY_CONSULTANTS}
      isLoadingServices={isLoading}
      {...props}
    />
  );
}
