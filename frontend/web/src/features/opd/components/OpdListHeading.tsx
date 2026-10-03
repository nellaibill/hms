import type { ReactNode } from 'react';
import { Card, CardContent } from '@/components/ui/card';
import { formatDateRangeLabel } from '@/lib/reportDateRange';

interface OpdListHeadingProps {
  title: string;
  /** The filter bar's From/To date-input values — shown as the range the list covers. */
  from: string;
  to: string;
  /** Right-aligned extras, e.g. the Patient List's total badge. */
  children?: ReactNode;
}

/** Heading card above each date-filtered OPD tab, labelled with the From/To range the filter
 * bar is actually querying (not just one end of it). */
export function OpdListHeading({ title, from, to, children }: OpdListHeadingProps) {
  const range = formatDateRangeLabel(from, to);
  return (
    <Card>
      <CardContent className="flex flex-wrap items-center justify-between gap-3 py-4">
        <h2 className="text-sm font-semibold text-foreground">
          {title}
          {range && ` (${range})`}
        </h2>
        {children}
      </CardContent>
    </Card>
  );
}
