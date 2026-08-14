'use client';

import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { Toaster } from 'react-hot-toast';
import { GoogleOAuthProvider } from '@react-oauth/google';
import { useState } from 'react';
import { GlobalLoadingBar } from '@/components/layout/GlobalLoadingBar';

const GOOGLE_CLIENT_ID = process.env.NEXT_PUBLIC_GOOGLE_CLIENT_ID ?? '';

export function Providers({ children }: { children: React.ReactNode }) {
  const [queryClient] = useState(() => new QueryClient({
    defaultOptions: {
      queries: { staleTime: 60_000, retry: 1 },
    },
  }));

  return (
    <GoogleOAuthProvider clientId={GOOGLE_CLIENT_ID}>
      <QueryClientProvider client={queryClient}>
        <GlobalLoadingBar />
        {children}
        <Toaster
          position="top-right"
          toastOptions={{
            success: { duration: 3000 },
            error: { duration: 5000 },
          }}
        />
      </QueryClientProvider>
    </GoogleOAuthProvider>
  );
}
