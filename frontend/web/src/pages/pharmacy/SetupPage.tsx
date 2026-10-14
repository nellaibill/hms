import { ArrowRight, Building2, FileSpreadsheet, type LucideIcon, Package, SlidersHorizontal, Store, Truck } from 'lucide-react';
import { Link } from 'react-router-dom';
import { SETUP_LINKS } from '@/features/pharmacy/shell/links';
import { PharmacyPage } from '@/features/pharmacy/shell/PharmacyPage';

interface SetupCard {
  label: string;
  description: string;
  icon: LucideIcon;
  to: string;
}

const CARDS: SetupCard[] = [
  { label: 'Import products and opening stock', description: 'Load your product list and the stock on your shelf from Excel. The quickest way to get started.', icon: FileSpreadsheet, to: '/pharmacy/import' },
  { label: 'Bill and counter settings', description: 'GSTIN, drug licence and footer printed on bills; bill paper; the discount limit.', icon: SlidersHorizontal, to: '/pharmacy/settings' },
  { label: 'Products', description: 'The medicine catalog: names, generics, pack sizes, GST, schedule and rack.', icon: Package, to: '/support/inventory' },
  { label: 'Suppliers', description: 'The distributors you buy from, with their GSTIN and contact details.', icon: Truck, to: '/admin/masters/supplier' },
  { label: 'Stores', description: 'Where stock is held: main store, dispensing counters and ward sub-stores.', icon: Store, to: '/pharmacy/stores' },
  { label: 'Manufacturers', description: 'The companies that make the products you stock.', icon: Building2, to: '/admin/masters/manufacturer' },
];

/** One place for everything a pharmacy sets up once and then rarely touches. */
export default function SetupPage() {
  return (
    <PharmacyPage title="Pharmacy setup" subtitle="What you set up once: the catalog, suppliers, stores and your opening stock." subLinks={SETUP_LINKS}>
      <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
        {CARDS.map((card) => {
          const Icon = card.icon;
          return (
            <Link key={card.to} to={card.to} className="group flex items-start gap-3 rounded-lg border border-border bg-background p-4 transition-colors hover:border-primary/40 hover:bg-accent/40">
              <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-primary/10 text-primary">
                <Icon className="h-4.5 w-4.5" />
              </span>
              <span className="min-w-0 flex-1">
                <span className="block font-medium text-foreground">{card.label}</span>
                <span className="block text-sm text-muted-foreground">{card.description}</span>
              </span>
              <ArrowRight className="mt-1 h-4 w-4 shrink-0 text-muted-foreground transition-transform group-hover:translate-x-0.5" />
            </Link>
          );
        })}
      </div>
    </PharmacyPage>
  );
}
