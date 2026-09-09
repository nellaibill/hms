import { Radiation } from 'lucide-react';
import { CategoryProfitReportPage } from '@/features/reports';

export default function RadiologyReportPage() {
  return (
    <CategoryProfitReportPage
      billingType="Radiology"
      title="Radiology Report"
      description="Revenue, cost, and margin for every billed Radiology study in the selected period."
      icon={Radiation}
    />
  );
}
