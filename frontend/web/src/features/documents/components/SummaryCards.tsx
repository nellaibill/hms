import { Archive, Files, HardDrive, UploadCloud } from 'lucide-react';
import { Card } from '@/components/ui/card';
import { cn } from '@/lib/utils';
import { formatFileSize } from '../utils/format';
import type { DocumentSummaryResponse } from '@hms/shared';

interface SummaryCardsProps {
  stats: DocumentSummaryResponse;
}

const CARD_META = [
  { key: 'total', label: 'Total Documents', icon: Files, iconClass: 'bg-primary/10 text-primary' },
  { key: 'uploadedToday', label: 'Uploaded Today', icon: UploadCloud, iconClass: 'bg-success/10 text-success' },
  { key: 'archived', label: 'Archived Documents', icon: Archive, iconClass: 'bg-muted text-muted-foreground' },
  { key: 'storage', label: 'Storage Used', icon: HardDrive, iconClass: 'bg-info/10 text-info' },
] as const;

export function SummaryCards({ stats }: SummaryCardsProps) {
  const values: Record<(typeof CARD_META)[number]['key'], string> = {
    total: String(stats.total),
    uploadedToday: String(stats.uploadedToday),
    archived: String(stats.archived),
    storage: formatFileSize(stats.storageUsedBytes),
  };

  return (
    <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
      {CARD_META.map(({ key, label, icon: Icon, iconClass }) => (
        <Card key={key} className="flex items-center gap-4 p-5">
          <span className={cn('flex h-11 w-11 shrink-0 items-center justify-center rounded-lg', iconClass)}>
            <Icon className="h-5 w-5" aria-hidden="true" />
          </span>
          <div className="min-w-0">
            <p className="text-2xl font-semibold tabular-nums tracking-tight text-foreground">{values[key]}</p>
            <p className="truncate text-sm text-muted-foreground">{label}</p>
          </div>
        </Card>
      ))}
    </div>
  );
}
