import { Info, Printer } from 'lucide-react';
import { useEffect, useState, type ChangeEvent } from 'react';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { resolvePrintPageSetup } from '../printPageSetup';
import {
  PAPER_SIZE_LABELS,
  PAPER_SIZES,
  PRINT_DOCUMENT_META,
  PRINT_DOCUMENTS,
  PRINT_MARGIN_LIMITS,
  PRINT_ORIENTATION_LABELS,
  PRINT_ORIENTATIONS,
  type PaperSize,
  type PrintDocument,
  type PrintDocumentSetup,
  type PrintOrientation,
  type PrintPageSetup,
  type PrintSettings,
} from '../types';

/** Radix Select can't use '' as an item value, so "no override" is this sentinel. */
const USE_DEFAULT = 'default';

/** Portrait width × height in mm, for the page preview. */
const PAPER_MM: Record<PaperSize, [number, number]> = {
  A4: [210, 297],
  A3: [297, 420],
  A5: [148, 210],
  Letter: [215.9, 279.4],
  Legal: [215.9, 355.6],
};

/** Longest side of the preview page, in px. */
const PREVIEW_BOX_PX = 112;

interface PrintSettingsSectionProps {
  /** Unsaved print settings — persisted by the form's Save changes. */
  value: PrintSettings;
  onChange: (next: PrintSettings) => void;
}

/**
 * Theme & Branding → Print: the paper size, orientation and margin every printout starts with,
 * plus an optional paper size/orientation per kind of document (e.g. invoices on A5, reports
 * on A3 landscape). Applied by PrintPageStyle as a CSS `@page` rule.
 */
export function PrintSettingsSection({ value, onChange }: PrintSettingsSectionProps) {
  function updateDefault(patch: Partial<PrintPageSetup>) {
    onChange({ ...value, default: { ...value.default, ...patch } });
  }

  function updateDocument(document: PrintDocument, patch: Partial<PrintDocumentSetup>) {
    const current = value.documents[document] ?? { paperSize: null, orientation: null };
    const next = { ...current, ...patch };
    const documents = { ...value.documents };
    if (next.paperSize || next.orientation) documents[document] = next;
    else delete documents[document];
    onChange({ ...value, documents });
  }

  return (
    <section className="flex flex-col gap-5">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="flex items-start gap-3">
          <Printer className="mt-0.5 h-6 w-6 shrink-0 text-primary" />
          <div>
            <h3 className="text-base font-semibold text-foreground">Print Settings</h3>
            <p className="text-xs text-muted-foreground">
              Paper size, orientation and margins for printed documents. Chrome and Edge open the print dialog with these already selected.
            </p>
          </div>
        </div>
        <p className="flex items-center gap-2 rounded-md border border-info/30 bg-info/10 px-3 py-1.5 text-xs text-foreground">
          <Info className="h-4 w-4 shrink-0 text-info" />
          Pharmacy counter bills keep their own format (Pharmacy → Settings).
        </p>
      </div>

      <div className="flex flex-col gap-4 rounded-lg border border-primary/40 bg-primary/5 p-4 sm:flex-row sm:items-center">
        <div className="flex min-w-0 flex-1 flex-col gap-3">
          <div>
            <p className="text-sm font-semibold text-primary">Default page</p>
            <p className="text-xs text-muted-foreground">Used by every printout that doesn’t have its own setting below.</p>
          </div>
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
            <div className="flex flex-col gap-1">
              <Label htmlFor="print-default-paper" className="text-xs font-normal text-muted-foreground">
                Paper size
              </Label>
              <Select value={value.default.paperSize} onValueChange={(paperSize) => updateDefault({ paperSize: paperSize as PaperSize })}>
                <SelectTrigger id="print-default-paper" className="h-9">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {PAPER_SIZES.map((size) => (
                    <SelectItem key={size} value={size}>
                      {PAPER_SIZE_LABELS[size]}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="flex flex-col gap-1">
              <Label htmlFor="print-default-orientation" className="text-xs font-normal text-muted-foreground">
                Orientation
              </Label>
              <Select
                value={value.default.orientation}
                onValueChange={(orientation) => updateDefault({ orientation: orientation as PrintOrientation })}
              >
                <SelectTrigger id="print-default-orientation" className="h-9">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {PRINT_ORIENTATIONS.map((orientation) => (
                    <SelectItem key={orientation} value={orientation}>
                      {PRINT_ORIENTATION_LABELS[orientation]}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <MarginInput value={value.default.marginMm} onChange={(marginMm) => updateDefault({ marginMm })} />
          </div>
        </div>
        <PagePreview setup={value.default} />
      </div>

      <div className="flex flex-col gap-2">
        <div>
          <p className="text-sm font-semibold text-foreground">Per document</p>
          <p className="text-xs text-muted-foreground">Give a document its own paper size or orientation. The margin always follows the default page.</p>
        </div>
        <div className="flex flex-col divide-y divide-border rounded-lg border border-border">
          {PRINT_DOCUMENTS.map((document) => (
            <DocumentRow
              key={document}
              document={document}
              settings={value}
              onChange={(patch) => updateDocument(document, patch)}
            />
          ))}
        </div>
      </div>
    </section>
  );
}

function DocumentRow({
  document,
  settings,
  onChange,
}: {
  document: PrintDocument;
  settings: PrintSettings;
  onChange: (patch: Partial<PrintDocumentSetup>) => void;
}) {
  const meta = PRINT_DOCUMENT_META[document];
  const override = settings.documents[document];
  const effective = resolvePrintPageSetup(settings, document);
  const idPrefix = `print-${document}`;

  return (
    <div className="grid grid-cols-1 gap-2 p-3 md:grid-cols-[minmax(0,1fr)_13rem_11rem] md:items-center md:gap-3">
      <div className="min-w-0">
        <p className="text-sm font-medium text-foreground">{meta.label}</p>
        <p className="text-xs text-muted-foreground">{meta.usage}</p>
      </div>
      <div className="flex flex-col gap-1">
        <Label htmlFor={`${idPrefix}-paper`} className="text-xs font-normal text-muted-foreground md:sr-only">
          Paper size
        </Label>
        <Select
          value={override?.paperSize ?? USE_DEFAULT}
          onValueChange={(paperSize) => onChange({ paperSize: paperSize === USE_DEFAULT ? null : (paperSize as PaperSize) })}
        >
          <SelectTrigger id={`${idPrefix}-paper`} aria-label={`${meta.label} paper size`} className="h-8 text-xs">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value={USE_DEFAULT}>Default ({settings.default.paperSize})</SelectItem>
            {PAPER_SIZES.map((size) => (
              <SelectItem key={size} value={size}>
                {PAPER_SIZE_LABELS[size]}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>
      <div className="flex flex-col gap-1">
        <Label htmlFor={`${idPrefix}-orientation`} className="text-xs font-normal text-muted-foreground md:sr-only">
          Orientation
        </Label>
        <Select
          value={override?.orientation ?? USE_DEFAULT}
          onValueChange={(orientation) =>
            onChange({ orientation: orientation === USE_DEFAULT ? null : (orientation as PrintOrientation) })
          }
        >
          <SelectTrigger id={`${idPrefix}-orientation`} aria-label={`${meta.label} orientation`} className="h-8 text-xs">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value={USE_DEFAULT}>Default ({PRINT_ORIENTATION_LABELS[settings.default.orientation]})</SelectItem>
            {PRINT_ORIENTATIONS.map((orientation) => (
              <SelectItem key={orientation} value={orientation}>
                {PRINT_ORIENTATION_LABELS[orientation]}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>
      {override && (
        <p className="text-xs text-muted-foreground md:col-span-3">
          Prints on {effective.paperSize} {PRINT_ORIENTATION_LABELS[effective.orientation].toLowerCase()}.
        </p>
      )}
    </div>
  );
}

/** A to-scale outline of the default page — its proportions follow the paper size and
 * orientation, and the dashed box shows the printable area inside the margin. */
function PagePreview({ setup }: { setup: PrintPageSetup }) {
  const [portraitWidth, portraitHeight] = PAPER_MM[setup.paperSize];
  const [widthMm, heightMm] = setup.orientation === 'landscape' ? [portraitHeight, portraitWidth] : [portraitWidth, portraitHeight];
  const scale = PREVIEW_BOX_PX / Math.max(widthMm, heightMm);
  const marginPx = setup.marginMm * scale;

  return (
    <div className="flex shrink-0 flex-col items-center gap-1.5 self-center">
      <div className="flex items-center justify-center" style={{ width: PREVIEW_BOX_PX, height: PREVIEW_BOX_PX }}>
        <div
          className="relative rounded-sm border border-border bg-white shadow-soft-md"
          style={{ width: widthMm * scale, height: heightMm * scale }}
          aria-hidden
        >
          <div className="absolute border border-dashed border-primary/50" style={{ inset: marginPx }} />
        </div>
      </div>
      <p className="text-xs tabular-nums text-muted-foreground">
        {setup.paperSize} · {PRINT_ORIENTATION_LABELS[setup.orientation]} · {setup.marginMm} mm
      </p>
    </div>
  );
}

/** Number input that lets the field be cleared/retyped freely, commits only in-range values,
 * and snaps back to the nearest bound on blur (same behavior as the logo size inputs). */
function MarginInput({ value, onChange }: { value: number; onChange: (value: number) => void }) {
  const [text, setText] = useState(String(value));
  const { min, max } = PRINT_MARGIN_LIMITS;

  useEffect(() => setText(String(value)), [value]);

  function handleChange(event: ChangeEvent<HTMLInputElement>) {
    setText(event.target.value);
    const parsed = Number(event.target.value);
    if (event.target.value.trim() !== '' && Number.isInteger(parsed) && parsed >= min && parsed <= max) onChange(parsed);
  }

  function handleBlur() {
    const parsed = Math.round(Number(text));
    const clamped = Number.isFinite(parsed) && text.trim() !== '' ? Math.min(max, Math.max(min, parsed)) : value;
    setText(String(clamped));
    if (clamped !== value) onChange(clamped);
  }

  return (
    <div className="flex flex-col gap-1">
      <Label htmlFor="print-default-margin" className="text-xs font-normal text-muted-foreground">
        Margin (mm, {min}–{max})
      </Label>
      <Input
        id="print-default-margin"
        type="number"
        inputMode="numeric"
        min={min}
        max={max}
        step={1}
        value={text}
        onChange={handleChange}
        onBlur={handleBlur}
        className="h-9"
      />
    </div>
  );
}
