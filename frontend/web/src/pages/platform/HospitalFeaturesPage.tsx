import type { TenantListItemResponse } from '@hms/shared';
import { useQueryClient } from '@tanstack/react-query';
import { SlidersHorizontal } from 'lucide-react';
import { useLocation, useParams } from 'react-router-dom';
import { PageBanner } from '@/components/PageBanner';
import { PlatformTopBar } from '@/features/platformAuth/PlatformTopBar';
import { PLATFORM_CONTAINER } from '@/features/platformAuth/platformLayout';
import { HospitalFeaturesEditor } from '@/features/platformHospitals';
import { cn } from '@/lib/utils';

/** Finds the hospital's name, code and plan for the banner: from the link that opened the page,
 * else from a cached hospital list. There's no get-by-id endpoint, so a bare link opened in a
 * fresh tab shows a generic banner (the features themselves load by id either way). */
function useHospitalSummary(id: string): TenantListItemResponse | undefined {
  const location = useLocation();
  const queryClient = useQueryClient();
  const fromLink = (location.state as { hospital?: TenantListItemResponse } | null)?.hospital;
  if (fromLink?.id === id) return fromLink;
  return queryClient
    .getQueriesData<{ items: TenantListItemResponse[] }>({ queryKey: ['platformHospitals'] })
    .flatMap(([, page]) => page?.items ?? [])
    .find((hospital) => hospital.id === id);
}

export default function HospitalFeaturesPage() {
  const { id = '' } = useParams<{ id: string }>();
  const hospital = useHospitalSummary(id);

  const registeredOn = hospital
    ? new Date(hospital.createdAt).toLocaleDateString('en-IN', {
        day: 'numeric',
        month: 'short',
        year: 'numeric',
      })
    : null;
  const isActive = hospital?.status === 'Active';

  return (
    <div className="flex min-h-screen flex-col bg-muted/30">
      <PlatformTopBar />

      <div className="bg-page-banner">
        <PageBanner
          className={PLATFORM_CONTAINER}
          icon={SlidersHorizontal}
          title={hospital ? hospital.hospitalName : 'Hospital features'}
          titleExtra={
            hospital && (
              <span className="inline-flex items-center gap-1.5 rounded-full bg-page-banner-foreground/15 px-2.5 py-0.5 text-xs font-medium">
                <span
                  className={cn(
                    'h-1.5 w-1.5 rounded-full',
                    isActive ? 'bg-emerald-300' : 'bg-page-banner-foreground/60',
                  )}
                />
                {hospital.status}
              </span>
            )
          }
          subtitle={
            hospital
              ? `Manage features · Code ${hospital.hospitalCode} · ${hospital.subscriptionTier} plan · Registered ${registeredOn}`
              : 'Choose the modules this hospital can use.'
          }
          backTo="/platform/dashboard"
          backLabel="All hospitals"
        />
      </div>

      <HospitalFeaturesEditor hospitalId={id} hospitalName={hospital?.hospitalName} />
    </div>
  );
}
