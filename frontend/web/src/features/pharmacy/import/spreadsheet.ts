import type { ImportProductRow } from '@hms/shared';
import { parseExpiry } from '../common/format';

/** A column of the import sheet: the field it fills, the heading the template gives it, and
 * the other headings a pharmacy's own export is likely to use for the same thing. */
export interface SheetColumn<TField extends string = string> {
  field: TField;
  heading: string;
  aliases: string[];
  kind: 'text' | 'number' | 'expiry';
  sample: string | number;
}

type ImportColumn = SheetColumn<keyof ImportProductRow & string>;

export const IMPORT_COLUMNS: ImportColumn[] = [
  { field: 'productName', heading: 'Product Name', aliases: ['product', 'item', 'item name', 'name', 'medicine', 'medicine name', 'description'], kind: 'text', sample: 'Dolo 650 Tablet' },
  { field: 'genericName', heading: 'Generic Name', aliases: ['generic', 'salt', 'composition', 'molecule', 'content'], kind: 'text', sample: 'Paracetamol 650 mg' },
  { field: 'sku', heading: 'Code', aliases: ['sku', 'item code', 'product code'], kind: 'text', sample: '' },
  { field: 'category', heading: 'Category', aliases: ['group', 'type'], kind: 'text', sample: 'Tablets' },
  { field: 'manufacturer', heading: 'Manufacturer', aliases: ['company', 'mfr', 'mfg', 'make', 'manufacturer name'], kind: 'text', sample: 'Micro Labs' },
  { field: 'brand', heading: 'Brand', aliases: ['brand name'], kind: 'text', sample: '' },
  { field: 'unit', heading: 'Unit', aliases: ['uom', 'unit of measure'], kind: 'text', sample: 'Tablet' },
  { field: 'unitsPerPack', heading: 'Units Per Pack', aliases: ['pack', 'pack size', 'packing', 'units/pack', 'conversion'], kind: 'number', sample: 15 },
  { field: 'hsnCode', heading: 'HSN', aliases: ['hsn code', 'hsn/sac'], kind: 'text', sample: '3004' },
  { field: 'gstRatePercent', heading: 'GST %', aliases: ['gst', 'gst rate', 'tax', 'tax %', 'tax rate'], kind: 'number', sample: 12 },
  { field: 'schedule', heading: 'Schedule', aliases: ['drug schedule', 'sch'], kind: 'text', sample: 'H' },
  { field: 'dosageForm', heading: 'Dosage Form', aliases: ['form'], kind: 'text', sample: 'Tablet' },
  { field: 'strength', heading: 'Strength', aliases: [], kind: 'text', sample: '650 mg' },
  { field: 'rackLocation', heading: 'Rack', aliases: ['rack location', 'location', 'shelf', 'bin'], kind: 'text', sample: 'A-12' },
  { field: 'reorderLevel', heading: 'Reorder Level', aliases: ['reorder', 'min stock', 'minimum stock', 'rol'], kind: 'number', sample: 150 },
  { field: 'mrp', heading: 'MRP', aliases: ['mrp per pack', 'm.r.p', 'm.r.p.'], kind: 'number', sample: 33.6 },
  { field: 'purchaseRate', heading: 'Purchase Rate', aliases: ['rate', 'cost', 'cost price', 'purchase price', 'p.rate', 'ptr'], kind: 'number', sample: 24 },
  { field: 'sellingRate', heading: 'Selling Rate', aliases: ['selling price', 'sale rate', 'sale price', 's.rate'], kind: 'number', sample: '' },
  { field: 'batchNo', heading: 'Batch No', aliases: ['batch', 'batch number', 'lot', 'lot no'], kind: 'text', sample: 'DL2406' },
  { field: 'expiryDate', heading: 'Expiry', aliases: ['expiry date', 'exp', 'exp date', 'exp.', 'expiry (mm/yy)'], kind: 'expiry', sample: '03/27' },
  { field: 'openingQuantity', heading: 'Stock Qty (units)', aliases: ['stock', 'stock qty', 'qty', 'quantity', 'opening stock', 'opening qty', 'closing stock', 'balance'], kind: 'number', sample: 240 },
];

const normalize = (heading: string) => heading.toLowerCase().replace(/[^a-z0-9%/]+/g, ' ').trim();

export interface ParsedSheet {
  /** Sheet heading → the field it was matched to. */
  matched: { heading: string; field: keyof ImportProductRow }[];
  /** Headings in the file that matched nothing and were ignored. */
  ignored: string[];
  rows: ImportProductRow[];
  /** Spreadsheet row number of rows[0]. */
  firstRowNumber: number;
}

type Cell = string | number | Date | null;

function cellValue(raw: unknown): Cell {
  if (raw == null) return null;
  if (raw instanceof Date) return raw;
  if (typeof raw === 'number') return raw;
  if (typeof raw === 'string') return raw.trim() || null;
  if (typeof raw === 'boolean') return String(raw);
  if (typeof raw === 'object') {
    const value = raw as { text?: unknown; result?: unknown; richText?: { text: string }[] };
    if (value.richText) return value.richText.map((part) => part.text).join('').trim() || null;
    if (value.result !== undefined) return cellValue(value.result);
    if (value.text !== undefined) return cellValue(value.text);
  }
  return String(raw);
}

/** A minimal RFC-4180 reader: quoted fields, doubled quotes, commas and newlines inside quotes. */
function parseCsv(text: string): string[][] {
  const rows: string[][] = [];
  let row: string[] = [];
  let field = '';
  let quoted = false;

  for (let i = 0; i < text.length; i++) {
    const char = text[i];
    if (quoted) {
      if (char === '"' && text[i + 1] === '"') {
        field += '"';
        i++;
      } else if (char === '"') {
        quoted = false;
      } else {
        field += char;
      }
    } else if (char === '"') {
      quoted = true;
    } else if (char === ',') {
      row.push(field);
      field = '';
    } else if (char === '\n' || char === '\r') {
      if (char === '\r' && text[i + 1] === '\n') i++;
      row.push(field);
      rows.push(row);
      row = [];
      field = '';
    } else {
      field += char;
    }
  }
  if (field || row.length > 0) {
    row.push(field);
    rows.push(row);
  }
  return rows;
}

async function readGrid(file: File): Promise<Cell[][]> {
  if (file.name.toLowerCase().endsWith('.csv')) {
    return parseCsv(await file.text()).map((row) => row.map((cell) => cell.trim() || null));
  }

  const ExcelJS = (await import('exceljs')).default;
  const workbook = new ExcelJS.Workbook();
  await workbook.xlsx.load(await file.arrayBuffer());
  const sheet = workbook.worksheets[0];
  if (!sheet) return [];

  const grid: Cell[][] = [];
  sheet.eachRow({ includeEmpty: false }, (row) => {
    const cells: Cell[] = [];
    for (let column = 1; column <= sheet.columnCount; column++) {
      cells.push(cellValue(row.getCell(column).value));
    }
    grid.push(cells);
  });
  return grid;
}

function toNumber(cell: Cell): number | null {
  if (cell == null || cell instanceof Date) return null;
  if (typeof cell === 'number') return cell;
  const parsed = Number.parseFloat(cell.replace(/[,₹%\s]/g, ''));
  return Number.isFinite(parsed) ? parsed : null;
}

function toExpiry(cell: Cell): string | null {
  if (cell == null) return null;
  if (cell instanceof Date) {
    // A real date cell. Excel stores it as midnight UTC of the day shown.
    return cell.toISOString().slice(0, 10);
  }
  const text = String(cell).trim();
  // Day-first full dates (25-03-2027, 25/03/27), as Indian software exports them.
  const dayFirst = /^(\d{1,2})[-/.](\d{1,2})[-/.](\d{2}|\d{4})$/.exec(text);
  if (dayFirst) {
    const year = dayFirst[3].length === 2 ? 2000 + Number(dayFirst[3]) : Number(dayFirst[3]);
    const month = Number(dayFirst[2]);
    const day = Number(dayFirst[1]);
    if (month >= 1 && month <= 12 && day >= 1 && day <= 31) {
      return `${year}-${String(month).padStart(2, '0')}-${String(day).padStart(2, '0')}`;
    }
    return null;
  }
  return parseExpiry(text);
}

export interface MappedSheet<TField extends string> {
  matched: { heading: string; field: TField }[];
  ignored: string[];
  rows: Partial<Record<TField, string | number | null>>[];
  /** Spreadsheet row number of rows[0]. */
  firstRowNumber: number;
}

/**
 * Reads the first sheet of an .xlsx (or a .csv), finds the heading row, and turns every row
 * under it into a record keyed by field. Headings are matched loosely — the template's own,
 * or the usual names other pharmacy software exports — so a pharmacy can often load the file
 * it already has without retyping the headings. `keyField` is the column a row must have to
 * count (the product name): it is how the heading row is found and how blank and total rows
 * are skipped.
 */
export async function readMappedFile<TField extends string>(file: File, columnSet: SheetColumn<TField>[], keyField: TField): Promise<MappedSheet<TField>> {
  const grid = await readGrid(file);

  const lookup = new Map<string, SheetColumn<TField>>();
  for (const column of columnSet) {
    lookup.set(normalize(column.heading), column);
    column.aliases.forEach((alias) => lookup.set(normalize(alias), column));
  }

  // The heading row is the first of the top ten rows that names the key column — exports
  // often carry a title and a date above the real headings.
  const headerIndex = grid.slice(0, 10).findIndex((row) => row.some((cell) => typeof cell === 'string' && lookup.get(normalize(cell))?.field === keyField));
  if (headerIndex < 0) {
    const heading = columnSet.find((column) => column.field === keyField)?.heading ?? keyField;
    throw new Error(`Could not find a "${heading}" column. Download the template to see the headings this import reads.`);
  }

  const matched: MappedSheet<TField>['matched'] = [];
  const ignored: string[] = [];
  const columns: (SheetColumn<TField> | null)[] = grid[headerIndex].map((cell) => {
    if (cell == null) return null;
    const heading = String(cell);
    const column = lookup.get(normalize(heading)) ?? null;
    if (column && !matched.some((match) => match.field === column.field)) {
      matched.push({ heading, field: column.field });
      return column;
    }
    ignored.push(heading);
    return null;
  });

  const rows: MappedSheet<TField>['rows'] = [];
  let firstRowNumber = headerIndex + 2;
  let seenData = false;

  grid.slice(headerIndex + 1).forEach((cells, offset) => {
    const row: Partial<Record<TField, string | number | null>> = {};
    columns.forEach((column, index) => {
      if (!column) return;
      const cell = cells[index] ?? null;
      row[column.field] = column.kind === 'number' ? toNumber(cell) : column.kind === 'expiry' ? toExpiry(cell) : cell == null ? null : String(cell instanceof Date ? cell.toISOString().slice(0, 10) : cell);
    });

    // Blank rows, and the bare "Total" line many exports end with.
    const key = row[keyField];
    if (!key || /^(grand\s+|sub\s*)?total[:\s]*$/i.test(String(key).trim())) return;
    if (!seenData) {
      firstRowNumber = headerIndex + 2 + offset;
      seenData = true;
    }
    rows.push(row);
  });

  return { matched, ignored, rows, firstRowNumber };
}

/** Reads a products-and-opening-stock sheet into import rows. */
export async function readImportFile(file: File): Promise<ParsedSheet> {
  const sheet = await readMappedFile(file, IMPORT_COLUMNS, 'productName');
  return { ...sheet, rows: sheet.rows as unknown as ImportProductRow[] };
}

/** The columns of a supplier's invoice, as distributors' billing software exports it. */
export type PurchaseField = 'productName' | 'batchNo' | 'expiryDate' | 'quantity' | 'freeQuantity' | 'purchaseRate' | 'mrp' | 'discountPercent' | 'gstRatePercent';

export const PURCHASE_COLUMNS: SheetColumn<PurchaseField>[] = [
  { field: 'productName', heading: 'Product Name', aliases: ['product', 'item', 'item name', 'name', 'description', 'particulars', 'medicine'], kind: 'text', sample: 'Dolo 650 Tablet' },
  { field: 'batchNo', heading: 'Batch No', aliases: ['batch', 'batch number', 'lot', 'lot no'], kind: 'text', sample: 'DL2406' },
  { field: 'expiryDate', heading: 'Expiry', aliases: ['expiry date', 'exp', 'exp date', 'exp.'], kind: 'expiry', sample: '03/27' },
  { field: 'quantity', heading: 'Qty', aliases: ['quantity', 'billed qty', 'packs', 'sale qty'], kind: 'number', sample: 10 },
  { field: 'freeQuantity', heading: 'Free', aliases: ['free qty', 'free quantity', 'scheme', 'sch qty', 'bonus'], kind: 'number', sample: 1 },
  { field: 'purchaseRate', heading: 'Rate', aliases: ['purchase rate', 'ptr', 'p.rate', 'price', 'cost'], kind: 'number', sample: 14.5 },
  { field: 'mrp', heading: 'MRP', aliases: ['m.r.p', 'm.r.p.'], kind: 'number', sample: 21 },
  { field: 'discountPercent', heading: 'Disc %', aliases: ['disc', 'discount', 'discount %', 'cd %', 'td %'], kind: 'number', sample: 5 },
  { field: 'gstRatePercent', heading: 'GST %', aliases: ['gst', 'gst rate', 'tax %', 'tax'], kind: 'number', sample: 12 },
];

export type PurchaseSheetRow = Partial<Record<PurchaseField, string | number | null>>;

/** Reads a supplier's invoice exported to Excel or CSV into purchase lines. */
export async function readPurchaseFile(file: File): Promise<MappedSheet<PurchaseField>> {
  return readMappedFile(file, PURCHASE_COLUMNS, 'productName');
}

function saveBlob(blob: Blob, filename: string) {
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = filename;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  URL.revokeObjectURL(url);
}

/** The blank sheet to fill in: every heading the import reads, one worked example row, and a
 * second sheet explaining each column. */
export async function downloadImportTemplate(): Promise<void> {
  const ExcelJS = (await import('exceljs')).default;
  const workbook = new ExcelJS.Workbook();

  const sheet = workbook.addWorksheet('Products');
  sheet.addRow(IMPORT_COLUMNS.map((column) => column.heading)).font = { bold: true };
  sheet.addRow(IMPORT_COLUMNS.map((column) => column.sample));
  sheet.columns.forEach((column) => {
    column.width = 18;
  });
  sheet.views = [{ state: 'frozen', ySplit: 1 }];

  const notes = workbook.addWorksheet('How to fill');
  notes.addRow(['Column', 'What goes in it']).font = { bold: true };
  [
    ['Product Name', 'Required. The name as you want it on bills. A product already in the system with the same name is matched, not duplicated.'],
    ['Units Per Pack', 'How many tablets/capsules in one strip or box. 1 for a bottle or a tube. Prices are per pack.'],
    ['MRP / Purchase Rate / Selling Rate', 'Per pack. Selling Rate can be left blank to sell at MRP.'],
    ['Schedule', 'H, H1, X, G or Narcotic. Leave blank for over-the-counter items.'],
    ['Batch No, Expiry, Stock Qty', 'Fill all three to load the stock on your shelf. Stock Qty is in units (tablets, not strips). One row per batch — repeat the product on a new row for each batch.'],
    ['Expiry', 'MM/YY (03/27) or a date.'],
    ['Category, Manufacturer, Brand, Unit', 'Optional. New names are created; blank ones are filed under a default.'],
  ].forEach((row) => notes.addRow(row));
  notes.getColumn(1).width = 36;
  notes.getColumn(2).width = 120;

  const buffer = await workbook.xlsx.writeBuffer();
  saveBlob(new Blob([buffer], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' }), 'pharmacy-import-template.xlsx');
}

/** The rows that failed, with the reason, as a CSV to fix and load again. */
export function downloadFailedRows(failures: { rowNumber: number; productName: string; message: string }[]): void {
  const escape = (value: string | number) => {
    const text = String(value);
    return /[",\n]/.test(text) ? `"${text.replace(/"/g, '""')}"` : text;
  };
  const lines = [['Row', 'Product Name', 'Problem'], ...failures.map((failure) => [failure.rowNumber, failure.productName, failure.message])];
  saveBlob(new Blob([lines.map((line) => line.map(escape).join(',')).join('\n')], { type: 'text/csv;charset=utf-8;' }), 'pharmacy-import-problems.csv');
}
