import { useNavigate } from 'react-router-dom';
import { SalesBrowser } from '@/features/pharmacy/sales/SalesBrowser';
import { SALES_LINKS } from '@/features/pharmacy/shell/links';
import { PharmacyPage } from '@/features/pharmacy/shell/PharmacyPage';

/**
 * Every pharmacy bill, with the two follow-ups a bill can need: money still owed on it, and
 * goods returned against it. A return always starts from its bill — find the bill here, open
 * it, and return from there.
 */
export default function SalesListPage() {
  const navigate = useNavigate();

  return (
    <PharmacyPage
      title="Sales and returns"
      subtitle="Find a bill to reprint it, take a balance, or return goods against it."
      subLinks={SALES_LINKS}
    >
      <SalesBrowser onOpenBill={(saleId) => navigate(`/pharmacy/sales/${saleId}`)} autoFocusSearch />
    </PharmacyPage>
  );
}
