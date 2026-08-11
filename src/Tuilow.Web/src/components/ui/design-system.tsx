'use client';

// Design system "Hi Mentor" — portado quase verbatim de hi-mentor/src/components/ui.tsx
// (protótipo Figma Make), adaptado para Tailwind v3 (sintaxe de classes idêntica) e para
// rodar como Client Component do Next.js App Router. Reutilizado pelas páginas recriadas
// nesta rodada de redesign (ago/2026) — ver RELATORIO_REDESIGN_HI_MENTOR.md.

import { type ReactNode, type ButtonHTMLAttributes, type InputHTMLAttributes, forwardRef } from 'react';

// ─── Button ───────────────────────────────────────────────────────────────────

type BtnVariant = 'primary' | 'secondary' | 'ghost' | 'danger' | 'warning';
type BtnSize = 'xs' | 'sm' | 'md' | 'lg';

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: BtnVariant;
  size?: BtnSize;
  loading?: boolean;
  icon?: ReactNode;
  children: ReactNode;
}

const btnVariant: Record<BtnVariant, string> = {
  primary: 'bg-violet-600 text-white hover:bg-violet-700 shadow-sm shadow-violet-600/25',
  secondary: 'bg-surface border border-line text-ink hover:bg-subtle',
  ghost: 'text-ink-2 hover:bg-subtle hover:text-ink',
  danger: 'bg-red-600 text-white hover:bg-red-700',
  warning: 'bg-amber-500 text-white hover:bg-amber-600',
};
const btnSize: Record<BtnSize, string> = {
  xs: 'px-2.5 py-1 text-xs gap-1 rounded-lg',
  sm: 'px-3 py-1.5 text-sm gap-1.5 rounded-xl',
  md: 'px-4 py-2 text-sm gap-2 rounded-xl',
  lg: 'px-6 py-3 text-base gap-2.5 rounded-2xl',
};

export function Button({
  variant = 'primary',
  size = 'md',
  loading,
  icon,
  children,
  className = '',
  disabled,
  ...props
}: ButtonProps) {
  return (
    <button
      className={`inline-flex items-center justify-center font-semibold transition-all duration-150 cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-violet-500 focus-visible:outline-offset-2 ${btnVariant[variant]} ${btnSize[size]} ${className}`}
      disabled={disabled || loading}
      {...props}
    >
      {loading ? (
        <span className="w-4 h-4 border-2 border-current border-t-transparent rounded-full animate-spin shrink-0" />
      ) : icon ? (
        <span className="shrink-0">{icon}</span>
      ) : null}
      {children}
    </button>
  );
}

// ─── Badge ────────────────────────────────────────────────────────────────────

type BadgeVariant = 'active' | 'draft' | 'done' | 'risk' | 'warning' | 'neutral';

interface BadgeProps {
  variant?: BadgeVariant;
  children: ReactNode;
  dot?: boolean;
  className?: string;
}

const badgeStyles: Record<BadgeVariant, string> = {
  active: 'bg-emerald-50 text-emerald-700 border border-emerald-200',
  draft: 'bg-subtle text-ink-3 border border-line',
  done: 'bg-violet-50 text-violet-700 border border-violet-200',
  risk: 'bg-red-50 text-red-700 border border-red-200',
  warning: 'bg-amber-50 text-amber-700 border border-amber-200',
  neutral: 'bg-subtle text-ink-2 border border-line',
};

const dotStyles: Record<BadgeVariant, string> = {
  active: 'bg-emerald-500',
  draft: 'bg-ink-4',
  done: 'bg-violet-500',
  risk: 'bg-red-500',
  warning: 'bg-amber-500',
  neutral: 'bg-ink-3',
};

export function Badge({ variant = 'neutral', children, dot, className = '' }: BadgeProps) {
  return (
    <span className={`inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-semibold ${badgeStyles[variant]} ${className}`}>
      {dot && <span className={`w-1.5 h-1.5 rounded-full shrink-0 ${dotStyles[variant]}`} />}
      {children}
    </span>
  );
}

// ─── Avatar ───────────────────────────────────────────────────────────────────

interface AvatarProps {
  name: string;
  src?: string;
  size?: 'xs' | 'sm' | 'md' | 'lg';
}

const avatarSize = {
  xs: 'w-6 h-6 text-xs',
  sm: 'w-8 h-8 text-sm',
  md: 'w-10 h-10 text-base',
  lg: 'w-12 h-12 text-lg',
};

const avatarColors = [
  'bg-violet-600',
  'bg-indigo-600',
  'bg-blue-600',
  'bg-emerald-600',
  'bg-amber-600',
  'bg-rose-600',
];

export function Avatar({ name, src, size = 'md' }: AvatarProps) {
  const initial = (name.charAt(0) || '?').toUpperCase();
  const colorIdx = name.charCodeAt(0) % avatarColors.length;
  if (src) {
    // eslint-disable-next-line @next/next/no-img-element
    return <img src={src} alt={name} className={`${avatarSize[size]} rounded-full object-cover`} />;
  }
  return (
    <div className={`${avatarSize[size]} ${avatarColors[colorIdx] ?? avatarColors[0]} rounded-full flex items-center justify-center text-white font-bold shrink-0`}>
      {initial}
    </div>
  );
}

// ─── Progress ─────────────────────────────────────────────────────────────────

interface ProgressProps {
  value: number;
  variant?: 'primary' | 'success' | 'warning';
  size?: 'xs' | 'sm' | 'md';
  showLabel?: boolean;
}

const progressBar: Record<string, string> = {
  primary: 'bg-violet-600',
  success: 'bg-emerald-500',
  warning: 'bg-amber-500',
};

const progressHeight: Record<string, string> = {
  xs: 'h-1',
  sm: 'h-1.5',
  md: 'h-2',
};

export function Progress({ value, variant = 'primary', size = 'sm', showLabel }: ProgressProps) {
  const pct = Math.min(100, Math.max(0, value));
  return (
    <div className="flex items-center gap-2 w-full">
      <div className={`flex-1 bg-subtle rounded-full overflow-hidden ${progressHeight[size]}`}>
        <div
          className={`h-full rounded-full transition-all duration-500 ${progressBar[variant]}`}
          style={{ width: `${pct}%` }}
        />
      </div>
      {showLabel && <span className="text-xs font-semibold text-ink-3 shrink-0">{Math.round(pct)}%</span>}
    </div>
  );
}

// ─── Input ────────────────────────────────────────────────────────────────────

interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  label?: string;
  error?: string;
  hint?: string;
  icon?: ReactNode;
}

export const Input = forwardRef<HTMLInputElement, InputProps>(
  ({ label, error, hint, icon, className = '', ...props }, ref) => {
    return (
      <div className="flex flex-col gap-1.5">
        {label && (
          <label className="text-sm font-semibold text-ink-2">
            {label}
          </label>
        )}
        <div className="relative">
          {icon && (
            <div className="absolute left-3 top-1/2 -translate-y-1/2 text-ink-3">
              {icon}
            </div>
          )}
          <input
            ref={ref}
            className={`w-full bg-surface border rounded-xl px-4 py-2.5 text-sm text-ink placeholder:text-ink-4 transition-all duration-150
              focus:outline-none focus:ring-2 focus:ring-violet-500/30 focus:border-violet-400
              disabled:opacity-50 disabled:cursor-not-allowed
              ${error ? 'border-red-400 focus:ring-red-500/20 focus:border-red-400' : 'border-line hover:border-ink-4'}
              ${icon ? 'pl-10' : ''}
              ${className}`}
            {...props}
          />
        </div>
        {hint && !error && <p className="text-xs text-ink-3">{hint}</p>}
        {error && <p className="text-xs text-red-600 font-medium">{error}</p>}
      </div>
    );
  }
);
Input.displayName = 'Input';

// ─── Card ─────────────────────────────────────────────────────────────────────

interface CardProps {
  children: ReactNode;
  className?: string;
  onClick?: () => void;
  hover?: boolean;
  padding?: 'none' | 'sm' | 'md' | 'lg';
}

const cardPadding = { none: '', sm: 'p-4', md: 'p-5', lg: 'p-6' };

export function Card({ children, className = '', onClick, hover, padding = 'md' }: CardProps) {
  return (
    <div
      onClick={onClick}
      className={`bg-surface border border-line rounded-2xl shadow-sm shadow-black/[0.04] ${cardPadding[padding]} ${hover ? 'hover:shadow-md hover:border-line-light transition-all duration-200 cursor-pointer' : ''} ${className}`}
    >
      {children}
    </div>
  );
}

// ─── Stat Card ────────────────────────────────────────────────────────────────

interface StatCardProps {
  label: string;
  value: string;
  icon: ReactNode;
  trend?: { value: string; positive: boolean };
  iconBg?: string;
}

export function StatCard({ label, value, icon, trend, iconBg = 'bg-violet-50' }: StatCardProps) {
  return (
    <Card className="flex items-start gap-4">
      <div className={`w-11 h-11 rounded-xl ${iconBg} flex items-center justify-center shrink-0`}>
        {icon}
      </div>
      <div className="flex-1 min-w-0">
        <p className="text-sm text-ink-3 font-medium mb-0.5">{label}</p>
        <p className="text-2xl font-bold text-ink tracking-tight">{value}</p>
        {trend && (
          <p className={`text-xs font-semibold mt-1 ${trend.positive ? 'text-emerald-600' : 'text-red-500'}`}>
            {trend.positive ? '↑' : '↓'} {trend.value}
          </p>
        )}
      </div>
    </Card>
  );
}

// ─── Section Header ───────────────────────────────────────────────────────────

interface SectionHeaderProps {
  title: string;
  action?: { label: string; onClick: () => void };
}

export function SectionHeader({ title, action }: SectionHeaderProps) {
  return (
    <div className="flex items-center justify-between mb-4">
      <h2 className="text-base font-bold text-ink">{title}</h2>
      {action && (
        <button onClick={action.onClick} className="text-sm font-semibold text-violet-600 hover:text-violet-700 transition-colors cursor-pointer">
          {action.label}
        </button>
      )}
    </div>
  );
}

// ─── Empty State ──────────────────────────────────────────────────────────────

interface EmptyStateProps {
  icon?: ReactNode;
  title: string;
  description: string;
  action?: { label: string; onClick: () => void };
}

export function EmptyState({ icon, title, description, action }: EmptyStateProps) {
  return (
    <div className="flex flex-col items-center justify-center py-16 px-8 text-center">
      {icon && (
        <div className="w-16 h-16 bg-subtle rounded-2xl flex items-center justify-center mb-5 text-ink-3">
          {icon}
        </div>
      )}
      <h3 className="text-lg font-bold text-ink mb-2">{title}</h3>
      <p className="text-sm text-ink-3 max-w-xs leading-relaxed mb-6">{description}</p>
      {action && (
        <Button onClick={action.onClick}>{action.label}</Button>
      )}
    </div>
  );
}

// ─── Divider ──────────────────────────────────────────────────────────────────

export function Divider({ className = '' }: { className?: string }) {
  return <hr className={`border-0 border-t border-line ${className}`} />;
}
