import { ApiError, BILL_PRINT_FORMATS, type BillPrintFormat, type PharmacySettings } from '@hms/shared';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Loader2, Save } from 'lucide-react';
import { useEffect, useState } from 'react';
import { Button } from '@/components/ui/button';
import { useToast } from '@/components/ui/toast-context';
import { useAuth } from '@/features/auth/AuthContext';
import { SETUP_LINKS } from '@/features/pharmacy/shell/links';
import { PharmacyPage } from '@/features/pharmacy/shell/PharmacyPage';
import { pharmacyApi } from '@/services/apiClient';

const FORMAT_LABELS: Record<BillPrintFormat, string> = {
  A5: 'A5 half sheet (GST invoice)',
  A4: 'A4 full sheet (GST invoice)',
  Thermal80: '80 mm thermal roll',
};

const input = 'h-10 w-full rounded-md border border-input bg-background px-3 text-sm outline-none focus:border-primary focus:ring-2 focus:ring-primary/20 disabled:opacity-60';
const label = 'flex flex-col gap-1 text-xs font-medium uppercase tracking-wide text-muted-foreground';

/** What prints on every pharmacy bill, and the counter's rules. Set once at go-live. */
export default function SettingsPage() {
  const queryClient = useQueryClient();
  const { toast } = useToast();
  const { hasPermission } = useAuth();
  const canEdit = hasPermission('pharmacy.edit');

  const { data, isPending } = useQuery({ queryKey: ['pharmacy', 'settings'], queryFn: () => pharmacyApi.getSettings() });
  const [form, setForm] = useState<PharmacySettings | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (data) setForm(data);
  }, [data]);

  const mutation = useMutation({
    mutationFn: (request: PharmacySettings) => pharmacyApi.updateSettings(request),
    onSuccess: (saved) => {
      queryClient.setQueryData(['pharmacy', 'settings'], saved);
      setError(null);
      toast({ title: 'Settings saved', variant: 'success' });
    },
    onError: (failure) => setError(failure instanceof ApiError ? (failure.validationErrors?.[0]?.message ?? failure.message) : 'Could not save the settings.'),
  });

  const set = <K extends keyof PharmacySettings>(key: K, value: PharmacySettings[K]) => setForm((current) => (current ? { ...current, [key]: value } : current));

  return (
    <PharmacyPage
      title="Bill and counter settings"
      subtitle="What prints on every pharmacy bill, and the rules the counter follows."
      subLinks={SETUP_LINKS}
      actions={
        canEdit && (
          <Button size="sm" onClick={() => form && mutation.mutate(form)} disabled={!form || mutation.isPending}>
            {mutation.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Save className="h-4 w-4" />}
            Save
          </Button>
        )
      }
    >
      {isPending || !form ? (
        <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" />
          Loading…
        </div>
      ) : (
        <fieldset disabled={!canEdit} className="grid gap-4 lg:grid-cols-2">
          <section className="flex flex-col gap-3 rounded-lg border border-border bg-background p-4">
            <h2 className="text-sm font-semibold text-foreground">Printed on the bill</h2>
            <label className={label}>
              Pharmacy name
              <input value={form.pharmacyName ?? ''} maxLength={150} placeholder="Leave blank to print the hospital's name" onChange={(event) => set('pharmacyName', event.target.value)} className={input} />
            </label>
            <div className="grid gap-3 sm:grid-cols-2">
              <label className={label}>
                GSTIN
                <input value={form.gstin ?? ''} maxLength={15} placeholder="33ABCDE1234F1Z5" onChange={(event) => set('gstin', event.target.value.toUpperCase())} className={`${input} font-mono`} />
              </label>
              <label className={label}>
                Phone
                <input value={form.phone ?? ''} maxLength={50} onChange={(event) => set('phone', event.target.value)} className={input} />
              </label>
            </div>
            <label className={label}>
              Drug licence number(s)
              <input value={form.drugLicenceNo ?? ''} maxLength={200} placeholder="TN-04-20B-00123, TN-04-21B-00124" onChange={(event) => set('drugLicenceNo', event.target.value)} className={input} />
            </label>
            <label className={label}>
              Address
              <input value={form.address ?? ''} maxLength={500} placeholder="Leave blank to print the hospital's address" onChange={(event) => set('address', event.target.value)} className={input} />
            </label>
            <label className={label}>
              Footer note
              <input value={form.billFooter ?? ''} maxLength={500} placeholder="Medicines once sold are taken back only with the bill, within 7 days." onChange={(event) => set('billFooter', event.target.value)} className={input} />
            </label>
            <p className="text-xs normal-case text-muted-foreground">With a GSTIN the bill prints as a tax invoice; without one it prints as a pharmacy bill.</p>
          </section>

          <section className="flex h-fit flex-col gap-3 rounded-lg border border-border bg-background p-4">
            <h2 className="text-sm font-semibold text-foreground">Counter</h2>
            <label className={label}>
              Bill paper
              <select value={form.printFormat} onChange={(event) => set('printFormat', event.target.value as BillPrintFormat)} className={input}>
                {BILL_PRINT_FORMATS.map((format) => (
                  <option key={format} value={format}>
                    {FORMAT_LABELS[format]}
                  </option>
                ))}
              </select>
            </label>
            <label className={label}>
              Highest discount the counter may give (%)
              <input type="number" min={0} max={100} step={0.5} value={form.maxDiscountPercent} onChange={(event) => set('maxDiscountPercent', Number(event.target.value))} className={input} />
            </label>
            <label className={label}>
              Warn about expiry this many days ahead
              <input type="number" min={1} max={730} value={form.expiryAlertDays} onChange={(event) => set('expiryAlertDays', Number(event.target.value))} className={input} />
            </label>
          </section>

          {error && (
            <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive lg:col-span-2">
              {error}
            </p>
          )}
        </fieldset>
      )}
    </PharmacyPage>
  );
}
