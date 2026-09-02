import { ClipboardList, ListFilter, Loader2, Microscope } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { LabDashboardSummaryCards, LabWorklistTable, useLabDashboardSummaryQuery, useLabOrdersQuery } from '@/features/laboratory';

const RECENT_ORDERS_COUNT = 10;

/** The Laboratory Workflow landing page ('/diagnostics/lab/dashboard') — the ten
 * LabDashboardSummaryResponse tiles plus a "Recent Activity" strip. Reuses the same worklist
 * query at a small page size for recent activity rather than a dedicated endpoint, the same
 * principle the just-merged "Recent Patient Bills" feature used. */
export default function LabDashboardPage() {
  const { data: summary, isPending: summaryPending } = useLabDashboardSummaryQuery();
  const { data: recentOrders, isPending: ordersPending, isError } = useLabOrdersQuery({ pageSize: RECENT_ORDERS_COUNT, sort: '-createdAt' });

  return (
    <div className="flex flex-1 flex-col">
      <div className="relative flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        <div className="flex items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
            <Microscope className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight">Laboratory Workflow</h1>
        </div>
        <p className="max-w-2xl text-sm text-page-banner-foreground/85">
          Sample collection through result entry, verification, and report release — the day-to-day lab worklist.
        </p>
      </div>

      <div className="flex flex-1 flex-col gap-4 p-6 lg:p-8">
        <LabDashboardSummaryCards summary={summary} isLoading={summaryPending} />

        <div className="flex flex-col gap-2">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-1.5">
              <ClipboardList className="h-4 w-4 text-primary" />
              <h2 className="text-sm font-semibold text-foreground">Recent Activity</h2>
            </div>
            <Button asChild variant="outline" size="sm" className="gap-1.5">
              <Link to="/diagnostics/lab/worklist">
                <ListFilter className="h-3.5 w-3.5" />
                Open Full Worklist
              </Link>
            </Button>
          </div>

          {ordersPending && (
            <div className="flex items-center justify-center gap-2 py-10 text-sm text-muted-foreground">
              <Loader2 className="h-4 w-4 animate-spin" />
              Loading recent activity…
            </div>
          )}

          {isError && (
            <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
              Failed to load recent activity.
            </p>
          )}

          {!ordersPending && !isError && recentOrders && recentOrders.items.length === 0 && (
            <Card className="border-dashed">
              <CardContent className="flex flex-col items-center gap-2 py-10 text-center">
                <p className="text-sm text-muted-foreground">No lab orders recorded yet.</p>
              </CardContent>
            </Card>
          )}

          {!ordersPending && !isError && recentOrders && recentOrders.items.length > 0 && <LabWorklistTable orders={recentOrders.items} />}
        </div>
      </div>
    </div>
  );
}
