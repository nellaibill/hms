import { ApiError, type ProductImportRowResult } from '@hms/shared';
import { useQueryClient } from '@tanstack/react-query';
import { CheckCircle2, Download, FileSpreadsheet, Loader2, Upload } from 'lucide-react';
import { useRef, useState } from 'react';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { expiryLabel, money, quantity } from '@/features/pharmacy/common/format';
import { downloadFailedRows, downloadImportTemplate, IMPORT_COLUMNS, type ParsedSheet, readImportFile } from '@/features/pharmacy/import/spreadsheet';
import { SETUP_LINKS } from '@/features/pharmacy/shell/links';
import { PharmacyPage } from '@/features/pharmacy/shell/PharmacyPage';
import { StoreSelect } from '@/features/pharmacy/stores';
import { pharmacyApi } from '@/services/apiClient';

/** Rows sent per request — small enough that each call returns in a few seconds and the
 * progress bar moves, well inside the server's 200-row limit. */
const CHUNK_SIZE = 100;
const PREVIEW_ROWS = 50;

interface RunSummary {
  validateOnly: boolean;
  created: number;
  matched: number;
  stocked: number;
  results: ProductImportRowResult[];
}

/**
 * Go-live data load. A pharmacy arrives with a few thousand products and a shelf full of
 * stock already recorded somewhere — its old software, or a spreadsheet. This reads that
 * sheet, shows what it understood, checks it without saving, and then loads it: products
 * into the catalog and each batch's quantity in as opening stock.
 */
export default function ImportPage() {
  const queryClient = useQueryClient();
  const fileInputRef = useRef<HTMLInputElement>(null);

  const [fileName, setFileName] = useState('');
  const [sheet, setSheet] = useState<ParsedSheet | null>(null);
  const [storeId, setStoreId] = useState('');
  const [readError, setReadError] = useState<string | null>(null);
  const [isReading, setIsReading] = useState(false);
  const [progress, setProgress] = useState<{ done: number; total: number; validateOnly: boolean } | null>(null);
  const [summary, setSummary] = useState<RunSummary | null>(null);
  const [runError, setRunError] = useState<string | null>(null);

  const isRunning = progress !== null;
  const errors = summary?.results.filter((result) => result.status === 'Error') ?? [];
  const notes = summary?.results.filter((result) => result.status !== 'Error' && result.message) ?? [];
  const withStock = sheet?.rows.filter((row) => (row.openingQuantity ?? 0) > 0).length ?? 0;

  async function handleFile(file: File | undefined) {
    if (!file) return;
    setIsReading(true);
    setReadError(null);
    setSummary(null);
    setRunError(null);
    setSheet(null);
    setFileName(file.name);

    try {
      const parsed = await readImportFile(file);
      if (parsed.rows.length === 0) {
        setReadError('The file has headings but no product rows under them.');
      } else {
        setSheet(parsed);
      }
    } catch (error) {
      setReadError(error instanceof Error ? error.message : 'Could not read this file. Save it as .xlsx or .csv and try again.');
    } finally {
      setIsReading(false);
      if (fileInputRef.current) fileInputRef.current.value = '';
    }
  }

  async function run(validateOnly: boolean) {
    if (!sheet || isRunning) return;
    setRunError(null);
    setSummary(null);

    const total = sheet.rows.length;
    const collected: RunSummary = { validateOnly, created: 0, matched: 0, stocked: 0, results: [] };
    setProgress({ done: 0, total, validateOnly });

    try {
      for (let start = 0; start < total; start += CHUNK_SIZE) {
        const response = await pharmacyApi.importProducts({
          storeId: storeId || undefined,
          validateOnly,
          firstRowNumber: sheet.firstRowNumber + start,
          rows: sheet.rows.slice(start, start + CHUNK_SIZE),
        });
        collected.created += response.productsCreated;
        collected.matched += response.productsMatched;
        collected.stocked += response.batchesStocked;
        collected.results.push(...response.rows);
        setProgress({ done: Math.min(start + CHUNK_SIZE, total), total, validateOnly });
      }
      setSummary(collected);
    } catch (error) {
      // Whatever went in before the failure stays in — the import is safe to run again, so
      // say how far it got rather than pretending nothing happened.
      setSummary(collected.results.length > 0 ? collected : null);
      setRunError(
        `${error instanceof ApiError ? error.message : 'The import was interrupted.'} ${
          collected.results.length > 0 && !validateOnly ? `The first ${collected.results.length} rows were already loaded; running the import again will skip them.` : ''
        }`,
      );
    } finally {
      setProgress(null);
      if (!validateOnly) void queryClient.invalidateQueries({ queryKey: ['pharmacy'] });
    }
  }

  return (
    <PharmacyPage
      title="Import products and opening stock"
      subtitle="Load your existing product list and shelf stock from Excel, instead of keying it in."
      subLinks={SETUP_LINKS}
      actions={
        <Button size="sm" variant="outline" onClick={() => void downloadImportTemplate()}>
          <Download className="h-4 w-4" />
          Download template
        </Button>
      }
    >
      <section className="rounded-lg border border-border bg-background p-4">
        <div className="flex flex-wrap items-end gap-3">
          <input ref={fileInputRef} type="file" accept=".xlsx,.csv" className="hidden" onChange={(event) => void handleFile(event.target.files?.[0])} />
          <Button onClick={() => fileInputRef.current?.click()} disabled={isReading || isRunning}>
            {isReading ? <Loader2 className="h-4 w-4 animate-spin" /> : <Upload className="h-4 w-4" />}
            Choose Excel or CSV file
          </Button>
          <StoreSelect id="import-store" value={storeId} onValueChange={setStoreId} label="Opening stock goes into" />
          {fileName && !readError && sheet && (
            <p className="flex items-center gap-2 pb-2 text-sm text-foreground">
              <FileSpreadsheet className="h-4 w-4 text-muted-foreground" />
              {fileName} · {sheet.rows.length} products · {withStock} with stock
            </p>
          )}
        </div>
        <p className="mt-3 text-xs text-muted-foreground">
          Only the product name is required. Fill batch, expiry and stock quantity to load what is on your shelf. Running the same file twice is safe: products are
          matched by name, and a batch that already has opening stock is not stocked again.
        </p>
        {readError && (
          <p role="alert" className="mt-3 rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
            {readError}
          </p>
        )}
      </section>

      {sheet && (
        <>
          <section className="rounded-lg border border-border bg-background p-4">
            <div className="flex flex-wrap items-center justify-between gap-3">
              <div className="min-w-0 text-sm">
                <p className="font-medium text-foreground">Columns read from your file</p>
                <p className="mt-1 flex flex-wrap gap-1.5">
                  {sheet.matched.map((match) => (
                    <Badge key={match.field} variant="secondary">
                      {match.heading}
                    </Badge>
                  ))}
                </p>
                {sheet.ignored.length > 0 && <p className="mt-2 text-xs text-muted-foreground">Not used: {sheet.ignored.join(', ')}</p>}
              </div>
              <div className="flex gap-2">
                <Button variant="outline" onClick={() => void run(true)} disabled={isRunning}>
                  {progress?.validateOnly ? <Loader2 className="h-4 w-4 animate-spin" /> : <CheckCircle2 className="h-4 w-4" />}
                  Check file
                </Button>
                <Button onClick={() => void run(false)} disabled={isRunning}>
                  {progress && !progress.validateOnly ? <Loader2 className="h-4 w-4 animate-spin" /> : <Upload className="h-4 w-4" />}
                  Import {sheet.rows.length} rows
                </Button>
              </div>
            </div>

            {progress && (
              <div className="mt-4" role="status">
                <div className="h-2 overflow-hidden rounded-full bg-muted">
                  <div className="h-full bg-primary transition-all" style={{ width: `${(progress.done / progress.total) * 100}%` }} />
                </div>
                <p className="mt-1 text-xs text-muted-foreground">
                  {progress.validateOnly ? 'Checking' : 'Importing'} {progress.done} of {progress.total} rows…
                </p>
              </div>
            )}

            {runError && (
              <p role="alert" className="mt-3 rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
                {runError}
              </p>
            )}

            {summary && (
              <div className="mt-4 rounded-md border border-border bg-muted/30 p-3 text-sm" role="status">
                <p className="font-medium text-foreground">
                  {summary.validateOnly ? 'Check finished — nothing has been saved yet.' : 'Import finished.'}
                </p>
                <div className="mt-2 flex flex-wrap gap-2">
                  <Badge variant="success">
                    {summary.created} new {summary.validateOnly ? 'products to create' : 'products created'}
                  </Badge>
                  <Badge variant="secondary">{summary.matched} already in the catalog</Badge>
                  <Badge variant="secondary">
                    {summary.stocked} {summary.validateOnly ? 'batches to stock' : 'batches stocked'}
                  </Badge>
                  {errors.length > 0 && <Badge variant="destructive">{errors.length} rows with problems</Badge>}
                </div>

                {errors.length > 0 && (
                  <div className="mt-3">
                    <div className="flex items-center justify-between gap-3">
                      <p className="text-foreground">
                        {summary.validateOnly ? 'These rows will be skipped unless you fix them in the file:' : 'These rows were skipped. Fix them in the file and import again:'}
                      </p>
                      <Button size="sm" variant="outline" onClick={() => downloadFailedRows(errors.map((error) => ({ rowNumber: error.rowNumber, productName: error.productName, message: error.message ?? '' })))}>
                        <Download className="h-3.5 w-3.5" />
                        Download list
                      </Button>
                    </div>
                    <ul className="mt-2 max-h-56 overflow-y-auto rounded-md border border-border bg-background text-xs">
                      {errors.map((error) => (
                        <li key={error.rowNumber} className="flex gap-3 border-b border-border px-3 py-1.5 last:border-b-0">
                          <span className="w-16 shrink-0 text-muted-foreground">Row {error.rowNumber}</span>
                          <span className="w-56 shrink-0 truncate font-medium text-foreground">{error.productName || '(no name)'}</span>
                          <span className="text-destructive">{error.message}</span>
                        </li>
                      ))}
                    </ul>
                  </div>
                )}

                {notes.length > 0 && (
                  <details className="mt-3">
                    <summary className="cursor-pointer text-foreground">{notes.length} rows went in with a note</summary>
                    <ul className="mt-2 max-h-40 overflow-y-auto rounded-md border border-border bg-background text-xs">
                      {notes.map((note) => (
                        <li key={note.rowNumber} className="flex gap-3 border-b border-border px-3 py-1.5 last:border-b-0">
                          <span className="w-16 shrink-0 text-muted-foreground">Row {note.rowNumber}</span>
                          <span className="w-56 shrink-0 truncate font-medium text-foreground">{note.productName}</span>
                          <span className="text-muted-foreground">{note.message}</span>
                        </li>
                      ))}
                    </ul>
                  </details>
                )}
              </div>
            )}
          </section>

          <section className="overflow-hidden rounded-lg border border-border bg-background">
            <p className="border-b border-border px-4 py-2 text-xs text-muted-foreground">
              {sheet.rows.length > PREVIEW_ROWS ? `First ${PREVIEW_ROWS} of ${sheet.rows.length} rows, as read from the file` : `${sheet.rows.length} rows, as read from the file`}
            </p>
            <div className="max-h-[420px] overflow-auto">
              <table className="w-full text-sm">
                <thead className="sticky top-0 bg-sidebar-active text-left text-[11px] font-medium uppercase tracking-wide text-sidebar-active-foreground">
                  <tr>
                    <th className="px-3 py-2">Row</th>
                    <th className="px-3 py-2">Product</th>
                    <th className="px-3 py-2">Manufacturer</th>
                    <th className="px-3 py-2 text-right">Pack</th>
                    <th className="px-3 py-2 text-right">GST %</th>
                    <th className="px-3 py-2 text-right">MRP</th>
                    <th className="px-3 py-2 text-right">Rate</th>
                    <th className="px-3 py-2">Batch</th>
                    <th className="px-3 py-2">Expiry</th>
                    <th className="px-3 py-2 text-right">Stock</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-border">
                  {sheet.rows.slice(0, PREVIEW_ROWS).map((row, index) => (
                    <tr key={index}>
                      <td className="px-3 py-1.5 text-muted-foreground">{sheet.firstRowNumber + index}</td>
                      <td className="px-3 py-1.5">
                        <div className="font-medium text-foreground">{row.productName}</div>
                        {row.genericName && <div className="text-xs text-muted-foreground">{row.genericName}</div>}
                      </td>
                      <td className="px-3 py-1.5 text-muted-foreground">{row.manufacturer ?? '—'}</td>
                      <td className="px-3 py-1.5 text-right tabular-nums">{row.unitsPerPack ?? 1}</td>
                      <td className="px-3 py-1.5 text-right tabular-nums">{row.gstRatePercent ?? '—'}</td>
                      <td className="px-3 py-1.5 text-right tabular-nums">{row.mrp != null ? money(row.mrp) : '—'}</td>
                      <td className="px-3 py-1.5 text-right tabular-nums">{row.purchaseRate != null ? money(row.purchaseRate) : '—'}</td>
                      <td className="px-3 py-1.5 font-mono text-xs">{row.batchNo ?? '—'}</td>
                      <td className="px-3 py-1.5">{expiryLabel(row.expiryDate)}</td>
                      <td className="px-3 py-1.5 text-right tabular-nums">{row.openingQuantity ? quantity(row.openingQuantity) : '—'}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </section>
        </>
      )}

      {!sheet && !readError && (
        <section className="rounded-lg border border-dashed border-border bg-background p-6 text-sm text-muted-foreground">
          <p className="font-medium text-foreground">Headings the import understands</p>
          <p className="mt-2 flex flex-wrap gap-1.5">
            {IMPORT_COLUMNS.map((column) => (
              <Badge key={column.field} variant="secondary">
                {column.heading}
              </Badge>
            ))}
          </p>
          <p className="mt-3">It also recognises the usual names other pharmacy software uses for these, such as “Item Name”, “Company”, “Packing”, “Batch” and “Exp”.</p>
        </section>
      )}
    </PharmacyPage>
  );
}
