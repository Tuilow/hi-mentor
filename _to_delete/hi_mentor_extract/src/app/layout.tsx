import type { Metadata } from 'next';
import { Manrope } from 'next/font/google';
import './globals.css';
import { Providers } from '@/components/layout/Providers';

// Redesign "Hi Mentor" (ago/2026): tipografia trocada de Inter para Manrope, seguindo o
// design system definido no protótipo Figma Make (hi-mentor/src/index.css).
const manrope = Manrope({ subsets: ['latin'], variable: '--font-manrope', weight: ['400', '500', '600', '700', '800'] });

export const metadata: Metadata = {
  title: 'Hi Mentor — Plataforma de Mentoria',
  description: 'Uma plataforma moderna onde mentores estruturam programas, acompanham mentorados e recebem por suas mentorias.',
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="pt-BR" className={manrope.variable} suppressHydrationWarning>
      <body className={manrope.className}>
        <Providers>{children}</Providers>
      </body>
    </html>
  );
}
