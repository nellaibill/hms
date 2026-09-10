import { Palette } from 'lucide-react';
import { PageBanner } from '@/components/PageBanner';
import { BrandingForm } from '@/features/branding/components/BrandingForm';

export default function BrandingSettingsPage() {
  return (
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={Palette}
        title="Theme & Branding"
        subtitle="Customize colors, fonts, logo, and hospital identity. Changes apply across the app immediately after saving — no code changes or redeploy needed."
        backTo="/admin/settings"
        backLabel="Back to settings"
      />

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
      <BrandingForm />
      </div>
    </div>
  );
}
