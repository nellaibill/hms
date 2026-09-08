import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Label } from '@/components/ui/label';
import { useStaffDirectoryQuery } from '../hooks/useStaffDirectoryQuery';
import { StaffPicker } from './StaffPicker';

interface FinalizationCardProps {
  preparedByUserId: string;
  checkedByUserId: string;
  consultantApprovedByUserId: string;
  onPreparedByChange: (value: string) => void;
  onCheckedByChange: (value: string) => void;
  onConsultantApprovedByChange: (value: string) => void;
}

/**
 * Sign-off captured once at Finalize, not a multi-step approval workflow — all three pickers
 * are optional (a hospital that only ever fills in "Prepared By" still gets a valid,
 * finalized summary). Values chosen here are held by the edit page and sent on the Finalize
 * call, not on Save/PUT (they aren't part of UpdateDischargeSummaryRequest).
 */
export function FinalizationCard({
  preparedByUserId,
  checkedByUserId,
  consultantApprovedByUserId,
  onPreparedByChange,
  onCheckedByChange,
  onConsultantApprovedByChange,
}: FinalizationCardProps) {
  const directoryQuery = useStaffDirectoryQuery();
  const staff = directoryQuery.data ?? [];

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base">Finalization</CardTitle>
      </CardHeader>
      <CardContent className="grid grid-cols-1 gap-4 sm:grid-cols-3">
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="preparedByUserId">Prepared by</Label>
          <StaffPicker id="preparedByUserId" ariaLabel="Prepared by" value={preparedByUserId} onValueChange={onPreparedByChange} staff={staff} />
        </div>
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="checkedByUserId">Checked by</Label>
          <StaffPicker id="checkedByUserId" ariaLabel="Checked by" value={checkedByUserId} onValueChange={onCheckedByChange} staff={staff} />
        </div>
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="consultantApprovedByUserId">Consultant approved by</Label>
          <StaffPicker
            id="consultantApprovedByUserId"
            ariaLabel="Consultant approved by"
            value={consultantApprovedByUserId}
            onValueChange={onConsultantApprovedByChange}
            staff={staff}
          />
        </div>
      </CardContent>
    </Card>
  );
}
