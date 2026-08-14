// Logo "Hi Mentor" — portado de hi-mentor/src/components/Logo.tsx (protótipo Figma Make).
// SVG puro, sem dependências — funciona tanto em Server quanto em Client Components.
//
// Yellow  #fbbf24  oklch(68.1% 0.162 75.834)
// Purple  #7c3aed  oklch(54.1% 0.281 293.009)

const YELLOW = '#fbbf24';
const PURPLE = '#7c3aed';

// ─── Símbolo "Hi" (elemento SVG isolado) ──────────────────────────────────────

export function HiSymbol({
  size,
  color = YELLOW,
}: {
  size: number;
  color?: string;
}) {
  return (
    <svg
      width={size}
      height={Math.round((size * 60) / 52)}
      viewBox="0 0 52 60"
      fill="none"
      aria-hidden="true"
      style={{ flexShrink: 0, display: 'block' }}
    >
      <path
        d="M11,9 L11,51 M11,31 L38,31 L38,51"
        stroke={color}
        strokeWidth="14"
        strokeLinecap="round"
        strokeLinejoin="round"
        fill="none"
      />
      <circle cx="38" cy="9" r="8.5" fill={color} />
    </svg>
  );
}

// ─── Logo completa (símbolo + wordmark) ───────────────────────────────────────

type Size = 'xs' | 'sm' | 'md' | 'lg' | 'xl';

const cfg: Record<Size, { sym: number; fs: string; fw: number; gap: string }> = {
  xs: { sym: 20, fs: '12px', fw: 800, gap: '5px' },
  sm: { sym: 26, fs: '15px', fw: 800, gap: '7px' },
  md: { sym: 34, fs: '19px', fw: 800, gap: '9px' },
  lg: { sym: 44, fs: '25px', fw: 800, gap: '11px' },
  xl: { sym: 64, fs: '36px', fw: 800, gap: '16px' },
};

type LogoProps = {
  size?: Size;
  /** 'full' mostra símbolo + wordmark "Mentor"; 'icon' mostra só o símbolo */
  variant?: 'full' | 'icon';
  /** Quando true, renderiza sobre fundo escuro: símbolo amarelo + wordmark branco */
  onDark?: boolean;
};

export default function Logo({ size = 'md', variant = 'full', onDark = false }: LogoProps) {
  const c = cfg[size];
  const symbolColor = onDark ? '#ffffff' : YELLOW;
  const wordmarkColor = onDark ? '#ffffff' : PURPLE;

  return (
    <div
      role="img"
      aria-label="Hi Mentor"
      style={{
        display: 'inline-flex',
        alignItems: 'center',
        gap: c.gap,
        lineHeight: 1,
        userSelect: 'none',
      }}
    >
      <HiSymbol size={c.sym} color={symbolColor} />
      {variant === 'full' && (
        <span
          style={{
            color: wordmarkColor,
            fontSize: c.fs,
            fontWeight: c.fw,
            fontFamily: 'var(--font-manrope), system-ui, sans-serif',
            letterSpacing: '-0.025em',
            lineHeight: 1,
          }}
        >
          Mentor
        </span>
      )}
    </div>
  );
}

// ─── Ícone de app (quadrado roxo + "Hi" amarelo) ──────────────────────────────

export function LogoAppIcon({ size = 40 }: { size?: number }) {
  const r = Math.round((size * 13) / 60);
  return (
    <svg width={size} height={size} viewBox="0 0 60 60" fill="none" aria-label="Hi Mentor">
      <rect width="60" height="60" rx={r} fill={PURPLE} />
      <g transform="translate(4.5 1.5) scale(0.95)">
        <path
          d="M11,9 L11,51 M11,31 L38,31 L38,51"
          stroke={YELLOW}
          strokeWidth="14"
          strokeLinecap="round"
          strokeLinejoin="round"
          fill="none"
        />
        <circle cx="38" cy="9" r="8.5" fill={YELLOW} />
      </g>
    </svg>
  );
}
