import { ChevronDown, Download, FileSpreadsheet, FileText, Sheet } from 'lucide-react';
import { useState } from 'react';
import { Button, type ButtonProps } from '@/components/ui/button';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';

export type ExportFormatOption = 'pdf' | 'excel' | 'csv';

interface ExportMenuProps {
  /** May be async (e.g. OPD fetches every matching row first) — the button shows "Exporting…"
   * and is disabled until it settles, so a slow export can't be double-triggered. */
  onExport: (format: ExportFormatOption) => void | Promise<void>;
  variant?: ButtonProps['variant'];
  size?: ButtonProps['size'];
}

/** The Export ▾ dropdown (PDF / Excel / CSV) shared by the finance reports and the OPD page — one
 * menu instead of each screen rebuilding the same three items. Writing the file itself stays with
 * the caller (exportUtils' exportReportToPdf/Excel/Csv). */
export function ExportMenu({ onExport, variant = 'outline', size = 'sm' }: ExportMenuProps) {
  const [isExporting, setIsExporting] = useState(false);

  async function run(format: ExportFormatOption) {
    setIsExporting(true);
    try {
      await onExport(format);
    } finally {
      setIsExporting(false);
    }
  }

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button type="button" variant={variant} size={size} className="gap-1.5" disabled={isExporting}>
          {variant === 'default' ? <Download className="h-4 w-4" /> : <FileText className="h-4 w-4" />}
          {isExporting ? 'Exporting…' : 'Export'}
          <ChevronDown className="h-3.5 w-3.5" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        <DropdownMenuItem onClick={() => run('pdf')}>
          <Sheet className="h-4 w-4 text-destructive" />
          Export as PDF
        </DropdownMenuItem>
        <DropdownMenuItem onClick={() => run('excel')}>
          <FileSpreadsheet className="h-4 w-4 text-success" />
          Export as Excel
        </DropdownMenuItem>
        <DropdownMenuItem onClick={() => run('csv')}>
          <FileText className="h-4 w-4 text-muted-foreground" />
          Export as CSV
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
