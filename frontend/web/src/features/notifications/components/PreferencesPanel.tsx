import { Switch } from '@/components/ui/switch';
import { NOTIFICATION_CATEGORIES } from '../constants';
import { useNotificationPreferencesQuery, useUpsertNotificationPreferenceMutation } from '../hooks/useNotificationPreferencesQuery';

// Mirrors NotificationPreference.Create's own defaults (docs/DecisionLog.md ADR-029) — a
// category with no saved row uses these, so "never saved a preference" and "saved these
// values explicitly" render identically.
const DEFAULT_IN_APP = true;
const DEFAULT_EMAIL = true;
const DEFAULT_SMS = false;

export function PreferencesPanel() {
  const preferencesQuery = useNotificationPreferencesQuery();
  const upsertMutation = useUpsertNotificationPreferenceMutation();

  const savedByCategory = new Map((preferencesQuery.data ?? []).map((pref) => [pref.category, pref]));

  function handleToggle(category: string, field: 'inAppEnabled' | 'emailEnabled' | 'smsEnabled', value: boolean) {
    const saved = savedByCategory.get(category);
    upsertMutation.mutate({
      category,
      inAppEnabled: saved?.inAppEnabled ?? DEFAULT_IN_APP,
      emailEnabled: saved?.emailEnabled ?? DEFAULT_EMAIL,
      smsEnabled: saved?.smsEnabled ?? DEFAULT_SMS,
      [field]: value,
    });
  }

  if (preferencesQuery.isPending) {
    return <p className="py-6 text-center text-sm text-muted-foreground">Loading preferences…</p>;
  }

  return (
    <div className="overflow-x-auto">
      <table className="w-full min-w-[480px] border-collapse text-sm">
        <thead>
          <tr className="border-b border-border text-left text-xs font-medium uppercase tracking-wide text-muted-foreground">
            <th className="py-2 pr-4">Category</th>
            <th className="w-20 py-2 text-center">In-app</th>
            <th className="w-20 py-2 text-center">Email</th>
            <th className="w-20 py-2 text-center">SMS</th>
          </tr>
        </thead>
        <tbody>
          {NOTIFICATION_CATEGORIES.map(({ value, label }) => {
            const saved = savedByCategory.get(value);
            // Emergency notifications bypass preferences entirely on the server (every
            // channel, always — see NotificationService.ResolveChannelsAsync's own doc
            // comment) — showing editable toggles here would misrepresent that.
            const isEmergency = value === 'emergency';
            return (
              <tr key={value} className="border-b border-border/60">
                <td className="py-3 pr-4 font-medium text-foreground">
                  {label}
                  {isEmergency && <span className="ml-2 text-xs font-normal text-muted-foreground">(always sent, every channel)</span>}
                </td>
                <td className="py-3 text-center">
                  <Switch
                    aria-label={`In-app notifications for ${label}`}
                    checked={isEmergency || (saved?.inAppEnabled ?? DEFAULT_IN_APP)}
                    disabled={isEmergency}
                    onCheckedChange={(checked) => handleToggle(value, 'inAppEnabled', checked)}
                    className="mx-auto"
                  />
                </td>
                <td className="py-3 text-center">
                  <Switch
                    aria-label={`Email notifications for ${label}`}
                    checked={isEmergency || (saved?.emailEnabled ?? DEFAULT_EMAIL)}
                    disabled={isEmergency}
                    onCheckedChange={(checked) => handleToggle(value, 'emailEnabled', checked)}
                    className="mx-auto"
                  />
                </td>
                <td className="py-3 text-center">
                  <Switch
                    aria-label={`SMS notifications for ${label}`}
                    checked={isEmergency || (saved?.smsEnabled ?? DEFAULT_SMS)}
                    disabled={isEmergency}
                    onCheckedChange={(checked) => handleToggle(value, 'smsEnabled', checked)}
                    className="mx-auto"
                  />
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}
