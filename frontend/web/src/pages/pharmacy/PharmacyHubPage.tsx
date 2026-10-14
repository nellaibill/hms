import { ArrowRight, PackagePlus, Pill, Receipt } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Card, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { PageBanner } from '@/components/PageBanner';
import { useAuth } from '@/features/auth/AuthContext';
import { StockSummaryPanel } from '@/features/pharmacy/stock-balances';

interface HubCard {
  key: string;
  label: string;
  description: string;
  icon: typeof Pill;
  path: string;
}

/**
 * Pharmacy's landing page ('/pharmacy') — current stock per product (searchable, with
 * needs-reorder and expiry views), plus links into Receive Stock, Dispense, and the combined
 * Stock Ledger. Mirrors IpdDashboardPage's hub shape.
 */
export default function PharmacyHubPage() {
  const { hasPermission } = useAuth();

  const cards: HubCard[] = [
    { key: 'receive', label: 'Receive Stock', description: 'Record newly received stock against a product/batch.', icon: PackagePlus, path: '/pharmacy/stock-receipts/new' },
    { key: 'dispense', label: 'Dispense', description: 'Issue stock directly to a patient.', icon: Pill, path: '/pharmacy/dispenses/new' },
    { key: 'receipts', label: 'Stock Receipts', description: 'History of stock received.', icon: Receipt, path: '/pharmacy/stock-receipts' },
    { key: 'dispenses', label: 'Dispenses', description: 'History of stock dispensed to patients.', icon: Pill, path: '/pharmacy/dispenses' },
    { key: 'ledger', label: 'Stock Ledger', description: 'Combined, filterable receipt + dispense history.', icon: Receipt, path: '/pharmacy/stock-ledger' },
  ];

  return (
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={Pill}
        title="Pharmacy"
        subtitle="Stock/batch/expiry tracking and direct-dispense to patients."
      />

      <div className="flex flex-1 flex-col gap-8 p-6 lg:p-8">
        {hasPermission('pharmacy.create') && (
          <section className="flex flex-col gap-3">
            <h2 className="text-sm font-semibold uppercase tracking-wide text-muted-foreground">Actions</h2>
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              {cards.slice(0, 2).map((card) => {
                const Icon = card.icon;
                return (
                  <Link key={card.key} to={card.path} className="block">
                    <Card className="h-full transition-all hover:border-primary/40 hover:bg-accent/40 hover:shadow-soft-lg">
                      <CardHeader>
                        <div className="flex items-center justify-between">
                          <span className="flex h-9 w-9 items-center justify-center rounded-md bg-primary/10 text-primary">
                            <Icon className="h-4.5 w-4.5" />
                          </span>
                          <ArrowRight className="h-4 w-4 text-muted-foreground" />
                        </div>
                        <CardTitle className="text-base">{card.label}</CardTitle>
                        <CardDescription>{card.description}</CardDescription>
                      </CardHeader>
                    </Card>
                  </Link>
                );
              })}
            </div>
          </section>
        )}

        <section className="flex flex-col gap-3">
          <h2 className="text-sm font-semibold uppercase tracking-wide text-muted-foreground">Browse</h2>
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {cards.slice(2).map((card) => {
              const Icon = card.icon;
              return (
                <Link key={card.key} to={card.path} className="block">
                  <Card className="h-full transition-all hover:border-primary/40 hover:bg-accent/40 hover:shadow-soft-lg">
                    <CardHeader>
                      <div className="flex items-center justify-between">
                        <span className="flex h-9 w-9 items-center justify-center rounded-md bg-primary/10 text-primary">
                          <Icon className="h-4.5 w-4.5" />
                        </span>
                        <ArrowRight className="h-4 w-4 text-muted-foreground" />
                      </div>
                      <CardTitle className="text-base">{card.label}</CardTitle>
                      <CardDescription>{card.description}</CardDescription>
                    </CardHeader>
                  </Card>
                </Link>
              );
            })}
          </div>
        </section>

        <section className="flex flex-col gap-3">
          <h2 className="text-sm font-semibold uppercase tracking-wide text-muted-foreground">Current Stock</h2>
          <StockSummaryPanel />
        </section>
      </div>
    </div>
  );
}
