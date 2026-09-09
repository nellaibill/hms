import { ApiError } from '@hms/shared';
import { FileText, Loader2, Plus } from 'lucide-react';
import { Link, useNavigate } from 'react-router-dom';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { useCreateDischargeSummaryMutation } from '../hooks/useDischargeSummaryMutations';
import { useDischargeSummaryByAdmissionQuery } from '../hooks/useDischargeSummaryByAdmissionQuery';

interface DischargeSummaryEntryCardProps {
  admissionId: string;
}

/**
 * The AdmissionViewPage entry point for Discharge Summary — only rendered once
 * Admission.Status is Discharged (see AdmissionViewPage.tsx). Tries GET .../discharge-summary
 * first: a 404 means none exists yet, offering "Create Discharge Summary" (POST, then
 * navigate to the new Draft's edit page); a successful fetch offers "Continue Editing" or
 * "View Discharge Summary" depending on Status, per the approved plan.
 */
export function DischargeSummaryEntryCard({ admissionId }: DischargeSummaryEntryCardProps) {
  const navigate = useNavigate();
  const summaryQuery = useDischargeSummaryByAdmissionQuery(admissionId);
  const createMutation = useCreateDischargeSummaryMutation();

  function handleCreate() {
    createMutation.mutate(admissionId, {
      onSuccess: () => {
        navigate(`/clinical/ipd/admissions/${admissionId}/discharge-summary/edit`);
      },
    });
  }

  const notFound = summaryQuery.isError && summaryQuery.error instanceof ApiError && summaryQuery.error.status === 404;
  const otherError = summaryQuery.isError && !notFound;

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base">Discharge Summary</CardTitle>
      </CardHeader>
      <CardContent>
        {summaryQuery.isPending && (
          <div className="flex items-center gap-2 text-sm text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" />
            Checking for an existing discharge summary…
          </div>
        )}

        {otherError && <p className="text-sm text-destructive">Failed to load the discharge summary. Please try again.</p>}

        {notFound && (
          <div className="flex flex-col gap-2">
            <p className="text-sm text-muted-foreground">No discharge summary has been created for this admission yet.</p>
            <Button type="button" className="w-fit gap-1.5" onClick={handleCreate} disabled={createMutation.isPending}>
              <Plus className="h-4 w-4" />
              {createMutation.isPending ? 'Creating…' : 'Create Discharge Summary'}
            </Button>
            {createMutation.isError && <p className="text-sm text-destructive">Failed to create the discharge summary. Please try again.</p>}
          </div>
        )}

        {summaryQuery.isSuccess && (
          <div className="flex items-center gap-3">
            <Badge variant={summaryQuery.data.status === 'Finalized' ? 'success' : 'secondary'}>{summaryQuery.data.status}</Badge>
            <Button asChild variant="outline" size="sm" className="gap-1.5">
              <Link
                to={
                  summaryQuery.data.status === 'Draft'
                    ? `/clinical/ipd/admissions/${admissionId}/discharge-summary/edit`
                    : `/clinical/ipd/admissions/${admissionId}/discharge-summary`
                }
              >
                <FileText className="h-4 w-4" />
                {summaryQuery.data.status === 'Draft' ? 'Continue Discharge Summary' : 'View Discharge Summary'}
              </Link>
            </Button>
          </div>
        )}
      </CardContent>
    </Card>
  );
}
