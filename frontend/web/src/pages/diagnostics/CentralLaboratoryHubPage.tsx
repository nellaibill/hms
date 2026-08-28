import { ArrowRight, Building2, FlaskConical, ListTree, PackageSearch } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Card, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';

interface HubCard {
  key: string;
  label: string;
  description: string;
  icon: typeof FlaskConical;
  path: string;
}

const cards: HubCard[] = [
  { key: 'categories', label: 'Categories', description: 'Test categories used to organize the service catalog.', icon: ListTree, path: '/diagnostics/lab/categories' },
  { key: 'services', label: 'Services', description: 'The Laboratory/Radiology test catalog — pricing, category, and outsourcing.', icon: FlaskConical, path: '/diagnostics/lab/services' },
  { key: 'packages', label: 'Packages', description: 'Bundled test packages (e.g. Lipid Profile) at a fixed price.', icon: PackageSearch, path: '/diagnostics/lab/packages' },
  { key: 'external-labs', label: 'External Labs', description: 'Providers tests are outsourced to.', icon: Building2, path: '/diagnostics/lab/external-labs' },
];

/**
 * Central Laboratory's landing page ('/diagnostics/lab') — a card grid linking into
 * Categories/Services/Packages/External Labs, no persistent sub-nav/tab strip. Mirrors
 * PharmacyHubPage's exact skeleton.
 */
export default function CentralLaboratoryHubPage() {
  return (
    <div className="flex flex-1 flex-col">
      <div className="flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        <div className="flex items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
            <FlaskConical className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight">Central Laboratory</h1>
        </div>
        <p className="max-w-2xl text-sm text-page-banner-foreground/85">
          Test category, service, and package reference data for Laboratory billing.
        </p>
      </div>

      <div className="flex flex-1 flex-col gap-8 p-6 lg:p-8">
        <section className="flex flex-col gap-3">
          <h2 className="text-sm font-semibold uppercase tracking-wide text-muted-foreground">Browse</h2>
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
            {cards.map((card) => {
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
      </div>
    </div>
  );
}
