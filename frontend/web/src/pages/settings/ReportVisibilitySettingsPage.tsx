import { ApiError } from '@hms/shared';
import { Eye, Info, Loader2 } from 'lucide-react';
import { useEffect, useState } from 'react';
import { PageBanner } from '@/components/PageBanner';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Switch } from '@/components/ui/switch';
import { useToast } from '@/components/ui/toast-context';
import { useAuth } from '@/features/auth/AuthContext';
import { useUpdateReportVisibilityMutation } from '@/features/branding/hooks/useBrandingMutations';
import { useBrandingQuery } from '@/features/branding/hooks/useBrandingQuery';
import { ACCOUNTS_REPORT_LABELS, ACCOUNTS_REPORTS, type AccountsReport } from '@/features/branding/types';

/**
 * Settings → Reports Visibility (ADR-120): which Accounts & Finance report tabs the hospital
 * shows. Hospital-wide — a report switched off is hidden from everyone, Super Admins
 * included. OP Report, Collection Summary and the bill lists/approval queues always show, so they aren't listed.
 * Viewing needs identity-administration.view (the route guard); saving needs .edit, which the
 * backend enforces, so the switches are read-only without it.
 */
export default function ReportVisibilitySettingsPage() {
  const { hasPermission } = useAuth();
  const canEdit = hasPermission('identity-administration.edit');
  const { toast } = useToast();
  const { data: branding, isPending } = useBrandingQuery();
  const mutation = useUpdateReportVisibilityMutation();

  const saved = branding?.reportVisibility.visibleReports;
  const [draft, setDraft] = useState<AccountsReport[]>([]);
  useEffect(() => {
    if (saved) setDraft(saved);
  }, [saved]);

  const isDirty = !!saved && (saved.length !== draft.length || saved.some((report) => !draft.includes(report)));

  function toggle(report: AccountsReport, on: boolean) {
    setDraft((current) => (on ? [...current, report] : current.filter((r) => r !== report)));
  }

  function handleSave() {
    mutation.mutate(draft, {
      onSuccess: () => toast({ title: 'Reports visibility saved', variant: 'success' }),
      onError: (error) =>
        toast({
          title: 'Could not save reports visibility',
          description: error instanceof ApiError ? error.message : 'Something went wrong. Please try again.',
          variant: 'error',
        }),
    });
  }

  return (
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={Eye}
        title="Reports Visibility"
        subtitle="Choose which Accounts & Finance reports this hospital shows."
        backTo="/admin/settings"
        backLabel="Back to settings"
      />

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
        <Card className="max-w-3xl">
          <CardHeader>
            <CardTitle className="text-base">Accounts & Finance reports</CardTitle>
            <CardDescription>
              A report that is switched off is hidden for everyone, Super Admins included. OP Report, Collection Summary,
              Recent Bills, All Invoices, Revert Requests and Discount Requests always show.
            </CardDescription>
          </CardHeader>
          <CardContent className="flex flex-col gap-1">
            {isPending ? (
              <div className="flex items-center gap-2 py-4 text-sm text-muted-foreground">
                <Loader2 className="h-4 w-4 animate-spin" />
                Loading…
              </div>
            ) : (
              ACCOUNTS_REPORTS.map((report) => {
                const { label, description } = ACCOUNTS_REPORT_LABELS[report];
                const id = `report-visibility-${report}`;
                return (
                  <div key={report} className="flex items-center justify-between gap-4 border-b border-border py-3 last:border-b-0">
                    <div className="min-w-0">
                      <label htmlFor={id} className="text-sm font-medium text-foreground">
                        {label}
                      </label>
                      <p className="text-xs text-muted-foreground">{description}</p>
                    </div>
                    <Switch
                      id={id}
                      checked={draft.includes(report)}
                      onCheckedChange={(on) => toggle(report, on)}
                      disabled={!canEdit || mutation.isPending}
                    />
                  </div>
                );
              })
            )}
          </CardContent>
        </Card>

        {canEdit ? (
          <div className="flex gap-2">
            <Button type="button" onClick={handleSave} disabled={!isDirty || mutation.isPending}>
              {mutation.isPending ? 'Saving…' : 'Save changes'}
            </Button>
            <Button type="button" variant="outline" onClick={() => saved && setDraft(saved)} disabled={!isDirty || mutation.isPending}>
              Discard
            </Button>
          </div>
        ) : (
          <p className="flex items-center gap-2 text-sm text-muted-foreground">
            <Info className="h-4 w-4" />
            You can view these settings but not change them.
          </p>
        )}
      </div>
    </div>
  );
}
