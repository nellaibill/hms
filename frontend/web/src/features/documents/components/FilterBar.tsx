import { useEffect, useState } from 'react';
import { RotateCcw, Search } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { SearchableSelect } from '@/components/ui/searchable-select';
import { Card } from '@/components/ui/card';
import { cn } from '@/lib/utils';
import { DOCUMENT_TYPE_LABELS, ENTITY_TYPE_META } from '../constants';
import { ENTITY_OPTIONS } from '../mockEntities';
import { createEmptyFilters, DOCUMENT_TYPES, ENTITY_TYPES } from '../types';
import type { DocumentFilters, DocumentStatusFilter, EntityType } from '../types';

const STATUS_OPTIONS: DocumentStatusFilter[] = ['All', 'Active', 'Archived'];

interface FilterBarProps {
  filters: DocumentFilters;
  onApply: (filters: DocumentFilters) => void;
  onReset: () => void;
  uploaders: string[];
}

export function FilterBar({ filters, onApply, onReset, uploaders }: FilterBarProps) {
  const [draft, setDraft] = useState<DocumentFilters>(filters);

  // Keep the draft in sync when filters are reset/changed from outside (e.g. Reset button).
  useEffect(() => {
    setDraft(filters);
  }, [filters]);

  const entityOptions = draft.entityType ? ENTITY_OPTIONS[draft.entityType].map((e) => ({ value: e.id, label: e.label })) : [];

  function handleEntityTypeChange(value: string) {
    const entityType = value === 'all' ? undefined : (value as EntityType);
    setDraft((prev) => ({ ...prev, entityType, entityId: undefined }));
  }

  function handleSearch() {
    onApply(draft);
  }

  function handleReset() {
    setDraft(createEmptyFilters());
    onReset();
  }

  return (
    <Card className="flex flex-col gap-4 p-5">
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="filter-entity-type">Entity Type</Label>
          <Select value={draft.entityType ?? 'all'} onValueChange={handleEntityTypeChange}>
            <SelectTrigger id="filter-entity-type">
              <SelectValue placeholder="All entity types" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All entity types</SelectItem>
              {ENTITY_TYPES.map((type) => (
                <SelectItem key={type} value={type}>
                  {ENTITY_TYPE_META[type].label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="filter-entity">Entity</Label>
          <SearchableSelect
            id="filter-entity"
            value={draft.entityId ?? ''}
            onValueChange={(value) => setDraft((prev) => ({ ...prev, entityId: value || undefined }))}
            options={entityOptions}
            placeholder={draft.entityType ? 'All entities' : 'Select entity type first'}
            searchPlaceholder="Search entity…"
            ariaLabel="Entity"
            disabled={!draft.entityType}
          />
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="filter-document-type">Document Type</Label>
          <Select
            value={draft.documentType ?? 'all'}
            onValueChange={(value) => setDraft((prev) => ({ ...prev, documentType: value === 'all' ? undefined : (value as DocumentFilters['documentType']) }))}
          >
            <SelectTrigger id="filter-document-type">
              <SelectValue placeholder="All document types" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All document types</SelectItem>
              {DOCUMENT_TYPES.map((type) => (
                <SelectItem key={type} value={type}>
                  {DOCUMENT_TYPE_LABELS[type]}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="filter-uploaded-by">Uploaded By</Label>
          <Select
            value={draft.uploadedBy ?? 'all'}
            onValueChange={(value) => setDraft((prev) => ({ ...prev, uploadedBy: value === 'all' ? undefined : value }))}
          >
            <SelectTrigger id="filter-uploaded-by">
              <SelectValue placeholder="Anyone" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">Anyone</SelectItem>
              {uploaders.map((name) => (
                <SelectItem key={name} value={name}>
                  {name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="filter-date-from">Date Range</Label>
          <div className="flex items-center gap-2">
            <Input
              id="filter-date-from"
              type="date"
              aria-label="From date"
              value={draft.dateFrom ?? ''}
              onChange={(e) => setDraft((prev) => ({ ...prev, dateFrom: e.target.value || undefined }))}
            />
            <span className="text-sm text-muted-foreground">to</span>
            <Input
              type="date"
              aria-label="To date"
              value={draft.dateTo ?? ''}
              onChange={(e) => setDraft((prev) => ({ ...prev, dateTo: e.target.value || undefined }))}
            />
          </div>
        </div>

        <div className="flex flex-col gap-1.5">
          <Label id="filter-status-label">Status</Label>
          <div role="radiogroup" aria-labelledby="filter-status-label" className="flex items-center gap-0.5 rounded-md border border-border bg-muted/40 p-0.5">
            {STATUS_OPTIONS.map((status) => (
              <button
                key={status}
                type="button"
                role="radio"
                aria-checked={draft.status === status}
                onClick={() => setDraft((prev) => ({ ...prev, status }))}
                className={cn(
                  'flex-1 rounded px-3 py-1.5 text-sm font-medium transition-colors',
                  draft.status === status ? 'bg-background text-foreground shadow-soft' : 'text-muted-foreground hover:text-foreground',
                )}
              >
                {status}
              </button>
            ))}
          </div>
        </div>

        <div className="flex flex-col gap-1.5 sm:col-span-2 lg:col-span-1 xl:col-span-2">
          <Label htmlFor="filter-search">Search</Label>
          <div className="relative">
            <Search className="pointer-events-none absolute left-2.5 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              id="filter-search"
              value={draft.search ?? ''}
              onChange={(e) => setDraft((prev) => ({ ...prev, search: e.target.value || undefined }))}
              onKeyDown={(e) => e.key === 'Enter' && handleSearch()}
              placeholder="Search by filename…"
              className="pl-8"
            />
          </div>
        </div>
      </div>

      <div className="flex justify-end gap-2 border-t border-border pt-4">
        <Button variant="outline" onClick={handleReset}>
          <RotateCcw className="h-4 w-4" />
          Reset
        </Button>
        <Button onClick={handleSearch}>
          <Search className="h-4 w-4" />
          Search
        </Button>
      </div>
    </Card>
  );
}
