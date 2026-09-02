import { FileText } from 'lucide-react';
import { useDiagnosticTestServices } from '../hooks/useDiagnosticTestServices';
import { SimpleServiceBillingCard } from './SimpleServiceBillingCard';

interface FileBillingCardProps {
  expanded: boolean;
  onToggle: () => void;
  hasError: boolean;
}

export function FileBillingCard(props: FileBillingCardProps) {
  const { services, isLoading } = useDiagnosticTestServices('File');

  return (
    <SimpleServiceBillingCard
      category="file"
      title="File Billing"
      description="File and documentation charges (case sheet, ANC file, neonatal file…) for this visit."
      icon={<FileText className="h-5 w-5" />}
      services={services}
      isLoadingServices={isLoading}
      {...props}
    />
  );
}
