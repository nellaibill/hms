import { Banknote, Loader2, Printer } from 'lucide-react';
import { useMemo } from 'react';
import { PageBanner } from '@/components/PageBanner';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { branding } from '@/config/branding';
import { formatCurrency, PAYMENT_METHOD_LABELS, useCollectionsQuery, type PaymentMethod } from '@/features/billing';
import { PrintPageStyle } from '@/features/branding/components/PrintPageStyle';
import { useBrandingQuery } from '@/features/branding/hooks/useBrandingQuery';
import { useDiagnosticServices, usePrimeDiagnosticPackageCache } from '@/features/diagnostics';
import { resolveRecordLabel, useMasterOptionsQuery } from '@/features/masters';
import {
  AccountsNavTabs,
  CollectionSummarySections,
  COLLECTION_HEADS,
  exportReportToCsv,
  exportReportToExcel,
  exportReportToPdf,
  ExportMenu,
  filterProfitRows,
  getCollectionRows,
  getCollectionsByMethod,
  getConsultantCollections,
  getDailyCollections,
  getHeadCollections,
  paymentsForEncounterTypes,
  ReportFilterBar,
  sumCollections,
  sumDailyCollections,
  useReportFilters,
  type CollectionSummaryData,
  type ExportFormatOption,
  type ReportSection,
  useAccountsCategory,
} from '@/features/reports';
import { formatDateRangeLabel, formatReportDate } from '@/lib/reportDateRange';

function methodLabel(method: string): string {
  return PAYMENT_METHOD_LABELS[method as PaymentMethod] ?? method;
}

function consultantName(consultantId: string | undefined): string {
  return consultantId ? resolveRecordLabel('consultant', consultantId) : 'No consultant';
}

/** Every section as plain rows, for CSV / Excel / PDF — the same figures the page shows. */
function buildExportSections(data: CollectionSummaryData): ReportSection[] {
  const heads = [...COLLECTION_HEADS, 'Other'] as const;
  const amounts = (t: { consultantAmount: number; hospitalAmount: number; collected: number }) => [t.consultantAmount, t.hospitalAmount, t.collected];
  return [
    {
      heading: 'Day-wise collection',
      headers: ['Date', ...heads, 'Consultant Amount', 'Hospital Amount', 'Total'],
      rows: [
        ...data.days.map((day) => [
          formatReportDate(day.date),
          ...heads.map((head) => day.byHead[head]),
          ...amounts({ consultantAmount: day.consultantAmount, hospitalAmount: day.hospitalAmount, collected: day.total }),
        ]),
        [
          'Total',
          ...heads.map((head) => data.dayTotals.byHead[head]),
          ...amounts({ consultantAmount: data.dayTotals.consultantAmount, hospitalAmount: data.dayTotals.hospitalAmount, collected: data.dayTotals.total }),
        ],
      ],
    },
    {
      heading: 'Consultant summary (all heads)',
      headers: ['Consultant', 'Consultant Amount', 'Hospital Amount', 'Total'],
      rows: [...data.consultants.map((row) => [consultantName(row.consultantId), ...amounts(row)]), ['All consultants', ...amounts(data.grandTotal)]],
    },
    {
      heading: 'Head-wise summary',
      headers: ['Head', 'Consultant', 'Consultant Amount', 'Hospital Amount', 'Total'],
      rows: [
        ...data.heads.flatMap((head) => [
          ...head.consultants.map((row) => [head.billingType, consultantName(row.consultantId), ...amounts(row)]),
          [head.billingType, 'Subtotal', ...amounts(head.subtotal)],
        ]),
        ['Grand total (all heads)', '', ...amounts(data.grandTotal)],
      ],
    },
  ];
}

/**
 * Accounts & Finance → Collection Summary (ADR-125): money actually received in the chosen
 * period on the current Accounts category's visits (ADR-134) — OP visits under OP Accounts, IP,
 * Day Care and Observation visits (discharge final bills included) under IP Accounts, by the day it was taken. Each payment is split into the consultant's and the
 * hospital's amount by the same rule OP Report uses (resolveLineCost). Three sections: day-wise
 * by head, per consultant, and per head with its consultants, ending in a grand total. The
 * filter bar (consultant, department, billing type, search) narrows all three, and a column or
 * section the filters make repetitive is left out (ADR-126).
 */
export default function CollectionSummaryReportPage() {
  const category = useAccountsCategory();
  const isOp = category.key === 'op';
  // OP keeps the report's original title and remembered filters, so its printouts don't change.
  const reportTitle = isOp ? 'Collection Summary' : `${category.shortLabel} Collection Summary`;
  const visitsLabel = isOp ? 'OP visits' : 'IP, day care and observation visits';
  const encounterTypes = category.collectionEncounterTypes ?? ['OP'];
  const { range, filters, appliedRange, appliedFilters, setRange, setFilters, reset } = useReportFilters(isOp ? 'collection' : `collection-${category.key}`);
  const { data: payments, isPending, isError } = useCollectionsQuery(appliedRange);
  const { data: brandingConfig } = useBrandingQuery();
  const hospitalName = brandingConfig?.hospitalName ?? branding.hospitalName;

  // Same reference caches OP Report primes — the cost split and service labels read them
  // synchronously, so their data is listed below to recompute once they load.
  const { data: diagnosticTestOptions } = useMasterOptionsQuery('diagnosticTest');
  const { data: departmentOptions } = useMasterOptionsQuery('department');
  const { data: consultantOptions } = useMasterOptionsQuery('consultant');
  const { data: consultationTypeOptions } = useMasterOptionsQuery('consultationType');
  const { services: radiologyServices } = useDiagnosticServices('Radiology');
  const { services: laboratoryServices } = useDiagnosticServices('Laboratory');
  usePrimeDiagnosticPackageCache();

  const rows = useMemo(
    () => getCollectionRows(paymentsForEncounterTypes(payments ?? [], encounterTypes)),
    // eslint-disable-next-line react-hooks/exhaustive-deps -- the cache data is a recompute trigger, see above
    [payments, encounterTypes, diagnosticTestOptions, departmentOptions, consultantOptions, consultationTypeOptions, radiologyServices, laboratoryServices],
  );
  const filteredRows = useMemo(() => filterProfitRows(rows, appliedFilters), [rows, appliedFilters]);

  const data = useMemo<CollectionSummaryData>(() => {
    const days = getDailyCollections(filteredRows);
    return {
      days,
      dayTotals: sumDailyCollections(days),
      consultants: getConsultantCollections(filteredRows),
      heads: getHeadCollections(filteredRows),
      grandTotal: sumCollections(filteredRows),
    };
  }, [filteredRows]);
  const byMethod = useMemo(() => getCollectionsByMethod(filteredRows), [filteredRows]);

  const singleDay = appliedRange.from === appliedRange.to;
  const singleConsultant = Boolean(appliedFilters.consultantId);
  const filterSummary = [
    appliedFilters.billingType && `Type: ${appliedFilters.billingType}`,
    appliedFilters.departmentId && `Department: ${resolveRecordLabel('department', appliedFilters.departmentId)}`,
    appliedFilters.consultantId && `Consultant: ${resolveRecordLabel('consultant', appliedFilters.consultantId)}`,
    appliedFilters.search?.trim() && `Search: "${appliedFilters.search.trim()}"`,
  ].filter((part): part is string => Boolean(part));
  const periodLine = [formatDateRangeLabel(appliedRange.from, appliedRange.to), ...filterSummary].join(' · ');

  async function handleExport(format: ExportFormatOption) {
    const filenameBase = `${isOp ? '' : `${category.key}-`}collection-summary_${appliedRange.from}_to_${appliedRange.to}`;
    const sections = buildExportSections(data);
    if (format === 'csv') exportReportToCsv(`${filenameBase}.csv`, sections);
    else if (format === 'excel') await exportReportToExcel(`${filenameBase}.xlsx`, sections);
    else exportReportToPdf(`${filenameBase}.pdf`, `${reportTitle} (${periodLine})`, sections);
  }

  const summaryCards: [string, string, string?][] = [
    ['Total Collected', formatCurrency(data.grandTotal.collected), `${data.grandTotal.payments} payment${data.grandTotal.payments === 1 ? '' : 's'}`],
    ['Consultant Amount', formatCurrency(data.grandTotal.consultantAmount)],
    ['Hospital Amount', formatCurrency(data.grandTotal.hospitalAmount)],
    ['By payment method', byMethod.length === 0 ? '—' : byMethod.map((m) => `${methodLabel(m.method)} ${formatCurrency(m.amount)}`).join(' · ')],
  ];

  return (
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={Banknote}
        title={reportTitle}
        subtitle={`Money received on ${visitsLabel} — by day, consultant and head, with the consultant's and the hospital's amount.`}
        backTo="/finance/accounts"
        backLabel="Back to Accounts and Finance"
      />

      <div className="flex flex-1 flex-col gap-4 p-6 lg:p-8">
        <AccountsNavTabs />

        <div className="flex w-full flex-col gap-4">
          <ReportFilterBar
            range={range}
            filters={filters}
            onRangeChange={setRange}
            onFiltersChange={setFilters}
            onReset={reset}
            exportSlot={
              <>
                <Button type="button" variant="outline" size="sm" className="gap-1.5" onClick={() => window.print()} disabled={filteredRows.length === 0}>
                  <Printer className="h-4 w-4" />
                  Print
                </Button>
                <ExportMenu onExport={handleExport} />
              </>
            }
          />

          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
            {summaryCards.map(([label, value, note]) => (
              <Card key={label}>
                <CardContent className="flex flex-col gap-0.5 py-4">
                  <span className="text-xs text-muted-foreground">{label}</span>
                  <span className={label === 'By payment method' ? 'text-sm font-semibold text-foreground' : 'text-lg font-semibold text-foreground'}>{value}</span>
                  {note && <span className="text-xs text-muted-foreground">{note}</span>}
                </CardContent>
              </Card>
            ))}
          </div>

          <p className="text-xs text-muted-foreground">{periodLine}</p>

          {isPending ? (
            <div className="flex items-center gap-2 py-6 text-sm text-muted-foreground">
              <Loader2 className="h-4 w-4 animate-spin" />
              Loading payments…
            </div>
          ) : isError ? (
            <p className="text-sm text-destructive">Couldn't load payments for this period. Try again, or pick a shorter period (up to a year).</p>
          ) : filteredRows.length === 0 ? (
            <p className="text-sm text-muted-foreground">No money was received on {visitsLabel} for the current filters.</p>
          ) : (
            <CollectionSummarySections data={data} variant="screen" singleDay={singleDay} singleConsultant={singleConsultant} />
          )}
        </div>
      </div>

      {/* Printed view — hidden on screen, shown alone once printing starts (index.css's
       * `.print-target` rule), the same convention as OP Report's ProfitReportPrint. */}
      <div className="print-target hidden bg-white p-6 text-black print:block" style={{ fontFamily: 'Georgia, "Times New Roman", serif' }}>
        <PrintPageStyle document="report" />
        <div className="flex flex-col items-center gap-1 border-b-2 border-black pb-3 text-center">
          <span className="text-2xl font-bold tracking-tight">{hospitalName}</span>
          <span className="text-sm font-semibold">{reportTitle}</span>
        </div>
        <div className="mt-3 flex items-start justify-between gap-6 text-sm">
          <span>{periodLine}</span>
          <span className="whitespace-nowrap text-gray-600">Printed {new Date().toLocaleString('en-IN')}</span>
        </div>
        <div className="mt-3 grid grid-cols-4 border border-black text-sm">
          {summaryCards.map(([label, value]) => (
            <div key={label} className="flex flex-col gap-0.5 border-r border-black px-2 py-1.5 last:border-r-0">
              <span className="text-xs text-gray-600">{label}</span>
              <span className="font-semibold">{value}</span>
            </div>
          ))}
        </div>
        <CollectionSummarySections data={data} variant="print" singleDay={singleDay} singleConsultant={singleConsultant} />
      </div>
    </div>
  );
}
