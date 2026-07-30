import { ArrowRight, Database, Palette, Settings as SettingsIcon, ShieldCheck, Users as UsersIcon, type LucideIcon } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Card, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';

interface SettingsSection {
  title: string;
  description: string;
  icon: LucideIcon;
  path?: string;
  status: 'available' | 'placeholder';
}

const sections: SettingsSection[] = [
  {
    title: 'User Accounts',
    description: 'Manage system user accounts — the Identity reference module, fully connected to the live API.',
    icon: UsersIcon,
    path: '/users',
    status: 'available',
  },
  {
    title: 'Roles & Permissions',
    description: 'Role-based access control and the permission matrix for every module.',
    icon: ShieldCheck,
    path: '/admin/roles',
    status: 'available',
  },
  {
    title: 'Master Data',
    description: 'Product classification, brands, units, tax, warehouses, business partners, and finance reference data.',
    icon: Database,
    path: '/admin/masters',
    status: 'available',
  },
  {
    title: 'Theme & Branding',
    description: 'Colors, fonts, logo, and hospital identity — applied across the app immediately, no code changes needed.',
    icon: Palette,
    path: '/admin/settings/branding',
    status: 'available',
  },
];

export default function SettingsPage() {
  return (
    <div className="flex flex-1 flex-col">
      {/* Centered, brand-colored banner — matches the Page banner style used
          across module pages (Theme & Branding → Section headers). */}
      <div className="flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        <div className="flex items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
            <SettingsIcon className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight">Settings</h1>
        </div>
        <p className="max-w-2xl text-sm text-page-banner-foreground/85">
          Roles &amp; permissions, master data, and system configuration.
        </p>
      </div>

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {sections.map((section) => {
          const Icon = section.icon;
          const content = (
            <Card
              key={section.title}
              className={
                section.status === 'available'
                  ? 'transition-all hover:border-primary/40 hover:bg-accent/40 hover:shadow-soft-lg'
                  : 'border-dashed'
              }
            >
              <CardHeader>
                <div className="flex items-center justify-between">
                  <span className="flex h-9 w-9 items-center justify-center rounded-md bg-primary/10 text-primary">
                    <Icon className="h-4.5 w-4.5" />
                  </span>
                  {section.status === 'available' ? (
                    <ArrowRight className="h-4 w-4 text-muted-foreground" />
                  ) : (
                    <span className="rounded-full bg-muted px-2 py-0.5 text-[11px] font-medium text-muted-foreground">
                      Coming soon
                    </span>
                  )}
                </div>
                <CardTitle className="text-base">{section.title}</CardTitle>
                <CardDescription>{section.description}</CardDescription>
              </CardHeader>
            </Card>
          );

          return section.path ? (
            <Link key={section.title} to={section.path} className="block">
              {content}
            </Link>
          ) : (
            <div key={section.title}>{content}</div>
          );
        })}
      </div>
      </div>
    </div>
  );
}
