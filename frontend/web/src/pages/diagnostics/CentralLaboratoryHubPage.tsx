import { ArrowRight, Building2, FlaskConical, ListTree, Microscope, PackageSearch } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Card, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { useAuth } from '@/features/auth/AuthContext';

interface HubCard {
  key: string;
  label: string;
  description: string;
  icon: typeof FlaskConical;
  path: string;
}

const catalogCards: HubCard[] = [
  { key: 'categories', label: 'Categories', description: 'Test categories used to organize the service catalog.', icon: ListTree, path: '/diagnostics/lab/categories' },
  { key: 'services', label: 'Services', description: 'The Laboratory/Radiology test catalog — pricing, category, and outsourcing.', icon: FlaskConical, path: '/diagnostics/lab/services' },
  { key: 'packages', label: 'Packages', description: 'Bundled test packages (e.g. Lipid Profile) at a fixed price.', icon: PackageSearch, path: '/diagnostics/lab/packages' },
  { key: 'external-labs', label: 'External Labs', description: 'Providers tests are outsourced to.', icon: Building2, path: '/diagnostics/lab/external-labs' },
];

// The real backend workflow module (HMS.Modules.Laboratory) — sample collection through
// result entry, verification, and report release. Gated by its own 'laboratory' tenant
// feature (distinct from this hub's own 'central-laboratory' feature — see
// FeatureCatalog.cs's doc comment), so this card is only shown when a user actually holds
// that permission/feature; a direct URL visit is still enforced server- and route-side
// regardless.
const workflowCard: HubCard = {
  key: 'workflow',
  label: 'Laboratory Workflow',
  description: 'Sample collection through result entry, verification, and report release — the day-to-day lab worklist.',
  icon: Microscope,
  path: '/diagnostics/lab/dashboard',
};

/**
 * Central Laboratory's landing page ('/diagnostics/lab') — a card grid linking into
 * Categories/Services/Packages/External Labs, no persistent sub-nav/tab strip. Mirrors
 * PharmacyHubPage's exact skeleton.
 */
function HubCardGrid({ cards }: { cards: HubCard[] }) {
  return (
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
  );
}

export default function CentralLaboratoryHubPage() {
  const { hasFeature } = useAuth();

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
        {hasFeature('laboratory') && (
          <section className="flex flex-col gap-3">
            <h2 className="text-sm font-semibold uppercase tracking-wide text-muted-foreground">Workflow</h2>
            <HubCardGrid cards={[workflowCard]} />
          </section>
        )}

        <section className="flex flex-col gap-3">
          <h2 className="text-sm font-semibold uppercase tracking-wide text-muted-foreground">Browse</h2>
          <HubCardGrid cards={catalogCards} />
        </section>
      </div>
    </div>
  );
}
