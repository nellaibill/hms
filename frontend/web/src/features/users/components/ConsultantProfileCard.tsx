import { useQuery } from '@tanstack/react-query';
import { Stethoscope } from 'lucide-react';
import { Card, CardContent, CardHeader } from '@/components/ui/card';
import { CardHeading } from './CardHeading';
import { DepartmentName } from '@/components/DepartmentName';
import { Label } from '@/components/ui/label';
import { SearchableSelect } from '@/components/ui/searchable-select';
import { Switch } from '@/components/ui/switch';
import { consultantsApi } from '@/services/apiClient';

interface ConsultantProfileCardProps {
  linkToConsultant: boolean;
  consultantId: string;
  onToggle: (checked: boolean) => void;
  onChangeConsultant: (consultantId: string) => void;
  error?: string | null;
}

/**
 * Shared by UserCreateForm and UserEditForm so the two stay visually and behaviorally
 * identical — a consultant mapping is independent of Role, so this never reads or gates
 * on the form's selected role (see the "requires a consultant" check in each form, which
 * decides only whether to show `error`).
 */
export function ConsultantProfileCard({ linkToConsultant, consultantId, onToggle, onChangeConsultant, error }: ConsultantProfileCardProps) {
  // Deliberately not scoped to a department (unlike ConsultantSelect elsewhere) — any
  // active consultant in the hospital should be pickable here.
  const { data: consultantsData } = useQuery({
    queryKey: ['consultants', 'select-list', 'all-departments'],
    queryFn: () => consultantsApi.getConsultants({ pageSize: 100, isActive: true }),
    enabled: linkToConsultant,
  });

  const selectedConsultant = consultantsData?.items.find((c) => c.id === consultantId);
  const consultantOptions = (consultantsData?.items ?? []).map((c) => ({
    value: c.id,
    label: c.specialization ? `${c.name} — ${c.specialization}` : c.name,
  }));

  return (
    <Card>
      <CardHeader>
        <CardHeading
          icon={Stethoscope}
          title={
            <>
              Consultant Profile <span className="font-normal text-muted-foreground">(Optional)</span>
            </>
          }
          description="Link this user to a consultant/doctor. This can be set for any role."
        />
      </CardHeader>
      <CardContent className="flex flex-col gap-3">
        <div className="flex items-start gap-3">
          <Switch id="linkConsultant" checked={linkToConsultant} onCheckedChange={onToggle} aria-label="Link this user to a consultant" />
          <div className="flex flex-col gap-0.5">
            <Label htmlFor="linkConsultant" className="cursor-pointer">
              Link this user to a consultant
            </Label>
            <p className="text-xs text-muted-foreground">Enable this if the user is a doctor/consultant in the hospital.</p>
          </div>
        </div>

        {linkToConsultant && (
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="consultantId">Consultant</Label>
            <SearchableSelect
              id="consultantId"
              value={consultantId}
              onValueChange={onChangeConsultant}
              options={consultantOptions}
              placeholder="Select consultant…"
              searchPlaceholder="Search by name…"
              ariaLabel="Consultant"
            />

            {selectedConsultant && (
              <div className="mt-1 flex flex-col gap-1 rounded-md bg-muted/50 px-3 py-2 text-xs text-muted-foreground">
                <span>
                  <strong className="text-foreground">Specialization:</strong> {selectedConsultant.specialization ?? '—'}
                </span>
                <span>
                  <strong className="text-foreground">Department:</strong>{' '}
                  {selectedConsultant.departmentId ? <DepartmentName departmentId={selectedConsultant.departmentId} /> : '—'}
                </span>
              </div>
            )}
          </div>
        )}

        {error && <p className="text-sm text-destructive">{error}</p>}
      </CardContent>
    </Card>
  );
}
