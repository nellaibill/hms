import {
  ArrowRight,
  BadgeCheck,
  ClipboardCheck,
  Database,
  DatabaseBackup,
  Eye,
  Palette,
  Settings as SettingsIcon,
  ShieldCheck,
  type LucideIcon,
} from 'lucide-react';
import { Link } from 'react-router-dom';
import { PageBanner } from '@/components/PageBanner';
import { Card, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { useAuth } from '@/features/auth/AuthContext';

interface SettingsSection {
  title: string;
  description: string;
  icon: LucideIcon;
  path?: string;
  status: 'available' | 'placeholder';
  /** Backend permission gating this card — omitted for Master Data/Theme & Branding,
   * which have no corresponding entry in the permission catalog (see final report's
   * gap list); those stay reachable to anyone who can open Settings at all. */
  permission?: string;
  /** Shown only to a Super Admin's sign-in, whatever the role's permissions (ADR-148). */
  superAdminOnly?: boolean;
  /** Tenant feature (ADR-145) a Platform Admin can switch this page off with — omitted for
   * Roles & Permissions, which can never be switched off. */
  feature?: string;
}

// No "User Accounts" card: logins are now created and managed from each Employee's and
// Consultant's own Login access card (ADR-108), one login per person. The /users pages still
// exist for the login card's "Open in Users" link and for accounts that aren't staff records.
const sections: SettingsSection[] = [
  {
    title: 'Roles & Permissions',
    description: 'Role-based access control and the permission matrix for every module.',
    icon: ShieldCheck,
    path: '/admin/roles',
    status: 'available',
    permission: 'identity-administration.view',
  },
  {
    title: 'Hospital Reference Data', /*Master Data)*/
    description: 'Product classification, brands, units, tax, warehouses, business partners, and finance reference data.',
    icon: Database,
    path: '/admin/masters',
    feature: 'settings.reference-data',
    status: 'available',
  },
  {
    title: 'Theme & Branding',
    description: 'Colors, fonts, logo, and hospital identity — applied across the app immediately, no code changes needed.',
    icon: Palette,
    path: '/admin/settings/branding',
    feature: 'settings.branding',
    status: 'available',
  },
  {
    title: 'Reports Visibility',
    description: 'Choose which Accounts & Finance reports this hospital shows. OP Report and Collection Summary always show.',
    icon: Eye,
    path: '/admin/settings/reports-visibility',
    feature: 'settings.reports-visibility',
    status: 'available',
    permission: 'identity-administration.view',
  },
  {
    title: 'Patient Registration Fields',
    description: 'Choose which New Patient Registration fields are mandatory — last name, address, emergency contact, ID proof and more.',
    icon: ClipboardCheck,
    path: '/admin/settings/patient-registration-fields',
    feature: 'settings.patient-fields',
    status: 'available',
    permission: 'identity-administration.view',
  },
  {
    title: 'Billing Approvals',
    description: 'Turn discounts off, or choose whether discounts and bill cancellations need Super Admin approval.',
    icon: BadgeCheck,
    path: '/admin/settings/billing-approvals',
    feature: 'settings.billing-approvals',
    status: 'available',
    permission: 'identity-administration.view',
  },
  {
    title: 'Backup',
    description: 'Download this hospital’s latest daily database backup, or a zip of its documents and images.',
    icon: DatabaseBackup,
    path: '/admin/settings/backup',
    feature: 'settings.backup',
    status: 'available',
    permission: 'identity-administration.view',
    // Every patient's record is in a backup: a Super Admin's sign-in only (ADR-148).
    superAdminOnly: true,
  },
];

export default function SettingsPage() {
  const { hasPermission, hasFeature, user } = useAuth();
  const visibleSections = sections.filter(
    (section) =>
      (!section.permission || hasPermission(section.permission)) &&
      (!section.feature || hasFeature(section.feature)) &&
      (!section.superAdminOnly || user?.role === 'superAdmin'),
  );
  return (
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={SettingsIcon}
        title="Settings"
        subtitle="Roles & permissions, master data, and system configuration."
      />

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {visibleSections.map((section) => {
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
