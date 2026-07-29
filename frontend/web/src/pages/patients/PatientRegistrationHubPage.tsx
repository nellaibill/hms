import { ArrowRight, ClipboardList, UserPlus, UserSearch } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Card, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { cn } from '@/lib/utils';

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

      <div className="grid flex-1 grid-cols-1 md:grid-cols-2">
        {sections.map((section, index) => {
          const Icon = section.icon;
          return (
            <div
              key={section.title}
              className={cn(
                'flex items-center justify-center p-6 lg:p-8',
                index === 0 && 'md:border-r md:border-border',
              )}
            >
              <Link to={section.path} className="block w-full max-w-sm">
                <Card className="transition-all hover:border-primary/40 hover:bg-accent/40 hover:shadow-soft-lg">
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
            </div>
          );
        })}
      </div>
    </div>
  );
}
