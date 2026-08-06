import { FileImage, FileSpreadsheet, FileText, FileType2, File as FileIcon } from 'lucide-react';
import { cn } from '@/lib/utils';
import { getFileKind } from '../utils/fileKind';

const ICON_BY_KIND = {
  pdf: { icon: FileText, className: 'text-rose-600 dark:text-rose-400' },
  image: { icon: FileImage, className: 'text-sky-600 dark:text-sky-400' },
  docx: { icon: FileType2, className: 'text-indigo-600 dark:text-indigo-400' },
  xlsx: { icon: FileSpreadsheet, className: 'text-emerald-600 dark:text-emerald-400' },
  other: { icon: FileIcon, className: 'text-muted-foreground' },
} as const;

interface FileTypeIconProps {
  mimeType: string;
  className?: string;
}

export function FileTypeIcon({ mimeType, className }: FileTypeIconProps) {
  const { icon: Icon, className: colorClass } = ICON_BY_KIND[getFileKind(mimeType)];
  return <Icon className={cn('h-5 w-5 shrink-0', colorClass, className)} aria-hidden="true" />;
}
