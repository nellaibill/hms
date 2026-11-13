import { ClipboardList, CloudUpload, UserPlus, UserSearch, Wallet } from 'lucide-react';
import { HubCard } from '@/components/HubCard';
import { PageBanner } from '@/components/PageBanner';
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
  const showBulkImport = hasPermission(bulkImportSection.permission);
  return (
    <RequirePermission permission="patient-management.view">
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={ClipboardList}
        title="Reception & Registration"
        subtitle="Register a new patient, or find an existing one to update their registration."
      />

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
          {visibleSections.map((section) => (
            <div key={section.title} className="flex items-center justify-center px-0 py-0 sm:px-10">
              <HubCard to={section.path} title={section.title} description={section.description} icon={section.icon} />
            </div>
          ))}
        </div>

        {showBilling && (
          <HubCard to={billingSection.path} title={billingSection.title} description={billingSection.description} icon={billingSection.icon} />
        )}

        {showBulkImport && (
          <HubCard
            to={bulkImportSection.path}
            title={bulkImportSection.title}
            description={bulkImportSection.description}
            icon={bulkImportSection.icon}
          />
        )}
      </div>
    </div>
    </RequirePermission>
  );
}
