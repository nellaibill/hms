import { Plus, X } from 'lucide-react';
import { useState } from 'react';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { generateClientId } from '@/lib/id';
import { LAB_RESULT_FLAGS, type LabOrderItem, type LabResultFlag, type ResultParameterRequest } from '../types';

const EDITABLE_STATUSES: LabOrderItem['status'][] = ['Processing', 'ResultEntryInProgress', 'CorrectionRequired'];

interface DraftRow extends ResultParameterRequest {
  key: string;
}

function toDraftRows(item: LabOrderItem): DraftRow[] {
  if (item.parameters.length === 0) {
    return [{ key: generateClientId(), parameterName: '', resultValue: '', unit: '', referenceRange: '', flag: null, remarks: '' }];
  }
  return item.parameters.map((parameter) => ({
    key: parameter.id,
    parameterName: parameter.parameterName,
    resultValue: parameter.resultValue,
    unit: parameter.unit ?? '',
    referenceRange: parameter.referenceRange ?? '',
    flag: parameter.flag ?? null,
    remarks: parameter.remarks ?? '',
  }));
}

interface ResultEntryFormProps {
  item: LabOrderItem;
  isSavingDraft: boolean;
  isSubmitting: boolean;
  onSaveDraft: (parameters: ResultParameterRequest[]) => void;
  onSubmitForVerification: (parameters: ResultParameterRequest[]) => void;
}

/**
 * A dynamic list of result-parameter rows for one LabOrderItem — no placeholder reference-range/
 * unit/parameter defaults anywhere; every row starts genuinely empty, a technician's own typed
 * values are the only source. Meant to be mounted keyed by item.id (one instance per item in
 * the Tests/Results tab's item list) so its local draft state naturally resets when the viewer
 * switches items, with no extra effect needed.
 */
export function ResultEntryForm({ item, isSavingDraft, isSubmitting, onSaveDraft, onSubmitForVerification }: ResultEntryFormProps) {
  const [rows, setRows] = useState<DraftRow[]>(() => toDraftRows(item));
  const isEditable = EDITABLE_STATUSES.includes(item.status);

  function updateRow(key: string, patch: Partial<DraftRow>) {
    setRows((prev) => prev.map((row) => (row.key === key ? { ...row, ...patch } : row)));
  }

  function addRow() {
    setRows((prev) => [...prev, { key: generateClientId(), parameterName: '', resultValue: '', unit: '', referenceRange: '', flag: null, remarks: '' }]);
  }

  function removeRow(key: string) {
    setRows((prev) => (prev.length > 1 ? prev.filter((row) => row.key !== key) : prev));
  }

  function toRequest(): ResultParameterRequest[] {
    return rows
      .filter((row) => row.parameterName.trim().length > 0 || row.resultValue.trim().length > 0)
      .map((row) => ({
        parameterName: row.parameterName.trim(),
        resultValue: row.resultValue.trim(),
        unit: row.unit?.trim() || null,
        referenceRange: row.referenceRange?.trim() || null,
        flag: row.flag ?? null,
        remarks: row.remarks?.trim() || null,
      }));
  }

  if (!isEditable) {
    return (
      <div className="flex flex-col gap-2">
        {item.parameters.length === 0 ? (
          <p className="text-sm text-muted-foreground">No result parameters recorded.</p>
        ) : (
          <div className="overflow-x-auto rounded-md border border-border">
            <table className="w-full text-sm">
              <thead className="bg-muted/50 text-left text-xs font-medium uppercase tracking-wide text-muted-foreground">
                <tr>
                  <th className="px-3 py-2">Parameter</th>
                  <th className="px-3 py-2">Result</th>
                  <th className="px-3 py-2">Unit</th>
                  <th className="px-3 py-2">Reference Range</th>
                  <th className="px-3 py-2">Flag</th>
                  <th className="px-3 py-2">Remarks</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border">
                {item.parameters.map((parameter) => (
                  <tr key={parameter.id}>
                    <td className="px-3 py-2 font-medium text-foreground">{parameter.parameterName}</td>
                    <td className="px-3 py-2 text-foreground">{parameter.resultValue}</td>
                    <td className="px-3 py-2 text-muted-foreground">{parameter.unit ?? '—'}</td>
                    <td className="px-3 py-2 text-muted-foreground">{parameter.referenceRange ?? '—'}</td>
                    <td className="px-3 py-2">
                      {parameter.flag ? (
                        <Badge variant={parameter.flag === 'Normal' ? 'secondary' : parameter.flag === 'Critical' ? 'destructive' : 'warning'}>
                          {parameter.flag}
                        </Badge>
                      ) : (
                        <span className="text-muted-foreground">—</span>
                      )}
                    </td>
                    <td className="px-3 py-2 text-muted-foreground">{parameter.remarks ?? '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-3">
      <div className="flex flex-col gap-2">
        {rows.map((row) => (
          <div key={row.key} className="grid grid-cols-1 gap-2 rounded-md border border-dashed border-border p-2.5 sm:grid-cols-12 sm:items-end">
            <div className="flex flex-col gap-1 sm:col-span-3">
              <Label htmlFor={`param-name-${row.key}`} className="text-[11px]">
                Parameter
              </Label>
              <Input
                id={`param-name-${row.key}`}
                value={row.parameterName}
                onChange={(event) => updateRow(row.key, { parameterName: event.target.value })}
                className="h-8 text-xs"
              />
            </div>
            <div className="flex flex-col gap-1 sm:col-span-2">
              <Label htmlFor={`param-value-${row.key}`} className="text-[11px]">
                Result
              </Label>
              <Input
                id={`param-value-${row.key}`}
                value={row.resultValue}
                onChange={(event) => updateRow(row.key, { resultValue: event.target.value })}
                className="h-8 text-xs"
              />
            </div>
            <div className="flex flex-col gap-1 sm:col-span-1">
              <Label htmlFor={`param-unit-${row.key}`} className="text-[11px]">
                Unit
              </Label>
              <Input
                id={`param-unit-${row.key}`}
                value={row.unit ?? ''}
                onChange={(event) => updateRow(row.key, { unit: event.target.value })}
                className="h-8 text-xs"
              />
            </div>
            <div className="flex flex-col gap-1 sm:col-span-2">
              <Label htmlFor={`param-range-${row.key}`} className="text-[11px]">
                Reference Range
              </Label>
              <Input
                id={`param-range-${row.key}`}
                value={row.referenceRange ?? ''}
                onChange={(event) => updateRow(row.key, { referenceRange: event.target.value })}
                className="h-8 text-xs"
              />
            </div>
            <div className="flex flex-col gap-1 sm:col-span-2">
              <Label htmlFor={`param-flag-${row.key}`} className="text-[11px]">
                Flag
              </Label>
              <Select
                value={row.flag ?? 'none'}
                onValueChange={(value) => updateRow(row.key, { flag: value === 'none' ? null : (value as LabResultFlag) })}
              >
                <SelectTrigger id={`param-flag-${row.key}`} className="h-8 text-xs">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="none">—</SelectItem>
                  {LAB_RESULT_FLAGS.map((flag) => (
                    <SelectItem key={flag} value={flag}>
                      {flag}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="flex flex-col gap-1 sm:col-span-1">
              <Label htmlFor={`param-remarks-${row.key}`} className="text-[11px]">
                Remarks
              </Label>
              <Input
                id={`param-remarks-${row.key}`}
                value={row.remarks ?? ''}
                onChange={(event) => updateRow(row.key, { remarks: event.target.value })}
                className="h-8 text-xs"
              />
            </div>
            <div className="flex sm:col-span-1 sm:justify-end">
              <Button
                type="button"
                variant="ghost"
                size="icon"
                className="h-8 w-8"
                aria-label="Remove parameter"
                disabled={rows.length <= 1}
                onClick={() => removeRow(row.key)}
              >
                <X className="h-3.5 w-3.5" />
              </Button>
            </div>
          </div>
        ))}
      </div>

      <div className="flex items-center justify-between">
        <Button type="button" variant="outline" size="sm" className="gap-1.5" onClick={addRow}>
          <Plus className="h-3.5 w-3.5" />
          Add Parameter
        </Button>
        <div className="flex gap-2">
          <Button type="button" variant="outline" size="sm" disabled={isSavingDraft || isSubmitting} onClick={() => onSaveDraft(toRequest())}>
            {isSavingDraft ? 'Saving…' : 'Save Draft'}
          </Button>
          <Button
            type="button"
            size="sm"
            disabled={isSavingDraft || isSubmitting || toRequest().length === 0}
            onClick={() => onSubmitForVerification(toRequest())}
          >
            {isSubmitting ? 'Submitting…' : 'Submit for Verification'}
          </Button>
        </div>
      </div>
    </div>
  );
}
