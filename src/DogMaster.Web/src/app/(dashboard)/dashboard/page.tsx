'use client';

import { useQuery } from '@tanstack/react-query';
import { authApi, subscriptionsApi, enrollmentsApi } from '@/lib/api';
import Link from 'next/link';

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
    <div className="max-w-5xl mx-auto">
      <h1 className="text-2xl font-bold text-gray-900 mb-2">
        Olá, {user?.firstName}! 👋
      </h1>
      <p className="text-gray-500 mb-8">Continue de onde parou ou explore novos cursos.</p>

      {/* Status assinatura */}
      {!subscription && (
        <div className="bg-brand-50 border border-brand-200 rounded-xl p-4 mb-8 flex items-center justify-between">
          <div>
            <p className="font-semibold text-brand-800">Você não tem assinatura ativa</p>
            <p className="text-sm text-brand-600">Acesse todos os cursos por um preço fixo mensal.</p>
          </div>
          <Link href="/assinatura" className="btn-primary whitespace-nowrap">
            Ver Planos
          </Link>
        </div>
      )}

      {subscription && (
        <div className="bg-green-50 border border-green-200 rounded-xl p-4 mb-8">
          <p className="font-semibold text-green-800">
            Plano {subscription.planName} ativo até{' '}
            {new Date(subscription.currentPeriodEnd).toLocaleDateString('pt-BR')}
          </p>
        </div>
      )}

      {/* Quick Actions */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        {[
          { href: '/cursos', icon: '📚', title: 'Explorar Cursos', desc: 'Encontre o curso ideal para o seu cão' },
          { href: '/cachorros', icon: '🐕', title: 'Meus Cães', desc: 'Gerencie o perfil dos seus cães' },
          { href: '/assinatura', icon: '💎', title: 'Assinatura', desc: 'Gerencie seu plano atual' },
        ].map((action) => (
          <Link key={action.href} href={action.href}
            className="card hover:shadow-md transition-shadow cursor-pointer group">
            <div className="text-3xl mb-3">{action.icon}</div>
            <h3 className="font-semibold text-gray-900 group-hover:text-brand-700">{action.title}</h3>
            <p className="text-sm text-gray-500 mt-1">{action.desc}</p>
          </Link>
        ))}
      </div>
    </div>
  );
}
