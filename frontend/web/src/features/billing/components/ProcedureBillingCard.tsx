import { Syringe } from 'lucide-react';
import { PROCEDURE_CONSULTANTS, PROCEDURE_SERVICES } from '../billingCatalog';
import { ServiceBillingCard } from './ServiceBillingCard';

interface ProcedureBillingCardProps {
  expanded: boolean;
  onToggle: () => void;
  hasError: boolean;
}

export function ProcedureBillingCard(props: ProcedureBillingCardProps) {
  return (
    <ServiceBillingCard
      category="procedure"
      title="Procedure Billing"
      description="Minor procedures and bedside interventions for this visit."
      icon={<Syringe className="h-5 w-5" />}
      services={PROCEDURE_SERVICES}
      consultants={PROCEDURE_CONSULTANTS}
      {...props}
    />
  );
}
