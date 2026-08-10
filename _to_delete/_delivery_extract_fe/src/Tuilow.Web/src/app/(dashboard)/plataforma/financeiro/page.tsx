'use client';

import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { Check, X, Pencil, Lock, Unlock } from 'lucide-react';
import { adminFinanceApi } from '@/lib/api';
import type { AdminCreatorAsaasAccountItem, AdminCreatorOnboardingItem } from '@/lib/api';

const statusLabel: Record<AdminCreatorAsaasAccountItem['status'], string> = {
  NotConnected: 'Não conectado',
  PendingValidation: 'Validando',
  Active: 'Ativo',
  Restricted: 'Restrito',
  Rejected: 'Rejeitado',
  Disabled: 'Desativado',
};

const statusColor: Record<AdminCreatorAsaasAccountItem['status'], string> = {
  NotConnected: 'badge bg-gray-100 text-gray-500 border border-gray-200',
  PendingValidation: 'badge-yellow',
  Active: 'badge-green',
  Restricted: 'badge bg-orange-50 text-orange-700 border border-orange-200',
  Rejected: 'badge bg-red-50 text-red-700 border border-red-200',
  Disabled: 'badge bg-gray-100 text-gray-500 border border-gray-200',
};

// Onboarding financeiro via subconta Asaas/BaaS (novo modelo) -- rótulos/cores em cima do enum
// bruto que o backend devolve (CreatorOnboardingStatus.ToString()), nunca um código da Asaas.
const onboardingStatusLabel: Record<string, string> = {
  NotStarted: 'Não iniciado',
  CollectingData: 'Preenchendo dados',
  AccountCreationPending: 'Criando conta',
  AccountCreated: 'Conta criada',
  DocumentsPending: 'Documentos pendentes',
  UnderReview: 'Em análise',
  Approved: 'Aprovado',
  Rejected: 'Rejeitado',
  Blocked: 'Bloqueado',
};

const onboardingStatusColor: Record<string, string> = {
  NotStarted: 'badge bg-gray-100 text-gray-500 border border-gray-200',
  CollectingData: 'badge bg-gray-100 text-gray-500 border border-gray-200',
  AccountCreationPending: 'badge-yellow',
  AccountCreated: 'badge-yellow',
  DocumentsPending: 'badge-yellow',
  UnderReview: 'badge-yellow',
  Approved: 'badge-green',
  Rejected: 'badge bg-red-50 text-red-700 border border-red-200',
  Blocked: 'badge bg-red-50 text-red-700 border border-red-200',
};

/**
 * Painel do dono da plataforma — "Financeiro". Duas abas: o pipeline NOVO de onboarding
 * financeiro via subconta Asaas/BaaS criada pela própria Tuilow (ver
 * Finance.Api.Controllers.AdminFinanceController, endpoints /onboardings) e a listagem LEGADA de
 * contas Asaas que criadores conectaram colando sua própria API Key (modelo anterior, mantido só
 * por auditoria/histórico -- criadores novos passam a usar exclusivamente o pipeline novo). Nenhuma
 * API Key é exibida em nenhuma das duas abas — só os dados mascarados que o backend já retorna prontos.
 */
export default function PlataformaFinanceiroPage() {
  const [tab, setTab] = useState<'onboarding' | 'legacy'>('onboarding');

  return (
    <div className="max-w-6xl mx-auto">
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-800">Financeiro</h1>
        <p className="text-gray-500 text-sm mt-1">
          Acompanhe o onboarding financeiro dos criadores e, para referência histórica, as contas
          Asaas conectadas pelo modelo antigo.
        </p>
      </div>

      <div className="flex items-center gap-1 mb-6 border-b border-gray-100">
        <button
          onClick={() => setTab('onboarding')}
          className={
            'px-4 py-2 text-sm font-medium border-b-2 -mb-px ' +
            (tab === 'onboarding' ? 'border-brand-500 text-brand-600' : 'border-transparent text-gray-400 hover:text-gray-600')
          }
        >
          Onboarding financeiro
        </button>
        <button
          onClick={() => setTab('legacy')}
          className={
            'px-4 py-2 text-sm font-medium border-b-2 -mb-px ' +
            (tab === 'legacy' ? 'border-brand-500 text-brand-600' : 'border-transparent text-gray-400 hover:text-gray-600')
          }
        >
          Contas conectadas (modelo antigo)
        </button>
      </div>

      {tab === 'onboarding' ? <OnboardingsTab /> : <LegacyAccountsTab />}
    </div>
  );
}

function OnboardingsTab() {
  const queryClient = useQueryClient();

  const { data: onboardings, isLoading, error } = useQuery<AdminCreatorOnboardingItem[]>({
    queryKey: ['admin-financial-onboardings'],
    queryFn: () => adminFinanceApi.listOnboardings({ skip: 0, take: 200 }).then(r => r.data),
  });

  const setBlockedMutation = useMutation({
    mutationFn: ({ id, blocked }: { id: string; blocked: boolean }) =>
      adminFinanceApi.setOnboardingBlocked(id, blocked, blocked ? 'Bloqueado manualmente pelo admin.' : undefined),
    onSuccess: (_, vars) => {
      toast.success(vars.blocked ? 'Criador bloqueado.' : 'Criador desbloqueado.');
      queryClient.invalidateQueries({ queryKey: ['admin-financial-onboardings'] });
    },
    onError: () => toast.error('Não foi possível atualizar o bloqueio.'),
  });

  if (isLoading) return <p className="text-gray-400 text-sm">Carregando onboardings...</p>;
  if (error) return <p className="text-red-500 text-sm">Erro ao carregar o pipeline de onboarding.</p>;

  if (!onboardings?.length) {
    return (
      <div className="card text-center py-16">
        <p className="text-4xl mb-3">💰</p>
        <p className="font-semibold text-gray-800 mb-1">Nenhum onboarding iniciado ainda</p>
        <p className="text-gray-500 text-sm">
          Quando um criador começar a configurar os recebimentos em Financeiro, ele aparece aqui.
        </p>
      </div>
    );
  }

  return (
    <div className="card overflow-x-auto">
      <table className="w-full text-sm">
        <thead>
          <tr className="text-left text-gray-400 text-xs uppercase tracking-wider border-b border-gray-100">
            <th className="pb-3 pr-4 font-medium">Criador</th>
            <th className="pb-3 pr-4 font-medium">Status</th>
            <th className="pb-3 pr-4 font-medium">CPF/CNPJ</th>
            <th className="pb-3 pr-4 font-medium">Documentos pendentes</th>
            <th className="pb-3 pr-4 font-medium">Aprovado em</th>
            <th className="pb-3 font-medium">Ação</th>
          </tr>
        </thead>
        <tbody>
          {onboardings.map(item => {
            const isBlocked = item.status === 'Blocked';
            return (
              <tr key={item.id} className="border-b border-gray-50 last:border-0 hover:bg-gray-50/60 align-top">
                <td className="py-3 pr-4">
                  <p className="font-medium text-gray-800">{item.creatorName}</p>
                  <p className="text-gray-400 text-xs">{item.creatorEmail}</p>
                </td>
                <td className="py-3 pr-4">
                  <span className={onboardingStatusColor[item.status] ?? 'badge bg-gray-100 text-gray-500 border border-gray-200'}>
                    {onboardingStatusLabel[item.status] ?? item.status}
                  </span>
                  {item.rejectionReason && (
                    <p className="text-red-500 text-xs mt-1 max-w-[220px]">{item.rejectionReason}</p>
                  )}
                </td>
                <td className="py-3 pr-4 text-gray-500 font-mono text-xs">{item.cpfCnpjMasked ?? '—'}</td>
                <td className="py-3 pr-4 text-gray-700">{item.pendingDocumentsCount}</td>
                <td className="py-3 pr-4 text-gray-500 text-xs">
                  {item.approvedAt ? new Date(item.approvedAt).toLocaleString('pt-BR') : '—'}
                </td>
                <td className="py-3">
                  <button
                    onClick={() => setBlockedMutation.mutate({ id: item.id, blocked: !isBlocked })}
                    disabled={setBlockedMutation.isPending}
                    className={
                      'flex items-center gap-1.5 text-xs px-2.5 py-1.5 rounded-lg border ' +
                      (isBlocked
                        ? 'border-green-200 text-green-700 hover:bg-green-50'
                        : 'border-red-200 text-red-700 hover:bg-red-50')
                    }
                  >
                    {isBlocked ? <Unlock className="w-3.5 h-3.5" /> : <Lock className="w-3.5 h-3.5" />}
                    {isBlocked ? 'Desbloquear' : 'Bloquear'}
                  </button>
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}

function LegacyAccountsTab() {
  const queryClient = useQueryClient();

  const { data: accounts, isLoading, error } = useQuery<AdminCreatorAsaasAccountItem[]>({
    queryKey: ['admin-asaas-accounts'],
    queryFn: () => adminFinanceApi.listAsaasAccounts({ skip: 0, take: 200 }).then(r => r.data),
  });

  const [editingCommissionId, setEditingCommissionId] = useState<string | null>(null);
  const [commissionInput, setCommissionInput] = useState('');

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ['admin-asaas-accounts'] });

  const setEnabledMutation = useMutation({
    mutationFn: ({ id, enabled }: { id: string; enabled: boolean }) =>
      adminFinanceApi.setAsaasAccountEnabled(id, enabled),
    onSuccess: (_, vars) => {
      toast.success(vars.enabled ? 'Venda habilitada.' : 'Venda desabilitada.');
      invalidate();
    },
    onError: () => toast.error('Não foi possível atualizar a conta.'),
  });

  const setCommissionMutation = useMutation({
    mutationFn: ({ id, percentage }: { id: string; percentage: number | null }) =>
      adminFinanceApi.setCommissionOverride(id, percentage),
    onSuccess: () => {
      toast.success('Comissão atualizada.');
      setEditingCommissionId(null);
      invalidate();
    },
    onError: () => toast.error('Não foi possível atualizar a comissão.'),
  });

  const startEditCommission = (account: AdminCreatorAsaasAccountItem) => {
    setEditingCommissionId(account.id);
    setCommissionInput(account.commissionOverridePercentage?.toString() ?? '');
  };

  const saveCommission = (id: string) => {
    const trimmed = commissionInput.trim();
    const percentage = trimmed === '' ? null : Number(trimmed.replace(',', '.'));
    if (percentage != null && (Number.isNaN(percentage) || percentage < 0 || percentage > 100)) {
      toast.error('Informe um percentual entre 0 e 100, ou deixe em branco para usar o padrão.');
      return;
    }
    setCommissionMutation.mutate({ id, percentage });
  };

  return (
    <div>
      <p className="text-gray-500 text-sm mb-4">
        Contas Asaas conectadas pelos Criadores no modelo antigo (colando a própria API Key) para
        vender via marketplace com split. Mantido só para referência histórica — criadores novos
        usam exclusivamente a aba &quot;Onboarding financeiro&quot;.
      </p>

      {isLoading && <p className="text-gray-400 text-sm">Carregando contas...</p>}
      {!!error && <p className="text-red-500 text-sm">Erro ao carregar as contas Asaas.</p>}

      {!isLoading && (accounts?.length ?? 0) === 0 && (
        <div className="card text-center py-16">
          <p className="text-4xl mb-3">💳</p>
          <p className="font-semibold text-gray-800 mb-1">Nenhuma conta conectada</p>
          <p className="text-gray-500 text-sm">Nenhum criador se conectou pelo modelo antigo.</p>
        </div>
      )}

      {!!accounts?.length && (
        <div className="card overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="text-left text-gray-400 text-xs uppercase tracking-wider border-b border-gray-100">
                <th className="pb-3 pr-4 font-medium">Criador</th>
                <th className="pb-3 pr-4 font-medium">Status</th>
                <th className="pb-3 pr-4 font-medium">Wallet</th>
                <th className="pb-3 pr-4 font-medium">CPF/CNPJ</th>
                <th className="pb-3 pr-4 font-medium">Comissão</th>
                <th className="pb-3 pr-4 font-medium">Vendendo?</th>
                <th className="pb-3 font-medium">Último webhook</th>
              </tr>
            </thead>
            <tbody>
              {accounts.map(account => (
                <tr key={account.id} className="border-b border-gray-50 last:border-0 hover:bg-gray-50/60 align-top">
                  <td className="py-3 pr-4">
                    <p className="font-medium text-gray-800">{account.creatorName}</p>
                    <p className="text-gray-400 text-xs">{account.creatorEmail}</p>
                  </td>
                  <td className="py-3 pr-4">
                    <span className={statusColor[account.status]}>{statusLabel[account.status]}</span>
                    {account.lastValidationError && (
                      <p className="text-red-500 text-xs mt-1 max-w-[220px]">{account.lastValidationError}</p>
                    )}
                  </td>
                  <td className="py-3 pr-4 text-gray-500 font-mono text-xs">
                    {account.walletIdMasked ?? '—'}
                  </td>
                  <td className="py-3 pr-4 text-gray-500 font-mono text-xs">
                    {account.cpfCnpjMasked ?? '—'}
                  </td>
                  <td className="py-3 pr-4">
                    {editingCommissionId === account.id ? (
                      <div className="flex items-center gap-1.5">
                        <input
                          value={commissionInput}
                          onChange={e => setCommissionInput(e.target.value)}
                          placeholder="Padrão"
                          className="w-16 px-2 py-1 border border-gray-200 rounded-lg text-xs
                                     focus:outline-none focus:ring-2 focus:ring-brand-300"
                        />
                        <button
                          onClick={() => saveCommission(account.id)}
                          disabled={setCommissionMutation.isPending}
                          className="p-1 rounded text-green-600 hover:bg-green-50"
                        >
                          <Check className="w-3.5 h-3.5" />
                        </button>
                        <button
                          onClick={() => setEditingCommissionId(null)}
                          className="p-1 rounded text-gray-400 hover:bg-gray-100"
                        >
                          <X className="w-3.5 h-3.5" />
                        </button>
                      </div>
                    ) : (
                      <button
                        onClick={() => startEditCommission(account)}
                        className="flex items-center gap-1.5 text-gray-700 hover:text-brand-600"
                      >
                        {account.commissionOverridePercentage != null
                          ? `${account.commissionOverridePercentage}%`
                          : <span className="text-gray-400">Padrão</span>}
                        <Pencil className="w-3 h-3 text-gray-300" />
                      </button>
                    )}
                  </td>
                  <td className="py-3 pr-4">
                    <button
                      onClick={() => setEnabledMutation.mutate({ id: account.id, enabled: !account.isEnabledForSelling })}
                      disabled={setEnabledMutation.isPending}
                      className={
                        account.isEnabledForSelling
                          ? 'badge-green'
                          : 'badge bg-gray-100 text-gray-500 border border-gray-200'
                      }
                    >
                      {account.isEnabledForSelling ? 'Sim' : 'Não'}
                    </button>
                  </td>
                  <td className="py-3 text-gray-500 text-xs">
                    {account.lastWebhookReceivedAt
                      ? new Date(account.lastWebhookReceivedAt).toLocaleString('pt-BR')
                      : '—'}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
