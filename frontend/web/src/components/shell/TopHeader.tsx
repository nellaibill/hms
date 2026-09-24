import { useEffect, useState, type ComponentType } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { Calendar as CalendarIcon, FileText, Menu, MoreHorizontal, Search } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Sheet, SheetContent, SheetTitle } from '@/components/ui/sheet';
import { HeaderCalculator } from '@/components/shell/HeaderCalculator';
import { HeaderIconLabel } from '@/components/shell/HeaderIconLabel';
import { HeaderLabelsVisibleContext } from '@/components/shell/headerLabelsContext';
import { HeaderSearchBox } from '@/components/shell/HeaderSearchBox';
import { HospitalLogo } from '@/components/shell/HospitalLogo';
import { branding } from '@/config/branding';
import { useBrandingQuery } from '@/features/branding/hooks/useBrandingQuery';
import { LanguageMenu } from '@/components/shell/LanguageMenu';
import { NotificationsMenu } from '@/components/shell/NotificationsMenu';
import { PendingTasksMenu } from '@/components/shell/PendingTasksMenu';
import { ProfileMenu } from '@/components/shell/ProfileMenu';
import { SidebarNav } from '@/components/shell/SidebarNav';
import { useAuth } from '@/features/auth/AuthContext';

interface HeaderLinkIconProps {
  to: string;
  label: string;
  icon: ComponentType<{ className?: string }>;
}

function HeaderLinkIcon({ to, label, icon }: HeaderLinkIconProps) {
  return (
    <Button asChild variant="ghost" size="icon" className="h-auto w-auto px-2.5 py-1" aria-label={label}>
      <Link to={to}>
        <HeaderIconLabel icon={icon} label={label} />
      </Link>
    </Button>
  );
}

export function TopHeader() {
  const [mobileNavOpen, setMobileNavOpen] = useState(false);
  const [mobileSearchOpen, setMobileSearchOpen] = useState(false);
  const [mobileMoreOpen, setMobileMoreOpen] = useState(false);
  const { data: brandingConfig } = useBrandingQuery();
  const appTitle = brandingConfig?.appTitle ?? branding.systemName;
  const { hasPermission, hasFeature } = useAuth();
  const { pathname } = useLocation();

  // Anything picked inside the mobile sheets (a menu link, a notification, a search result)
  // navigates — close them so the new page isn't hidden behind an open sheet.
  useEffect(() => {
    setMobileSearchOpen(false);
    setMobileMoreOpen(false);
  }, [pathname]);

  // Rendered in the desktop header row, and inside the mobile More sheet (below the `shell`
  // breakpoint), where there isn't room for all of them next to the logo.
  const secondaryActions = (
    <>
      <LanguageMenu />
      <NotificationsMenu />
      {hasPermission('engagement.view') && hasFeature('calendar') && (
        <HeaderLinkIcon to="/engagement/programmes" label="Calendar" icon={CalendarIcon} />
      )}
      <HeaderCalculator />
      <PendingTasksMenu />
      {hasPermission('records-compliance.view') && <HeaderLinkIcon to="/documents" label="Documents" icon={FileText} />}
    </>
  );

  return (
    // No left padding: the wider logo box (see HospitalLogo below) reads better sitting flush
    // against the header's left edge than with the same 1.5rem gap the right side keeps.
    <header className="sticky top-0 z-[1000] flex h-16 items-center gap-3 border-b border-header-foreground/15 bg-header pr-3 text-header-foreground lg:gap-6 lg:pr-6 shadow-soft-md">
      {/* Mobile nav trigger — the sidebar collapses to a drawer below the `shell` breakpoint
          (narrow, or short like a phone in landscape), per docs/LayoutFramework.md §14 */}
      <Button variant="ghost" size="icon" className="shrink-0 shell:hidden" onClick={() => setMobileNavOpen(true)} aria-label="Open navigation">
        <Menu className="h-5 w-5" />
      </Button>
      <Sheet open={mobileNavOpen} onOpenChange={setMobileNavOpen}>
        <SheetContent side="left" className="flex flex-col bg-sidebar p-0">
          <div className="flex h-16 items-center border-b border-border px-4">
            <HospitalLogo />
          </div>
          {/* Extra bottom clearance (well beyond safe-area-inset-bottom alone) — a mobile
              browser's own bottom toolbar isn't covered by the safe-area env vars, only
              device notches/home-indicators are, so the last item or two in a tall list
              can end up under real, un-tappable browser chrome without this. */}
          <div className="flex-1 overflow-y-auto py-3 pb-[calc(2.5rem+env(safe-area-inset-bottom))]">
            <SidebarNav onNavigate={() => setMobileNavOpen(false)} />
          </div>
        </SheetContent>
      </Sheet>

      {/* The long app title only shows from 2xl (below that it squeezes the search box to ~80px);
          below 2xl this block is just the logo, which keeps its size so it never overlaps the
          icon row — the logo itself steps down in size instead (imageClassName). */}
      <div className="flex shrink-0 items-center gap-3 2xl:min-w-0 2xl:shrink">
        {/* Bigger than HospitalLogo's own default box (h-10 max-w-32) — a wordmark-style logo
            (wide, short — the bundled default is 699x138px) barely reads at that size. Sized to
            reach the header's own height for a typical wordmark logo; object-contain still
            protects a differently-shaped upload from ever being stretched or cropped. */}
        <HospitalLogo invert showName={false} imageClassName="h-10 max-w-32 sm:h-12 sm:max-w-48 xl:h-16 xl:max-w-80" />
        <span className="hidden min-w-0 truncate text-base font-bold leading-tight tracking-tight text-header-foreground 2xl:inline 2xl:text-lg">
          {appTitle}
        </span>
      </div>

      <div className="hidden min-w-0 flex-1 justify-center shell:flex">
        <div className="w-full max-w-2xl">
          <HeaderSearchBox />
        </div>
      </div>

      {/* Icon actions — icon-over-label, vertically centered. In the desktop shell the whole row
          shows and never shrinks (the logo/title/search give way first). Below it (phones, and
          short landscape screens) only Search, More and Profile stay in the bar: the rest used
          to sit in a sideways-scrolling strip that pushed Profile/Logout off-screen. */}
      <div className="ml-auto flex shrink-0 items-center gap-1 py-1 [&>*]:shrink-0">
        <div className="hidden items-center gap-1 shell:flex [&>*]:shrink-0">{secondaryActions}</div>

        <Button variant="ghost" size="icon" className="shell:hidden" onClick={() => setMobileSearchOpen(true)} aria-label="Search patients">
          <Search className="h-5 w-5" />
        </Button>
        <Button variant="ghost" size="icon" className="shell:hidden" onClick={() => setMobileMoreOpen(true)} aria-label="More actions">
          <MoreHorizontal className="h-5 w-5" />
        </Button>

        <div className="mx-1 hidden h-8 w-px bg-header-foreground/20 shell:block" aria-hidden="true" />
        <ProfileMenu />
      </div>

      <Sheet open={mobileSearchOpen} onOpenChange={setMobileSearchOpen}>
        <SheetContent side="top" className="p-4 pr-12">
          <SheetTitle className="sr-only">Search patients</SheetTitle>
          <HeaderSearchBox autoFocus onNavigate={() => setMobileSearchOpen(false)} />
        </SheetContent>
      </Sheet>

      <Sheet open={mobileMoreOpen} onOpenChange={setMobileMoreOpen}>
        <SheetContent side="right" className="flex flex-col gap-4 p-4">
          <SheetTitle>More</SheetTitle>
          <HeaderLabelsVisibleContext.Provider value={true}>
            <div className="grid grid-cols-3 gap-2 [&>*]:h-auto [&>*]:w-full [&>*]:py-2">{secondaryActions}</div>
          </HeaderLabelsVisibleContext.Provider>
        </SheetContent>
      </Sheet>
    </header>
  );
}
