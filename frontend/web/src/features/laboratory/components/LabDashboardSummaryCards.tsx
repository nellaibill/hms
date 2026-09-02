import {
  AlertTriangle,
  CheckCircle2,
  ClipboardList,
  Clock3,
  FileCheck2,
  FlaskConical,
  Loader2,
  Microscope,
  PackageCheck,
  SendToBack,
  TestTube,
} from 'lucide-react';
import { Card, CardContent } from '@/components/ui/card';
import type { LabDashboardSummary } from '../types';

interface Tile {
  key: keyof LabDashboardSummary;
  label: string;
  icon: typeof FlaskConical;
  emphasis?: 'warning' | 'destructive' | 'success';
}

const TILES: Tile[] = [
  { key: 'totalRequestsToday', label: "Today's Requests", icon: ClipboardList },
  { key: 'pendingSampleCollection', label: 'Pending Collection', icon: Clock3, emphasis: 'warning' },
  { key: 'samplesCollected', label: 'Samples Collected', icon: TestTube },
  { key: 'samplesReceived', label: 'Samples Received', icon: PackageCheck },
  { key: 'testsInProgress', label: 'Tests In Progress', icon: Microscope, emphasis: 'warning' },
  { key: 'resultsPendingEntry', label: 'Results Pending Entry', icon: FlaskConical, emphasis: 'warning' },
  { key: 'pendingVerification', label: 'Pending Verification', icon: FileCheck2, emphasis: 'warning' },
  { key: 'reportsReady', label: 'Reports Ready', icon: CheckCircle2, emphasis: 'success' },
  { key: 'reportsReleased', label: 'Reports Released', icon: SendToBack, emphasis: 'success' },
  { key: 'rejectedOrRecollectionRequired', label: 'Rejected / Recollection', icon: AlertTriangle, emphasis: 'destructive' },
];

const EMPHASIS_CLASSES: Record<NonNullable<Tile['emphasis']>, string> = {
  warning: 'bg-warning/15 text-warning',
  destructive: 'bg-destructive/15 text-destructive',
  success: 'bg-success/15 text-success',
};

interface LabDashboardSummaryCardsProps {
  summary: LabDashboardSummary | undefined;
  isLoading: boolean;
}

/** The lab worklist dashboard's ten summary tiles — one per LabDashboardSummaryResponse field. */
export function LabDashboardSummaryCards({ summary, isLoading }: LabDashboardSummaryCardsProps) {
  if (isLoading) {
    return (
      <div className="flex items-center justify-center gap-2 py-10 text-sm text-muted-foreground">
        <Loader2 className="h-4 w-4 animate-spin" />
        Loading dashboard…
      </div>
    );
  }

  return (
    <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-5">
      {TILES.map((tile) => {
        const Icon = tile.icon;
        const iconClasses = tile.emphasis ? EMPHASIS_CLASSES[tile.emphasis] : 'bg-primary/10 text-primary';
        return (
          <Card key={tile.key}>
            <CardContent className="flex items-center gap-3 p-4">
              <span className={`flex h-9 w-9 shrink-0 items-center justify-center rounded-md ${iconClasses}`}>
                <Icon className="h-4.5 w-4.5" />
              </span>
              <div className="flex min-w-0 flex-col">
                <span className="text-lg font-semibold tabular-nums text-foreground">{summary?.[tile.key] ?? 0}</span>
                <span className="truncate text-[11px] uppercase tracking-wide text-muted-foreground">{tile.label}</span>
              </div>
            </CardContent>
          </Card>
        );
      })}
    </div>
  );
}
