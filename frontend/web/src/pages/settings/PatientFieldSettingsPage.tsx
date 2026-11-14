import { ApiError, type PatientFieldKey, type PatientFieldRequirements } from '@hms/shared';
import { ClipboardCheck, Info, Loader2, Lock } from 'lucide-react';
import { useEffect, useState } from 'react';
import { PageBanner } from '@/components/PageBanner';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Switch } from '@/components/ui/switch';
import { useToast } from '@/components/ui/toast-context';
import { useAuth } from '@/features/auth/AuthContext';
import {
  usePatientFieldSettingsQuery,
  useUpdatePatientFieldSettingsMutation,
} from '@/features/patients';

interface FieldRow {
  key: PatientFieldKey;
  label: string;
  /** What changes for the receptionist when the field is optional. */
  whenOptional: string;
}

interface FieldGroup {
  title: string;
  fields: FieldRow[];
}

// Grouped the way the New Patient Registration form's tabs are.
const FIELD_GROUPS: FieldGroup[] = [
  {
    title: 'Patient Information',
    fields: [
      {
        key: 'lastName',
        label: 'Last name',
        whenOptional: 'Patients with a single name can be registered without a last name.',
      },
      {
        key: 'bloodGroup',
        label: 'Blood group',
        whenOptional:
          'Starts on "Unknown" and can be left alone. When mandatory it starts empty and must be picked — "Unknown" is still a choice.',
      },
      {
        key: 'maritalStatus',
        label: 'Marital status',
        whenOptional: 'Patients 18 or older may stay "N/A". Under-18s must be "N/A" either way.',
      },
    ],
  },
  {
    title: 'Contact Information',
    fields: [
      {
        key: 'addressLine1',
        label: 'Address line 1',
        whenOptional: 'Door no. & building name can be left blank.',
      },
      {
        key: 'state',
        label: 'State',
        whenOptional: 'Can be left blank. Turning it off also makes District optional.',
      },
      {
        key: 'district',
        label: 'District',
        whenOptional: 'Can be left blank. Turning it on also makes State mandatory.',
      },
      {
        key: 'pincode',
        label: 'Pincode',
        whenOptional: 'Can be left blank — a pincode that is entered must still be 6 digits.',
      },
      {
        key: 'emergencyContact',
        label: 'Emergency contact',
        whenOptional:
          'A patient can be saved without one — but a contact that is started needs both a name and a phone.',
      },
    ],
  },
  {
    title: 'Medical Information',
    fields: [
      {
        key: 'modeOfArrivalDetail',
        label: 'Mode of arrival detail',
        whenOptional:
          'The referring department, referral source or advertisement channel can be left blank. "Please specify" after "Other" is still required.',
      },
      {
        key: 'idProofNumber',
        label: 'ID proof number',
        whenOptional:
          'Can be left blank — a number that is entered is still checked against its ID type (e.g. 12-digit Aadhaar).',
      },
    ],
  },
];

const ALWAYS_MANDATORY = [
  'Title',
  'First name',
  'Date of birth',
  'Gender',
  'Primary phone',
  'Mode of arrival source',
  'Encounter type',
  'Department',
  'Consultant',
];

function isSame(a: PatientFieldRequirements, b: PatientFieldRequirements) {
  return FIELD_GROUPS.every((group) => group.fields.every(({ key }) => a[key] === b[key]));
}

/**
 * Settings → Patient Registration Fields (ADR-135): which New Patient Registration fields this
 * hospital makes mandatory. Applies to Edit Patient too, and the API enforces the same choices;
 * the bulk Excel import keeps its own fixed rules. Viewing needs identity-administration.view
 * (the route guard); saving needs .edit, which the backend enforces, so the switches are
 * read-only without it.
 */
export default function PatientFieldSettingsPage() {
  const { hasPermission } = useAuth();
  const canEdit = hasPermission('identity-administration.edit');
  const { toast } = useToast();
  const { data: saved, isPending, isError } = usePatientFieldSettingsQuery();
  const mutation = useUpdatePatientFieldSettingsMutation();

  const [draft, setDraft] = useState<PatientFieldRequirements | null>(null);
  useEffect(() => {
    if (saved) setDraft(saved);
  }, [saved]);

  const isDirty = !!saved && !!draft && !isSame(saved, draft);

  function toggle(key: PatientFieldKey, mandatory: boolean) {
    setDraft((current) => {
      if (!current) return current;
      const next = { ...current, [key]: mandatory };
      // A district is picked from the chosen state's list, so the two move together.
      if (key === 'district' && mandatory) next.state = true;
      if (key === 'state' && !mandatory) next.district = false;
      return next;
    });
  }

  function handleSave() {
    if (!draft) return;
    mutation.mutate(draft, {
      onSuccess: () =>
        toast({
          title: 'Patient registration fields saved',
          description: 'They apply to the next registration or edit.',
          variant: 'success',
        }),
      onError: (error) =>
        toast({
          title: 'Could not save the registration fields',
          description:
            error instanceof ApiError ? error.message : 'Something went wrong. Please try again.',
          variant: 'error',
        }),
    });
  }

  return (
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={ClipboardCheck}
        title="Patient Registration Fields"
        subtitle="Choose which New Patient Registration fields are mandatory for this hospital."
        backTo="/admin/settings"
        backLabel="Back to settings"
      />

      <div className="flex flex-1 flex-col gap-6 p-4 sm:p-6 lg:p-8">
        <p className="flex max-w-3xl items-start gap-2 text-sm text-muted-foreground">
          <Info className="mt-0.5 h-4 w-4 shrink-0" />
          <span>
            Switched on means mandatory. The same choices apply to Edit Patient and are enforced
            when the patient is saved. The bulk Excel import keeps its own fixed rules.
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
            The registration field settings could not be loaded. Refresh the page to try again.
          </p>
        ) : (
          <>
            {FIELD_GROUPS.map((group) => (
              <Card key={group.title} className="max-w-3xl">
                <CardHeader className="pb-2">
                  <CardTitle className="text-base">{group.title}</CardTitle>
                </CardHeader>
                <CardContent className="flex flex-col gap-1">
                  {group.fields.map(({ key, label, whenOptional }) => {
                    const id = `patient-field-${key}`;
                    const mandatory = draft[key];
                    return (
                      <div
                        key={key}
                        className="flex items-center justify-between gap-4 border-b border-border py-3 last:border-b-0"
                      >
                        <div className="min-w-0">
                          <label htmlFor={id} className="text-sm font-medium text-foreground">
                            {label}
                          </label>
                          <p className="text-xs text-muted-foreground">{whenOptional}</p>
                        </div>
                        <div className="flex shrink-0 items-center gap-2">
                          <span
                            className={
                              mandatory
                                ? 'w-16 text-right text-xs font-medium text-primary'
                                : 'w-16 text-right text-xs text-muted-foreground'
                            }
                          >
                            {mandatory ? 'Mandatory' : 'Optional'}
                          </span>
                          <Switch
                            id={id}
                            checked={mandatory}
                            onCheckedChange={(on) => toggle(key, on)}
                            disabled={!canEdit || mutation.isPending}
                          />
                        </div>
                      </div>
                    );
                  })}
                </CardContent>
              </Card>
            ))}

            <Card className="max-w-3xl">
              <CardHeader className="pb-2">
                <CardTitle className="flex items-center gap-2 text-base">
                  <Lock className="h-4 w-4 text-muted-foreground" />
                  Always mandatory
                </CardTitle>
                <CardDescription>
                  Needed to identify the patient, find returning patients by phone, and bill the
                  visit, so these can't be switched off. Allergy details (when the patient has a
                  known allergy) and MLC/NMLC for IP and Emergency stay required too.
                </CardDescription>
              </CardHeader>
              <CardContent className="flex flex-wrap gap-2">
                {ALWAYS_MANDATORY.map((field) => (
                  <span
                    key={field}
                    className="rounded-full border border-border bg-muted px-2.5 py-0.5 text-xs text-foreground"
                  >
                    {field}
                  </span>
                ))}
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
