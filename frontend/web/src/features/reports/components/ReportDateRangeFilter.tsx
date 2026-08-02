import { CalendarRange } from 'lucide-react';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import type { ReportDateRange } from '../types';

interface ReportDateRangeFilterProps {
  range: ReportDateRange;
  onChange: (range: ReportDateRange) => void;
}

export function ReportDateRangeFilter({ range, onChange }: ReportDateRangeFilterProps) {
  return (
    <div className="flex flex-wrap items-end gap-3 rounded-lg border border-border bg-card p-4 shadow-soft-md">
      <CalendarRange className="mb-2 h-4 w-4 text-muted-foreground" />
      <div className="flex flex-col gap-1">
        <Label htmlFor="report-from">From</Label>
        <Input
          id="report-from"
          type="date"
          value={range.from}
          max={range.to}
          onChange={(e) => onChange({ ...range, from: e.target.value })}
          className="w-44"
        />
      </div>
      <div className="flex flex-col gap-1">
        <Label htmlFor="report-to">To</Label>
        <Input
          id="report-to"
          type="date"
          value={range.to}
          min={range.from}
          onChange={(e) => onChange({ ...range, to: e.target.value })}
          className="w-44"
        />
      </div>
    </div>
  );
}
