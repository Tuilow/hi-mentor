'use client';

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { subscriptionsApi } from '@/lib/api';
import { Plan, Subscription } from '@/types';
import toast from 'react-hot-toast';

export default function AssinaturaPage() {
  const qc = useQueryClient();

  const { data: plans = [] } = useQuery<Plan[]>({
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
      toast.success('Assinatura cancelada. Você tem acesso até o fim do período.');
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
      // Redirecionar para URL de pagamento do Asaas se disponível
      window.open(res.data?.paymentUrl ?? '#', '_blank');
    },
    onError: () => toast.error('Erro ao iniciar assinatura.'),
  });

  const cycleLabel: Record<string, string> = {
    Monthly: '/mês',
    Quarterly: '/trimestre',
    Annual: '/ano',
  };

  return (
    <div className="max-w-3xl mx-auto">
      <h1 className="text-2xl font-bold text-gray-900 mb-2">Assinatura</h1>
      <p className="text-gray-500 mb-8">Escolha o plano ideal para você e seu cão.</p>

      {subscription && (
        <div className="card bg-green-50 border-green-200 mb-8">
          <div className="flex justify-between items-start">
            <div>
              <h2 className="font-semibold text-green-800">Plano {subscription.planName}</h2>
              <p className="text-sm text-green-600 mt-1">
                Status: <strong>{subscription.status}</strong> · Válido até{' '}
                {new Date(subscription.currentPeriodEnd).toLocaleDateString('pt-BR')}
              </p>
            </div>
            <button onClick={() => cancel.mutate()} disabled={cancel.isPending}
              className="text-sm text-red-500 hover:text-red-700 font-medium">
              {cancel.isPending ? 'Cancelando...' : 'Cancelar assinatura'}
            </button>
          </div>
        </div>
      )}

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {plans.map((plan) => (
          <div key={plan.id} className="card relative">
            <h3 className="text-xl font-bold text-gray-900">{plan.name}</h3>
            {plan.description && <p className="text-gray-500 text-sm mt-1 mb-4">{plan.description}</p>}
            <p className="text-3xl font-bold text-brand-700 my-4">
              R$ {plan.price.toFixed(2).replace('.', ',')}
              <span className="text-sm text-gray-500 font-normal">{cycleLabel[plan.billingCycle]}</span>
            </p>
            {plan.trialDays > 0 && (
              <p className="text-sm text-green-600 mb-4">{plan.trialDays} dias grátis para começar</p>
            )}
            <ul className="space-y-2 mb-6">
              {plan.features.map((f) => (
                <li key={f.featureKey} className="flex items-center gap-2 text-sm text-gray-700">
                  <span className="text-green-500">✓</span>
                  {f.displayName ?? f.featureKey}
                </li>
              ))}
            </ul>
            <button
              onClick={() => subscribe.mutate(plan.id)}
              disabled={subscribe.isPending || !!subscription}
              className="btn-primary w-full">
              {subscription ? 'Plano já ativo' : 'Assinar agora'}
            </button>
          </div>
        ))}
      </div>
    </div>
  );
}
