import { cn } from '@/lib/utils';
import { EVENT_TYPE_META } from '../constants';
import { EVENT_TYPES, type EventType } from '../types';

interface EventTypeFilterListProps {
  selected: EventType[];
  onChange: (types: EventType[]) => void;
  counts: Partial<Record<EventType, number>>;
}

export function EventTypeFilterList({ selected, onChange, counts }: EventTypeFilterListProps) {
  function toggle(type: EventType) {
    onChange(selected.includes(type) ? selected.filter((t) => t !== type) : [...selected, type]);
  }

  return (
    <fieldset className="flex flex-col gap-1">
      <legend className="mb-1.5 text-xs font-semibold uppercase tracking-wide text-muted-foreground">Event Type</legend>
      {EVENT_TYPES.map((type) => {
        const meta = EVENT_TYPE_META[type];
        const checked = selected.includes(type);
        return (
          <label
            key={type}
            className={cn(
              'flex cursor-pointer items-center justify-between gap-2 rounded-md px-2 py-1.5 text-sm hover:bg-accent',
              checked && 'bg-accent',
            )}
          >
            <span className="flex items-center gap-2">
              <input
                type="checkbox"
                checked={checked}
                onChange={() => toggle(type)}
                className="h-3.5 w-3.5 rounded border-input text-primary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
              />
              <span className={cn('h-2.5 w-2.5 rounded-full', meta.dotClass)} aria-hidden="true" />
              <span className="text-foreground">{meta.label}</span>
            </span>
            <span className="text-xs tabular-nums text-muted-foreground">{counts[type] ?? 0}</span>
          </label>
        );
      })}
    </fieldset>
  );
}
