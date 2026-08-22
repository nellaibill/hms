import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { RotateCcw } from 'lucide-react';
import { z } from 'zod';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { hexToHslTriple, hslTripleToHex } from '@/lib/color';
import { useBrandingQuery } from '../hooks/useBrandingQuery';
import { useResetBrandingMutation, useUpdateBrandingMutation } from '../hooks/useBrandingMutations';
import {
  FONT_FAMILIES,
  FONT_FAMILY_LABELS,
  FONT_SIZE_SCALES,
  FONT_SIZE_SCALE_LABELS,
  TOKEN_GROUPS,
  type BrandingConfig,
} from '../types';
import { BrandingLivePreview } from './BrandingLivePreview';

const identitySchema = z.object({
  hospitalName: z.string().trim().min(1, 'Hospital name is required'),
  appTitle: z.string().trim().min(1, 'App title is required'),
  fontFamily: z.enum(FONT_FAMILIES),
  fontSizeScale: z.enum(FONT_SIZE_SCALES),
});

type IdentityFormValues = z.infer<typeof identitySchema>;

interface ColorFieldProps {
  label: string;
  tokenKey: string;
  value: string;
  onChange: (tokenKey: string, hex: string) => void;
}

function ColorField({ label, tokenKey, value, onChange }: ColorFieldProps) {
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
}: {
  items: readonly TokenGroupItem[];
  tokens: Record<string, string>;
  onChange: (tokenKey: string, hex: string) => void;
}) {
  return (
    <div className="rounded-lg border border-border p-4">
      {items.map((item) => (
        <div key={item.key}>
          <ColorField label={item.label} tokenKey={item.key} value={tokens[item.key] ?? '0 0% 50%'} onChange={onChange} />
          {item.pairedForeground && (
            <ColorField
              label={`${item.label} — text`}
              tokenKey={item.pairedForeground}
              value={tokens[item.pairedForeground] ?? '0 0% 100%'}
              onChange={onChange}
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

  const [editingTheme, setEditingTheme] = useState<'light' | 'dark'>('light');
  const [draftTokensLight, setDraftTokensLight] = useState<Record<string, string>>({});
  const [draftTokensDark, setDraftTokensDark] = useState<Record<string, string>>({});
  // Still synced from the persisted config (below) and fed into the live preview even
  // though the upload UI that used to let you set it is hidden — see the Identity tab.
  const [previewLogoUrl, setPreviewLogoUrl] = useState<string | null>(null);
  const [savedMessage, setSavedMessage] = useState(false);

  const {
    register,
    handleSubmit,
    watch,
    setValue,
    reset,
    formState: { errors },
  } = useForm<IdentityFormValues>({
    resolver: zodResolver(identitySchema),
    defaultValues: { hospitalName: '', appTitle: '', fontFamily: 'Inter', fontSizeScale: 'md' },
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
      fontFamily: query.data.fontFamily,
      fontSizeScale: query.data.fontSizeScale,
    });
  }, [query.data, reset]);

  const activeTokens = editingTheme === 'light' ? draftTokensLight : draftTokensDark;
  const setActiveTokens = editingTheme === 'light' ? setDraftTokensLight : setDraftTokensDark;

  const handleTokenChange = (tokenKey: string, hex: string) => {
    setActiveTokens((prev) => ({ ...prev, [tokenKey]: hexToHslTriple(hex) }));
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

            </div>
          </TabsContent>

          <TabsContent value="core">
            <TokenGroupFields items={TOKEN_GROUPS.core} tokens={activeTokens} onChange={handleTokenChange} />
          </TabsContent>

          <TabsContent value="topbar">
            <TokenGroupFields items={TOKEN_GROUPS.topBar} tokens={activeTokens} onChange={handleTokenChange} />
          </TabsContent>

          <TabsContent value="nav">
            <TokenGroupFields items={TOKEN_GROUPS.leftNav} tokens={activeTokens} onChange={handleTokenChange} />
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
