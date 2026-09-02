import { Badge, type BadgeProps } from '@/components/ui/badge';
import { humanize } from '@/features/patients/humanize';
import type { LabOrderItemStatus, LabOrderStatus } from '../types';

/** Covers every LabOrderItemStatus value plus LabOrderStatus's two order-only reporting
 * milestones (ReadyForRelease/Released) — the two enums share the rest of their vocabulary
 * (see HMS.Modules.Laboratory.Contracts.LaboratoryEnums's own doc comment), so one badge
 * component serves both item-level and order-level status display. */
const STATUS_VARIANTS: Record<LabOrderItemStatus | LabOrderStatus, BadgeProps['variant']> = {
  PendingCollection: 'outline',
  Collected: 'outline',
  Received: 'secondary',
  Processing: 'warning',
  ResultEntryInProgress: 'warning',
  PendingVerification: 'warning',
  CorrectionRequired: 'destructive',
  Verified: 'success',
  Rejected: 'destructive',
  RecollectionRequired: 'destructive',
  ReadyForRelease: 'success',
  Released: 'success',
};

interface LabStatusBadgeProps {
  status: LabOrderItemStatus | LabOrderStatus;
  className?: string;
}

/** The single most-reused visual element across every Laboratory screen — one item/order
 * status value in, a consistently-colored, human-readable Badge out. */
export function LabStatusBadge({ status, className }: LabStatusBadgeProps) {
  return (
    <Badge variant={STATUS_VARIANTS[status] ?? 'outline'} className={className}>
      {humanize(status)}
    </Badge>
  );
}
