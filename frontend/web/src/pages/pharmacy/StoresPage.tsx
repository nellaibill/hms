import { ApiError, STORE_TYPES, type StoreResponse, type StoreType } from '@hms/shared';
import { Loader2 } from 'lucide-react';
import { useState } from 'react';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Switch } from '@/components/ui/switch';
import { useToast } from '@/components/ui/toast-context';
import { useAuth } from '@/features/auth/AuthContext';
import { SETUP_LINKS } from '@/features/pharmacy/shell/links';
import { PharmacyPage } from '@/features/pharmacy/shell/PharmacyPage';
import { useCreateStoreMutation, useStoresQuery, useUpdateStoreMutation } from '@/features/pharmacy/stores';

const STORE_TYPE_LABELS: Record<StoreType, string> = {
  MainStore: 'Main store',
  Pharmacy: 'Dispensing pharmacy',
  SubStore: 'Sub-store (ward, theatre)',
};

interface StoreDraft {
  code: string;
  name: string;
  storeType: StoreType;
  isActive: boolean;
  isDefault: boolean;
}

const emptyDraft: StoreDraft = { code: '', name: '', storeType: 'Pharmacy', isActive: true, isDefault: false };

/**
 * The places the pharmacy holds stock. Every hospital starts with one default store; this is
 * where a main store, a second counter or a ward sub-store is added. Stock is always received
 * into and dispensed from one named store.
 */
export default function StoresPage() {
  const { hasPermission } = useAuth();
  const { toast } = useToast();
  const { data: stores, isPending, isError } = useStoresQuery();
  const createMutation = useCreateStoreMutation();
  const updateMutation = useUpdateStoreMutation();

  // null = the form is closed; a store = editing it; 'new' = adding one.
  const [editing, setEditing] = useState<StoreResponse | 'new' | null>(null);
  const [draft, setDraft] = useState<StoreDraft>(emptyDraft);
  const [formError, setFormError] = useState<string | null>(null);

  const canCreate = hasPermission('pharmacy.create');
  const canEdit = hasPermission('pharmacy.edit');
  const isSaving = createMutation.isPending || updateMutation.isPending;

  function openNew() {
    setEditing('new');
    setDraft(emptyDraft);
    setFormError(null);
  }

  function openEdit(store: StoreResponse) {
    setEditing(store);
    setDraft({ code: store.code, name: store.name, storeType: store.storeType, isActive: store.isActive, isDefault: store.isDefault });
    setFormError(null);
  }

  function handleSave() {
    if (!draft.name.trim() || (editing === 'new' && !draft.code.trim())) {
      setFormError('Enter a code and a name.');
      return;
    }
    setFormError(null);

    const onError = (error: unknown) => setFormError(error instanceof ApiError ? error.message : 'Could not save the store.');
    const onSuccess = (store: StoreResponse) => {
      toast({ title: 'Store saved', description: `${store.name} (${store.code})`, variant: 'success' });
      setEditing(null);
    };

    if (editing === 'new') {
      createMutation.mutate({ code: draft.code.trim(), name: draft.name.trim(), storeType: draft.storeType, isDefault: draft.isDefault }, { onSuccess, onError });
    } else if (editing) {
      updateMutation.mutate(
        { id: editing.id, request: { name: draft.name.trim(), storeType: draft.storeType, isActive: draft.isActive, isDefault: draft.isDefault } },
        { onSuccess, onError },
      );
    }
  }

  const editingDefault = editing !== null && editing !== 'new' && editing.isDefault;

  return (
    <PharmacyPage title="Stores" subtitle="Where stock is held — main store, dispensing counters and sub-stores." subLinks={SETUP_LINKS}>
        {isPending && (
          <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" />
            Loading stores…
          </div>
        )}

        {isError && (
          <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
            Failed to load stores.
          </p>
        )}

        {stores && (
          <>
            <div className="flex items-center justify-between">
              <p className="text-sm text-muted-foreground">
                Stock goes to the default store whenever a receipt or dispense doesn&apos;t name one.
              </p>
              {canCreate && editing === null && (
                <Button type="button" onClick={openNew}>
                  Add store
                </Button>
              )}
            </div>

            {editing !== null && (
              <Card>
                <CardHeader>
                  <CardTitle className="text-base">{editing === 'new' ? 'Add store' : `Edit ${editing.name}`}</CardTitle>
                </CardHeader>
                <CardContent className="flex flex-col gap-4">
                  {formError && (
                    <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
                      {formError}
                    </p>
                  )}
                  <div className="flex flex-wrap gap-4">
                    <div className="flex w-40 flex-col gap-1.5">
                      <Label htmlFor="store-code">Code</Label>
                      <Input
                        id="store-code"
                        value={draft.code}
                        disabled={editing !== 'new'}
                        maxLength={30}
                        placeholder="IP"
                        onChange={(e) => setDraft((d) => ({ ...d, code: e.target.value }))}
                      />
                    </div>
                    <div className="flex min-w-[220px] flex-1 flex-col gap-1.5">
                      <Label htmlFor="store-name">Name</Label>
                      <Input
                        id="store-name"
                        value={draft.name}
                        maxLength={100}
                        placeholder="IP Pharmacy"
                        onChange={(e) => setDraft((d) => ({ ...d, name: e.target.value }))}
                      />
                    </div>
                    <div className="flex min-w-[220px] flex-col gap-1.5">
                      <Label htmlFor="store-type">Type</Label>
                      <Select value={draft.storeType} onValueChange={(value) => setDraft((d) => ({ ...d, storeType: value as StoreType }))}>
                        <SelectTrigger id="store-type" aria-label="Type">
                          <SelectValue />
                        </SelectTrigger>
                        <SelectContent>
                          {STORE_TYPES.map((type) => (
                            <SelectItem key={type} value={type}>
                              {STORE_TYPE_LABELS[type]}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    </div>
                  </div>
                  <div className="flex flex-wrap gap-8">
                    <label className="flex items-center gap-2 text-sm text-foreground">
                      <Switch
                        checked={draft.isDefault}
                        // The default can only be moved to another store, never just switched off.
                        disabled={editingDefault}
                        onCheckedChange={(checked) => setDraft((d) => ({ ...d, isDefault: checked }))}
                        aria-label="Default store"
                      />
                      Default store
                    </label>
                    {editing !== 'new' && (
                      <label className="flex items-center gap-2 text-sm text-foreground">
                        <Switch
                          checked={draft.isActive}
                          disabled={editingDefault}
                          onCheckedChange={(checked) => setDraft((d) => ({ ...d, isActive: checked }))}
                          aria-label="Active"
                        />
                        Active
                      </label>
                    )}
                  </div>
                  <div className="flex gap-3">
                    <Button type="button" onClick={handleSave} disabled={isSaving}>
                      {isSaving ? 'Saving…' : 'Save store'}
                    </Button>
                    <Button type="button" variant="outline" onClick={() => setEditing(null)}>
                      Cancel
                    </Button>
                  </div>
                </CardContent>
              </Card>
            )}

            <div className="overflow-x-auto rounded-lg border border-border">
              <table className="w-full text-sm">
                <thead className="bg-sidebar-active text-left text-xs font-medium uppercase tracking-wide text-sidebar-active-foreground">
                  <tr>
                    <th className="px-4 py-2.5">Code</th>
                    <th className="px-4 py-2.5">Name</th>
                    <th className="px-4 py-2.5">Type</th>
                    <th className="px-4 py-2.5">Status</th>
                    <th className="px-4 py-2.5" />
                  </tr>
                </thead>
                <tbody className="divide-y divide-border">
                  {stores.map((store) => (
                    <tr key={store.id} className="hover:bg-muted/30">
                      <td className="px-4 py-3 font-mono text-xs text-muted-foreground">{store.code}</td>
                      <td className="px-4 py-3 font-medium text-foreground">{store.name}</td>
                      <td className="px-4 py-3 text-foreground">{STORE_TYPE_LABELS[store.storeType]}</td>
                      <td className="px-4 py-3">
                        <div className="flex flex-wrap gap-1.5">
                          {store.isDefault && <Badge>Default</Badge>}
                          <Badge variant={store.isActive ? 'success' : 'secondary'}>{store.isActive ? 'Active' : 'Inactive'}</Badge>
                        </div>
                      </td>
                      <td className="px-4 py-3 text-right">
                        {canEdit && (
                          <Button type="button" variant="ghost" size="sm" onClick={() => openEdit(store)}>
                            Edit
                          </Button>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </>
        )}
    </PharmacyPage>
  );
}
