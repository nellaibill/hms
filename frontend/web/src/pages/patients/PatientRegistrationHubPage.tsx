import { ArrowRight, ClipboardList, UserPlus, UserSearch } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Card, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';

const sections = [
  {
    title: 'New Patient Registration',
    description: 'Register a first-time patient — demographics, contacts, and encounter details.',
    icon: UserPlus,
    path: '/patients/registration/new',
  },
  {
    title: 'Old Patient Registration',
    description: 'Find an existing patient by name, UHID, or phone to view or update their registration.',
    icon: UserSearch,
    path: '/patients/enquiry',
  },
];

/** Reception & Registration landing hub — the single entry point for both registration flows. */
export default function PatientRegistrationHubPage() {
  return (
    <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
      <div className="flex items-start gap-3 border-b border-border pb-4">
        <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-primary/10 text-primary">
          <ClipboardList className="h-5 w-5" />
        </span>
        <div>
          <h1 className="text-xl font-semibold tracking-tight text-primary">Reception &amp; Registration</h1>
          <p className="mt-1 max-w-2xl text-sm text-muted-foreground">
            Register a new patient, or find an existing one to update their registration.
          </p>
        </div>
      </div>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 sm:max-w-2xl">
        {sections.map((section) => {
          const Icon = section.icon;
          return (
            <Link key={section.title} to={section.path} className="block">
              <Card className="h-full transition-all hover:border-primary/40 hover:bg-accent/40 hover:shadow-soft-lg">
                <CardHeader>
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
          );
        })}
      </div>
    </div>
  );
}
