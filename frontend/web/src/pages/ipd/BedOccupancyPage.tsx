import type { Bed } from '@hms/shared';
import { Grid3x3, Loader2 } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { cn } from '@/lib/utils';
import { useAdmissionsQuery } from '../../features/ipd/admissions';
import { useBedsQuery } from '../../features/ipd/beds';
import { useWardsQuery } from '../../features/ipd/wards';

const bedTileClasses: Record<Bed['status'], string> = {
  Available: 'border-success/40 bg-success/10',
  Occupied: 'border-destructive/40 bg-destructive/10',
  Maintenance: 'border-warning/40 bg-warning/10',
};

const bedDotClasses: Record<Bed['status'], string> = {
  Available: 'bg-success',
  Occupied: 'bg-destructive',
  Maintenance: 'bg-warning',
};

export default function BedOccupancyPage() {
  const wardsQuery = useWardsQuery({ pageSize: 100, isActive: true, sort: 'name' });
  const bedsQuery = useBedsQuery({ pageSize: 500, isActive: true, sort: 'bedNumber' });
  // Only Admitted admissions can be currently occupying a bed — this is the lookup that
  // resolves a bed tile to "who's in it" without a per-bed round-trip.
  const admittedQuery = useAdmissionsQuery({ pageSize: 500, status: 'Admitted' });

  const isPending = wardsQuery.isPending || bedsQuery.isPending || admittedQuery.isPending;
  const isError = wardsQuery.isError || bedsQuery.isError || admittedQuery.isError;

  const admissionByBedId = new Map((admittedQuery.data?.items ?? []).map((admission) => [admission.bedId, admission]));

  const bedsByWardId = new Map<string, Bed[]>();
  for (const bed of bedsQuery.data?.items ?? []) {
    const existing = bedsByWardId.get(bed.wardId) ?? [];
    existing.push(bed);
    bedsByWardId.set(bed.wardId, existing);
  }

  return (
    <div className="flex flex-1 flex-col">
      <div className="px-6 pt-4 lg:px-8">
        <Link to="/clinical/ipd" className="text-sm text-muted-foreground hover:text-foreground">
          &larr; Back to IPD
        </Link>
      </div>

      <div className="mt-3 flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        <div className="flex items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
            <Grid3x3 className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight">Bed Occupancy</h1>
        </div>
        <p className="max-w-2xl text-sm text-page-banner-foreground/85">Ward-by-ward view of every bed's current status.</p>
      </div>

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
        <div className="flex flex-wrap items-center gap-4 text-sm text-muted-foreground">
          <span className="inline-flex items-center gap-1.5">
            <span className="h-2.5 w-2.5 rounded-full bg-success" />
            Available
          </span>
          <span className="inline-flex items-center gap-1.5">
            <span className="h-2.5 w-2.5 rounded-full bg-destructive" />
            Occupied
          </span>
          <span className="inline-flex items-center gap-1.5">
            <span className="h-2.5 w-2.5 rounded-full bg-warning" />
            Maintenance
          </span>
        </div>

        {isPending && (
          <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" />
            Loading bed occupancy…
          </div>
        )}

        {isError && (
          <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
            Failed to load bed occupancy.
          </p>
        )}

        {!isPending && !isError && (wardsQuery.data?.items.length ?? 0) === 0 && (
          <Card className="border-dashed">
            <CardContent className="flex flex-col items-center gap-2 py-16 text-center">
              <p className="text-sm font-medium text-foreground">No wards found</p>
              <p className="text-sm text-muted-foreground">Create a ward and add beds to see occupancy here.</p>
            </CardContent>
          </Card>
        )}

        {!isPending &&
          !isError &&
          wardsQuery.data?.items.map((ward) => {
            const beds = bedsByWardId.get(ward.id) ?? [];
            return (
              <Card key={ward.id}>
                <CardHeader className="flex flex-row items-center justify-between">
                  <CardTitle className="text-base">
                    {ward.name} <span className="font-mono text-xs font-normal text-muted-foreground">({ward.code})</span>
                  </CardTitle>
                  <Badge variant="outline">{beds.length} bed{beds.length === 1 ? '' : 's'}</Badge>
                </CardHeader>
                <CardContent>
                  {beds.length === 0 ? (
                    <p className="text-sm text-muted-foreground">No beds in this ward yet.</p>
                  ) : (
                    <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5">
                      {beds.map((bed) => {
                        const admission = admissionByBedId.get(bed.id);
                        return (
                          <div key={bed.id} className={cn('flex flex-col gap-1 rounded-lg border p-3', bedTileClasses[bed.status])}>
                            <div className="flex items-center justify-between">
                              <span className="font-mono text-sm font-semibold text-foreground">{bed.bedNumber}</span>
                              <span className={cn('h-2.5 w-2.5 rounded-full', bedDotClasses[bed.status])} />
                            </div>
                            <span className="text-xs text-muted-foreground">{bed.bedType}</span>
                            <span className="text-xs font-medium text-foreground">
                              {bed.status} • ₹{bed.dailyCharge.toLocaleString('en-IN')}/day
                            </span>
                            {admission && (
                              <Link
                                to={`/clinical/ipd/admissions/${admission.id}`}
                                className="mt-1 flex flex-col gap-0.5 border-t border-border/60 pt-1.5 hover:opacity-80"
                              >
                                <span className="truncate text-xs font-medium text-foreground">{admission.patientName}</span>
                                <span className="truncate text-[11px] text-muted-foreground">{admission.consultantName}</span>
                              </Link>
                            )}
                          </div>
                        );
                      })}
                    </div>
                  )}
                </CardContent>
              </Card>
            );
          })}
      </div>
    </div>
  );
}
