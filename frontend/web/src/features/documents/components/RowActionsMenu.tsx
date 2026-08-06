import { Archive, Download, Eye, MoreVertical, Trash2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip';
import type { HmsDocument } from '../types';

interface RowActionsMenuProps {
  doc: HmsDocument;
  onPreview: (doc: HmsDocument) => void;
  onDownload: (doc: HmsDocument) => void;
  onArchive: (doc: HmsDocument) => void;
  onDelete: (doc: HmsDocument) => void;
}

export function RowActionsMenu({ doc, onPreview, onDownload, onArchive, onDelete }: RowActionsMenuProps) {
  return (
    <div className="flex items-center justify-end gap-1">
      <Tooltip>
        <TooltipTrigger asChild>
          <Button
            variant="ghost"
            size="icon"
            className="h-8 w-8"
            aria-label={`Preview ${doc.originalFileName}`}
            onClick={() => onPreview(doc)}
          >
            <Eye className="h-4 w-4" />
          </Button>
        </TooltipTrigger>
        <TooltipContent>Preview</TooltipContent>
      </Tooltip>

      <Tooltip>
        <TooltipTrigger asChild>
          <Button
            variant="ghost"
            size="icon"
            className="h-8 w-8"
            aria-label={`Download ${doc.originalFileName}`}
            onClick={() => onDownload(doc)}
          >
            <Download className="h-4 w-4" />
          </Button>
        </TooltipTrigger>
        <TooltipContent>Download</TooltipContent>
      </Tooltip>

      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button variant="ghost" size="icon" className="h-8 w-8" aria-label={`More actions for ${doc.originalFileName}`}>
            <MoreVertical className="h-4 w-4" />
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="end">
          {!doc.isArchived && (
            <DropdownMenuItem onClick={() => onArchive(doc)}>
              <Archive className="h-4 w-4" />
              Archive
            </DropdownMenuItem>
          )}
          <DropdownMenuItem onClick={() => onDelete(doc)} className="text-destructive focus:text-destructive">
            <Trash2 className="h-4 w-4" />
            Delete
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>
    </div>
  );
}
