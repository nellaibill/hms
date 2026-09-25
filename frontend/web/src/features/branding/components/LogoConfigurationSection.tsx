import { ApiError } from '@hms/shared';
import { ImageIcon, Info, Loader2, Trash2, Upload } from 'lucide-react';
import { useEffect, useRef, useState, type ChangeEvent } from 'react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { cn } from '@/lib/utils';
import { resolveBrandLogoUrl } from '../brandLogo';
import { useRemoveLogoMutation, useUploadLogoMutation } from '../hooks/useBrandingMutations';
import {
  LOGO_DIMENSION_LIMITS,
  LOGO_FIT_LABELS,
  LOGO_FITS,
  LOGO_SLOT_META,
  LOGO_SLOTS,
  type BrandingConfig,
  type LogoDisplaySettings,
  type LogoFit,
  type LogoSlot,
  type LogoSlotDisplay,
} from '../types';

const MAX_LOGO_BYTES = 500 * 1024;
/** Inner height of each card's preview area (h-24 minus p-2) — the configured box is scaled down to fit it. */
const PREVIEW_MAX_HEIGHT = 80;

interface LogoConfigurationSectionProps {
  /** The persisted config — the source of each slot's current logo URL. */
  config: BrandingConfig;
  /** Unsaved display settings (width/height/fit/fallback) — persisted by the form's Save changes. */
  display: LogoDisplaySettings;
  onDisplayChange: (next: LogoDisplaySettings) => void;
  /** Called right before an upload/remove updates the cached config, so the form can keep its
   * other unsaved edits instead of re-syncing them from the server response. */
  onBeforeLogoChange: () => void;
  onLogoChangeFailed: () => void;
}

/**
 * Theme & Branding → Identity → Logo Configuration: one compact card per logo slot with a
 * preview, Replace/Remove and its display options. Replace/Remove take effect immediately
 * (the same as the old single "Upload logo" button); width, height, fit and the Primary-
 * fallback toggle are part of the form draft and apply on Save changes.
 */
export function LogoConfigurationSection({ config, display, onDisplayChange, onBeforeLogoChange, onLogoChangeFailed }: LogoConfigurationSectionProps) {
  const uploadMutation = useUploadLogoMutation();
  const removeMutation = useRemoveLogoMutation();
  const [errors, setErrors] = useState<Partial<Record<LogoSlot, string>>>({});

  function setError(slot: LogoSlot, message: string | undefined) {
    setErrors((prev) => ({ ...prev, [slot]: message }));
  }

  function failure(slot: LogoSlot, error: unknown, fallbackMessage: string) {
    onLogoChangeFailed();
    setError(slot, error instanceof ApiError ? error.message : fallbackMessage);
  }

  // The real validation (content decode, exact pixel bounds) is server-side — see
  // BrandingService.UploadLogoAsync — so this is just fast, obvious-case feedback before
  // spending a request; the server's own message is what's shown on rejection either way.
  function handleUpload(slot: LogoSlot, file: File) {
    setError(slot, undefined);
    if (file.size > MAX_LOGO_BYTES) {
      setError(slot, 'Logo must be 500KB or smaller.');
      return;
    }
    if (!/\.(png|jpe?g|webp|svg)$/i.test(file.name)) {
      setError(slot, 'Logo must be a PNG, JPG, WEBP, or SVG image.');
      return;
    }
    onBeforeLogoChange();
    uploadMutation.mutate({ file, slot }, { onError: (error) => failure(slot, error, 'Could not upload the logo. Please try again.') });
  }

  function handleRemove(slot: LogoSlot) {
    setError(slot, undefined);
    onBeforeLogoChange();
    removeMutation.mutate(slot, { onError: (error) => failure(slot, error, 'Could not remove the logo. Please try again.') });
  }

  function updateSlot(slot: LogoSlot, patch: Partial<LogoSlotDisplay>) {
    onDisplayChange({ ...display, slots: { ...display.slots, [slot]: { ...display.slots[slot], ...patch } } });
  }

  // Previews resolve the fallback against the draft toggle, so unticking it shows the effect before Save.
  const previewConfig: BrandingConfig = { ...config, logoDisplay: display };

  return (
    <section className="flex flex-col gap-4">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="flex items-start gap-3">
          <ImageIcon className="mt-0.5 h-6 w-6 shrink-0 text-primary" />
          <div>
            <h3 className="text-base font-semibold text-foreground">Logo Configuration</h3>
            <p className="text-xs text-muted-foreground">
              Upload and configure the logos used across the application. PNG, JPG, WEBP or SVG · max 500KB · 16–2000px per side.
            </p>
          </div>
        </div>
        <p className="flex items-center gap-2 rounded-md border border-info/30 bg-info/10 px-3 py-1.5 text-xs text-foreground">
          <Info className="h-4 w-4 shrink-0 text-info" />
          Primary logo is used when a specific logo is not configured.
        </p>
      </div>

      <div className="grid grid-cols-[repeat(auto-fill,minmax(12.5rem,1fr))] gap-3">
        {LOGO_SLOTS.map((slot) => (
          <LogoSlotCard
            key={slot}
            slot={slot}
            ownUrl={config[LOGO_SLOT_META[slot].configKey]}
            effectiveUrl={resolveBrandLogoUrl(previewConfig, slot, null)}
            display={display}
            error={errors[slot]}
            isUploading={uploadMutation.isPending && uploadMutation.variables?.slot === slot}
            isRemoving={removeMutation.isPending && removeMutation.variables === slot}
            onUpload={(file) => handleUpload(slot, file)}
            onRemove={() => handleRemove(slot)}
            onSlotChange={(patch) => updateSlot(slot, patch)}
            onFallbackChange={(usePrimaryAsFallback) => onDisplayChange({ ...display, usePrimaryAsFallback })}
          />
        ))}
      </div>
    </section>
  );
}

interface LogoSlotCardProps {
  slot: LogoSlot;
  /** This slot's own upload, or null. */
  ownUrl: string | null;
  /** What actually renders for this slot (own upload, else the Primary logo when enabled), or null for the bundled default. */
  effectiveUrl: string | null;
  display: LogoDisplaySettings;
  error?: string;
  isUploading: boolean;
  isRemoving: boolean;
  onUpload: (file: File) => void;
  onRemove: () => void;
  onSlotChange: (patch: Partial<LogoSlotDisplay>) => void;
  onFallbackChange: (value: boolean) => void;
}

function LogoSlotCard({
  slot,
  ownUrl,
  effectiveUrl,
  display,
  error,
  isUploading,
  isRemoving,
  onUpload,
  onRemove,
  onSlotChange,
  onFallbackChange,
}: LogoSlotCardProps) {
  const meta = LOGO_SLOT_META[slot];
  const slotDisplay = display.slots[slot];
  const fileInputRef = useRef<HTMLInputElement>(null);
  const isPrimary = slot === 'primary';
  const busy = isUploading || isRemoving;
  const idPrefix = `logo-${slot}`;

  function handleFileChange(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    event.target.value = '';
    if (file) onUpload(file);
  }

  return (
    <div className={cn('flex flex-col gap-3 rounded-lg border p-3', isPrimary ? 'border-primary/40 bg-primary/5' : 'border-border bg-card')}>
      <div>
        <p className={cn('text-sm font-semibold', isPrimary ? 'text-primary' : 'text-foreground')}>{meta.label}</p>
        <p className="text-xs text-muted-foreground">{meta.usage}</p>
      </div>

      <LogoPreview url={effectiveUrl} display={slotDisplay} isFallback={!ownUrl && Boolean(effectiveUrl)} label={meta.label} isFavicon={slot === 'favicon'} />

      <div className="grid grid-cols-2 gap-2">
        <Button type="button" variant="outline" size="sm" className="gap-1.5" disabled={busy} onClick={() => fileInputRef.current?.click()}>
          {isUploading ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <Upload className="h-3.5 w-3.5" />}
          {ownUrl ? 'Replace' : 'Upload'}
        </Button>
        <Button
          type="button"
          variant="outline"
          size="sm"
          className="gap-1.5 text-destructive hover:text-destructive"
          disabled={busy || !ownUrl}
          onClick={onRemove}
        >
          {isRemoving ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <Trash2 className="h-3.5 w-3.5" />}
          Remove
        </Button>
        <input
          ref={fileInputRef}
          type="file"
          accept=".png,.jpg,.jpeg,.webp,.svg,image/png,image/jpeg,image/webp,image/svg+xml"
          className="hidden"
          onChange={handleFileChange}
          aria-label={`Upload ${meta.label}`}
        />
      </div>

      <div className="grid grid-cols-2 gap-2">
        <DimensionInput
          id={`${idPrefix}-width`}
          label="Width (px)"
          value={slotDisplay.width}
          max={LOGO_DIMENSION_LIMITS.maxWidth}
          onChange={(width) => onSlotChange({ width })}
        />
        <DimensionInput
          id={`${idPrefix}-height`}
          label="Height (px)"
          value={slotDisplay.height}
          max={LOGO_DIMENSION_LIMITS.maxHeight}
          onChange={(height) => onSlotChange({ height })}
        />
      </div>

      <div className="flex flex-col gap-1">
        <Label htmlFor={`${idPrefix}-fit`} className="text-xs font-normal text-muted-foreground">
          Fit
        </Label>
        <Select value={slotDisplay.fit} onValueChange={(value) => onSlotChange({ fit: value as LogoFit })}>
          <SelectTrigger id={`${idPrefix}-fit`} className="h-8 text-xs">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {LOGO_FITS.map((fit) => (
              <SelectItem key={fit} value={fit}>
                {LOGO_FIT_LABELS[fit]}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      {slot === 'favicon' && <p className="text-xs text-muted-foreground">Recommended size: 32 × 32 px (PNG or SVG).</p>}

      {isPrimary && (
        <label className="flex items-center gap-2 text-xs text-foreground">
          <input
            type="checkbox"
            className="h-4 w-4 rounded border-input accent-primary"
            checked={display.usePrimaryAsFallback}
            onChange={(event) => onFallbackChange(event.target.checked)}
          />
          Use as fallback for other logos
        </label>
      )}

      {error && (
        <p role="alert" className="text-xs text-destructive">
          {error}
        </p>
      )}
    </div>
  );
}

/** Draws the logo in a dashed box of the configured width × height with the configured fit —
 * uniformly scaled down (never distorted) when that box is bigger than the card's preview area,
 * so the proportions shown are the real ones. Updates on every Width/Height/Fit change. */
function LogoPreview({ url, display, isFallback, label, isFavicon }: { url: string | null; display: LogoSlotDisplay; isFallback: boolean; label: string; isFavicon: boolean }) {
  const areaRef = useRef<HTMLDivElement>(null);
  const [areaWidth, setAreaWidth] = useState(0);

  useEffect(() => {
    const element = areaRef.current;
    if (!element) return;
    const observer = new ResizeObserver(([entry]) => setAreaWidth(entry.contentRect.width));
    observer.observe(element);
    return () => observer.disconnect();
  }, []);

  const scale = areaWidth > 0 ? Math.min(1, areaWidth / display.width, PREVIEW_MAX_HEIGHT / display.height) : 1;

  return (
    <div ref={areaRef} className="relative flex h-24 items-center justify-center overflow-hidden rounded-md border border-border bg-background p-2">
      {url ? (
        <span
          className="flex shrink-0 items-center justify-center overflow-hidden outline-dashed outline-1 outline-border"
          style={{ width: display.width * scale, height: display.height * scale }}
        >
          <img src={url} alt={`${label} preview`} className={cn('h-full w-full', isFallback && 'opacity-50')} style={{ objectFit: display.fit }} />
        </span>
      ) : (
        <span className="text-xs text-muted-foreground">{isFavicon ? 'Browser default' : 'Default logo'}</span>
      )}
      {isFallback && <span className="absolute bottom-1 right-1 rounded bg-muted px-1.5 py-0.5 text-[10px] text-muted-foreground">Using primary</span>}
      {url && (
        <span className="absolute left-1 top-1 rounded bg-muted/80 px-1 text-[10px] tabular-nums text-muted-foreground">
          {display.width}×{display.height}
        </span>
      )}
    </div>
  );
}

/** Number input that lets the field be cleared/retyped freely, commits only in-range values,
 * and snaps back to the nearest bound on blur. */
function DimensionInput({ id, label, value, max, onChange }: { id: string; label: string; value: number; max: number; onChange: (value: number) => void }) {
  const [text, setText] = useState(String(value));
  const min = LOGO_DIMENSION_LIMITS.min;

  useEffect(() => setText(String(value)), [value]);

  function handleChange(event: ChangeEvent<HTMLInputElement>) {
    setText(event.target.value);
    const parsed = Number(event.target.value);
    if (Number.isInteger(parsed) && parsed >= min && parsed <= max) onChange(parsed);
  }

  function handleBlur() {
    const parsed = Math.round(Number(text));
    const clamped = Number.isFinite(parsed) && text.trim() !== '' ? Math.min(max, Math.max(min, parsed)) : value;
    setText(String(clamped));
    if (clamped !== value) onChange(clamped);
  }

  return (
    <div className="flex flex-col gap-1">
      <Label htmlFor={id} className="text-xs font-normal text-muted-foreground">
        {label}
      </Label>
      <Input id={id} type="number" inputMode="numeric" min={min} max={max} step={1} value={text} onChange={handleChange} onBlur={handleBlur} className="h-8 text-xs" />
    </div>
  );
}
