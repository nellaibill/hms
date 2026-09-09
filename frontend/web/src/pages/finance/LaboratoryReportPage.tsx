import { FlaskConical } from 'lucide-react';
import { CategoryProfitReportPage } from '@/features/reports';

export default function LaboratoryReportPage() {
  return (
    <CategoryProfitReportPage
      billingType="Laboratory"
      title="Laboratory Report"
      description="Revenue, cost, and margin for every billed Laboratory test in the selected period."
      icon={FlaskConical}
    />
  );
}
