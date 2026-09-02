import { ArrowRight, ClipboardList, CloudUpload, UserPlus, UserSearch, Wallet } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Card, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { useAuth } from '@/features/auth/AuthContext';
import { RequirePermission } from '@/features/auth/RequirePermission';

const sections = [
  {
    title: 'New Patient Registration',
    description: 'Register a first-time patient — demographics, contacts, and encounter details.',
    icon: UserPlus,
    path: '/patients/registration/new',
    permission: 'patient-management.create',
  },
  {
    title: 'Old Patient Registration',
    description: 'Find an existing patient by name, UHID, or phone to view or update their registration.',
    icon: UserSearch,
    path: '/patients/enquiry',
    permission: 'patient-management.view',
  },
];

const billingSection = {
  title: 'Billing',
  description: 'Create a new invoice — charges, payments, and insurance/TPA claim details.',
  icon: Wallet,
  path: '/finance/accounts/new',
  permission: 'finance-billing.view',
};

// Super Admin only (see PermissionSeedData's "patient-management.import" entry — no other
// role is granted it by default) — deliberately not surfaced via the main sidebar, whose
// leaves only support gating by a module's blanket `.view` permission, not this narrower key.
const bulkImportSection = {
  title: 'Bulk Patient Import',
  description: 'Upload an Excel file to register many patients at once — validated and reviewed before anything is saved.',
  icon: CloudUpload,
  path: '/patients/import',
  permission: 'patient-management.import',
};

/** Reception & Registration landing hub — the single entry point for both registration flows. */
export default function PatientRegistrationHubPage() {
  const { hasPermission } = useAuth();
  const visibleSections = sections.filter((section) => hasPermission(section.permission));
  const showBilling = hasPermission(billingSection.permission);
  const BillingIcon = billingSection.icon;
  const showBulkImport = hasPermission(bulkImportSection.permission);
  const BulkImportIcon = bulkImportSection.icon;
  return (
    <RequirePermission permission="patient-management.view">
    <div className="flex flex-1 flex-col">
      <div className="flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        <div className="flex items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
            <ClipboardList className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight text-page-banner-foreground">Reception &amp; Registration</h1>
        </div>
        <p className="max-w-2xl text-sm text-page-banner-foreground/85">
          Register a new patient, or find an existing one to update their registration.
        </p>
      </div>

      {/* Centered as one pair (not a full-width 50/50 split, which centered each card
          independently within its own half — spreading them apart the wider the
          viewport instead of reading as a single centered choice). Falls back to a single
          centered card (no divider) when the signed-in user can only reach one flow.
          Billing sits in its own centered row below the pair, not inside the same grid,
          so it isn't forced into the pair's divider/column layout. */}
      <div className="flex flex-1 flex-col items-center justify-center gap-8 p-6 lg:p-8">
        <div
          className={
            visibleSections.length > 1
              ? 'grid grid-cols-1 gap-6 sm:grid-cols-2 sm:gap-0 sm:divide-x sm:divide-border'
              : 'grid grid-cols-1'
          }
        >
          {visibleSections.map((section) => {
            const Icon = section.icon;
            return (
              <div key={section.title} className="flex items-center justify-center px-0 py-0 sm:px-10">
                <Link to={section.path} className="block w-full max-w-sm sm:max-w-md">
                  <Card className="transition-all hover:border-primary/40 hover:bg-accent/40 hover:shadow-soft-lg">
                    <CardHeader className="p-7 sm:p-8">
                      <div className="flex items-center justify-between">
                        <span className="flex h-10 w-10 items-center justify-center rounded-md bg-primary/10 text-primary">
                          <Icon className="h-5 w-5" />
                        </span>
                        <ArrowRight className="h-4 w-4 text-muted-foreground" />
                      </div>
                      <CardTitle className="text-base">{section.title}</CardTitle>
                      <CardDescription>{section.description}</CardDescription>
                    </CardHeader>
                  </Card>
                </Link>
              </div>
            );
          })}
        </div>

        {showBilling && (
          <Link to={billingSection.path} className="block w-full max-w-sm sm:max-w-md">
            <Card className="transition-all hover:border-primary/40 hover:bg-accent/40 hover:shadow-soft-lg">
              <CardHeader className="p-7 sm:p-8">
                <div className="flex items-center justify-between">
                  <span className="flex h-10 w-10 items-center justify-center rounded-md bg-primary/10 text-primary">
                    <BillingIcon className="h-5 w-5" />
                  </span>
                  <ArrowRight className="h-4 w-4 text-muted-foreground" />
                </div>
                <CardTitle className="text-base">{billingSection.title}</CardTitle>
                <CardDescription>{billingSection.description}</CardDescription>
              </CardHeader>
            </Card>
          </Link>
        )}

        {showBulkImport && (
          <Link to={bulkImportSection.path} className="block w-full max-w-sm sm:max-w-md">
            <Card className="transition-all hover:border-primary/40 hover:bg-accent/40 hover:shadow-soft-lg">
              <CardHeader className="p-7 sm:p-8">
                <div className="flex items-center justify-between">
                  <span className="flex h-10 w-10 items-center justify-center rounded-md bg-primary/10 text-primary">
                    <BulkImportIcon className="h-5 w-5" />
                  </span>
                  <ArrowRight className="h-4 w-4 text-muted-foreground" />
                </div>
                <CardTitle className="text-base">{bulkImportSection.title}</CardTitle>
                <CardDescription>{bulkImportSection.description}</CardDescription>
              </CardHeader>
            </Card>
          </Link>
        )}
      </div>
    </div>
    </RequirePermission>
  );
}
