import { useQuery } from '@tanstack/react-query';
import { ArrowLeft, Building2, DatabaseBackup, Download, Loader2, LogOut, ShieldCheck } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { ThemeToggle } from '@/components/shell/ThemeToggle';
import { formatFileSize } from '@/features/documents/utils/format';
import { usePlatformAuth } from '@/features/platformAuth/PlatformAuthContext';
import { platformBackupsApi } from '@/services/apiClient';

async function downloadBackup(key: string) {
  const blob = await platformBackupsApi.download(key);
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = `${key}-${new Date().toISOString().slice(0, 10)}.dump`;
  link.click();
  URL.revokeObjectURL(url);
}

export default function PlatformBackupsPage() {
  const { logout } = usePlatformAuth();
  const navigate = useNavigate();
  const { data, isPending, isError } = useQuery({
    queryKey: ['platform', 'backups'],
    queryFn: () => platformBackupsApi.getAll(),
  });

  return (
    <div className="min-h-screen bg-muted/30">
      <header className="flex items-center justify-between border-b bg-background px-6 py-4">
        <div className="flex items-center gap-2 font-semibold">
          <Building2 className="size-5 text-primary" />
          Platform Portal
        </div>
        <div className="flex items-center gap-2">
          <ThemeToggle />
          <Button variant="outline" size="sm" onClick={() => navigate('/platform/security')}>
            <ShieldCheck className="mr-2 size-4" />
            Security
          </Button>
          <Button variant="outline" size="sm" onClick={logout}>
            <LogOut className="mr-2 size-4" />
            Sign out
          </Button>
        </div>
      </header>

      <main className="flex flex-col gap-6 p-6 lg:p-8">
        <div className="flex flex-col gap-1">
          <Button variant="ghost" size="sm" className="w-fit gap-1.5 text-muted-foreground" onClick={() => navigate('/platform/dashboard')}>
            <ArrowLeft className="h-4 w-4" />
            Back to dashboard
          </Button>
          <div className="flex items-center gap-2">
            <DatabaseBackup className="h-5 w-5 text-primary" />
            <h1 className="text-xl font-semibold tracking-tight">Database Backups</h1>
          </div>
          <p className="text-sm text-muted-foreground">
            Masters plus every active hospital's latest automated daily backup (1:00 AM IST). Older backups are retained on the server for a limited window but not browsable here — only the most recent one per database is available to download.
          </p>
        </div>

        {isPending && (
          <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" />
            Loading…
          </div>
        )}

        {!isPending && isError && (
          <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
            Failed to load backups.
          </p>
        )}

        {!isPending && !isError && data && (
          <div className="overflow-x-auto rounded-lg border border-border bg-background">
            <table className="w-full text-sm">
              <thead className="bg-sidebar-active text-left text-xs font-medium uppercase tracking-wide text-sidebar-active-foreground">
                <tr>
                  <th className="px-4 py-2">Database</th>
                  <th className="px-4 py-2">Last Backup</th>
                  <th className="px-4 py-2">Size</th>
                  <th className="px-4 py-2 text-right">Action</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border">
                {data.map((backup) => (
                  <tr key={backup.key}>
                    <td className="px-4 py-2.5 font-medium text-foreground">{backup.label}</td>
                    <td className="px-4 py-2.5 text-muted-foreground">
                      {backup.isAvailable ? (
                        backup.lastBackupDate
                      ) : (
                        <Badge variant="secondary" className="text-[10px]">
                          Not backed up yet
                        </Badge>
                      )}
                    </td>
                    <td className="px-4 py-2.5 text-muted-foreground">{backup.sizeBytes !== null ? formatFileSize(backup.sizeBytes) : '—'}</td>
                    <td className="px-4 py-2.5 text-right">
                      <Button
                        type="button"
                        size="sm"
                        variant="outline"
                        className="gap-1.5"
                        disabled={!backup.isAvailable}
                        onClick={() => downloadBackup(backup.key)}
                      >
                        <Download className="h-4 w-4" />
                        Download
                      </Button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </main>
    </div>
  );
}
