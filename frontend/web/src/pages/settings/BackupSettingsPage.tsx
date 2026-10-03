import { useMutation, useQuery } from '@tanstack/react-query';
import { DatabaseBackup, Download, FolderArchive, Loader2, type LucideIcon } from 'lucide-react';
import type { ReactNode } from 'react';
import { PageBanner } from '@/components/PageBanner';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { formatFileSize, todayIsoDate } from '@/features/documents/utils/format';
import { backupsApi } from '@/services/apiClient';

function saveBlob(blob: Blob, fileName: string) {
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = fileName;
  link.click();
  URL.revokeObjectURL(url);
}

async function downloadMyBackup() {
  saveBlob(await backupsApi.downloadMine(), `backup-${new Date().toISOString().slice(0, 10)}.dump`);
}

async function downloadMyFiles() {
  saveBlob(await backupsApi.downloadMyFiles(), `documents-and-images-${todayIsoDate()}.zip`);
}

function pluralizeFiles(count: number) {
  return `${count.toLocaleString('en-IN')} ${count === 1 ? 'file' : 'files'}`;
}

function BackupSection({
  icon: Icon,
  title,
  children,
}: {
  icon: LucideIcon;
  title: string;
  children: ReactNode;
}) {
  return (
    <section className="flex max-w-xl flex-col gap-2">
      <h2 className="flex items-center gap-2 text-sm font-semibold text-foreground">
        <Icon className="h-4 w-4 text-muted-foreground" />
        {title}
      </h2>
      <Card>{children}</Card>
    </section>
  );
}

function LoadingRow() {
  return (
    <div className="flex items-center gap-2 text-sm text-muted-foreground">
      <Loader2 className="h-4 w-4 animate-spin" />
      Loading…
    </div>
  );
}

function DatabaseBackupCard() {
  const { data, isPending, isError } = useQuery({
    queryKey: ['backups', 'mine'],
    queryFn: () => backupsApi.getMine(),
  });

  return (
    <BackupSection icon={DatabaseBackup} title="Database">
      <CardContent className="flex items-center justify-between gap-4 p-4">
        {isPending && <LoadingRow />}

        {!isPending && isError && (
          <p className="text-sm text-destructive">Failed to load backup status.</p>
        )}

        {!isPending && !isError && data && (
          <>
            <div className="flex flex-col gap-0.5">
              {data.isAvailable ? (
                <>
                  <span className="text-sm font-medium text-foreground">
                    Last backup: {data.lastBackupDate}
                  </span>
                  <span className="text-xs text-muted-foreground">
                    {data.sizeBytes !== null ? formatFileSize(data.sizeBytes) : null}
                  </span>
                </>
              ) : (
                <span className="text-sm text-muted-foreground">
                  No backup has run yet — check back after 1:00 AM IST.
                </span>
              )}
            </div>
            <Button
              type="button"
              variant="outline"
              className="shrink-0 gap-1.5"
              disabled={!data.isAvailable}
              onClick={downloadMyBackup}
            >
              <Download className="h-4 w-4" />
              Download
            </Button>
          </>
        )}
      </CardContent>
    </BackupSection>
  );
}

function DocumentsAndImagesCard() {
  const { data, isPending, isError } = useQuery({
    queryKey: ['backups', 'mine', 'files'],
    queryFn: () => backupsApi.getMyFiles(),
  });
  const download = useMutation({ mutationFn: downloadMyFiles });

  return (
    <BackupSection icon={FolderArchive} title="Documents & images">
      <CardContent className="flex flex-col gap-3 p-4">
        {isPending && <LoadingRow />}

        {!isPending && isError && (
          <p className="text-sm text-destructive">Failed to load the list of uploaded files.</p>
        )}

        {!isPending && !isError && data && (
          <>
            <div className="flex items-center justify-between gap-4">
              <div className="flex flex-col gap-0.5">
                {data.fileCount > 0 ? (
                  <>
                    <span className="text-sm font-medium text-foreground">
                      {pluralizeFiles(data.fileCount)}
                    </span>
                    <span className="text-xs text-muted-foreground">
                      {formatFileSize(data.totalSizeBytes)} · zipped when you download
                    </span>
                  </>
                ) : (
                  <span className="text-sm text-muted-foreground">
                    No documents or images have been uploaded yet.
                  </span>
                )}
              </div>
              <Button
                type="button"
                variant="outline"
                className="shrink-0 gap-1.5"
                disabled={data.fileCount === 0 || download.isPending}
                onClick={() => download.mutate()}
              >
                {download.isPending ? (
                  <Loader2 className="h-4 w-4 animate-spin" />
                ) : (
                  <Download className="h-4 w-4" />
                )}
                {download.isPending ? 'Preparing…' : 'Download zip'}
              </Button>
            </div>

            {data.fileCount > 0 && (
              <ul className="grid grid-cols-1 gap-x-6 gap-y-1 border-t pt-3 text-xs sm:grid-cols-2">
                {data.categories.map((category) => (
                  <li key={category.key} className="flex justify-between gap-2">
                    <span className="text-foreground">{category.label}</span>
                    <span className="text-muted-foreground">
                      {category.fileCount > 0
                        ? `${pluralizeFiles(category.fileCount)} · ${formatFileSize(category.sizeBytes)}`
                        : '—'}
                    </span>
                  </li>
                ))}
              </ul>
            )}

            {download.isError && (
              <p className="text-xs text-destructive">
                Couldn’t prepare the zip — please try again.
              </p>
            )}
          </>
        )}
      </CardContent>
    </BackupSection>
  );
}

export default function BackupSettingsPage() {
  return (
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={DatabaseBackup}
        title="Backup"
        subtitle="A full backup of this hospital's data is taken automatically every day at 1:00 AM IST — download the latest one, or a zip of every uploaded document and image, here."
        backTo="/admin/settings"
        backLabel="Back to settings"
      />

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
        <DatabaseBackupCard />
        <DocumentsAndImagesCard />
      </div>
    </div>
  );
}
