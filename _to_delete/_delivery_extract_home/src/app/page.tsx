'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import {
  CheckCircle, Play, Lock, Circle, Zap, Users, BookOpen,
  MessageSquare, TrendingUp, Calendar, ArrowRight, Shield,
  BarChart2, AlertTriangle, Bell, Mail,
} from 'lucide-react';
import Logo from '@/components/brand/Logo';

/**
 * Home pública ("/") — redesign "Hi Mentor" (ago/2026), seguindo
 * hi-mentor/src/pages/LandingPage.tsx do protótipo Figma Make e o brief
 * hi-mentor-landing-page.md ("Tuilow é a empresa, Hi Mentor é o produto").
 *
 * Página inteiramente estática (sem dados de backend) — troquei os callbacks
 * onGetStarted/onLogin do protótipo por Link reais para /registro e /login, e
 * os itens de navegação do header por âncoras para seções desta mesma página.
 *
 * Princípio "nunca inventar dado" aplicado aqui de duas formas:
 * 1) Removi o bloco de prova social com números fixos ("2.400+ mentorados",
 *    "340+ mentores", "4.9★") que existia no protótipo — não há como validar
 *    esses números hoje, mesmo tratamento já dado à página de login/registro
 *    na rodada anterior do redesign.
 * 2) Vários blocos do protótipo descrevem funcionalidades que a Tuilow real
 *    ainda não tem (automação/WhatsApp, agenda de encontros ao vivo,
 *    acompanhamento granular por mentorado com "dias sem acessar", perfil de
 *    mentorados, comunidade). Por decisão explícita do usuário, essas seções
 *    foram MANTIDAS visualmente (fazem parte da visão do produto) mas
 *    marcadas com o selo <SoonBadge /> ("Em breve"), em vez de removidas ou
 *    reescritas — ver cada uso abaixo.
 */

function SoonBadge({ dark = false, className = '' }: { dark?: boolean; className?: string }) {
  return (
    <span
      className={`inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-[10px] font-bold uppercase tracking-wide shrink-0 ${
        dark ? 'bg-white/15 text-amber-300' : 'bg-amber-100 text-amber-700 border border-amber-200'
      } ${className}`}
    >
      Em breve
    </span>
  );
}

// ─── Header (fixo, ganha fundo sólido ao rolar) ───────────────────────────────

function Header() {
  const [scrolled, setScrolled] = useState(false);
  useEffect(() => {
    const fn = () => setScrolled(window.scrollY > 40);
    window.addEventListener('scroll', fn, { passive: true });
    return () => window.removeEventListener('scroll', fn);
  }, []);

  const navItems = [
    { label: 'Produto', href: '#produto' },
    { label: 'Para Mentores', href: '#para-mentores' },
    { label: 'Como funciona', href: '#como-funciona' },
    { label: 'Recursos', href: '#recursos' },
  ];

  return (
    <header
      className="fixed top-0 left-0 right-0 z-50 transition-all duration-300"
      style={{
        background: scrolled ? 'rgba(255,255,255,0.95)' : 'transparent',
        backdropFilter: scrolled ? 'blur(12px)' : 'none',
        borderBottom: scrolled ? '1px solid oklch(90% 0.01 280)' : 'none',
      }}
    >
      <div className="max-w-7xl mx-auto px-6 h-16 flex items-center gap-8">
        <Link href="/">
          <Logo size="sm" />
        </Link>
        <nav className="hidden md:flex items-center gap-1 flex-1 justify-center">
          {navItems.map((item) => (
            <Link
              key={item.label}
              href={item.href}
              className="px-4 py-2 text-sm font-semibold text-ink-2 hover:text-ink rounded-xl hover:bg-black/5 transition-all"
            >
              {item.label}
            </Link>
          ))}
        </nav>
        <div className="flex items-center gap-3 ml-auto">
          <Link
            href="/login"
            className="text-sm font-semibold text-ink-2 hover:text-ink transition-colors px-3 py-2"
          >
            Entrar
          </Link>
          <Link
            href="/registro"
            className="text-sm font-bold text-white bg-violet-600 hover:bg-violet-700 px-5 py-2.5 rounded-xl shadow-sm shadow-violet-600/30 transition-all"
          >
            Começar agora
          </Link>
        </div>
      </div>
    </header>
  );
}

// ─── Mockup do produto no Hero ─────────────────────────────────────────────────

function ProductMockup() {
  return (
    <div className="relative" style={{ width: 560, height: 400 }}>
      <div
        className="absolute inset-0 bg-surface rounded-2xl overflow-hidden shadow-2xl"
        style={{ border: '1px solid oklch(88% 0.012 280)' }}
      >
        <div className="h-9 bg-subtle border-b border-line flex items-center px-4 gap-1.5">
          <div className="w-3 h-3 rounded-full bg-red-400" />
          <div className="w-3 h-3 rounded-full bg-amber-400" />
          <div className="w-3 h-3 rounded-full bg-emerald-400" />
          <div className="flex-1 flex justify-center">
            <div className="w-40 h-4 bg-line rounded-full opacity-60" />
          </div>
        </div>

        <div className="flex h-full">
          <div className="w-44 bg-surface border-r border-line flex flex-col shrink-0">
            <div className="px-4 py-3 border-b border-line">
              <Logo size="xs" />
            </div>
            <div className="px-2 py-2 flex flex-col gap-0.5">
              {[
                { label: 'Início', active: true },
                { label: 'Programas', active: false },
                { label: 'Minha Jornada', active: false },
                { label: 'Financeiro', active: false },
              ].map((item) => (
                <div
                  key={item.label}
                  className={`flex items-center gap-2 px-3 py-1.5 rounded-lg text-[10px] font-semibold ${item.active ? 'bg-violet-50 text-violet-700' : 'text-ink-3'}`}
                >
                  <div className={`w-1.5 h-1.5 rounded-full ${item.active ? 'bg-violet-500' : 'bg-line'}`} />
                  {item.label}
                </div>
              ))}
            </div>
          </div>

          <div className="flex-1 bg-canvas p-4 overflow-hidden">
            <p className="text-[11px] font-bold text-ink mb-3">Olá 👋</p>
            <div className="grid grid-cols-2 gap-2 mb-3">
              {[
                { label: 'Mentorados', value: '48', color: 'bg-violet-50 text-violet-600' },
                { label: 'Engajamento', value: '84%', color: 'bg-emerald-50 text-emerald-600', soon: true },
                { label: 'Programas', value: '5', color: 'bg-indigo-50 text-indigo-600' },
                { label: 'Receita/mês', value: 'R$12k', color: 'bg-amber-50 text-amber-600' },
              ].map((s) => (
                <div key={s.label} className="relative bg-surface border border-line rounded-xl p-2.5">
                  {s.soon && (
                    <span className="absolute -top-1.5 -right-1.5 bg-amber-400 text-white text-[6px] font-bold px-1.5 py-0.5 rounded-full leading-none">
                      EM BREVE
                    </span>
                  )}
                  <p className="text-[9px] text-ink-3 mb-0.5">{s.label}</p>
                  <p className={`text-base font-extrabold ${s.color}`}>{s.value}</p>
                </div>
              ))}
            </div>
            <div className="bg-amber-50 border border-amber-200 rounded-xl p-2.5 flex items-center gap-2">
              <AlertTriangle size={12} className="text-amber-600 shrink-0" />
              <p className="text-[9px] font-semibold text-amber-800">8 mentorados precisam de atenção</p>
              <div className="ml-auto bg-amber-500 text-white text-[7px] font-bold px-2 py-0.5 rounded-full">Em breve</div>
            </div>
          </div>
        </div>
      </div>

      <FloatCard style={{ top: -18, right: -24 }} delay="0s" soon>
        <div className="flex items-center gap-2">
          <div className="w-7 h-7 bg-violet-100 rounded-lg flex items-center justify-center">
            <Calendar size={12} className="text-violet-600" />
          </div>
          <div>
            <p className="text-[9px] font-bold text-ink">Próximo encontro</p>
            <p className="text-[8px] text-ink-3">Hoje · 20:00</p>
          </div>
        </div>
      </FloatCard>

      <FloatCard style={{ bottom: 30, right: -32 }} delay="0.4s" soon>
        <div className="flex items-center gap-2">
          <div className="w-7 h-7 bg-emerald-100 rounded-lg flex items-center justify-center">
            <TrendingUp size={12} className="text-emerald-600" />
          </div>
          <div>
            <p className="text-[9px] font-bold text-ink">Engajamento</p>
            <p className="text-[8px] font-bold text-emerald-600">84% esta semana</p>
          </div>
        </div>
      </FloatCard>

      <FloatCard style={{ bottom: -20, left: 60 }} delay="0.8s" soon>
        <div className="flex items-center gap-2.5">
          <div className="w-7 h-7 bg-violet-600 rounded-lg flex items-center justify-center">
            <Zap size={10} className="text-white" />
          </div>
          <div>
            <p className="text-[9px] font-bold text-ink">Automação ativa</p>
            <p className="text-[8px] text-ink-3">7 dias → WhatsApp</p>
          </div>
        </div>
      </FloatCard>

      <FloatCard style={{ top: 100, left: -32 }} delay="0.2s">
        <div>
          <p className="text-[9px] font-bold text-ink mb-1">Jornada concluída</p>
          <div className="flex items-center gap-1.5">
            <div className="flex-1 bg-subtle rounded-full h-1 overflow-hidden">
              <div className="h-full bg-violet-500 rounded-full" style={{ width: '63%' }} />
            </div>
            <span className="text-[8px] font-bold text-violet-600">63%</span>
          </div>
        </div>
      </FloatCard>

      <div
        className="absolute pointer-events-none"
        style={{
          inset: '-40px',
          background: 'radial-gradient(ellipse at center, oklch(54% 0.281 293 / 0.12) 0%, transparent 70%)',
          zIndex: -1,
        }}
      />
    </div>
  );
}

function FloatCard({
  children, style, delay, soon = false,
}: { children: React.ReactNode; style: React.CSSProperties; delay: string; soon?: boolean }) {
  return (
    <div
      className="absolute bg-surface rounded-xl shadow-lg border border-line px-3 py-2.5 z-10"
      style={{ ...style, animation: 'floatCard 3s ease-in-out infinite', animationDelay: delay, minWidth: 160 }}
    >
      {soon && (
        <span className="absolute -top-2 -right-2 bg-amber-400 text-white text-[7px] font-bold px-1.5 py-0.5 rounded-full leading-none shadow-sm">
          EM BREVE
        </span>
      )}
      {children}
    </div>
  );
}

// ─── Helpers de seção ───────────────────────────────────────────────────────────

function SectionLabel({ children }: { children: React.ReactNode }) {
  return (
    <div className="inline-flex items-center gap-2 px-3 py-1.5 bg-violet-50 border border-violet-100 rounded-full mb-5">
      <div className="w-1.5 h-1.5 rounded-full bg-violet-500" />
      <span className="text-xs font-bold text-violet-600 uppercase tracking-wider">{children}</span>
    </div>
  );
}

function Headline({ children, className = '' }: { children: React.ReactNode; className?: string }) {
  return (
    <h2 className={`font-extrabold text-ink leading-tight ${className}`} style={{ letterSpacing: '-0.03em' }}>
      {children}
    </h2>
  );
}

function MenteeCard({
  name, progress, lastAccess, status,
}: { name: string; progress: number; lastAccess: string; status: 'good' | 'warning' | 'risk' }) {
  const cfg = {
    good: { label: 'Ótimo', bg: 'bg-emerald-50 border-emerald-100', badge: 'bg-emerald-500 text-white', bar: 'bg-emerald-500' },
    warning: { label: 'Acompanhar', bg: 'bg-amber-50 border-amber-100', badge: 'bg-amber-500 text-white', bar: 'bg-amber-500' },
    risk: { label: 'Atenção', bg: 'bg-red-50 border-red-100', badge: 'bg-red-500 text-white', bar: 'bg-red-400' },
  }[status];
  return (
    <div className={`bg-white border rounded-2xl p-5 flex flex-col gap-4 ${cfg.bg}`}>
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-full bg-violet-600 flex items-center justify-center text-white font-bold text-sm">
            {name[0]}
          </div>
          <div>
            <p className="font-bold text-ink text-sm">{name}</p>
            <p className="text-xs text-ink-3">{lastAccess}</p>
          </div>
        </div>
        <span className={`text-xs font-bold px-2.5 py-1 rounded-full ${cfg.badge}`}>{cfg.label}</span>
      </div>
      <div>
        <div className="flex justify-between text-xs text-ink-3 mb-1.5">
          <span>Jornada</span>
          <span className="font-bold text-ink">{progress}%</span>
        </div>
        <div className="h-1.5 bg-black/10 rounded-full overflow-hidden">
          <div className={`h-full rounded-full ${cfg.bar}`} style={{ width: `${progress}%` }} />
        </div>
      </div>
    </div>
  );
}

function AutoRule({ when, action, icon }: { when: string; action: string; icon: React.ReactNode }) {
  return (
    <div className="flex items-stretch gap-0">
      <div className="bg-surface border border-line rounded-2xl p-5 flex-1">
        <p className="text-[10px] font-bold text-ink-3 uppercase tracking-widest mb-2">Quando</p>
        <div className="flex items-center gap-2.5">
          <div className="w-8 h-8 bg-violet-50 rounded-xl flex items-center justify-center text-violet-600 shrink-0">
            {icon}
          </div>
          <p className="text-sm font-semibold text-ink leading-snug">{when}</p>
        </div>
      </div>
      <div className="flex items-center px-3 shrink-0">
        <div className="flex items-center gap-1">
          <div className="w-6 h-0.5 bg-violet-300" />
          <div className="w-2 h-2 border-r-2 border-t-2 border-violet-400 rotate-45 -ml-1.5" />
        </div>
      </div>
      <div className="bg-violet-600 rounded-2xl p-5 flex-1">
        <p className="text-[10px] font-bold text-violet-300 uppercase tracking-widest mb-2">Ação</p>
        <p className="text-sm font-semibold text-white leading-snug">{action}</p>
      </div>
    </div>
  );
}

function BentoCard({
  title, description, icon, children, className = '', dark = false, soon = false,
}: {
  title: string; description?: string; icon?: React.ReactNode; children?: React.ReactNode;
  className?: string; dark?: boolean; soon?: boolean;
}) {
  return (
    <div className={`relative rounded-2xl p-6 border overflow-hidden flex flex-col ${dark ? 'bg-violet-600 border-violet-500 text-white' : 'bg-surface border-line'} ${className}`}>
      {soon && (
        <span className={`absolute top-4 right-4 text-[9px] font-bold uppercase tracking-wide px-2 py-1 rounded-full ${dark ? 'bg-white/20 text-white' : 'bg-amber-100 text-amber-700 border border-amber-200'}`}>
          Em breve
        </span>
      )}
      {icon && (
        <div className={`w-10 h-10 rounded-xl flex items-center justify-center mb-4 shrink-0 ${dark ? 'bg-white/15' : 'bg-violet-50'}`}>
          <span className={dark ? 'text-white' : 'text-violet-600'}>{icon}</span>
        </div>
      )}
      <p className={`font-bold text-base mb-1 pr-16 ${dark ? 'text-white' : 'text-ink'}`}>{title}</p>
      {description && <p className={`text-sm leading-relaxed ${dark ? 'text-violet-200' : 'text-ink-3'}`}>{description}</p>}
      {children}
    </div>
  );
}

// ─── Página ─────────────────────────────────────────────────────────────────────

const antesDepois = [
  { before: 'Conteúdo espalhado em vários lugares', after: 'Jornada estruturada em um só ambiente', soon: false },
  { before: 'Mensagens manuais no WhatsApp', after: 'Comunicação automatizada no momento certo', soon: true },
  { before: 'Mentorado some sem sinal de alerta', after: 'Sinal de atenção antes do abandono', soon: true },
  { before: 'Comunidade solta e desconectada', after: 'Comunidade ligada à jornada', soon: true },
  { before: 'Vários sistemas e integrações frágeis', after: 'Um único ambiente integrado', soon: false },
];

const comoFunciona = [
  { num: '01', title: 'Crie seu programa', desc: 'Dê um nome, escreva a descrição e defina o modelo da sua mentoria.' },
  { num: '02', title: 'Estruture a jornada', desc: 'Organize módulos e aulas em sequência, com progressão entre etapas.' },
  { num: '03', title: 'Convide seus mentorados', desc: 'Compartilhe o link de acesso privado com quem você escolheu.' },
  { num: '04', title: 'Acompanhe a evolução', desc: 'Veja o progresso de cada mentorado dentro da jornada.' },
  { num: '05', title: 'Automatize', desc: 'Configure ações automáticas e deixe a plataforma trabalhar por você.', soon: true },
];

export default function HomePage() {
  return (
    <div className="min-h-screen">
      <style>{`
        @keyframes floatCard {
          0%, 100% { transform: translateY(0px); }
          50% { transform: translateY(-8px); }
        }
        @keyframes fadeUp {
          from { opacity: 0; transform: translateY(24px); }
          to { opacity: 1; transform: translateY(0); }
        }
      `}</style>

      <Header />

      {/* ── HERO ─────────────────────────────────────────────────────────── */}
      <section
        className="relative min-h-screen flex items-center overflow-hidden pt-16"
        style={{ background: 'oklch(98.5% 0.004 280)' }}
      >
        <div className="absolute top-0 right-0 w-[900px] h-[900px] pointer-events-none" style={{ background: 'radial-gradient(ellipse at 80% 20%, oklch(54% 0.281 293 / 0.07) 0%, transparent 60%)' }} />
        <div className="absolute bottom-0 left-0 w-[600px] h-[600px] pointer-events-none" style={{ background: 'radial-gradient(ellipse at 20% 80%, oklch(68% 0.162 75 / 0.06) 0%, transparent 60%)' }} />

        <div className="max-w-7xl mx-auto px-6 py-20 grid grid-cols-1 lg:grid-cols-2 gap-16 items-center w-full">
          <div style={{ animation: 'fadeUp 0.8s ease both' }}>
            <div className="inline-flex items-center gap-2 px-4 py-2 bg-amber-50 border border-amber-200 rounded-full mb-8">
              <div className="w-2 h-2 rounded-full bg-amber-400" />
              <span className="text-xs font-bold text-amber-700 uppercase tracking-wider">Plataforma privada de mentoria</span>
            </div>

            <h1
              className="font-extrabold text-ink leading-[1.05] mb-6"
              style={{ fontSize: 'clamp(36px, 5vw, 62px)', letterSpacing: '-0.035em' }}
            >
              Transforme sua{' '}
              <span className="text-violet-600">mentoria</span>
              {' '}em uma{' '}
              <span className="relative inline-block">
                <span className="text-violet-600">jornada</span>
                <svg className="absolute -bottom-1 left-0 w-full" viewBox="0 0 200 8" fill="none" preserveAspectRatio="none" style={{ height: 6 }}>
                  <path d="M2 6 Q50 2 100 5 Q150 8 198 4" stroke="#fbbf24" strokeWidth="3" strokeLinecap="round" fill="none" />
                </svg>
              </span>
              {' '}que{' '}
              <span className="text-violet-600">evolui</span>
              {' '}com seus mentorados.
            </h1>

            <p className="text-ink-2 text-lg leading-relaxed mb-8 max-w-lg">
              Conteúdo, acompanhamento e automação em um único ambiente criado para você conduzir seus mentorados do primeiro passo à transformação.
            </p>

            <div className="flex flex-wrap items-center gap-4 mb-8">
              <Link
                href="/registro"
                className="flex items-center gap-2.5 bg-violet-600 hover:bg-violet-700 text-white font-bold text-base px-7 py-3.5 rounded-2xl shadow-lg shadow-violet-600/30 transition-all"
              >
                Criar minha mentoria <ArrowRight size={18} />
              </Link>
              <Link href="#produto" className="flex items-center gap-2 text-ink-2 hover:text-ink font-semibold text-sm transition-colors">
                <div className="w-9 h-9 rounded-full bg-violet-100 flex items-center justify-center">
                  <Play size={14} fill="currentColor" className="text-violet-600 ml-0.5" />
                </div>
                Conhecer o Hi Mentor
              </Link>
            </div>

            <p className="text-xs text-ink-3 font-medium">Comece estruturando sua primeira jornada. Sem cartão de crédito.</p>
          </div>

          <div className="hidden lg:flex justify-end" style={{ animation: 'fadeUp 0.8s ease 0.2s both' }}>
            <ProductMockup />
          </div>
        </div>
      </section>

      {/* ── DOR ──────────────────────────────────────────────────────────── */}
      <section className="py-28 bg-white">
        <div className="max-w-6xl mx-auto px-6">
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-16 items-start">
            <div>
              <SectionLabel>O problema</SectionLabel>
              <Headline className="text-4xl md:text-5xl mb-6">
                Sua mentoria cresceu.<br />
                <span className="text-ink-3">A operação também?</span>
              </Headline>
              <p className="text-ink-2 text-lg leading-relaxed mb-8">
                Quando a mentoria escala, a desorganização escala junto. E o que era apaixonante começa a virar trabalho operacional.
              </p>
              <div className="p-6 bg-violet-600 rounded-2xl text-white">
                <p className="font-extrabold text-xl mb-1" style={{ letterSpacing: '-0.025em' }}>O Hi Mentor reúne tudo isso</p>
                <p className="text-violet-200 text-sm">em uma única jornada organizada e acompanhável.</p>
              </div>
            </div>

            <div className="space-y-0">
              {[
                { pain: 'Conteúdo espalhado em drives, PDFs e links soltos', icon: '📂' },
                { pain: 'WhatsApp cheio de mensagens e dúvidas sem controle', icon: '📱' },
                { pain: 'Mentorados desaparecendo sem sinal de alerta', icon: '👻' },
                { pain: 'Pagamentos em plataformas separadas', icon: '💳' },
                { pain: 'Sem visibilidade sobre quem está evoluindo', icon: '📊' },
              ].map((item) => (
                <div key={item.pain} className="flex items-center gap-4 py-4 border-b border-line last:border-0">
                  <span className="text-2xl shrink-0">{item.icon}</span>
                  <p className="text-ink-2 font-medium text-sm leading-snug">{item.pain}</p>
                </div>
              ))}
            </div>
          </div>
        </div>
      </section>

      {/* ── TRANSFORMAÇÃO / PRODUTO ─────────────────────────────────────── */}
      <section id="produto" className="py-28 scroll-mt-16" style={{ background: 'linear-gradient(160deg, oklch(97% 0.01 293) 0%, oklch(95% 0.02 293) 100%)' }}>
        <div className="max-w-5xl mx-auto px-6 text-center">
          <SectionLabel>Jornada completa</SectionLabel>
          <Headline className="text-4xl md:text-5xl mb-4">Do primeiro encontro à transformação.</Headline>
          <p className="text-ink-3 text-lg mb-16 max-w-xl mx-auto">O Hi Mentor acompanha cada etapa do caminho do seu mentorado.</p>

          <div className="flex flex-col md:flex-row items-center justify-center gap-0">
            {[
              { stage: 'Entrada', icon: '🚪', color: 'bg-violet-50 border-violet-200 text-violet-700' },
              { stage: 'Onboarding', icon: '🎯', color: 'bg-violet-100 border-violet-200 text-violet-700' },
              { stage: 'Conteúdo', icon: '🎬', color: 'bg-violet-200 border-violet-300 text-violet-800' },
              { stage: 'Acompanhamento', icon: '📈', color: 'bg-violet-400 border-violet-500 text-white' },
              { stage: 'Evolução', icon: '🏆', color: 'bg-violet-600 border-violet-700 text-white' },
            ].map((s, i, arr) => (
              <div key={s.stage} className="flex flex-col md:flex-row items-center">
                <div className={`flex flex-col items-center gap-2 p-4 rounded-2xl border-2 w-28 ${s.color}`}>
                  <span className="text-2xl">{s.icon}</span>
                  <span className="text-xs font-bold text-center leading-tight">{s.stage}</span>
                </div>
                {i < arr.length - 1 && (
                  <div className="w-px h-6 md:w-6 md:h-px bg-violet-300 mx-1 rotate-90 md:rotate-0" />
                )}
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* ── JORNADA ESTRUTURADA (dark) ────────────────────────────────────── */}
      <section className="py-28" style={{ background: 'linear-gradient(160deg, oklch(18% 0.12 293) 0%, oklch(24% 0.16 293) 100%)' }}>
        <div className="max-w-6xl mx-auto px-6 grid grid-cols-1 lg:grid-cols-2 gap-16 items-center">
          <div>
            <div className="inline-flex items-center gap-2 px-3 py-1.5 bg-white/10 border border-white/20 rounded-full mb-6">
              <div className="w-1.5 h-1.5 rounded-full bg-amber-400" />
              <span className="text-xs font-bold text-white/70 uppercase tracking-wider">Grande diferencial</span>
            </div>
            <h2 className="font-extrabold text-white text-4xl md:text-5xl mb-6 leading-tight" style={{ letterSpacing: '-0.03em' }}>
              Não entregue apenas conteúdo.<br />
              <span className="text-amber-400">Conduza uma jornada.</span>
            </h2>
            <div className="space-y-4 mb-8">
              {[
                'Organize módulos, conteúdos e etapas',
                'Defina progressão e desbloqueie próximos passos',
                'Acompanhe a evolução de cada mentorado',
                'O mentorado sempre sabe onde está e o que vem depois',
              ].map((t) => (
                <div key={t} className="flex items-start gap-3">
                  <CheckCircle size={16} className="text-emerald-400 shrink-0 mt-0.5" />
                  <p className="text-white/80 text-sm font-medium">{t}</p>
                </div>
              ))}
            </div>
          </div>

          <div className="bg-white/10 backdrop-blur border border-white/10 rounded-2xl p-6">
            <p className="text-white/50 text-xs font-bold uppercase tracking-widest mb-5">Liderança de Alta Performance</p>
            {[
              {
                module: 'Módulo 1 — Fundamentos', done: true,
                lessons: [
                  { title: 'Introdução ao programa', state: 'done' },
                  { title: 'Mindset do líder', state: 'done' },
                  { title: 'Autoconhecimento', state: 'done' },
                ],
              },
              {
                module: 'Módulo 2 — Proposta de Valor', done: false,
                lessons: [
                  { title: 'Definindo seu ICP', state: 'done' },
                  { title: 'Construindo valor', state: 'current' },
                  { title: 'Estratégia de preços', state: 'locked' },
                ],
              },
              {
                module: 'Módulo 3 — Estratégia', done: false,
                lessons: [
                  { title: 'Planejamento', state: 'locked' },
                  { title: 'Execução', state: 'locked' },
                ],
              },
            ].map((mod) => (
              <div key={mod.module} className="mb-4 last:mb-0">
                <div className="flex items-center gap-2 mb-2">
                  {mod.done ? <CheckCircle size={13} className="text-emerald-400" /> : <Circle size={13} className="text-white/30" />}
                  <p className="text-white/60 text-xs font-semibold">{mod.module}</p>
                </div>
                <div className="pl-5 space-y-1.5">
                  {mod.lessons.map((l) => (
                    <div key={l.title} className={`flex items-center gap-2.5 px-3 py-2 rounded-xl text-xs font-medium ${l.state === 'current' ? 'bg-violet-500/40 text-white border border-violet-400/30' : l.state === 'done' ? 'text-white/40' : 'text-white/20'}`}>
                      {l.state === 'done' ? <CheckCircle size={12} className="text-emerald-400 shrink-0" />
                        : l.state === 'current' ? <Play size={12} fill="currentColor" className="text-white shrink-0 ml-0.5" />
                        : <Lock size={11} className="shrink-0" />}
                      {l.title}
                    </div>
                  ))}
                </div>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* ── PLAYER (bem escuro) ───────────────────────────────────────────── */}
      <section className="py-28" style={{ background: 'oklch(10% 0.02 280)' }}>
        <div className="max-w-6xl mx-auto px-6">
          <div className="text-center mb-12">
            <SectionLabel>Experiência imersiva</SectionLabel>
            <h2 className="font-extrabold text-white text-4xl md:text-5xl" style={{ letterSpacing: '-0.03em' }}>
              Conteúdo que faz parte da jornada.
            </h2>
          </div>

          <div className="rounded-2xl overflow-hidden border border-white/10 shadow-2xl">
            <div className="flex" style={{ minHeight: 340 }}>
              <div className="flex-1 relative" style={{ background: 'linear-gradient(135deg, oklch(20% 0.14 293), oklch(28% 0.2 280))' }}>
                <div className="absolute inset-0 flex items-center justify-center">
                  <div className="text-center">
                    <div className="w-16 h-16 rounded-full bg-white/20 flex items-center justify-center mx-auto mb-3">
                      <Play size={24} fill="white" className="text-white ml-1" />
                    </div>
                    <p className="text-white/50 text-sm">Posicionamento de Mercado</p>
                    <p className="text-white/30 text-xs mt-1">Módulo 2 · Aula 3 · 19:40</p>
                  </div>
                </div>
                <div className="absolute bottom-0 left-0 right-0 px-4 pb-3">
                  <div className="h-0.5 bg-white/20 rounded-full mb-2 relative">
                    <div className="h-full bg-violet-400 rounded-full" style={{ width: '42%' }} />
                  </div>
                  <div className="flex items-center justify-between text-white/60 text-xs">
                    <span>7:52 / 19:40</span>
                    <span>1x velocidade</span>
                  </div>
                </div>
              </div>

              <div className="w-60 shrink-0 border-l border-white/10" style={{ background: 'oklch(14% 0.02 280)' }}>
                <div className="px-4 py-3 border-b border-white/10">
                  <p className="text-white text-xs font-bold">Sua jornada</p>
                </div>
                <div className="p-3 space-y-1">
                  {[
                    { title: 'Introdução', state: 'done' },
                    { title: 'Fundamentos', state: 'done' },
                    { title: 'Estratégia', state: 'current' },
                    { title: 'Exercício', state: 'available' },
                    { title: 'Próximo módulo', state: 'locked' },
                  ].map((l) => (
                    <div
                      key={l.title}
                      className={`flex items-center gap-2 px-3 py-2 rounded-lg text-xs font-medium ${l.state === 'current' ? 'bg-violet-600/30 text-violet-200 border border-violet-500/30' : l.state === 'done' ? 'text-white/40' : l.state === 'locked' ? 'text-white/20' : 'text-white/60'}`}
                    >
                      {l.state === 'done' ? <CheckCircle size={12} className="text-emerald-400 shrink-0" />
                        : l.state === 'current' ? <Play size={11} fill="currentColor" className="text-violet-400 shrink-0" />
                        : l.state === 'locked' ? <Lock size={11} className="shrink-0" />
                        : <Circle size={11} className="shrink-0" />}
                      {l.title}
                    </div>
                  ))}
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* ── ACOMPANHAMENTO (em breve) ────────────────────────────────────── */}
      <section className="py-28 bg-white">
        <div className="max-w-6xl mx-auto px-6">
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-16 items-center">
            <div>
              <div className="flex items-center flex-wrap gap-3">
                <SectionLabel>Acompanhamento</SectionLabel>
                <SoonBadge className="-mt-4" />
              </div>
              <Headline className="text-4xl md:text-5xl mb-5">
                Saiba quem está avançando.<br />
                <span className="text-violet-600">E quem precisa de você.</span>
              </Headline>
              <p className="text-ink-2 text-base leading-relaxed mb-6">
                O Hi Mentor vai além da visualização de vídeo: a visão em desenvolvimento é ajudar você a acompanhar pessoas — com dados que levam a ações concretas.
              </p>
              <div className="flex items-center gap-3 p-4 bg-amber-50 border border-amber-100 rounded-xl">
                <Bell size={18} className="text-amber-600 shrink-0" />
                <p className="text-sm font-semibold text-amber-800">
                  &ldquo;8 mentorados estão há mais de 7 dias sem acessar.&rdquo; — em breve
                </p>
              </div>
            </div>
            <div className="space-y-4">
              <MenteeCard name="Ana" progress={82} lastAccess="Ativa hoje" status="good" />
              <MenteeCard name="Carlos" progress={47} lastAccess="Último acesso há 3 dias" status="warning" />
              <MenteeCard name="Marina" progress={31} lastAccess="7 dias sem acessar" status="risk" />
            </div>
          </div>
        </div>
      </section>

      {/* ── AUTOMAÇÃO (em breve) ─────────────────────────────────────────── */}
      <section className="py-28" style={{ background: 'oklch(97.5% 0.005 280)' }}>
        <div className="max-w-5xl mx-auto px-6">
          <div className="text-center mb-14">
            <div className="inline-flex items-center flex-wrap justify-center gap-3">
              <SectionLabel>Automação inteligente</SectionLabel>
              <SoonBadge className="-mt-4" />
            </div>
            <Headline className="text-4xl md:text-5xl mb-4">
              Esteja presente mesmo<br />quando não estiver online.
            </Headline>
            <p className="text-ink-3 text-lg max-w-lg mx-auto">
              Transforme comportamento em ações de acompanhamento. Automaticamente.
            </p>
          </div>
          <div className="space-y-4">
            <AutoRule icon={<Zap size={16} />} when="Mentorado ficar 7 dias sem acessar" action="Enviar mensagem personalizada pelo WhatsApp" />
            <AutoRule icon={<Calendar size={16} />} when="Faltar 2 horas para o encontro ao vivo" action="Enviar lembrete com link de acesso" />
            <AutoRule icon={<CheckCircle size={16} />} when="Mentorado concluir uma etapa da jornada" action="Parabenizar e liberar o próximo passo" />
          </div>
        </div>
      </section>

      {/* ── BENTO GRID / RECURSOS ────────────────────────────────────────── */}
      <section id="recursos" className="py-28 bg-white scroll-mt-16">
        <div className="max-w-6xl mx-auto px-6">
          <div className="text-center mb-14">
            <SectionLabel>Tudo em um lugar</SectionLabel>
            <Headline className="text-4xl md:text-5xl mb-4">Uma plataforma. Um ecossistema.</Headline>
            <p className="text-ink-3 text-lg max-w-xl mx-auto">Sem integrações frágeis, sem sistemas dispersos — e alguns recursos abaixo ainda estão a caminho.</p>
          </div>

          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            <BentoCard className="md:col-span-2 md:row-span-2" title="Jornadas estruturadas" description="Crie programas com módulos, aulas e exercícios. Defina progressão e pré-requisitos." icon={<BookOpen size={20} />} dark>
              <div className="mt-4 space-y-1.5">
                {['Módulo 1 ✓', 'Módulo 2 ▶', 'Módulo 3 🔒'].map((m) => (
                  <div key={m} className="text-xs text-violet-200 bg-white/10 px-3 py-2 rounded-lg font-medium">{m}</div>
                ))}
              </div>
            </BentoCard>

            <BentoCard title="Conteúdo e streaming" description="Vídeo em alta qualidade, protegido, com progresso salvo automaticamente." icon={<Play size={20} />} />
            <BentoCard title="Encontros ao vivo" description="Agende e acompanhe sessões em grupo ou individuais." icon={<Calendar size={20} />} soon />

            <BentoCard title="Mentorados" description="Perfil, progresso e histórico de cada mentorado." icon={<Users size={20} />} soon />
            <BentoCard title="Comunidade" description="Conectada à jornada, não solta." icon={<MessageSquare size={20} />} soon />

            <BentoCard className="md:col-span-2" title="Pagamentos & financeiro" description="Gerencie o recebimento das suas mentorias sem sair da plataforma." icon={<Shield size={20} />} />

            <BentoCard title="Comunicação" description="E-mail, WhatsApp e notificações." icon={<Mail size={20} />} soon />
            <BentoCard title="Automações" description="Regras de reengajamento e lembretes." icon={<Zap size={20} />} dark soon />
            <BentoCard title="Engajamento" description="Métricas de acesso e frequência." icon={<TrendingUp size={20} />} soon />
            <BentoCard title="Acompanhamento" description="Saiba quem evoluiu e quem precisa de atenção." icon={<BarChart2 size={20} />} soon />
          </div>
        </div>
      </section>

      {/* ── COMO FUNCIONA ────────────────────────────────────────────────── */}
      <section id="como-funciona" className="py-28 scroll-mt-16" style={{ background: 'linear-gradient(160deg, oklch(97% 0.01 293) 0%, oklch(95.5% 0.018 293) 100%)' }}>
        <div className="max-w-5xl mx-auto px-6">
          <div className="text-center mb-16">
            <SectionLabel>Como funciona</SectionLabel>
            <Headline className="text-4xl md:text-5xl">Comece em minutos. Escale com segurança.</Headline>
          </div>
          <div className="flex flex-col md:flex-row gap-0 relative">
            <div className="hidden md:block absolute top-8 left-0 right-0 h-0.5 bg-violet-100 mx-16" />
            {comoFunciona.map((step) => (
              <div key={step.num} className="flex-1 flex flex-col items-center text-center px-4 mb-8 md:mb-0 relative">
                <div className="w-16 h-16 rounded-2xl bg-violet-600 flex items-center justify-center text-white font-extrabold text-lg mb-4 shadow-lg shadow-violet-600/30 z-10">
                  {step.num}
                </div>
                <p className="font-bold text-ink text-sm mb-2">{step.title}</p>
                <p className="text-ink-3 text-xs leading-relaxed mb-2">{step.desc}</p>
                {step.soon && <SoonBadge />}
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* ── PARA QUEM ────────────────────────────────────────────────────── */}
      <section id="para-mentores" className="py-28 bg-white scroll-mt-16">
        <div className="max-w-5xl mx-auto px-6 text-center">
          <SectionLabel>Para quem é</SectionLabel>
          <Headline className="text-4xl md:text-5xl mb-4">
            Feito para quem transforma<br />conhecimento em acompanhamento.
          </Headline>
          <p className="text-ink-3 text-lg mb-12 max-w-xl mx-auto">Não é uma plataforma para vender curso. É uma plataforma para conduzir pessoas.</p>

          <div className="flex flex-wrap justify-center gap-3">
            {['Mentores', 'Especialistas', 'Consultores', 'Coaches', 'Creators com programas premium', 'Comunidades de desenvolvimento', 'Masterminds', 'Formadores de líderes'].map((tag) => (
              <div key={tag} className="px-5 py-2.5 bg-subtle border border-line rounded-full text-sm font-semibold text-ink-2 hover:border-violet-300 hover:text-violet-700 hover:bg-violet-50 transition-all">
                {tag}
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* ── ANTES / DEPOIS ───────────────────────────────────────────────── */}
      <section className="py-28" style={{ background: 'linear-gradient(135deg, oklch(36% 0.2 293) 0%, oklch(24% 0.16 293) 100%)' }}>
        <div className="max-w-5xl mx-auto px-6">
          <div className="text-center mb-14">
            <h2 className="font-extrabold text-white text-4xl md:text-5xl" style={{ letterSpacing: '-0.03em' }}>
              Menos operação.<br />
              <span className="text-amber-400">Mais acompanhamento.</span>
            </h2>
          </div>

          <div className="space-y-3">
            {antesDepois.map((row) => (
              <div key={row.before} className="grid grid-cols-2 gap-3">
                <div className="bg-white/5 border border-white/10 rounded-2xl px-5 py-4 flex items-center gap-3">
                  <div className="w-5 h-5 rounded-full bg-red-500/30 flex items-center justify-center shrink-0">
                    <div className="w-2 h-0.5 bg-red-400 rounded-full" />
                  </div>
                  <p className="text-white/50 text-sm font-medium line-through decoration-red-400/50">{row.before}</p>
                </div>
                <div className="bg-white/10 border border-white/20 rounded-2xl px-5 py-4 flex items-center gap-3">
                  <CheckCircle size={18} className="text-emerald-400 shrink-0" />
                  <p className="text-white font-semibold text-sm flex-1">{row.after}</p>
                  {row.soon && <SoonBadge dark />}
                </div>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* ── CTA FINAL ────────────────────────────────────────────────────── */}
      <section
        className="relative py-32 overflow-hidden"
        style={{ background: 'linear-gradient(145deg, oklch(20% 0.15 293) 0%, oklch(30% 0.22 293) 50%, oklch(22% 0.18 280) 100%)' }}
      >
        <div className="absolute inset-0 pointer-events-none">
          <div className="absolute top-[-80px] right-[-80px] w-96 h-96 rounded-full opacity-20" style={{ background: 'radial-gradient(circle, oklch(80% 0.15 75) 0%, transparent 70%)' }} />
          <div className="absolute bottom-[-60px] left-[-60px] w-80 h-80 rounded-full opacity-10" style={{ background: 'radial-gradient(circle, oklch(75% 0.15 75) 0%, transparent 70%)' }} />
          <svg className="absolute inset-0 w-full h-full opacity-[0.04]" xmlns="http://www.w3.org/2000/svg">
            <defs>
              <pattern id="ctag" width="40" height="40" patternUnits="userSpaceOnUse">
                <path d="M 40 0 L 0 0 0 40" fill="none" stroke="white" strokeWidth="1" />
              </pattern>
            </defs>
            <rect width="100%" height="100%" fill="url(#ctag)" />
          </svg>
        </div>

        <div className="relative max-w-3xl mx-auto px-6 text-center">
          <div className="flex justify-center mb-8">
            <Logo size="lg" variant="full" onDark />
          </div>

          <h2 className="font-extrabold text-white mb-5 leading-tight" style={{ fontSize: 'clamp(30px, 4vw, 52px)', letterSpacing: '-0.035em' }}>
            Sua mentoria já transforma pessoas.<br />
            <span className="text-amber-400">Agora dê a ela uma estrutura à altura.</span>
          </h2>

          <p className="text-violet-200 text-lg leading-relaxed mb-10 max-w-xl mx-auto">
            Crie uma experiência privada, acompanhe cada jornada e tenha tudo que precisa para fazer sua mentoria crescer.
          </p>

          <div className="flex flex-wrap items-center justify-center gap-4">
            <Link
              href="/registro"
              className="flex items-center gap-2.5 bg-amber-400 hover:bg-amber-300 text-ink font-extrabold text-base px-8 py-4 rounded-2xl shadow-lg shadow-black/20 transition-all"
            >
              Criar minha mentoria <ArrowRight size={18} />
            </Link>
            <Link href="#produto" className="text-white/70 hover:text-white font-semibold text-sm transition-colors underline underline-offset-4">
              Conhecer a plataforma
            </Link>
          </div>

          <p className="text-violet-400 text-xs mt-8">Sem cartão de crédito · Setup em minutos</p>
        </div>
      </section>

      {/* ── FOOTER ───────────────────────────────────────────────────────── */}
      <footer style={{ background: 'oklch(12% 0.02 280)' }}>
        <div className="max-w-6xl mx-auto px-6 py-16">
          <div className="grid grid-cols-2 md:grid-cols-5 gap-8 mb-12">
            <div className="col-span-2">
              <Logo size="sm" variant="full" onDark />
              <p className="text-white/40 text-sm mt-3 max-w-xs leading-relaxed">
                Plataforma privada para mentores criarem, operarem e escalarem seus programas de mentoria.
              </p>
              <p className="text-white/25 text-xs mt-3">Um produto da Tuilow</p>
            </div>
            {[
              { title: 'Produto', items: ['Recursos', 'Para Mentores', 'Como funciona'] },
              { title: 'Empresa', items: ['Sobre', 'Contato'] },
              { title: 'Legal', items: ['Privacidade', 'Termos'] },
            ].map((col) => (
              <div key={col.title}>
                <p className="text-white/50 text-xs font-bold uppercase tracking-wider mb-4">{col.title}</p>
                <ul className="space-y-2.5">
                  {col.items.map((item) => (
                    <li key={item}>
                      <a href="#" className="text-white/40 hover:text-white/70 text-sm transition-colors">{item}</a>
                    </li>
                  ))}
                </ul>
              </div>
            ))}
          </div>

          <div className="border-t border-white/10 pt-8 flex flex-col md:flex-row items-center justify-between gap-4">
            <p className="text-white/30 text-xs">© {new Date().getFullYear()} Tuilow. Todos os direitos reservados.</p>
          </div>
        </div>
      </footer>
    </div>
  );
}
