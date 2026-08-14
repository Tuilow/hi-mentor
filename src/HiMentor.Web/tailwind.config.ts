import type { Config } from 'tailwindcss';

// Redesign "Hi Mentor" (ago/2026): a paleta de marca passa a usar as cores padrão do
// Tailwind (violet/amber/emerald), exatamente como o protótipo Figma Make faz — não há
// necessidade de uma escala de marca custom. `brand`/`accent` continuam existindo (várias
// classes globais em globals.css e páginas ainda não recriadas nesta rodada usam
// `bg-brand-600`/`text-accent-600` etc.) mas agora apontam para os mesmos valores de
// violet-*/amber-*, então qualquer tela que não foi tocada nesta rodada herda a cor nova
// automaticamente, em vez de continuar azul/laranja.
const config: Config = {
  content: [
    './src/pages/**/*.{js,ts,jsx,tsx,mdx}',
    './src/components/**/*.{js,ts,jsx,tsx,mdx}',
    './src/app/**/*.{js,ts,jsx,tsx,mdx}',
  ],
  theme: {
    extend: {
      colors: {
        brand: {
          50: '#f5f3ff',
          100: '#ede9fe',
          200: '#ddd6fe',
          300: '#c4b5fd',
          400: '#a78bfa',
          500: '#8b5cf6',
          600: '#7c3aed',
          700: '#6d28d9',
          800: '#5b21b6',
          900: '#4c1d95',
          950: '#2e1065',
        },
        accent: {
          50: '#fffbeb',
          100: '#fef3c7',
          200: '#fde68a',
          300: '#fcd34d',
          400: '#fbbf24',
          500: '#f59e0b',
          600: '#d97706',
          700: '#b45309',
          800: '#92400e',
          900: '#78350f',
          950: '#451a03',
        },
        // Escala neutra do design system "Hi Mentor" (mesmos valores OKLCH do protótipo
        // Figma Make, hi-mentor/src/index.css) — usada diretamente pelas páginas recriadas
        // (bg-canvas, bg-surface, text-ink, border-line etc.).
        canvas: 'oklch(97.5% 0.005 280)',
        surface: {
          DEFAULT: 'oklch(100% 0 0)',
          2: 'oklch(98.8% 0.004 280)',
          elevated: 'oklch(98.8% 0.004 280)',
          border: 'oklch(88% 0.011 280)',
        },
        subtle: 'oklch(95.5% 0.008 280)',
        line: {
          DEFAULT: 'oklch(88% 0.011 280)',
          light: 'oklch(93.5% 0.007 280)',
        },
        ink: {
          DEFAULT: 'oklch(17% 0.02 280)',
          2: 'oklch(40% 0.018 280)',
          3: 'oklch(60% 0.012 280)',
          4: 'oklch(73% 0.008 280)',
        },
      },
      fontFamily: {
        sans: ['var(--font-manrope)', 'system-ui', '-apple-system', 'sans-serif'],
      },
      backgroundImage: {
        'gradient-brand': 'linear-gradient(135deg, #6d28d9 0%, #7c3aed 50%, #fbbf24 100%)',
        'gradient-card': 'linear-gradient(145deg, #ffffff 0%, #f8fafc 100%)',
      },
      animation: {
        'fade-in': 'fadeIn 0.4s ease-out',
        'slide-up': 'slideUp 0.4s ease-out',
      },
      keyframes: {
        fadeIn: {
          '0%': { opacity: '0' },
          '100%': { opacity: '1' },
        },
        slideUp: {
          '0%': { opacity: '0', transform: 'translateY(16px)' },
          '100%': { opacity: '1', transform: 'translateY(0)' },
        },
      },
    },
  },
  plugins: [],
};

export default config;
