import { Check, ChevronDown, Search } from 'lucide-react';
import * as React from 'react';
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover';
import { cn } from '@/lib/utils';

export interface SearchableSelectOption {
  value: string;
  label: string;
  /** Extra text matched during search but not shown in the trigger/list, e.g. a synonym or code. */
  keywords?: string;
}

interface SearchableSelectProps {
  id: string;
  value: string;
  onValueChange: (value: string) => void;
  options: SearchableSelectOption[];
  placeholder?: string;
  searchPlaceholder?: string;
  ariaLabel?: string;
  disabled?: boolean;
  className?: string;
}

/**
 * A filterable dropdown for long option lists — a real hospital's lab/radiology/procedure
 * catalog runs to hundreds of entries, and a plain `<Select>` (fine for something short like
 * Department) becomes a scroll-fest at that size. Built on Radix Popover (Radix Select has
 * no built-in filtering) with a real search input and roving keyboard focus.
 */
export function SearchableSelect({
  id,
  value,
  onValueChange,
  options,
  placeholder = 'Select…',
  searchPlaceholder = 'Search…',
  ariaLabel,
  disabled,
  className,
}: SearchableSelectProps) {
  const [open, setOpen] = React.useState(false);
  const [query, setQuery] = React.useState('');
  const [highlighted, setHighlighted] = React.useState(0);
  const inputRef = React.useRef<HTMLInputElement>(null);

  const filtered = React.useMemo(() => {
    const q = query.trim().toLowerCase();
    if (!q) return options;
    return options.filter((o) => o.label.toLowerCase().includes(q) || o.keywords?.toLowerCase().includes(q));
  }, [options, query]);

  const selected = options.find((o) => o.value === value);

  function handleOpenChange(next: boolean) {
    setOpen(next);
    if (next) {
      setQuery('');
      setHighlighted(0);
      // Popover content mounts on the next tick — focus once it's actually in the DOM.
      requestAnimationFrame(() => inputRef.current?.focus());
    }
  }

  function selectOption(option: SearchableSelectOption) {
    onValueChange(option.value);
    setOpen(false);
  }

  function handleKeyDown(e: React.KeyboardEvent) {
    if (e.key === 'ArrowDown') {
      e.preventDefault();
      setHighlighted((i) => Math.min(i + 1, filtered.length - 1));
    } else if (e.key === 'ArrowUp') {
      e.preventDefault();
      setHighlighted((i) => Math.max(i - 1, 0));
    } else if (e.key === 'Enter') {
      e.preventDefault();
      const option = filtered[highlighted];
      if (option) selectOption(option);
    }
  }

  return (
    <Popover open={open} onOpenChange={handleOpenChange}>
      <PopoverTrigger asChild>
        <button
          type="button"
          id={id}
          aria-label={ariaLabel}
          disabled={disabled}
          className={cn(
            'flex h-10 w-full items-center justify-between rounded-md border border-input bg-background px-3 py-2 text-sm shadow-sm focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50',
            className,
          )}
        >
          <span className={cn('truncate text-left', !selected && 'text-muted-foreground')}>{selected ? selected.label : placeholder}</span>
          <ChevronDown className="h-4 w-4 shrink-0 opacity-60" />
        </button>
      </PopoverTrigger>
      {/* PopoverContent defaults to exactly the trigger's width (w-[--radix-popover-trigger-width])
          — fine for a wide trigger, but this component is often used in a multi-column row
          (e.g. Registration Details' 5-across Department/Consultant/Consultation Type fields)
          where the trigger itself is narrow, which used to also clip every option's label down
          to the same narrow width. w-max lets the panel size to its own content instead, floored
          at the trigger's width (never narrower than what opened it) and capped so one very long
          label can't blow out to something absurd on an ultrawide monitor. */}
      <PopoverContent className="w-max min-w-[--radix-popover-trigger-width] max-w-[min(28rem,90vw)] p-0">
        <div className="flex items-center gap-2 border-b border-border px-3 py-2">
          <Search className="h-4 w-4 shrink-0 text-muted-foreground" />
          <input
            ref={inputRef}
            value={query}
            onChange={(e) => {
              setQuery(e.target.value);
              setHighlighted(0);
            }}
            onKeyDown={handleKeyDown}
            placeholder={searchPlaceholder}
            aria-label={searchPlaceholder}
            className="h-6 w-full bg-transparent text-sm text-foreground outline-none placeholder:text-muted-foreground"
          />
        </div>
        <div className="max-h-64 overflow-y-auto p-1" role="listbox">
          {filtered.length === 0 && <p className="px-2 py-3 text-center text-sm text-muted-foreground">No matches</p>}
          {filtered.map((option, index) => (
            <button
              key={option.value}
              type="button"
              role="option"
              aria-selected={option.value === value}
              onClick={() => selectOption(option)}
              onMouseEnter={() => setHighlighted(index)}
              className={cn(
                'flex w-full items-start gap-2 rounded-sm px-2 py-1.5 text-left text-sm',
                index === highlighted ? 'bg-accent text-accent-foreground' : 'text-foreground',
              )}
            >
              <span className="mt-px flex h-3.5 w-3.5 shrink-0 items-center justify-center">
                {option.value === value && <Check className="h-4 w-4" />}
              </span>
              {/* No truncate here on purpose — the whole point of widening the panel above is
                  so a long option (a doctor's name with degrees, a long department name) is
                  fully readable; wrapping instead of clipping is the fallback for anything
                  still longer than the max-width cap. */}
              <span className="whitespace-normal break-words">{option.label}</span>
            </button>
          ))}
        </div>
      </PopoverContent>
    </Popover>
  );
}
