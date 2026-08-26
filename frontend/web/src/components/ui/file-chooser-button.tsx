import { useRef } from 'react';
import { Button } from './button';

interface FileChooserButtonProps {
  id: string;
  accept: string;
  disabled?: boolean;
  onFileSelected: (file: File) => void;
  /** Shown beside the button in place of "No file chosen" — e.g. the selected filename, an
   * upload-in-progress/success message. Falls back to "No file chosen" (matching the native
   * <input type="file"> convention) when nothing is passed. */
  status?: React.ReactNode;
}

/**
 * A file-picker button with the chosen file's status shown beside it, instead of relying on
 * the browser's own native filename text next to a plain <input type="file">. That native text
 * can't be trusted here: callers that clear `event.target.value` right after reading the file
 * (so the same file can be re-picked, e.g. after a validation error, without the browser
 * treating it as "no change") leave the native input permanently showing "No file chosen" even
 * once a file has genuinely been staged or uploaded. This hides the native input entirely and
 * drives the same picker through a real button + an explicit status node instead.
 */
export function FileChooserButton({ id, accept, disabled, onFileSelected, status }: FileChooserButtonProps) {
  const inputRef = useRef<HTMLInputElement>(null);

  function handleChange(event: React.ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    event.target.value = '';
    if (file) onFileSelected(file);
  }

  return (
    <div className="flex flex-wrap items-center gap-2">
      <input ref={inputRef} id={id} type="file" accept={accept} disabled={disabled} onChange={handleChange} className="sr-only" />
      <Button type="button" size="sm" disabled={disabled} onClick={() => inputRef.current?.click()}>
        Choose File
      </Button>
      <span className="truncate text-sm text-muted-foreground">{status ?? 'No file chosen'}</span>
    </div>
  );
}
