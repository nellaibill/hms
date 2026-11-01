import { useId, useRef, useState, type KeyboardEvent, type TextareaHTMLAttributes } from 'react';
import { cn } from '@/lib/utils';
import { completionFor, matchSuggestions } from '@/lib/suggestions';

const FIELD_CLASSES = 'w-full rounded-md border px-3 py-2 text-sm leading-6';

interface SuggestionTextareaProps extends Omit<TextareaHTMLAttributes<HTMLTextAreaElement>, 'value' | 'onChange'> {
  id: string;
  value: string;
  onValueChange: (value: string) => void;
  /** Most useful first — the first one that fits is the one Tab fills in. */
  suggestions: readonly string[];
  /** How many suggestions show under the box (default 6). Tab cycles through up to twice that. */
  maxVisible?: number;
}

/**
 * A textarea that suggests what to write, for short free-text answers like a discount reason.
 *
 * - While typing, the rest of the best match shows in grey after the text ("Sen" → "ior citizen");
 *   in an empty box, the top suggestion shows in place of the placeholder.
 * - **Tab** fills that in. **Tab** again swaps in the next match, **Shift+Tab** the one before
 *   (↓/↑ do the same); **Enter** keeps the current one, **Esc** goes back to what was typed.
 * - Matching suggestions also show under the box, to click.
 *
 * Tab is only taken over while there's a grey completion or a run of Tab suggestions in progress
 * — otherwise it moves to the next field as usual. Exposes `data-suggesting="true"` while it is
 * handling keys, so a surrounding dialog can leave Esc to it (see DiscountDialog).
 */
export function SuggestionTextarea({
  id,
  value,
  onValueChange,
  suggestions,
  maxVisible = 6,
  className,
  placeholder,
  onFocus,
  onBlur,
  onKeyDown,
  ...props
}: SuggestionTextareaProps) {
  const hintId = useId();
  const mirrorRef = useRef<HTMLDivElement>(null);
  const [focused, setFocused] = useState(false);
  // A run of Tab presses: what the user had typed, and which match is showing.
  const [cycle, setCycle] = useState<{ base: string; index: number } | null>(null);
  // Esc hides the grey completion until the next keystroke.
  const [dismissed, setDismissed] = useState(false);

  const matches = matchSuggestions(cycle ? cycle.base : value, suggestions).slice(0, maxVisible * 2);
  const completion = focused && !cycle && !dismissed ? completionFor(value, matches[0]) : null;
  const suggesting = Boolean(cycle || completion);
  const visible = matches.slice(0, maxVisible);

  function pick(index: number, base: string) {
    const wrapped = (index + matches.length) % matches.length;
    setCycle({ base, index: wrapped });
    onValueChange(matches[wrapped]);
  }

  function handleKeyDown(event: KeyboardEvent<HTMLTextAreaElement>) {
    onKeyDown?.(event);
    if (matches.length === 0) return;

    // Before the defaultPrevented check: a surrounding dialog prevents Esc's default to stay open
    // (see the data-suggesting note above), and Esc still has to undo here.
    if (event.key === 'Escape') {
      if (!suggesting) return;
      if (cycle) onValueChange(cycle.base);
      setCycle(null);
      setDismissed(true);
      return;
    }
    if (event.defaultPrevented) return;

    const forward = (event.key === 'Tab' && !event.shiftKey) || event.key === 'ArrowDown';
    const backward = (event.key === 'Tab' && event.shiftKey) || event.key === 'ArrowUp';

    if (cycle && (forward || backward)) {
      event.preventDefault();
      pick(cycle.index + (forward ? 1 : -1), cycle.base);
    } else if (!cycle && completion && forward) {
      event.preventDefault();
      pick(0, value);
    } else if (cycle && event.key === 'Enter') {
      event.preventDefault();
      setCycle(null);
    }
  }

  return (
    <div className="flex flex-col gap-1.5">
      <div className="relative rounded-md bg-background">
        {/* Mirror of the text behind the (transparent) textarea, to draw the grey completion right after it. */}
        <div
          ref={mirrorRef}
          aria-hidden
          className={cn(FIELD_CLASSES, 'pointer-events-none absolute inset-0 overflow-hidden whitespace-pre-wrap break-words border-transparent')}
        >
          <span className="text-transparent">{value}</span>
          {completion && <span className="text-muted-foreground/70">{completion}</span>}
        </div>
        <textarea
          {...props}
          id={id}
          value={value}
          placeholder={completion !== null && value === '' ? '' : placeholder}
          aria-autocomplete="inline"
          aria-describedby={focused && suggesting ? hintId : undefined}
          data-suggesting={suggesting ? 'true' : undefined}
          onChange={(event) => {
            setCycle(null);
            setDismissed(false);
            onValueChange(event.target.value);
          }}
          onKeyDown={handleKeyDown}
          onScroll={(event) => {
            if (mirrorRef.current) mirrorRef.current.scrollTop = event.currentTarget.scrollTop;
          }}
          onFocus={(event) => {
            setFocused(true);
            onFocus?.(event);
          }}
          onBlur={(event) => {
            setFocused(false);
            setCycle(null);
            onBlur?.(event);
          }}
          className={cn(
            FIELD_CLASSES,
            'relative block border-input bg-transparent shadow-sm placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2',
            className,
          )}
        />
      </div>

      {visible.length > 0 && (
        <div className="flex flex-wrap items-center gap-1.5" role="group" aria-label="Suggestions">
          {visible.map((suggestion, index) => (
            <button
              key={suggestion}
              type="button"
              tabIndex={-1}
              aria-pressed={cycle?.index === index}
              // Keep focus in the box, so typing can carry on straight after a click.
              onMouseDown={(event) => event.preventDefault()}
              onClick={() => {
                setCycle(null);
                setDismissed(true);
                onValueChange(suggestion);
              }}
              className={cn(
                'rounded-full border px-2.5 py-0.5 text-xs transition-colors',
                cycle?.index === index
                  ? 'border-primary bg-primary/10 text-foreground'
                  : 'border-border bg-muted/40 text-muted-foreground hover:border-primary/50 hover:text-foreground',
              )}
            >
              {suggestion}
            </button>
          ))}
        </div>
      )}

      {focused && suggesting && (
        <p id={hintId} className="text-xs text-muted-foreground">
          <kbd className="rounded border border-border bg-muted px-1 font-sans text-[0.7rem]">Tab</kbd> to fill in
          {matches.length > 1 && (
            <>
              {' · '}
              <kbd className="rounded border border-border bg-muted px-1 font-sans text-[0.7rem]">Tab</kbd> again for the next suggestion
            </>
          )}
          {cycle && (
            <>
              {' · '}
              <kbd className="rounded border border-border bg-muted px-1 font-sans text-[0.7rem]">Enter</kbd> to keep ·{' '}
              <kbd className="rounded border border-border bg-muted px-1 font-sans text-[0.7rem]">Esc</kbd> to undo
            </>
          )}
        </p>
      )}
    </div>
  );
}
