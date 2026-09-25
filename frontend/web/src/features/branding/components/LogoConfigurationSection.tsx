import { ApiError } from '@hms/shared';
import { ImageIcon, Info, Loader2, Trash2, Upload } from 'lucide-react';
import { useRef, useState, type ChangeEvent } from 'react';
import { Button } from '@/components/ui/button';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { cn } from '@/lib/utils';
import { resolveBrandLogoUrl } from '../brandLogo';
import { useRemoveLogoMutation, useUploadLogoMutation } from '../hooks/useBrandingMutations';
import { LOGO_FIT_LABELS, LOGO_FITS, LOGO_SLOT_META, LOGO_SLOTS, type BrandingConfig, type LogoDisplaySettings, type LogoFit, type LogoSlot } from '../types';

const MAX_LOGO_BYTES = 500 * 1024;

interface LogoConfigurationSectionProps {
  /** The persisted config — the source of each slot's current logo URL. */
  config: BrandingConfig;
  /** Unsaved display settings (height/fit/fallback) — persisted by the form's Save changes. */
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
 * (the same as the old single "Upload logo" button); display height, fit and the Primary-
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

  function updateSlot(slot: LogoSlot, patch: Partial<LogoDisplaySettings['slots'][LogoSlot]>) {
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
              Upload and manage the logos used across the application. PNG, JPG, WEBP or SVG · max 500KB · 16–2000px per side.
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
            onHeightChange={(height) => updateSlot(slot, { height })}
            onFitChange={(fit) => updateSlot(slot, { fit })}
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
  onHeightChange: (height: number) => void;
  onFitChange: (fit: LogoFit) => void;
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
  onHeightChange,
  onFitChange,
  onFallbackChange,
}: LogoSlotCardProps) {
  const meta = LOGO_SLOT_META[slot];
  const slotDisplay = display.slots[slot];
  const fileInputRef = useRef<HTMLInputElement>(null);
  const isPrimary = slot === 'primary';
  const isFavicon = slot === 'favicon';
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

      {/* Fixed-height preview box; the image is contained within it, never stretched or cropped. */}
      <div className="relative flex h-20 items-center justify-center overflow-hidden rounded-md border border-border bg-background p-2">
        {effectiveUrl ? (
          <img
            src={effectiveUrl}
            alt={`${meta.label} preview`}
            className={cn('h-full w-full object-contain', !ownUrl && 'opacity-50')}
            style={isFavicon ? { maxWidth: 32, maxHeight: 32 } : undefined}
          />
        ) : (
          <span className="text-xs text-muted-foreground">{isFavicon ? 'Browser default' : 'Default logo'}</span>
        )}
        {!ownUrl && effectiveUrl && (
          <span className="absolute bottom-1 right-1 rounded bg-muted px-1.5 py-0.5 text-[10px] text-muted-foreground">Using primary</span>
        )}
      </div>

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

      {isFavicon ? (
        <p className="text-xs text-muted-foreground">Recommended size: 32 × 32 px (PNG or SVG).</p>
      ) : (
        <div className="grid grid-cols-2 gap-2">
          <div className="flex flex-col gap-1">
            <Label htmlFor={`${idPrefix}-height`} className="text-xs font-normal text-muted-foreground">
              Display height
            </Label>
            <Select value={String(slotDisplay.height)} onValueChange={(value) => onHeightChange(Number(value))}>
              <SelectTrigger id={`${idPrefix}-height`} className="h-8 text-xs">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {/* Keeps a stored height that's no longer in the curated list selectable. */}
                {[...new Set([...meta.heights, slotDisplay.height])]
                  .sort((a, b) => a - b)
                  .map((height) => (
                    <SelectItem key={height} value={String(height)}>
                      {height} px
                    </SelectItem>
                  ))}
              </SelectContent>
            </Select>
          </div>
          <div className="flex flex-col gap-1">
            <Label htmlFor={`${idPrefix}-fit`} className="text-xs font-normal text-muted-foreground">
              Fit
            </Label>
            <Select value={slotDisplay.fit} onValueChange={(value) => onFitChange(value as LogoFit)}>
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
        </div>
      )}

      {isPrimary && (
        <label className="flex items-center gap-2 text-xs text-foreground">
          <input
            type="checkbox"
            className="h-4 w-4 rounded border-input accent-primary"
            checked={display.usePrimaryAsFallback}
            onChange={(event) => onFallbackChange(event.target.checked)}
          />
          Use as primary logo (fallback)
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
