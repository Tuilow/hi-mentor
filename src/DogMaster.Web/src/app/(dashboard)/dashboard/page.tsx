'use client';

import { useQuery } from '@tanstack/react-query';
import { authApi, subscriptionsApi } from '@/lib/api';
import Link from 'next/link';

const quickActions = [
  { href: '/cursos',    icon: '📚', title: 'Explorar Cursos',  desc: 'Encontre o curso ideal para o seu cão',  color: 'from-brand-600/20 to-purple-600/10' },
  { href: '/cachorros', icon: '🐕', title: 'Meus Cães',        desc: 'Gerencie o perfil dos seus cães',         color: 'from-emerald-600/20 to-teal-600/10' },
  { href: '/assinatura',icon: '💎', title: 'Assinatura',       desc: 'Gerencie seu plano atual',               color: 'from-pink-600/20 to-rose-600/10' },
];

export default function DashboardPage() {
  const { data: user } = useQuery({
    queryKey: ['user-profile'],
    queryFn: () => authApi.me().then(r => r.data),
  });

  const { data: subscription } = useQuery({
    queryKey: ['my-subscription'],
    queryFn: () => subscriptionsApi.getMySubscription().then(r => r.data).catch(() => null),
  });

  return (
    <div className="max-w-4xl mx-auto">
      {/* Header */}
      <div className="mb-8">
        <h1 className="text-2xl font-bold text-zinc-100">
          Olá, {user?.firstName ?? '...'}! 👋
        </h1>
        <p className="text-zinc-400 mt-1">Continue de onde parou ou explore novos cursos.</p>
      </div>

      {/* Subscription banner */}
      {!subscription ? (
        <div className="card border-brand-800/60 bg-brand-950/40 mb-8 flex items-center justify-between gap-4">
          <div>
            <p className="font-semibold text-zinc-100">Você não tem assinatura ativa</p>
            <p className="text-sm text-zinc-400 mt-0.5">Acesse todos os cursos por um preço fixo mensal.</p>
          </div>
          <Link href="/assinatura" className="btn-primary whitespace-nowrap flex-shrink-0">
            Ver Planos
          </Link>
        </div>
      ) : (
        <div className="card border-emerald-800/40 bg-emerald-950/30 mb-8 flex items-center gap-3">
          <span className="text-emerald-400 text-xl">✓</span>
          <div>
            <p className="font-semibold text-zinc-100">Plano <strong>{subscription.planName}</strong> ativo</p>
            <p className="text-sm text-zinc-400 mt-0.5">
              Válido até {new Date(subscription.currentPeriodEnd).toLocaleDateString('pt-BR')}
            </p>
          </div>
        </div>
      )}

      {/* Stats */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-8">
        {[
          { label: 'Cursos',      value: '—', sub: 'matriculados' },
          { label: 'Aulas',       value: '—', sub: 'concluídas' },
          { label: 'Progresso',   value: '—', sub: 'médio' },
          { label: 'Cães',        value: '—', sub: 'cadastrados' },
        ].map(s => (
          <div key={s.label} className="card border-zinc-800 text-center">
            <p className="text-2xl font-bold gradient-text">{s.value}</p>
            <p className="text-xs text-zinc-500 mt-1">{s.sub}</p>
          </div>
        ))}
      </div>

      {/* Quick actions */}
      <h2 className="text-sm font-semibold text-zinc-500 uppercase tracking-wider mb-4">Acesso rápido</h2>
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        {quickActions.map(action => (
          <Link
            key={action.href}
            href={action.href}
            className={`card border-zinc-800 hover:border-zinc-700 bg-gradient-to-br ${action.color}
                        transition-all duration-200 group cursor-pointer`}
          >
            <div className="text-2xl mb-3 group-hover:scale-110 transition-transform duration-200">
              {action.icon}
            </div>
            <h3 className="font-semibold text-zinc-100 group-hover:text-white">{action.title}</h3>
            <p className="text-xs text-zinc-500 mt-1">{action.desc}</p>
          </Link>
        ))}
      </div>
    </div>
  );
}
