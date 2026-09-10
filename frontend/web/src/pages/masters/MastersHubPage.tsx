import { useQueries } from '@tanstack/react-query';
import { ArrowRight, Building2, Database, FlaskConical, ListTree, PackageSearch, type LucideIcon } from 'lucide-react';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { PageBanner } from '@/components/PageBanner';
import { Card, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { getAllMasterConfigs, getMasterStore, MASTER_SECTIONS } from '@/features/masters';

// Diagnostic Tests is superseded by Central Laboratory's own Categories/Services/Packages/
// External Labs screens (frontend/web/src/pages/diagnostics/) — every Laboratory/Radiology row
// has been migrated there and deactivated. Excluded from this hub only; the config/store stays
// registered in Masters' registry.ts since Procedure Billing still reads it (useDiagnosticTestServices).
const HIDDEN_FROM_HUB = new Set(['diagnosticTest']);

interface StaticReferenceCard {
  key: string;
  label: string;
  description: string;
  icon: LucideIcon;
  path: string;
}

// The Laboratory/Radiology test catalog (HMS.Modules.Masters' Diagnostic* entities) — bespoke
// hand-written pages, not the generic MasterEntityConfig engine every other card below is driven
// from, so they're listed here as static cards rather than through getAllMasterConfigs(). Lives
// under the 'Hospital Reference Data' section since it's reference data both Laboratory and
// Radiology billing read, not a Central Laboratory-only concern — see
// CentralLaboratoryHubPage.tsx's own note on why its old "Browse" cards moved here. Routes stay
// under '/diagnostics/lab/...' and keep their existing feature/permission route guards
// (routes.tsx's diagnosticsRoutes) — only the entry point moved.
const diagnosticCatalogCards: StaticReferenceCard[] = [
  { key: 'diagnostic-categories', label: 'Diagnostic Categories', description: 'Test categories used to organize the Laboratory/Radiology service catalog.', icon: ListTree, path: '/diagnostics/lab/categories' },
  { key: 'diagnostic-services', label: 'Diagnostic Services', description: 'The Laboratory/Radiology test catalog — pricing, category, and outsourcing.', icon: FlaskConical, path: '/diagnostics/lab/services' },
  { key: 'diagnostic-packages', label: 'Diagnostic Packages', description: 'Bundled test packages (e.g. Lipid Profile) at a fixed price.', icon: PackageSearch, path: '/diagnostics/lab/packages' },
  { key: 'diagnostic-external-labs', label: 'External Labs', description: 'Providers tests are outsourced to.', icon: Building2, path: '/diagnostics/lab/external-labs' },
];

function ReferenceDataCard({
  icon: Icon,
  label,
  description,
  path,
  count,
  showCount = false,
}: {
  icon: LucideIcon;
  label: string;
  description: string;
  path: string;
  count?: number;
  showCount?: boolean;
}) {
  return (
    <Link to={path} className="block">
      <Card className="h-full transition-all hover:border-primary/40 hover:bg-accent/40 hover:shadow-soft-lg">
        <CardHeader>
          <div className="flex items-center justify-between">
            <span className="flex h-9 w-9 items-center justify-center rounded-md bg-primary/10 text-primary">
              <Icon className="h-4.5 w-4.5" />
            </span>
            <div className="flex items-center gap-2">
              {showCount && (
                <span className="rounded-full bg-muted px-2 py-0.5 text-[11px] font-medium text-muted-foreground">
                  {count === undefined ? '…' : `${count} record${count === 1 ? '' : 's'}`}
                </span>
              )}
              <ArrowRight className="h-4 w-4 text-muted-foreground" />
            </div>
          </div>
          <CardTitle className="text-base">{label}</CardTitle>
          <CardDescription>{description}</CardDescription>
        </CardHeader>
      </Card>
    </Link>
  );
}

export default function MastersHubPage() {
  const configs = getAllMasterConfigs().filter((config) => !HIDDEN_FROM_HUB.has(config.key));
  const [activeSection, setActiveSection] = useState<string>(MASTER_SECTIONS[0]);

  // Cheap per-entity count for the hub cards below — only meta.totalCount is needed, so a
  // pageSize of 1 keeps this to 16 lightweight requests instead of fetching every record.
  const countQueries = useQueries({
    queries: configs.map((config) => ({
      queryKey: ['masters', config.key, 'count'],
      queryFn: () => getMasterStore(config.key)?.list({ page: 1, pageSize: 1 }),
    })),
  });
  const countByKey = new Map(configs.map((config, index) => [config.key, countQueries[index].data?.meta.totalCount]));

  return (
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={Database}
        title="Hospital Reference Data"
        subtitle="Reference data grouped by the module that owns it — Hospital, HR, Pharmacy & Inventory, and Finance."
      />

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
        {/* Tabs are the primary nav here, not an add-on filter — selecting one shows only that
            module's masters, rather than every section stacked one under another. */}
        <Tabs value={activeSection} onValueChange={setActiveSection}>
          <TabsList>
            {MASTER_SECTIONS.map((section) => (
              <TabsTrigger key={section} value={section}>
                {section}
              </TabsTrigger>
            ))}
          </TabsList>

          {MASTER_SECTIONS.map((section) => {
            const sectionConfigs = configs.filter((config) => config.section === section);
            const extraCards = section === 'Hospital Reference Data' ? diagnosticCatalogCards : [];
            return (
              <TabsContent key={section} value={section} className="pt-4">
                {sectionConfigs.length === 0 && extraCards.length === 0 ? (
                  <p className="text-sm text-muted-foreground">No reference data in this section yet.</p>
                ) : (
                  <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
                    {sectionConfigs.map((config) => (
                      <ReferenceDataCard
                        key={config.key}
                        icon={config.icon}
                        label={config.labelPlural}
                        description={config.description}
                        path={`/admin/masters/${config.key}`}
                        count={countByKey.get(config.key)}
                        showCount
                      />
                    ))}
                    {extraCards.map((card) => (
                      <ReferenceDataCard key={card.key} icon={card.icon} label={card.label} description={card.description} path={card.path} />
                    ))}
                  </div>
                )}
              </TabsContent>
            );
          })}
        </Tabs>
      </div>
    </div>
  );
}
