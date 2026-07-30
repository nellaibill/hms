import { ArrowRight, Database } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Card, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { getAllMasterConfigs, getMasterStore, MASTER_SECTIONS } from '@/features/masters';

export default function MastersHubPage() {
  const configs = getAllMasterConfigs();

  return (
    <div className="flex flex-1 flex-col">
      {/* Centered, brand-colored banner — matches the Page banner style used across module pages (Theme & Branding → Section headers). */}
      <div className="relative flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        <span className="absolute right-6 top-5 hidden rounded-full bg-page-banner-foreground/15 px-2.5 py-1 text-xs font-medium sm:inline-block">
          Demo data — no backend yet
        </span>
        <div className="flex items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
            <Database className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight">Masters (Reference Data)</h1>
        </div>
        <p className="max-w-2xl text-sm text-page-banner-foreground/85">
          Product classification, brands, units, tax, warehouses, business partners, and finance reference data used
          across Inventory &amp; ERP.
        </p>
      </div>

      <div className="flex flex-1 flex-col gap-8 p-6 lg:p-8">
        {MASTER_SECTIONS.map((section) => {
          const sectionConfigs = configs.filter((config) => config.section === section);
          if (sectionConfigs.length === 0) return null;

          return (
            <section key={section} className="flex flex-col gap-3">
              <h2 className="text-sm font-semibold uppercase tracking-wide text-muted-foreground">{section}</h2>
              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
                {sectionConfigs.map((config) => {
                  const Icon = config.icon;
                  const count = getMasterStore(config.key)?.getAll().length ?? 0;
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
                                {count} record{count === 1 ? '' : 's'}
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
            </section>
          );
        })}
      </div>
    </div>
  );
}
