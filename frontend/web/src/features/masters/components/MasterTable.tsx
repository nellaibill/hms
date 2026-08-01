import { ArrowDown, ArrowUp } from 'lucide-react';
import type { ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { getDisplayLabel, resolveRecordLabel } from '../engine/registry';
import type { MasterEntityConfig, MasterFieldDef, MasterRecord } from '../engine/types';
import { useMasterReferenceOptions } from '../hooks/useMasterQuery';
import { MasterStatusBadge } from './MasterStatusBadge';

interface MasterTableProps {
  config: MasterEntityConfig;
  records: MasterRecord[];
  sort: string;
  onSortChange: (sort: string) => void;
}

function renderFieldValue(field: MasterFieldDef, record: MasterRecord): ReactNode {
  const value = record[field.key];
  if (field.type === 'reference') {
    return resolveRecordLabel(field.referenceEntityKey as string, value as string | undefined);
  }
  if (field.type === 'select') {
    return field.options?.find((option) => option.value === value)?.label ?? (value ? String(value) : '—');
  }
  if (field.type === 'boolean') {
    return value ? 'Yes' : 'No';
  }
  if (value === undefined || value === null || value === '') {
    return '—';
  }
  return String(value);
}

export function MasterTable({ config, records, sort, onSortChange }: MasterTableProps) {
  // Primes the reference cache for any entity this config's reference-type fields point at,
  // so resolveRecordLabel (used for both the primary row label and reference columns below)
  // resolves to a friendly label instead of a raw id once these queries settle.
  useMasterReferenceOptions(config);

  const currentField = sort.startsWith('-') ? sort.slice(1) : sort;
  const isDescending = sort.startsWith('-');

  function toggleSort(field: string) {
    if (currentField !== field) {
      onSortChange(field);
      return;
    }
    onSortChange(isDescending ? field : `-${field}`);
  }

  function sortButton(field: string, label: string) {
    return (
      <button type="button" onClick={() => toggleSort(field)} className="inline-flex items-center gap-1 hover:text-foreground">
        {label}
        {currentField === field && (isDescending ? <ArrowDown className="h-3.5 w-3.5" /> : <ArrowUp className="h-3.5 w-3.5" />)}
      </button>
    );
  }

  const columnFields = config.fields
    .filter((field) => field.type !== 'textarea' && field.showInTable !== false && field.key !== config.nameField)
    .slice(0, 4);
  const primarySortField = config.nameField ?? config.codeField;

  return (
    <div className="overflow-x-auto rounded-lg border border-border">
      <table className="w-full text-sm">
        <thead className="bg-muted/50 text-left text-xs font-medium uppercase tracking-wide text-muted-foreground">
          <tr>
            <th className="whitespace-nowrap px-4 py-2.5">
              {primarySortField ? sortButton(primarySortField, config.label) : config.label}
            </th>
            {columnFields.map((field) => (
              <th key={field.key} className="whitespace-nowrap px-4 py-2.5">
                {field.label}
              </th>
            ))}
            <th className="whitespace-nowrap px-4 py-2.5">Status</th>
            <th className="whitespace-nowrap px-4 py-2.5">{sortButton('updatedAt', 'Last Updated')}</th>
            <th className="whitespace-nowrap px-4 py-2.5 text-right">Actions</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {records.map((record) => (
            <tr key={record.id} className="hover:bg-muted/30">
              <td className="whitespace-nowrap px-4 py-3">
                <Link
                  to={`/admin/masters/${config.key}/${record.id}`}
                  className="font-medium text-foreground hover:text-primary hover:underline"
                >
                  {getDisplayLabel(config, record)}
                </Link>
              </td>
              {columnFields.map((field) => (
                <td key={field.key} className="max-w-xs truncate px-4 py-3 text-muted-foreground">
                  {renderFieldValue(field, record)}
                </td>
              ))}
              <td className="px-4 py-3">
                <MasterStatusBadge isActive={record.isActive} />
              </td>
              <td className="whitespace-nowrap px-4 py-3 font-mono text-xs text-muted-foreground">
                {new Date(record.updatedAt).toLocaleDateString('en-IN')}
              </td>
              <td className="px-4 py-3">
                <div className="flex justify-end gap-1.5">
                  <Button asChild variant="ghost" size="sm">
                    <Link to={`/admin/masters/${config.key}/${record.id}`}>View</Link>
                  </Button>
                  <Button asChild variant="ghost" size="sm">
                    <Link to={`/admin/masters/${config.key}/${record.id}/edit`}>Edit</Link>
                  </Button>
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
