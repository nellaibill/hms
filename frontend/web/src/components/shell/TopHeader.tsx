import { useState, type ComponentType } from 'react';
import { Link } from 'react-router-dom';
import { Calendar as CalendarIcon, Menu, Wallet } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Sheet, SheetContent } from '@/components/ui/sheet';
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip';
import { HeaderCalculator } from '@/components/shell/HeaderCalculator';
import { HeaderSearchBox } from '@/components/shell/HeaderSearchBox';
import { HospitalLogo } from '@/components/shell/HospitalLogo';
import { branding } from '@/config/branding';
import { LanguageMenu } from '@/components/shell/LanguageMenu';
import { NotificationsMenu } from '@/components/shell/NotificationsMenu';
import { PendingTasksMenu } from '@/components/shell/PendingTasksMenu';
import { ProfileMenu } from '@/components/shell/ProfileMenu';
import { SidebarNav } from '@/components/shell/SidebarNav';

interface HeaderLinkIconProps {
  to: string;
  label: string;
  icon: ComponentType<{ className?: string }>;
}

function HeaderLinkIcon({ to, label, icon: Icon }: HeaderLinkIconProps) {
  return (
    <Tooltip>
      <TooltipTrigger asChild>
        <Button asChild variant="ghost" size="icon" aria-label={label}>
          <Link to={to}>
            <Icon className="h-5 w-5" />
          </Link>
        </Button>
      </TooltipTrigger>
      <TooltipContent>{label}</TooltipContent>
    </Tooltip>
  );
}

export function TopHeader() {
  const [mobileNavOpen, setMobileNavOpen] = useState(false);

  return (
    <header className="sticky top-0 z-[1000] flex h-16 items-center gap-6 border-b border-header-foreground/15 bg-header px-6 text-header-foreground shadow-soft-md">
      {/* Mobile nav trigger — sidebar collapses to a drawer below md, per docs/LayoutFramework.md §14 */}
      <Button variant="ghost" size="icon" className="shrink-0 md:hidden" onClick={() => setMobileNavOpen(true)} aria-label="Open navigation">
        <Menu className="h-5 w-5" />
      </Button>
      <Sheet open={mobileNavOpen} onOpenChange={setMobileNavOpen}>
        <SheetContent side="left" className="flex flex-col p-0">
          <div className="flex h-16 items-center border-b border-border px-4">
            <HospitalLogo />
          </div>
          <div className="flex-1 overflow-y-auto py-3">
            <SidebarNav onNavigate={() => setMobileNavOpen(false)} />
          </div>
        </SheetContent>
      </Sheet>

      <div className="flex shrink-0 items-center gap-3">
        <HospitalLogo invert showName={false} />
        <span className="hidden truncate text-base font-bold leading-tight tracking-tight text-header-foreground sm:inline lg:text-lg">
          {branding.systemName}
        </span>
      </div>

      <div className="hidden flex-1 justify-center sm:flex">
        <div className="w-full max-w-2xl">
          <HeaderSearchBox />
        </div>
      </div>

      {/* Icon actions — equally spaced, icon-only with a hover tooltip, vertically centered; the row itself (not its icons) shrinks and scrolls horizontally rather than clipping on narrow viewports. */}
      <div className="ml-auto flex min-w-0 items-center gap-2 overflow-x-auto py-3 [&>*]:shrink-0">
        <LanguageMenu />
        <NotificationsMenu />
        <HeaderLinkIcon to="/engagement/programmes" label="Calendar" icon={CalendarIcon} />
        <HeaderCalculator />
        <PendingTasksMenu />
        <HeaderLinkIcon to="/finance/accounts" label="Expenses Tracking" icon={Wallet} />
        <ProfileMenu />
      </div>
    </header>
  );
}
