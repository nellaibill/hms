import { cn } from '@/lib/utils';
import { ENTITY_TYPE_META } from '../constants';
import type { EntityType } from '../types';

interface EntityTypeBadgeProps {
  entityType: EntityType;
  className?: string;
}

export function EntityTypeBadge({ entityType, className }: EntityTypeBadgeProps) {
  const meta = ENTITY_TYPE_META[entityType];
  return (
    <span className={cn('inline-flex items-center rounded-full border px-2.5 py-0.5 text-xs font-semibold', meta.chipClass, className)}>
      {meta.label}
    </span>
  );
}
