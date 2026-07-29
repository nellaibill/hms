import { ArrowLeft, Palette } from 'lucide-react';
import { Link } from 'react-router-dom';
import { BrandingForm } from '@/features/branding/components/BrandingForm';

export default function BrandingSettingsPage() {
  return (
    <div className="flex flex-1 flex-col">
      <div className="px-6 pt-4 lg:px-8">
        <Link to="/admin/settings" className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
          <ArrowLeft className="h-4 w-4" />
          Back to settings
        </Link>
      </div>

      {/* Centered, brand-colored banner — matches the Page banner style used
          across module pages (Theme & Branding → Section headers). */}
      <div className="mt-3 flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        <div className="flex items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
            <Palette className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight">Theme &amp; Branding</h1>
        </div>
        <p className="max-w-2xl text-sm text-page-banner-foreground/85">
          Customize colors, fonts, logo, and hospital identity. Changes apply across the app immediately after saving — no code
          changes or redeploy needed.
        </p>
      </div>

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
      <BrandingForm />
      </div>
    </div>
  );
}
