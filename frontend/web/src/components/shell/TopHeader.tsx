import { useState, type ComponentType } from 'react';
import { Link } from 'react-router-dom';
import { Calendar as CalendarIcon, FileText, Menu } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Sheet, SheetContent } from '@/components/ui/sheet';
import { HeaderCalculator } from '@/components/shell/HeaderCalculator';
import { HeaderIconLabel } from '@/components/shell/HeaderIconLabel';
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
  const { data: brandingConfig } = useBrandingQuery();
  const appTitle = brandingConfig?.appTitle ?? branding.systemName;
  const { hasPermission, hasFeature } = useAuth();

  return (
    // No left padding: the wider logo box (see HospitalLogo below) reads better sitting flush
    // against the header's left edge than with the same 1.5rem gap the right side keeps.
    <header className="sticky top-0 z-[1000] flex h-16 items-center gap-3 border-b border-header-foreground/15 bg-header pr-3 text-header-foreground lg:gap-6 lg:pr-6 shadow-soft-md">
      {/* Mobile nav trigger — sidebar collapses to a drawer below md, per docs/LayoutFramework.md §14 */}
      <Button variant="ghost" size="icon" className="shrink-0 md:hidden" onClick={() => setMobileNavOpen(true)} aria-label="Open navigation">
        <Menu className="h-5 w-5" />
      </Button>
      <Sheet open={mobileNavOpen} onOpenChange={setMobileNavOpen}>
        <SheetContent side="left" className="flex flex-col bg-sidebar p-0">
          <div className="flex h-16 items-center border-b border-border px-4">
            <HospitalLogo imageClassName="max-h-12 max-w-48" />
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
        {/* The configured Primary logo height (Logo Configuration) is clamped by these caps,
            which never exceed the header's own h-16 — so no logo or configured size can make
            the top bar taller. Below sm the Compact logo (square/icon-style) is shown instead. */}
        <HospitalLogo slot="compact" className="sm:hidden" invert showName={false} imageClassName="max-h-10 max-w-32" />
        <HospitalLogo className="hidden sm:flex" invert showName={false} imageClassName="max-h-12 max-w-48 xl:max-h-16 xl:max-w-80" />
        <span className="hidden min-w-0 truncate text-base font-bold leading-tight tracking-tight text-header-foreground 2xl:inline 2xl:text-lg">
          {appTitle}
        </span>
      </div>

      <div className="hidden min-w-0 flex-1 justify-center sm:flex">
        <div className="w-full max-w-2xl">
          <HeaderSearchBox />
        </div>
      </div>

      {/* Icon actions — icon-over-label, vertically centered. Doesn't shrink from md up (the
          logo/title/search give way first), so the icons are never hidden behind a scroll
          strip; only on phones, where they genuinely can't all fit, does the row scroll. */}
      <div className="ml-auto flex min-w-0 items-center gap-1 overflow-x-auto py-1 md:shrink-0 [&>*]:shrink-0">
        <LanguageMenu />
        <NotificationsMenu />
        {hasPermission('engagement.view') && hasFeature('calendar') && (
          <HeaderLinkIcon to="/engagement/programmes" label="Calendar" icon={CalendarIcon} />
        )}
        <HeaderCalculator />
        <PendingTasksMenu />
        {hasPermission('records-compliance.view') && <HeaderLinkIcon to="/documents" label="Documents" icon={FileText} />}
        <div className="mx-1 h-8 w-px shrink-0 bg-header-foreground/20" aria-hidden="true" />
        <ProfileMenu />
      </div>
    </header>
  );
}
