import { Construction, type LucideIcon } from 'lucide-react';
import { Card, CardContent } from '@/components/ui/card';

interface PlaceholderPageProps {
  title: string;
  description: string;
  icon?: LucideIcon;
}

// Every module route renders this — title + description only, per the
// application-shell scope (no workflows, no data, no API calls).
export function PlaceholderPage({ title, description, icon: Icon }: PlaceholderPageProps) {
  return (
    <div className="flex flex-1 flex-col">
      {/* Centered, brand-colored banner — matches Reception & Registration's
          page header (uses the same admin-editable Page banner token from
          Theme & Branding), applied uniformly across every module page. */}
      <div className="flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        <div className="flex items-center gap-3">
          {Icon && (
            <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
              <Icon className="h-5 w-5" />
            </span>
          )}
          <h1 className="text-xl font-semibold tracking-tight">{title}</h1>
        </div>
        <p className="max-w-2xl text-sm text-page-banner-foreground/85">{description}</p>
      </div>

      <div className="flex flex-1 flex-col p-6 lg:p-8">
        <Card className="border-dashed">
          <CardContent className="flex flex-col items-center justify-center gap-3 py-16 text-center">
            <span className="flex h-12 w-12 items-center justify-center rounded-full bg-muted text-muted-foreground">
              <Construction className="h-6 w-6" />
            </span>
            <p className="text-sm font-medium text-foreground">Module not yet implemented</p>
            <p className="max-w-sm text-sm text-muted-foreground">
              This screen is a placeholder in the application shell. The full workflow, data model, and API for{' '}
              <span className="font-medium text-foreground">{title}</span> will be built out separately.
            </p>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
