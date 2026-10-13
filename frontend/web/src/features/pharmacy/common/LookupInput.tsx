import { useQuery } from '@tanstack/react-query';
import { Loader2, Search } from 'lucide-react';
import { forwardRef, useEffect, useImperativeHandle, useRef, useState, type ReactNode } from 'react';
import { useDebouncedValue } from '@/hooks/useDebouncedValue';
import { cn } from '@/lib/utils';

export interface LookupInputHandle {
  focus: () => void;
  clear: () => void;
}

interface LookupInputProps<T> {
  /** Distinguishes this lookup's cached searches from every other one. */
  queryKey: readonly unknown[];
  search: (term: string) => Promise<T[]>;
  getKey: (item: T) => string;
  renderItem: (item: T) => ReactNode;
  onSelect: (item: T) => void;
  placeholder?: string;
  ariaLabel: string;
  /** Characters needed before a search runs. 0 lists on focus. */
  minChars?: number;
  emptyText?: string;
  /** Keeps the chosen item's text in the box after selection; otherwise the box clears, ready
   * for the next one (the way an item-entry box behaves). */
  displayValue?: string;
  disabled?: boolean;
  autoFocus?: boolean;
  className?: string;
  inputClassName?: string;
}

/**
 * A type-to-search box with a keyboard-driven result list — arrow keys to move, Enter to
 * take the highlighted row, Escape to close. It is the item-entry control of the counter and
 * the purchase screen, where a pharmacist adds a line every few seconds and should never
 * need the mouse. A barcode scanner works unaided: it types the code and presses Enter, and
 * a search with exactly one result is taken straight away.
 */
function LookupInputInner<T>(
  {
    queryKey,
    search,
    getKey,
    renderItem,
    onSelect,
    placeholder,
    ariaLabel,
    minChars = 1,
    emptyText = 'Nothing found.',
    displayValue,
    disabled,
    autoFocus,
    className,
    inputClassName,
  }: LookupInputProps<T>,
  ref: React.Ref<LookupInputHandle>,
) {
  const inputRef = useRef<HTMLInputElement>(null);
  const listRef = useRef<HTMLUListElement>(null);
  const [text, setText] = useState('');
  const [open, setOpen] = useState(false);
  const [active, setActive] = useState(0);
  // Enter pressed before the results for the current text arrived (a scanner is faster than
  // the network): take the first result as soon as they do.
  const [pendingEnter, setPendingEnter] = useState(false);
  const term = useDebouncedValue(text.trim(), 180);
  const canSearch = term.length >= minChars;

  const { data, isFetching } = useQuery({
    queryKey: [...queryKey, term],
    queryFn: () => search(term),
    enabled: open && canSearch,
    staleTime: 15_000,
  });
  const items = canSearch ? (data ?? []) : [];
  const settled = term === text.trim() && !isFetching && data !== undefined;

  useImperativeHandle(ref, () => ({
    focus: () => inputRef.current?.focus(),
    clear: () => setText(''),
  }));

  useEffect(() => {
    setActive(0);
  }, [term]);

  function choose(item: T) {
    onSelect(item);
    setText('');
    setOpen(false);
    setPendingEnter(false);
  }

  useEffect(() => {
    if (pendingEnter && settled) {
      setPendingEnter(false);
      if (items.length > 0) {
        choose(items[Math.min(active, items.length - 1)]);
      }
    }
    // choose/items are derived from the same render as `settled`.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [pendingEnter, settled]);

  useEffect(() => {
    listRef.current?.querySelector<HTMLElement>(`[data-index="${active}"]`)?.scrollIntoView({ block: 'nearest' });
  }, [active]);

  function handleKeyDown(event: React.KeyboardEvent<HTMLInputElement>) {
    if (event.key === 'ArrowDown') {
      event.preventDefault();
      setOpen(true);
      setActive((index) => Math.min(index + 1, Math.max(items.length - 1, 0)));
    } else if (event.key === 'ArrowUp') {
      event.preventDefault();
      setActive((index) => Math.max(index - 1, 0));
    } else if (event.key === 'Enter') {
      if (!text.trim() && minChars > 0) return;
      event.preventDefault();
      if (settled) {
        if (items.length > 0) choose(items[Math.min(active, items.length - 1)]);
      } else {
        setPendingEnter(true);
      }
    } else if (event.key === 'Escape') {
      if (open) {
        event.stopPropagation();
        setOpen(false);
      }
    }
  }

  const showList = open && (canSearch || minChars === 0) && text.trim().length >= minChars;

  return (
    <div className={cn('relative', className)}>
      <Search className="pointer-events-none absolute left-2.5 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
      <input
        ref={inputRef}
        type="text"
        role="combobox"
        aria-label={ariaLabel}
        aria-expanded={showList}
        aria-autocomplete="list"
        autoComplete="off"
        spellCheck={false}
        autoFocus={autoFocus}
        disabled={disabled}
        value={open || !displayValue ? text : displayValue}
        placeholder={placeholder}
        onChange={(event) => {
          setText(event.target.value);
          setOpen(true);
        }}
        onFocus={() => setOpen(true)}
        onBlur={() => {
          setOpen(false);
          setPendingEnter(false);
          if (displayValue) setText('');
        }}
        onKeyDown={handleKeyDown}
        className={cn(
          'h-10 w-full rounded-md border border-input bg-background pl-8 pr-8 text-sm outline-none transition-colors placeholder:text-muted-foreground focus:border-primary focus:ring-2 focus:ring-primary/20 disabled:cursor-not-allowed disabled:opacity-50',
          inputClassName,
        )}
      />
      {isFetching && open && <Loader2 className="absolute right-2.5 top-1/2 h-4 w-4 -translate-y-1/2 animate-spin text-muted-foreground" />}

      {showList && (
        <ul
          ref={listRef}
          role="listbox"
          className="absolute left-0 right-0 top-full z-50 mt-1 max-h-80 overflow-y-auto rounded-md border border-border bg-popover py-1 shadow-soft-lg"
        >
          {items.length === 0 ? (
            <li className="px-3 py-2 text-sm text-muted-foreground">{isFetching || !settled ? 'Searching…' : emptyText}</li>
          ) : (
            items.map((item, index) => (
              <li
                key={getKey(item)}
                role="option"
                data-index={index}
                aria-selected={index === active}
                // mousedown, not click: the input's blur would close the list before a click landed.
                onMouseDown={(event) => {
                  event.preventDefault();
                  choose(item);
                }}
                onMouseEnter={() => setActive(index)}
                className={cn('cursor-pointer px-3 py-1.5 text-sm', index === active ? 'bg-primary/10 text-foreground' : 'text-foreground')}
              >
                {renderItem(item)}
              </li>
            ))
          )}
        </ul>
      )}
    </div>
  );
}

export const LookupInput = forwardRef(LookupInputInner) as <T>(
  props: LookupInputProps<T> & { ref?: React.Ref<LookupInputHandle> },
) => ReturnType<typeof LookupInputInner>;
