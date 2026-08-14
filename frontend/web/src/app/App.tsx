import { QueryClientProvider } from '@tanstack/react-query';
import { RouterProvider } from 'react-router-dom';
import { TooltipProvider } from '@/components/ui/tooltip';
import { ToastProvider } from '@/components/ui/toast-context';
import { Toaster } from '@/components/ui/toaster';
import { router } from '../routes/routes';
import { queryClient } from './queryClient';
import { ThemeProvider } from '../lib/theme-provider';
import { AuthProvider } from '../features/auth/AuthContext';
import { PlatformAuthProvider } from '../features/platformAuth/PlatformAuthContext';

export function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <ThemeProvider>
        <AuthProvider>
          <PlatformAuthProvider>
            <TooltipProvider delayDuration={200}>
              <ToastProvider>
                <RouterProvider router={router} />
                <Toaster />
              </ToastProvider>
            </TooltipProvider>
          </PlatformAuthProvider>
        </AuthProvider>
      </ThemeProvider>
    </QueryClientProvider>
  );
}
