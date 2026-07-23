import { ArrowRight, Database, ShieldCheck, SlidersHorizontal, Users as UsersIcon, type LucideIcon } from 'lucide-react';
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
    status: 'placeholder',
  },
  {
    title: 'Master Data',
    description: 'Departments, consultants, and shared dropdown reference data.',
    icon: Database,
    status: 'placeholder',
  },
  {
    title: 'System Configuration',
    description: 'Global configuration values and environment-level settings.',
    icon: SlidersHorizontal,
    status: 'placeholder',
  },
];

export default function SettingsPage() {
  return (
    <div className="flex flex-1 flex-col gap-6 p-6">
      <div className="border-b border-border pb-4">
        <h1 className="text-xl font-semibold tracking-tight text-foreground">Settings</h1>
        <p className="mt-1 max-w-2xl text-sm text-muted-foreground">
          Roles &amp; permissions, master data, and system configuration.
        </p>
      </div>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {sections.map((section) => {
          const Icon = section.icon;
          const content = (
            <Card
              key={section.title}
              className={section.status === 'available' ? 'transition-colors hover:border-primary/40 hover:bg-accent/40' : 'border-dashed'}
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
  );
}
