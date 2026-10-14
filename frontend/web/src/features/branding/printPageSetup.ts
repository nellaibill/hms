import {
  DEFAULT_PRINT_SETTINGS,
  PAPER_SIZES,
  PRINT_DOCUMENTS,
  PRINT_MARGIN_LIMITS,
  PRINT_ORIENTATIONS,
  type PaperSize,
  type PrintDocument,
  type PrintDocumentSetup,
  type PrintOrientation,
  type PrintPageSetup,
  type PrintSettings,
} from './types';

function toPaperSize(value: unknown): PaperSize | null {
  return (PAPER_SIZES as readonly unknown[]).includes(value) ? (value as PaperSize) : null;
}

function toOrientation(value: unknown): PrintOrientation | null {
  return (PRINT_ORIENTATIONS as readonly unknown[]).includes(value) ? (value as PrintOrientation) : null;
}

/** Fills whatever the server hasn't stored yet (a row saved before print settings existed, or a
 * partial object) from DEFAULT_PRINT_SETTINGS, and drops sizes/documents the UI doesn't know. */
export function normalizePrintSettings(raw: Partial<PrintSettings> | null | undefined): PrintSettings {
  const fallback = DEFAULT_PRINT_SETTINGS.default;
  const margin = raw?.default?.marginMm;
  const documents: PrintSettings['documents'] = {};
  for (const document of PRINT_DOCUMENTS) {
    const stored = raw?.documents?.[document];
    const setup: PrintDocumentSetup = { paperSize: toPaperSize(stored?.paperSize), orientation: toOrientation(stored?.orientation) };
    if (setup.paperSize || setup.orientation) documents[document] = setup;
  }
  return {
    default: {
      paperSize: toPaperSize(raw?.default?.paperSize) ?? fallback.paperSize,
      orientation: toOrientation(raw?.default?.orientation) ?? fallback.orientation,
      marginMm:
        typeof margin === 'number' && margin >= PRINT_MARGIN_LIMITS.min && margin <= PRINT_MARGIN_LIMITS.max ? margin : fallback.marginMm,
    },
    documents,
  };
}

/** The page one printout gets: its own paper size/orientation when it has an override, the
 * hospital default otherwise. The margin is hospital-wide. */
export function resolvePrintPageSetup(settings: PrintSettings | undefined, document?: PrintDocument): PrintPageSetup {
  const base = settings?.default ?? DEFAULT_PRINT_SETTINGS.default;
  const override = document ? settings?.documents[document] : undefined;
  return {
    paperSize: override?.paperSize ?? base.paperSize,
    orientation: override?.orientation ?? base.orientation,
    marginMm: base.marginMm,
  };
}

/** The CSS `@page` rule for a setup. Chrome and Edge preselect this paper size and layout in
 * their print dialog. */
export function printPageCss(setup: PrintPageSetup): string {
  return `@media print { @page { size: ${setup.paperSize} ${setup.orientation}; margin: ${setup.marginMm}mm; } }`;
}
