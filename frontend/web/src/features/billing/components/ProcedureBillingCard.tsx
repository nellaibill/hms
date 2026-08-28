import { Syringe } from 'lucide-react';
import { useAllActiveConsultants } from '../hooks/useAllActiveConsultants';
import { useDiagnosticTestServices } from '../hooks/useDiagnosticTestServices';
import { ServiceBillingCard } from './ServiceBillingCard';

interface ProcedureBillingCardProps {
  expanded: boolean;
  onToggle: () => void;
  hasError: boolean;
}

export function ProcedureBillingCard(props: ProcedureBillingCardProps) {
  const { services, isLoading } = useDiagnosticTestServices('Procedure');
  const { consultants } = useAllActiveConsultants();

  return (
    <ServiceBillingCard
      category="procedure"
      title="Procedure Billing"
      description="Minor procedures and bedside interventions for this visit."
      icon={<Syringe className="h-5 w-5" />}
      services={services}
      consultants={consultants}
      isLoadingServices={isLoading}
      {...props}
    />
  );
}
