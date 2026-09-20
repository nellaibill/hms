import { ChevronDown, Download, RotateCcw, Search } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { ACTIVITY_ACTIONS, ACTIVITY_MODULES, type ActivityLogFilterValues, type ExportFormat } from '../activityLogUtils';

interface ActivityLogFiltersProps {
  values: ActivityLogFilterValues;
  onChange: (values: ActivityLogFilterValues) => void;
  onSearch: () => void;
  onReset: () => void;
  onExport: (format: ExportFormat) => void;
  isExporting: boolean;
  users: { id: string; name: string }[];
}

const ALL = 'all';

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className="flex min-w-0 flex-col gap-1">
      <Label className="text-xs font-medium text-muted-foreground">{label}</Label>
      {children}
    </div>
  );
}

export function ActivityLogFilters({ values, onChange, onSearch, onReset, onExport, isExporting, users }: ActivityLogFiltersProps) {
  const set = (patch: Partial<ActivityLogFilterValues>) => onChange({ ...values, ...patch });

  return (
    <form
      className="flex flex-col gap-3"
      onSubmit={(event) => {
        event.preventDefault();
        onSearch();
      }}
    >
      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-5">
        <Field label="Date From">
          <Input type="date" value={values.from} max={values.to || undefined} onChange={(e) => set({ from: e.target.value })} />
        </Field>
        <Field label="Date To">
          <Input type="date" value={values.to} min={values.from || undefined} onChange={(e) => set({ to: e.target.value })} />
        </Field>
        <Field label="User">
          <Select value={values.userId || ALL} onValueChange={(v) => set({ userId: v === ALL ? '' : v })}>
            <SelectTrigger aria-label="Filter by user">
              <SelectValue placeholder="All Users" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={ALL}>All Users</SelectItem>
              {users.map((user) => (
                <SelectItem key={user.id} value={user.id}>
                  {user.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </Field>
        <Field label="Module">
          <Select value={values.module || ALL} onValueChange={(v) => set({ module: v === ALL ? '' : v })}>
            <SelectTrigger aria-label="Filter by module">
              <SelectValue placeholder="All Modules" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={ALL}>All Modules</SelectItem>
              {ACTIVITY_MODULES.map((m) => (
                <SelectItem key={m} value={m}>
                  {m}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </Field>
        <Field label="Action">
          <Select value={values.action || ALL} onValueChange={(v) => set({ action: v === ALL ? '' : v })}>
            <SelectTrigger aria-label="Filter by action">
              <SelectValue placeholder="All Actions" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={ALL}>All Actions</SelectItem>
              {ACTIVITY_ACTIONS.map((a) => (
                <SelectItem key={a} value={a}>
                  {a}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </Field>
      </div>

      <div className="flex flex-wrap items-end gap-3">
        <div className="min-w-[240px] flex-1">
          <Field label="Search Entity / ID">
            <div className="relative">
              <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                type="search"
                placeholder="Search entity, entity ID or description…"
                value={values.entity}
                onChange={(e) => set({ entity: e.target.value })}
                aria-label="Search entity or ID"
                className="pl-9"
              />
            </div>
          </Field>
        </div>
        <Button type="submit" className="gap-1.5">
          <Search className="h-4 w-4" />
          Search
        </Button>
        <div className="ml-auto flex gap-2">
          <Button type="button" variant="outline" className="gap-1.5" onClick={onReset}>
            <RotateCcw className="h-4 w-4" />
            Reset
          </Button>
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button type="button" variant="outline" className="gap-1.5" disabled={isExporting}>
                <Download className="h-4 w-4" />
                {isExporting ? 'Exporting…' : 'Export'}
                <ChevronDown className="h-4 w-4" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem onSelect={() => onExport('pdf')}>PDF</DropdownMenuItem>
              <DropdownMenuItem onSelect={() => onExport('excel')}>Excel</DropdownMenuItem>
              <DropdownMenuItem onSelect={() => onExport('csv')}>CSV</DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      </div>
    </form>
  );
}
