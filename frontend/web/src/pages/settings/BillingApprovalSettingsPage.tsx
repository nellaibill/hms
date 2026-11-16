import { ApiError, type BillingApprovalSettings } from '@hms/shared';
import { BadgeCheck, Info, Loader2 } from 'lucide-react';
import { useEffect, useState } from 'react';
import { PageBanner } from '@/components/PageBanner';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Switch } from '@/components/ui/switch';
import { useToast } from '@/components/ui/toast-context';
import { useAuth } from '@/features/auth/AuthContext';
import { useBillingSettingsQuery, useUpdateBillingSettingsMutation } from '@/features/billing';

type SettingKey = keyof BillingApprovalSettings;

interface SettingRow {
  key: SettingKey;
  label: string;
  whenOn: string;
  whenOff: string;
}

const DISCOUNT_ROWS: SettingRow[] = [
  {
    key: 'discountsDisabled',
    label: 'Disable discounts',
    whenOn:
      '"Add discount" is hidden on OPD billing and on saved bills, and new discounts are refused. Discounts already given stay.',
    whenOff: 'Billing staff can add a discount to a bill.',
  },
  {
    key: 'discountsNeedApproval',
    label: 'Send discounts for approval',
    whenOn:
      'A discount waits for approval — the consultant for their consultation fee, the Super Admin for the rest — and the bill can’t be paid until then.',
    whenOff: 'A discount applies at once, recorded as approved by the person who added it.',
  },
];

const REVERT_ROWS: SettingRow[] = [
  {
    key: 'revertsNeedApproval',
    label: 'Send bill reverts for approval',
    whenOn: 'A revert request waits for the Super Admin to approve or reject it.',
    whenOff:
      'The bill is reverted as soon as it’s requested, recorded as approved by the person who asked. Only today’s OP bills can be reverted either way.',
  },
];

const ALL_KEYS: SettingKey[] = [
  'discountsDisabled',
  'discountsNeedApproval',
  'revertsNeedApproval',
];

/**
 * Settings → Billing Approvals (ADR-136): whether discounts are allowed and whether discounts and
 * bill reverts go to the Super Admin for approval. Hospital-wide; the API enforces the same
 * choices. Viewing needs identity-administration.view (the route guard); saving needs .edit,
 * which the backend enforces, so the switches are read-only without it.
 */
export default function BillingApprovalSettingsPage() {
  const { hasPermission } = useAuth();
  const canEdit = hasPermission('identity-administration.edit');
  const { toast } = useToast();
  const { data: saved, isPending, isError } = useBillingSettingsQuery();
  const mutation = useUpdateBillingSettingsMutation();

  const [draft, setDraft] = useState<BillingApprovalSettings | null>(null);
  useEffect(() => {
    if (saved) setDraft(saved);
  }, [saved]);

  const isDirty = !!saved && !!draft && ALL_KEYS.some((key) => saved[key] !== draft[key]);

  function toggle(key: SettingKey, on: boolean) {
    setDraft((current) => (current ? { ...current, [key]: on } : current));
  }

  function handleSave() {
    if (!draft) return;
    mutation.mutate(draft, {
      onSuccess: () =>
        toast({
          title: 'Billing approvals saved',
          description: 'They apply to the next discount or revert.',
          variant: 'success',
        }),
      onError: (error) =>
        toast({
          title: 'Could not save billing approvals',
          description:
            error instanceof ApiError ? error.message : 'Something went wrong. Please try again.',
          variant: 'error',
        }),
    });
  }

  function renderRow(
    { key, label, whenOn, whenOff }: SettingRow,
    current: BillingApprovalSettings,
  ) {
    const id = `billing-setting-${key}`;
    const on = current[key];
    // Approval of discounts means nothing while discounts are switched off.
    const inactive = key === 'discountsNeedApproval' && current.discountsDisabled;
    return (
      <div
        key={key}
        className="flex items-center justify-between gap-4 border-b border-border py-3 last:border-b-0"
      >
        <div className={inactive ? 'min-w-0 opacity-60' : 'min-w-0'}>
          <label htmlFor={id} className="text-sm font-medium text-foreground">
            {label}
          </label>
          <p className="text-xs text-muted-foreground">
            {inactive ? 'Not used while discounts are disabled.' : on ? whenOn : whenOff}
          </p>
        </div>
        <div className="flex shrink-0 items-center gap-2">
          <span
            className={
              on
                ? 'w-7 text-right text-xs font-medium text-primary'
                : 'w-7 text-right text-xs text-muted-foreground'
            }
          >
            {on ? 'On' : 'Off'}
          </span>
          <Switch
            id={id}
            checked={on}
            onCheckedChange={(value) => toggle(key, value)}
            disabled={!canEdit || mutation.isPending || inactive}
          />
        </div>
      </div>
    );
  }

  return (
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={BadgeCheck}
        title="Billing Approvals"
        subtitle="Choose whether discounts are allowed and which billing changes need Super Admin approval."
        backTo="/admin/settings"
        backLabel="Back to settings"
      />

      <div className="flex flex-1 flex-col gap-6 p-4 sm:p-6 lg:p-8">
        <p className="flex max-w-3xl items-start gap-2 text-sm text-muted-foreground">
          <Info className="mt-0.5 h-4 w-4 shrink-0" />
          <span>
            A Super Admin’s own discount always applies at once. Requests already waiting for
            approval stay in the Discount Requests and Revert Requests queues after a change.
          </span>
        </p>

        {isPending || (!draft && !isError) ? (
          <div className="flex items-center gap-2 py-4 text-sm text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" />
            Loading…
          </div>
        ) : isError || !draft ? (
          <p
            role="alert"
            className="max-w-3xl rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive"
          >
            The billing approval settings could not be loaded. Refresh the page to try again.
          </p>
        ) : (
          <>
            <Card className="max-w-3xl">
              <CardHeader className="pb-2">
                <CardTitle className="text-base">Discounts</CardTitle>
                <CardDescription>
                  "Add discount" on OPD Billing Entry and "Request Discount" on a saved bill.
                </CardDescription>
              </CardHeader>
              <CardContent className="flex flex-col gap-1">
                {DISCOUNT_ROWS.map((row) => renderRow(row, draft))}
              </CardContent>
            </Card>

            <Card className="max-w-3xl">
              <CardHeader className="pb-2">
                <CardTitle className="text-base">Bill reverts</CardTitle>
                <CardDescription>"Request Revert" on a bill’s page.</CardDescription>
              </CardHeader>
              <CardContent className="flex flex-col gap-1">
                {REVERT_ROWS.map((row) => renderRow(row, draft))}
              </CardContent>
            </Card>
          </>
        )}

        {canEdit ? (
          <div className="flex gap-2">
            <Button type="button" onClick={handleSave} disabled={!isDirty || mutation.isPending}>
              {mutation.isPending ? 'Saving…' : 'Save changes'}
            </Button>
            <Button
              type="button"
              variant="outline"
              onClick={() => saved && setDraft(saved)}
              disabled={!isDirty || mutation.isPending}
            >
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
