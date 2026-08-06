'use client';

import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { Check, X, Pencil } from 'lucide-react';
import { adminFinanceApi } from '@/lib/api';
import type { AdminCreatorAsaasAccountItem } from '@/lib/api';

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

/**
 * Painel do dono da plataforma — "Financeiro". Lista as contas Asaas que cada Criador conectou
 * para vender no modelo de marketplace com split (ver Finance.Api.AdminFinanceController). O
 * dono da Tuilow pode habilitar/desabilitar a venda de um Criador específico e definir uma
 * comissão diferenciada por Criador (sobrepõe o percentual padrão da plataforma). Nenhuma API
 * Key é exibida aqui — só os dados mascarados que o backend já retorna prontos.
 */
export default function PlataformaFinanceiroPage() {
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
    <div className="max-w-6xl mx-auto">
      <div className="mb-8">
        <h1 className="text-2xl font-bold text-gray-800">Financeiro — Contas Asaas dos Criadores</h1>
        <p className="text-gray-500 text-sm mt-1">
          Contas Asaas conectadas pelos Criadores para vender no modelo de marketplace com split.
          A cobrança é criada na conta do próprio Criador; a Tuilow recebe apenas a comissão.
        </p>
      </div>

      {isLoading && <p className="text-gray-400 text-sm">Carregando contas...</p>}
      {!!error && <p className="text-red-500 text-sm">Erro ao carregar as contas Asaas.</p>}

      {!isLoading && (accounts?.length ?? 0) === 0 && (
        <div className="card text-center py-16">
          <p className="text-4xl mb-3">💳</p>
          <p className="font-semibold text-gray-800 mb-1">Nenhuma conta conectada ainda</p>
          <p className="text-gray-500 text-sm">
            Quando um Criador conectar sua conta Asaas em Financeiro, ela aparece aqui.
          </p>
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
