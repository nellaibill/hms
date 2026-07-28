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
    <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
      <div className="flex items-start gap-3 border-b border-border pb-4">
        {Icon && (
          <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-md bg-primary/10 text-primary">
            <Icon className="h-5 w-5" />
          </span>
        )}
        <div>
          <h1 className="text-xl font-semibold tracking-tight text-foreground">{title}</h1>
          <p className="mt-1 max-w-2xl text-sm text-muted-foreground">{description}</p>
        </div>
      </div>

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
  );
}
