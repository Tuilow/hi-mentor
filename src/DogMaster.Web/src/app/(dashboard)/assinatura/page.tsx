'use client';

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { subscriptionsApi } from '@/lib/api';
import { Plan, Subscription } from '@/types';
import toast from 'react-hot-toast';

const cycleLabel: Record<string, string> = {
  Monthly: '/mês',
  Quarterly: '/trimestre',
  Annual: '/ano',
};

export default function AssinaturaPage() {
  const qc = useQueryClient();

  const { data: plans = [], isLoading: loadingPlans } = useQuery<Plan[]>({
    queryKey: ['plans'],
    queryFn: () => subscriptionsApi.getPlans().then(r => r.data),
  });

  const { data: subscription } = useQuery<Subscription | null>({
    queryKey: ['my-subscription'],
    queryFn: () => subscriptionsApi.getMySubscription().then(r => r.data).catch(() => null),
  });

  const cancel = useMutation({
    mutationFn: () => subscriptionsApi.cancel('Solicitado pelo usuário'),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['my-subscription'] });
      toast.success('Assinatura cancelada. Acesso disponível até o fim do período.');
    },
    onError: () => toast.error('Erro ao cancelar assinatura.'),
  });

  const subscribe = useMutation({
    mutationFn: (planId: string) => subscriptionsApi.subscribe({
      planId,
      customerName: 'Usuário',
      customerEmail: 'usuario@email.com',
    }),
    onSuccess: (res) => {
      toast.success('Redirecionando para pagamento...');
      if (res.data?.paymentUrl) window.open(res.data.paymentUrl, '_blank');
    },
    onError: () => toast.error('Erro ao iniciar assinatura.'),
  });

  return (
    <div className="max-w-3xl mx-auto">
      <div className="mb-8">
        <h1 className="text-2xl font-bold text-zinc-100">Assinatura</h1>
        <p className="text-zinc-400 mt-1">Escolha o plano ideal para você e seu cão.</p>
      </div>

      {/* Plano ativo */}
      {subscription && (
        <div className="card border-emerald-800/40 bg-emerald-950/30 mb-8">
          <div className="flex items-start justify-between gap-4">
            <div>
              <div className="flex items-center gap-2 mb-1">
                <span className="badge-green">Ativo</span>
                <h2 className="font-semibold text-zinc-100">Plano {subscription.planName}</h2>
              </div>
              <p className="text-sm text-zinc-400">
                Válido até {new Date(subscription.currentPeriodEnd).toLocaleDateString('pt-BR')}
              </p>
            </div>
            <button
              onClick={() => cancel.mutate()}
              disabled={cancel.isPending}
              className="text-sm text-red-400 hover:text-red-300 font-medium transition-colors whitespace-nowrap"
            >
              {cancel.isPending ? 'Cancelando...' : 'Cancelar'}
            </button>
          </div>
        </div>
      )}

      {/* Planos */}
      {loadingPlans ? (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
          {[1,2].map(i => (
            <div key={i} className="card border-zinc-800 animate-pulse h-64" />
          ))}
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
          {plans.map((plan) => (
            <div key={plan.id}
              className="card border-zinc-700 hover:border-brand-700/60 transition-all duration-200 relative flex flex-col">
              <h3 className="text-xl font-bold text-zinc-100 mb-1">{plan.name}</h3>
              {plan.description && (
                <p className="text-sm text-zinc-400 mb-4">{plan.description}</p>
              )}

              <div className="my-4">
                <span className="text-4xl font-extrabold gradient-text">
                  R$ {plan.price.toFixed(2).replace('.', ',')}
                </span>
                <span className="text-sm text-zinc-500 ml-1">
                  {cycleLabel[plan.billingCycle]}
                </span>
              </div>

              {plan.trialDays > 0 && (
                <p className="badge-green mb-4 self-start">
                  {plan.trialDays} dias grátis
                </p>
              )}

              <ul className="space-y-2 mb-6 flex-1">
                {plan.features.map((f) => (
                  <li key={f.featureKey} className="flex items-start gap-2 text-sm text-zinc-300">
                    <span className="text-brand-400 mt-0.5">✓</span>
                    {f.displayName ?? f.featureKey}
                  </li>
                ))}
              </ul>

              <button
                onClick={() => subscribe.mutate(plan.id)}
                disabled={subscribe.isPending || !!subscription}
                className="btn-primary w-full mt-auto"
              >
                {subscription ? 'Plano já ativo' : 'Assinar agora'}
              </button>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
