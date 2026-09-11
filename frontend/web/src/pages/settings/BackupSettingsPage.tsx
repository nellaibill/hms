import { useQuery } from '@tanstack/react-query';
import { DatabaseBackup, Download, Loader2 } from 'lucide-react';
import { PageBanner } from '@/components/PageBanner';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { formatFileSize } from '@/features/documents/utils/format';
import { backupsApi } from '@/services/apiClient';

async function downloadMyBackup() {
  const blob = await backupsApi.downloadMine();
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = `backup-${new Date().toISOString().slice(0, 10)}.dump`;
  link.click();
  URL.revokeObjectURL(url);
}

export default function BackupSettingsPage() {
  const { data, isPending, isError } = useQuery({
    queryKey: ['backups', 'mine'],
    queryFn: () => backupsApi.getMine(),
  });

  return (
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={DatabaseBackup}
        title="Database Backup"
        subtitle="A full backup of this hospital's data is taken automatically every day at 1:00 AM IST — download the latest one here."
        backTo="/admin/settings"
        backLabel="Back to settings"
      />

      <div className="flex flex-1 flex-col gap-4 p-6 lg:p-8">
        <Card className="max-w-xl">
          <CardContent className="flex items-center justify-between gap-4 p-4">
            {isPending && (
              <div className="flex items-center gap-2 text-sm text-muted-foreground">
                <Loader2 className="h-4 w-4 animate-spin" />
                Loading…
              </div>
            )}

            {!isPending && isError && <p className="text-sm text-destructive">Failed to load backup status.</p>}

            {!isPending && !isError && data && (
              <>
                <div className="flex flex-col gap-0.5">
                  {data.isAvailable ? (
                    <>
                      <span className="text-sm font-medium text-foreground">Last backup: {data.lastBackupDate}</span>
                      <span className="text-xs text-muted-foreground">{data.sizeBytes !== null ? formatFileSize(data.sizeBytes) : null}</span>
                    </>
                  ) : (
                    <span className="text-sm text-muted-foreground">No backup has run yet — check back after 1:00 AM IST.</span>
                  )}
                </div>
                <Button type="button" variant="outline" className="shrink-0 gap-1.5" disabled={!data.isAvailable} onClick={downloadMyBackup}>
                  <Download className="h-4 w-4" />
                  Download
                </Button>
              </>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
