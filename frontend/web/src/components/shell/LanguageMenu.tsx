import { useState } from 'react';
import { Check, Languages } from 'lucide-react';
import { Button } from '@/components/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip';

const LANGUAGES = [
  { id: 'en', label: 'English' },
  { id: 'ta', label: 'தமிழ் (Tamil)' },
  { id: 'hi', label: 'हिन्दी (Hindi)' },
];

/** UI-only — selecting a language doesn't translate the app yet, just tracks the preference locally. */
export function LanguageMenu() {
  const [selected, setSelected] = useState('en');

  return (
    <DropdownMenu>
      <Tooltip>
        <TooltipTrigger asChild>
          <DropdownMenuTrigger asChild>
            <Button variant="ghost" size="icon" aria-label="Language Selector">
              <Languages className="h-5 w-5" />
            </Button>
          </DropdownMenuTrigger>
        </TooltipTrigger>
        <TooltipContent>Language Selector</TooltipContent>
      </Tooltip>
      <DropdownMenuContent align="end" className="w-48">
        <DropdownMenuLabel>Language</DropdownMenuLabel>
        <DropdownMenuSeparator />
        {LANGUAGES.map((language) => (
          <DropdownMenuItem key={language.id} onSelect={() => setSelected(language.id)} className="justify-between">
            {language.label}
            {selected === language.id && <Check className="h-4 w-4" />}
          </DropdownMenuItem>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
