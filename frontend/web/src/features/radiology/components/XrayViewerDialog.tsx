import { Contrast, RotateCcw, ZoomIn, ZoomOut } from 'lucide-react';
import { useEffect, useState } from 'react';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from '@/components/ui/dialog';

const MIN_ZOOM = 1;
const MAX_ZOOM = 4;

interface XrayViewerDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  url: string | null;
  title: string;
}

/** A full-size viewer for one stored X-ray, with the few adjustments a clinician actually reaches for
 * when reading a film on screen: zoom, invert (negative), and brightness/contrast. Adjustments are
 * display-only (CSS filters) — the stored image and what the AI is sent are never changed. */
export function XrayViewerDialog({ open, onOpenChange, url, title }: XrayViewerDialogProps) {
  const [zoom, setZoom] = useState(MIN_ZOOM);
  const [inverted, setInverted] = useState(false);
  const [brightness, setBrightness] = useState(100);
  const [contrast, setContrast] = useState(100);

  const reset = () => {
    setZoom(MIN_ZOOM);
    setInverted(false);
    setBrightness(100);
    setContrast(100);
  };

  // Each opening starts from the untouched image.
  useEffect(() => {
    if (open) reset();
  }, [open]);

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="flex max-h-[92vh] w-[95vw] max-w-5xl flex-col gap-3 p-4 sm:p-6">
        <DialogHeader>
          <DialogTitle className="truncate pr-8 text-base">{title}</DialogTitle>
          <DialogDescription>Display adjustments only — the stored image is not changed.</DialogDescription>
        </DialogHeader>

        <div className="flex flex-wrap items-center gap-x-4 gap-y-2 rounded-lg border border-border bg-muted/40 px-3 py-2 text-xs">
          <div className="flex items-center gap-1">
            <Button size="icon" variant="outline" className="h-8 w-8" onClick={() => setZoom((z) => Math.max(MIN_ZOOM, z - 0.5))} disabled={zoom <= MIN_ZOOM} aria-label="Zoom out">
              <ZoomOut className="h-4 w-4" />
            </Button>
            <span className="w-12 text-center tabular-nums text-muted-foreground">{Math.round(zoom * 100)}%</span>
            <Button size="icon" variant="outline" className="h-8 w-8" onClick={() => setZoom((z) => Math.min(MAX_ZOOM, z + 0.5))} disabled={zoom >= MAX_ZOOM} aria-label="Zoom in">
              <ZoomIn className="h-4 w-4" />
            </Button>
          </div>

          <Button size="sm" variant={inverted ? 'default' : 'outline'} onClick={() => setInverted((value) => !value)} aria-pressed={inverted}>
            <Contrast className="mr-2 h-4 w-4" />
            Invert
          </Button>

          <label className="flex items-center gap-2 text-muted-foreground">
            Brightness
            <input type="range" min={50} max={200} value={brightness} onChange={(event) => setBrightness(Number(event.target.value))} className="w-28" />
          </label>
          <label className="flex items-center gap-2 text-muted-foreground">
            Contrast
            <input type="range" min={50} max={250} value={contrast} onChange={(event) => setContrast(Number(event.target.value))} className="w-28" />
          </label>

          <Button size="sm" variant="ghost" onClick={reset} className="ml-auto">
            <RotateCcw className="mr-2 h-4 w-4" />
            Reset
          </Button>
        </div>

        <div className="min-h-0 flex-1 overflow-auto rounded-lg bg-black">
          {url && (
            <img
              src={url}
              alt={title}
              draggable={false}
              className="mx-auto block max-w-none select-none"
              style={{
                width: `${zoom * 100}%`,
                filter: `${inverted ? 'invert(1) ' : ''}brightness(${brightness}%) contrast(${contrast}%)`,
              }}
            />
          )}
        </div>
      </DialogContent>
    </Dialog>
  );
}
