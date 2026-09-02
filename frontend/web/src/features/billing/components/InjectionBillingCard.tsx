import { Droplet } from 'lucide-react';
import { useDiagnosticTestServices } from '../hooks/useDiagnosticTestServices';
import { SimpleServiceBillingCard } from './SimpleServiceBillingCard';

interface InjectionBillingCardProps {
  expanded: boolean;
  onToggle: () => void;
  hasError: boolean;
}

export function InjectionBillingCard(props: InjectionBillingCardProps) {
  const { services, isLoading } = useDiagnosticTestServices('Injection');

  return (
    <SimpleServiceBillingCard
      category="injection"
      title="Injection Billing"
      description="Injections (IM/SC/ID, IV, drip, transfusion…) administered during this visit."
      icon={<Droplet className="h-5 w-5" />}
      services={services}
      isLoadingServices={isLoading}
      {...props}
    />
  );
}
