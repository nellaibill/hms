import { FlaskConical } from 'lucide-react';
import { LABORATORY_CONSULTANTS, LABORATORY_SERVICES } from '../billingCatalog';
import { ServiceBillingCard } from './ServiceBillingCard';

interface LaboratoryBillingCardProps {
  expanded: boolean;
  onToggle: () => void;
  hasError: boolean;
}

export function LaboratoryBillingCard(props: LaboratoryBillingCardProps) {
  return (
    <ServiceBillingCard
      category="laboratory"
      title="Laboratory Billing"
      description="Pathology and diagnostic lab tests for this visit."
      icon={<FlaskConical className="h-5 w-5" />}
      services={LABORATORY_SERVICES}
      consultants={LABORATORY_CONSULTANTS}
      {...props}
    />
  );
}
