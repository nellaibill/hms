import { zodResolver } from '@hookform/resolvers/zod';
import { ApiError } from '@hms/shared';
import { useEffect, useRef, useState, type ChangeEvent } from 'react';
import { useForm } from 'react-hook-form';
import { RotateCcw, Upload } from 'lucide-react';
import { z } from 'zod';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { hexToHslTriple, hslTripleToHex } from '@/lib/color';
import { useBrandingQuery } from '../hooks/useBrandingQuery';
import { useResetBrandingMutation, useUpdateBrandingMutation, useUploadLogoMutation } from '../hooks/useBrandingMutations';
import { DEFAULT_TOKENS_DARK, DEFAULT_TOKENS_LIGHT } from '../mockBrandingStore';
import {
  FONT_FAMILIES,
  FONT_FAMILY_LABELS,
  FONT_SIZE_SCALES,
  FONT_SIZE_SCALE_LABELS,
  ICON_SIZE_SCALES,
  ICON_SIZE_SCALE_LABELS,
  TOKEN_GROUPS,
  type BrandingConfig,
} from '../types';
import { BrandingLivePreview } from './BrandingLivePreview';

const identitySchema = z.object({
  hospitalName: z.string().trim().min(1, 'Hospital name is required'),
  appTitle: z.string().trim().min(1, 'App title is required'),
  address: z.string().trim().max(500),
  phoneNumber: z.string().trim().max(20),
  fontFamily: z.enum(FONT_FAMILIES),
  fontSizeScale: z.enum(FONT_SIZE_SCALES),
  iconSizeScale: z.enum(ICON_SIZE_SCALES),
});

type IdentityFormValues = z.infer<typeof identitySchema>;

interface ColorFieldProps {
  label: string;
  tokenKey: string;
  value: string;
  onChange: (tokenKey: string, hex: string) => void;
  /** Shows a small reset-to-default icon button at the end of the row when provided — only the
   * Left nav tab wires this up today; every other tab is unchanged. */
  onReset?: () => void;
}

function ColorField({ label, tokenKey, value, onChange, onReset }: ColorFieldProps) {
  const hex = hslTripleToHex(value);
  return (
    <div className="flex items-center justify-between gap-3 border-b border-border/60 py-2 last:border-b-0">
      <Label htmlFor={tokenKey} className="text-sm font-normal text-foreground">
        {label}
      </Label>
      <div className="flex items-center gap-2">
        <input
          id={tokenKey}
          type="color"
          value={hex}
          onChange={(event) => onChange(tokenKey, event.target.value)}
          className="h-8 w-12 cursor-pointer rounded border border-input bg-background p-0.5"
          aria-label={label}
        />
        <span className="w-16 text-right font-mono text-xs text-muted-foreground">{hex}</span>
        {onReset && (
          <Button type="button" variant="ghost" size="icon" className="h-7 w-7" onClick={onReset} aria-label={`Reset ${label} to default`}>
            <RotateCcw className="h-3.5 w-3.5" />
          </Button>
        )}
      </div>
    </div>
  );
}

interface TokenGroupItem {
  key: string;
  label: string;
  pairedForeground?: string;
}

function TokenGroupFields({
  items,
  tokens,
  onChange,
  onResetField,
}: {
  items: readonly TokenGroupItem[];
  tokens: Record<string, string>;
  onChange: (tokenKey: string, hex: string) => void;
  onResetField?: (tokenKey: string) => void;
}) {
  return (
    <div className="rounded-lg border border-border p-4">
      {items.map((item) => (
        <div key={item.key}>
          <ColorField
            label={item.label}
            tokenKey={item.key}
            value={tokens[item.key] ?? '0 0% 50%'}
            onChange={onChange}
            onReset={onResetField ? () => onResetField(item.key) : undefined}
          />
          {item.pairedForeground && (
            <ColorField
              label={`${item.label} — text`}
              tokenKey={item.pairedForeground}
              value={tokens[item.pairedForeground] ?? '0 0% 100%'}
              onChange={onChange}
              onReset={onResetField ? () => onResetField(item.pairedForeground!) : undefined}
            />
          )}
        </div>
      ))}
    </div>
  );
}

export function BrandingForm() {
  const query = useBrandingQuery();
  const updateMutation = useUpdateBrandingMutation();
  const resetMutation = useResetBrandingMutation();
  const uploadLogoMutation = useUploadLogoMutation();
  const logoFileInputRef = useRef<HTMLInputElement>(null);
  const [logoError, setLogoError] = useState<string | null>(null);

  const [editingTheme, setEditingTheme] = useState<'light' | 'dark'>('light');
  const [draftTokensLight, setDraftTokensLight] = useState<Record<string, string>>({});
  const [draftTokensDark, setDraftTokensDark] = useState<Record<string, string>>({});
  // Synced from the persisted config (below) and fed into the live preview, and now also the
  // actual upload target — see the Identity tab's Hospital logo control.
  const [previewLogoUrl, setPreviewLogoUrl] = useState<string | null>(null);
  const [savedMessage, setSavedMessage] = useState(false);

  // The real validation (content decode, exact pixel bounds) is server-side — see
  // BrandingService.UploadLogoAsync — so this is just fast, obvious-case feedback before
  // spending a request; the server's own message is what's shown on rejection either way.
  function handleLogoFileChange(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    event.target.value = '';
    if (!file) return;

    setLogoError(null);
    setSavedMessage(false);

    if (file.size > 500 * 1024) {
      setLogoError('Logo must be 500KB or smaller.');
      return;
    }
    if (!/\.(png|jpe?g|webp|svg)$/i.test(file.name)) {
      setLogoError('Logo must be a PNG, JPG, WEBP, or SVG image.');
      return;
    }

    uploadLogoMutation.mutate(file, {
      onError: (error) => {
        setLogoError(error instanceof ApiError ? error.message : 'Could not upload the logo. Please try again.');
      },
    });
  }

  const {
    register,
    handleSubmit,
    watch,
    setValue,
    reset,
    formState: { errors },
  } = useForm<IdentityFormValues>({
    resolver: zodResolver(identitySchema),
    defaultValues: { hospitalName: '', appTitle: '', address: '', phoneNumber: '', fontFamily: 'Inter', fontSizeScale: 'md', iconSizeScale: 'md' },
  });

  // Sync local editable state from the persisted config whenever it changes —
  // on first load, and after a successful Save/Reset/logo upload (all of
  // which write through the query cache). staleTime: Infinity means this
  // never fires mid-edit from a background refetch.
  useEffect(() => {
    if (!query.data) return;
    setDraftTokensLight(query.data.tokensLight);
    setDraftTokensDark(query.data.tokensDark);
    setPreviewLogoUrl(query.data.logoUrl);
    reset({
      hospitalName: query.data.hospitalName,
      appTitle: query.data.appTitle,
      address: query.data.address,
      phoneNumber: query.data.phoneNumber,
      fontFamily: query.data.fontFamily,
      fontSizeScale: query.data.fontSizeScale,
      iconSizeScale: query.data.iconSizeScale,
    });
  }, [query.data, reset]);

  const activeTokens = editingTheme === 'light' ? draftTokensLight : draftTokensDark;
  const setActiveTokens = editingTheme === 'light' ? setDraftTokensLight : setDraftTokensDark;

  const handleTokenChange = (tokenKey: string, hex: string) => {
    const value = hexToHslTriple(hex);
    // The full-panel background also repaints the odd/even item rows, which otherwise cover
    // most of the panel with their own colors — so picking it recolors the whole navbar.
    const linked = tokenKey === '--sidebar' ? { '--sidebar-odd-bg': value, '--sidebar-even-bg': value } : {};
    setActiveTokens((prev) => ({ ...prev, ...linked, [tokenKey]: value }));
    setSavedMessage(false);
  };

  // Per-field reset (Left nav tab only) — reverts just that one token back to the app's
  // pre-feature default for whichever theme (light/dark) is currently being edited, rather
  // than the "Reset to default theme" button below, which resets everything at once.
  const defaultTokensForActiveTheme = editingTheme === 'light' ? DEFAULT_TOKENS_LIGHT : DEFAULT_TOKENS_DARK;
  const handleTokenReset = (tokenKey: string) => {
    const fallback = defaultTokensForActiveTheme[tokenKey];
    if (fallback === undefined) return;
    setActiveTokens((prev) => ({ ...prev, [tokenKey]: fallback }));
    setSavedMessage(false);
  };

  const onSubmit = (values: IdentityFormValues) => {
    const patch: Partial<BrandingConfig> = {
      ...values,
      tokensLight: draftTokensLight,
      tokensDark: draftTokensDark,
    };
    updateMutation.mutate(patch, {
      onSuccess: () => {
        setSavedMessage(true);
      },
    });
  };

  const handleReset = () => {
    setSavedMessage(false);
    resetMutation.mutate();
  };

  const watched = watch();

  if (query.isLoading) {
    return <p className="text-sm text-muted-foreground">Loading current theme…</p>;
  }

  return (
    <div className="grid grid-cols-1 gap-6 xl:grid-cols-[minmax(0,1fr)_380px]">
      <form onSubmit={handleSubmit(onSubmit)} noValidate className="flex flex-col gap-6">
        <Tabs defaultValue="identity">
          <TabsList>
            <TabsTrigger value="identity" hasError={!!(errors.hospitalName || errors.appTitle)}>
              Identity
            </TabsTrigger>
            <TabsTrigger value="core">Core colors</TabsTrigger>
            <TabsTrigger value="topbar">Top bar</TabsTrigger>
            <TabsTrigger value="nav">Left nav</TabsTrigger>
            <TabsTrigger value="headers">Section headers</TabsTrigger>
            <TabsTrigger value="buttons">Buttons</TabsTrigger>
            <TabsTrigger value="typography">Typography</TabsTrigger>
          </TabsList>

          <TabsContent value="identity">
            <div className="flex flex-col gap-4 rounded-lg border border-border p-4">
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="hospitalName">Hospital name</Label>
                <Input id="hospitalName" {...register('hospitalName')} />
                {errors.hospitalName && <p className="text-sm text-destructive">{errors.hospitalName.message}</p>}
              </div>

              <div className="flex flex-col gap-1.5">
                <Label htmlFor="appTitle">Application title</Label>
                <Input id="appTitle" {...register('appTitle')} />
                {errors.appTitle && <p className="text-sm text-destructive">{errors.appTitle.message}</p>}
              </div>

              <div className="flex flex-col gap-1.5">
                <Label htmlFor="address">Hospital address</Label>
                <Input id="address" placeholder="e.g. 123 Anna Salai, Chennai, Tamil Nadu 600002" {...register('address')} />
                {errors.address && <p className="text-sm text-destructive">{errors.address.message}</p>}
                <p className="text-xs text-muted-foreground">Shown on printed/exported clinical documents (e.g. OPD Consultation).</p>
              </div>

              <div className="flex flex-col gap-1.5">
                <Label htmlFor="phoneNumber">Hospital phone number</Label>
                <Input id="phoneNumber" placeholder="e.g. 044-12345678" {...register('phoneNumber')} />
                {errors.phoneNumber && <p className="text-sm text-destructive">{errors.phoneNumber.message}</p>}
              </div>

              <div className="flex flex-col gap-1.5">
                <Label>Hospital logo</Label>
                <div className="flex items-center gap-3">
                  {/* Same fixed-box containment as the real header (HospitalLogo.tsx, sized via
                      TopHeader's imageClassName="h-16 max-w-80") — the preview here should
                      honestly reflect how any shape will actually render once saved, not a
                      generously-sized preview that hides a bad upload. */}
                  <span className="flex h-16 w-80 shrink-0 items-center justify-center overflow-hidden rounded-md border border-border bg-muted">
                    {previewLogoUrl ? (
                      <img src={previewLogoUrl} alt="Current logo" className="max-h-full max-w-full object-contain" />
                    ) : (
                      <span className="text-xs text-muted-foreground">No logo</span>
                    )}
                  </span>
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    className="gap-1.5"
                    disabled={uploadLogoMutation.isPending}
                    onClick={() => logoFileInputRef.current?.click()}
                  >
                    <Upload className="h-4 w-4" />
                    {uploadLogoMutation.isPending ? 'Uploading…' : 'Upload logo'}
                  </Button>
                  <input
                    ref={logoFileInputRef}
                    type="file"
                    accept=".png,.jpg,.jpeg,.webp,.svg,image/png,image/jpeg,image/webp,image/svg+xml"
                    className="hidden"
                    onChange={handleLogoFileChange}
                  />
                </div>
                <p className="text-xs text-muted-foreground">PNG, JPG, WEBP, or SVG · max 500KB · 16–2000px per side. Applies immediately once uploaded.</p>
                {logoError && <p className="text-sm text-destructive">{logoError}</p>}
              </div>
            </div>
          </TabsContent>

          <TabsContent value="core">
            <TokenGroupFields items={TOKEN_GROUPS.core} tokens={activeTokens} onChange={handleTokenChange} />
          </TabsContent>

          <TabsContent value="topbar">
            <TokenGroupFields items={TOKEN_GROUPS.topBar} tokens={activeTokens} onChange={handleTokenChange} />
          </TabsContent>

          <TabsContent value="nav">
            <div className="flex flex-col gap-1 pb-3">
              <h3 className="text-sm font-semibold text-foreground">Left Navigation Menu</h3>
              <p className="text-xs text-muted-foreground">Configure the appearance of the left navigation menu. The full-panel background recolors the whole menu; set the odd/even item colors afterwards if you want alternating stripes.</p>
            </div>
            <TokenGroupFields items={TOKEN_GROUPS.leftNav} tokens={activeTokens} onChange={handleTokenChange} onResetField={handleTokenReset} />
          </TabsContent>

          <TabsContent value="headers">
            <TokenGroupFields items={TOKEN_GROUPS.sectionHeaders} tokens={activeTokens} onChange={handleTokenChange} />
          </TabsContent>

          <TabsContent value="buttons">
            <TokenGroupFields items={TOKEN_GROUPS.buttons} tokens={activeTokens} onChange={handleTokenChange} />
          </TabsContent>

          <TabsContent value="typography">
            <div className="flex flex-col gap-4 rounded-lg border border-border p-4">
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="fontFamily">Font family</Label>
                <Select value={watched.fontFamily} onValueChange={(value) => setValue('fontFamily', value as IdentityFormValues['fontFamily'])}>
                  <SelectTrigger id="fontFamily">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {FONT_FAMILIES.map((font) => (
                      <SelectItem key={font} value={font}>
                        {FONT_FAMILY_LABELS[font]}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              <div className="flex flex-col gap-1.5">
                <Label htmlFor="fontSizeScale">Base font size</Label>
                <Select value={watched.fontSizeScale} onValueChange={(value) => setValue('fontSizeScale', value as IdentityFormValues['fontSizeScale'])}>
                  <SelectTrigger id="fontSizeScale">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {FONT_SIZE_SCALES.map((scale) => (
                      <SelectItem key={scale} value={scale}>
                        {FONT_SIZE_SCALE_LABELS[scale]}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              <div className="flex flex-col gap-1.5">
                <Label htmlFor="iconSizeScale">Top bar icon size</Label>
                <Select value={watched.iconSizeScale} onValueChange={(value) => setValue('iconSizeScale', value as IdentityFormValues['iconSizeScale'])}>
                  <SelectTrigger id="iconSizeScale">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {ICON_SIZE_SCALES.map((scale) => (
                      <SelectItem key={scale} value={scale}>
                        {ICON_SIZE_SCALE_LABELS[scale]}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                <p className="text-xs text-muted-foreground">Language, Notifications, Calendar, Tools, Tasks, and Documents icons in the top bar.</p>
              </div>
            </div>
          </TabsContent>
        </Tabs>

        {savedMessage && (
          <p role="status" className="rounded-md bg-success/10 px-3 py-2 text-sm text-success">
            Theme saved — applied across the app immediately.
          </p>
        )}

        <div className="flex items-center gap-3">
          <Button type="submit" disabled={updateMutation.isPending}>
            {updateMutation.isPending ? 'Saving…' : 'Save changes'}
          </Button>
          <Button type="button" variant="outline" onClick={handleReset} disabled={resetMutation.isPending}>
            <RotateCcw className="h-4 w-4" />
            Reset to default theme
          </Button>
        </div>
      </form>

      <div className="flex flex-col gap-3 xl:sticky xl:top-6 xl:self-start">
        <div className="flex items-center justify-between">
          <Label className="text-sm">Live preview</Label>
          <div className="flex items-center gap-1 rounded-md border border-input p-0.5 text-xs">
            <button
              type="button"
              onClick={() => setEditingTheme('light')}
              className={editingTheme === 'light' ? 'rounded bg-primary px-2 py-1 text-primary-foreground' : 'px-2 py-1 text-muted-foreground'}
            >
              Light
            </button>
            <button
              type="button"
              onClick={() => setEditingTheme('dark')}
              className={editingTheme === 'dark' ? 'rounded bg-primary px-2 py-1 text-primary-foreground' : 'px-2 py-1 text-muted-foreground'}
            >
              Dark
            </button>
          </div>
        </div>
        <BrandingLivePreview
          hospitalName={watched.hospitalName || 'Hospital name'}
          appTitle={watched.appTitle || 'Application title'}
          logoUrl={previewLogoUrl}
          fontFamily={watched.fontFamily}
          fontSizeScale={watched.fontSizeScale}
          tokens={activeTokens}
        />
        <p className="text-xs text-muted-foreground">
          Shows unsaved edits for the {editingTheme} theme. Colors apply app-wide once you Save.
        </p>
      </div>
    </div>
  );
}
