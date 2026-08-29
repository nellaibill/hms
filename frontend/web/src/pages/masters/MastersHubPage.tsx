import { useQueries } from '@tanstack/react-query';
import { ArrowRight, Database } from 'lucide-react';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { Card, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { getAllMasterConfigs, getMasterStore, MASTER_SECTIONS } from '@/features/masters';

// Diagnostic Tests is superseded by Central Laboratory's own Categories/Services/Packages/
// External Labs screens (frontend/web/src/pages/diagnostics/) — every Laboratory/Radiology row
// has been migrated there and deactivated. Excluded from this hub only; the config/store stays
// registered in Masters' registry.ts since Procedure Billing still reads it (useDiagnosticTestServices).
const HIDDEN_FROM_HUB = new Set(['diagnosticTest']);

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
      {/* Centered, brand-colored banner — matches the Page banner style used across module pages (Theme & Branding → Section headers). */}
      <div className="relative flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        <div className="flex items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
            <Database className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight">Hospital Reference Data</h1>  {/*Masters (Reference Data)*/}
        </div>
        <p className="max-w-2xl text-sm text-page-banner-foreground/85">
          Reference data grouped by the module that owns it — Hospital, HR, Pharmacy &amp; Inventory, and Finance.
        </p>
      </div>

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
            return (
              <TabsContent key={section} value={section} className="pt-4">
                {sectionConfigs.length === 0 ? (
                  <p className="text-sm text-muted-foreground">No reference data in this section yet.</p>
                ) : (
                  <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
                    {sectionConfigs.map((config) => {
                      const Icon = config.icon;
                      const count = countByKey.get(config.key);
                      return (
                        <Link key={config.key} to={`/admin/masters/${config.key}`} className="block">
                          <Card className="h-full transition-all hover:border-primary/40 hover:bg-accent/40 hover:shadow-soft-lg">
                            <CardHeader>
                              <div className="flex items-center justify-between">
                                <span className="flex h-9 w-9 items-center justify-center rounded-md bg-primary/10 text-primary">
                                  <Icon className="h-4.5 w-4.5" />
                                </span>
                                <div className="flex items-center gap-2">
                                  <span className="rounded-full bg-muted px-2 py-0.5 text-[11px] font-medium text-muted-foreground">
                                    {count === undefined ? '…' : `${count} record${count === 1 ? '' : 's'}`}
                                  </span>
                                  <ArrowRight className="h-4 w-4 text-muted-foreground" />
                                </div>
                              </div>
                              <CardTitle className="text-base">{config.labelPlural}</CardTitle>
                              <CardDescription>{config.description}</CardDescription>
                            </CardHeader>
                          </Card>
                        </Link>
                      );
                    })}
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
