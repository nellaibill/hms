import { Fragment } from 'react';
import { Link } from 'react-router-dom';
import { formatRupees } from '@/features/diagnostics/tariff';
import type { ConsultantRow, ConsultationTypeColumn } from '../priceListRows';

interface ConsultationRatesGridProps {
  columns: ConsultationTypeColumn[];
  rows: ConsultantRow[];
  canEdit: boolean;
}

/**
 * The Price List's Consultations tab (ADR-140): every consultant against every consultation
 * type — the doctor's charge, then what the hospital keeps (hospital charge − doctor's charge)
 * and its share of the hospital charge. Read-only; charges are edited on the consultant's page.
 */
export function ConsultationRatesGrid({ columns, rows, canEdit }: ConsultationRatesGridProps) {
  if (rows.length === 0) {
    return <p className="px-4 py-10 text-center text-sm text-muted-foreground">No active consultants.</p>;
  }

  const groups = (['In-house', 'Visiting'] as const).map((group) => ({ group, rows: rows.filter((row) => row.group === group) })).filter((group) => group.rows.length > 0);

  return (
    <>
      <div className="overflow-x-auto">
        <table className="w-full min-w-[960px] border-collapse text-sm">
          <thead>
            <tr className="border-b border-border bg-muted/50 text-left align-bottom text-xs font-semibold text-foreground">
              <th scope="col" className="min-w-[200px] px-4 py-2.5">
                Consultant
              </th>
              {columns.map((column) => (
                <th key={column.id} scope="col" className="px-3 py-2.5">
                  {column.name}
                  <span className="mt-0.5 block font-normal text-muted-foreground">{column.amount !== null ? formatRupees(column.amount) : 'No fixed rate'}</span>
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {groups.map(({ group, rows: groupRows }) => (
              <Fragment key={group}>
                <tr className="bg-amber-50 dark:bg-amber-950/30">
                  <th scope="colgroup" colSpan={columns.length + 1} className="px-4 py-2 text-left text-xs font-bold uppercase tracking-wide text-amber-900 dark:text-amber-200">
                    {group} · {groupRows.length}
                  </th>
                </tr>
                {groupRows.map((row) => (
                  <tr key={row.id} className="border-b border-border align-top last:border-0">
                    <th scope="row" className="px-4 py-2.5 text-left font-medium">
                      {canEdit ? (
                        <Link to={`/admin/masters/consultant/${row.id}/edit`} className="text-primary hover:underline">
                          {row.name}
                        </Link>
                      ) : (
                        row.name
                      )}
                    </th>
                    {row.cells.map((cell) => (
                      <td key={cell.typeId} className="px-3 py-2.5">
                        {!cell.offered ? (
                          <>
                            <span aria-hidden="true" className="text-muted-foreground">
                              —
                            </span>
                            <span className="sr-only">Not offered</span>
                          </>
                        ) : cell.doctorCharge === null ? (
                          <span className="rounded-md border border-dashed border-amber-500 px-2 py-0.5 text-xs font-semibold text-amber-900 dark:text-amber-200">
                            Charge not set
                          </span>
                        ) : (
                          <>
                            <span className="block font-semibold tabular-nums">{formatRupees(cell.doctorCharge)}</span>
                            {cell.hospitalKeeps !== null && (
                              <span className="block text-xs tabular-nums text-blue-900 dark:text-blue-200">
                                Hospital {formatRupees(cell.hospitalKeeps)}
                                {cell.percent !== null ? ` · ${cell.percent}%` : ''}
                              </span>
                            )}
                          </>
                        )}
                      </td>
                    ))}
                  </tr>
                ))}
              </Fragment>
            ))}
          </tbody>
        </table>
      </div>
      <p className="flex flex-wrap gap-x-5 gap-y-1 border-t border-border px-4 py-3 text-xs text-muted-foreground">
        <span>Each cell: the doctor's charge, then what the hospital keeps.</span>
        <span>— not offered</span>
        <span>Charge not set = offered, no charge yet</span>
      </p>
    </>
  );
}
