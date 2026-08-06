'use client';

import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { ShieldCheck, ExternalLink } from 'lucide-react';
import { financeAsaasAccountApi } from '@/lib/api';
import type { CreatorAsaasAccountStatus } from '@/lib/api';

const statusLabel: Record<CreatorAsaasAccountStatus['status'], string> = {
  NotConnected: 'Não conectado',
  PendingValidation: 'Validando com o Asaas...',
  Active: 'Conectado e ativo',
  Restricted: 'Restrito pelo Asaas',
  Rejected: 'Conexão rejeitada',
  Disabled: 'Desativado pela Tuilow',
};

const statusColor: Record<CreatorAsaasAccountStatus['status'], string> = {
  NotConnected: 'badge bg-gray-100 text-gray-500 border border-gray-200',
  PendingValidation: 'badge-yellow',
  Active: 'badge-green',
  Restricted: 'badge bg-orange-50 text-orange-700 border border-orange-200',
  Rejected: 'badge bg-red-50 text-red-700 border border-red-200',
  Disabled: 'badge bg-gray-100 text-gray-500 border border-gray-200',
};

/**
 * Tela "Financeiro" do Criador — conectar a própria conta Asaas para vender no modelo de
 * marketplace com split de pagamentos. A cobrança das vendas passa a ser criada diretamente na
 * conta Asaas do próprio Criador (ele é o vendedor legal); a Tuilow recebe apenas sua comissão
 * via Split, automaticamente, no momento do recebimento. O saldo restante já fica na conta do
 * Criador — não existe repasse manual neste modelo (ver Finance.Api.CreatorAsaasAccountController).
 *
 * A API Key informada aqui nunca é reexibida depois de salva (fica criptografada no backend via
 * Data Protection e só é usada internamente pela Tuilow para criar cobranças em nome do Criador).
 */
export default function FinanceiroCriadorPage() {
  const queryClient = useQueryClient();

  const { data: status, isLoading } = useQuery<CreatorAsaasAccountStatus>({
    queryKey: ['creator-asaas-status'],
    queryFn: () => financeAsaasAccountApi.getStatus().then(r => r.data),
  });

  const [apiKey, setApiKey] = useState('');
  const [cpfCnpj, setCpfCnpj] = useState('');
  const [legalName, setLegalName] = useState('');

  const connectMutation = useMutation({
    mutationFn: () => financeAsaasAccountApi.connect({
      apiKey,
      cpfCnpj: cpfCnpj || undefined,
      legalName: legalName || undefined,
    }),
    onSuccess: () => {
      toast.success('Conta Asaas conectada! Estamos validando os dados com o Asaas.');
      setApiKey('');
      queryClient.invalidateQueries({ queryKey: ['creator-asaas-status'] });
    },
    onError: (err: unknown) => {
      const message = (err as { response?: { data?: { message?: string; detail?: string } } })
        ?.response?.data;
      toast.error(message?.detail ?? message?.message ?? 'Não foi possível conectar a conta Asaas.');
    },
  });

  const isConnectedOrPending = status?.status === 'Active' || status?.status === 'PendingValidation';

  if (isLoading) {
    return <p className="text-gray-400 text-sm">Carregando...</p>;
  }

  return (
    <div className="max-w-2xl">
      <div className="flex items-center justify-between mb-1">
        <h1 className="text-2xl font-bold text-gray-800">Financeiro</h1>
      </div>
      <p className="text-sm text-gray-500 mb-6">
        Conecte sua própria conta Asaas para receber diretamente pelas suas vendas. A Tuilow
        recebe apenas a comissão da plataforma, automaticamente, via split — o restante já cai
        direto na sua conta Asaas.
      </p>

      {status && (
        <div className="card border-gray-200 mb-6">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-sm font-semibold text-gray-700">Status da conexão</h2>
            <span className={statusColor[status.status]}>{statusLabel[status.status]}</span>
          </div>

          <div className="grid grid-cols-2 gap-4 text-sm">
            <div>
              <p className="text-gray-400 text-xs mb-0.5">Pode vender?</p>
              <p className="font-medium text-gray-800">{status.canSell ? 'Sim' : 'Não'}</p>
            </div>
            <div>
              <p className="text-gray-400 text-xs mb-0.5">Wallet ID</p>
              <p className="font-medium text-gray-800">{status.walletId ?? '—'}</p>
            </div>
            <div>
              <p className="text-gray-400 text-xs mb-0.5">Comissão diferenciada</p>
              <p className="font-medium text-gray-800">
                {status.commissionOverridePercentage != null
                  ? `${status.commissionOverridePercentage}%`
                  : 'Padrão da plataforma'}
              </p>
            </div>
            <div>
              <p className="text-gray-400 text-xs mb-0.5">Última validação</p>
              <p className="font-medium text-gray-800">
                {status.lastValidatedAt
                  ? new Date(status.lastValidatedAt).toLocaleString('pt-BR')
                  : '—'}
              </p>
            </div>
          </div>

          {status.lastValidationError && (
            <div className="mt-4 p-3 rounded-lg bg-red-50 border border-red-200 text-red-700 text-xs">
              {status.lastValidationError}
            </div>
          )}
        </div>
      )}

      <div className="card border-gray-200 space-y-5">
        <div className="flex items-start gap-2 p-3 rounded-lg bg-brand-50 border border-brand-200 text-brand-700 text-xs">
          <ShieldCheck className="w-4 h-4 flex-shrink-0 mt-0.5" />
          <p>
            Sua API Key é armazenada de forma criptografada e nunca é exibida novamente após
            salvar. Ela é usada apenas para criar cobranças em seu nome dentro da sua própria
            conta Asaas.
          </p>
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1.5">API Key do Asaas</label>
          <input
            type="password"
            value={apiKey}
            onChange={e => setApiKey(e.target.value)}
            placeholder="$aact_..."
            className="w-full px-3 py-2 border border-gray-200 rounded-lg text-sm
                       focus:outline-none focus:ring-2 focus:ring-brand-300"
          />
          <p className="text-xs text-gray-400 mt-1">
            Gerada no painel do Asaas em Integrações → API. Sua conta pode ser pessoa física (CPF)
            ou jurídica (CNPJ).
          </p>
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1.5">CPF/CNPJ (opcional)</label>
          <input
            value={cpfCnpj}
            onChange={e => setCpfCnpj(e.target.value)}
            placeholder="000.000.000-00"
            className="w-full px-3 py-2 border border-gray-200 rounded-lg text-sm
                       focus:outline-none focus:ring-2 focus:ring-brand-300"
          />
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1.5">Nome/Razão social (opcional)</label>
          <input
            value={legalName}
            onChange={e => setLegalName(e.target.value)}
            placeholder="Como consta na sua conta Asaas"
            className="w-full px-3 py-2 border border-gray-200 rounded-lg text-sm
                       focus:outline-none focus:ring-2 focus:ring-brand-300"
          />
        </div>

        <button
          onClick={() => connectMutation.mutate()}
          disabled={!apiKey || connectMutation.isPending}
          className="btn-primary w-full py-2.5 disabled:opacity-50"
        >
          {connectMutation.isPending
            ? 'Conectando...'
            : isConnectedOrPending
              ? 'Reconectar / atualizar API Key'
              : 'Conectar conta Asaas'}
        </button>

        <a
          href="https://ajuda.asaas.com/pt-BR/articles/1595635-onde-encontro-minha-chave-de-api"
          target="_blank"
          rel="noopener noreferrer"
          className="flex items-center gap-1.5 text-xs text-brand-600 hover:underline justify-center"
        >
          Como encontrar minha API Key no Asaas <ExternalLink className="w-3.5 h-3.5" />
        </a>
      </div>
    </div>
  );
}
