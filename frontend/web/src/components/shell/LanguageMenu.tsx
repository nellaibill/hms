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
import { HeaderIconLabel } from '@/components/shell/HeaderIconLabel';

const LANGUAGES = [
  { id: 'en', label: 'English', shortLabel: 'English' },
  { id: 'ta', label: 'தமிழ் (Tamil)', shortLabel: 'தமிழ்' },
  { id: 'hi', label: 'हिन्दी (Hindi)', shortLabel: 'हिन्दी' },
];

/** UI-only — selecting a language doesn't translate the app yet, just tracks the preference locally. */
export function LanguageMenu() {
  const [selected, setSelected] = useState('en');
  const selectedLanguage = LANGUAGES.find((language) => language.id === selected) ?? LANGUAGES[0];

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" size="icon" className="h-auto w-auto px-2.5 py-1" aria-label="Language Selector">
          <HeaderIconLabel icon={Languages} label={selectedLanguage.shortLabel} showChevron />
        </Button>
      </DropdownMenuTrigger>
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
