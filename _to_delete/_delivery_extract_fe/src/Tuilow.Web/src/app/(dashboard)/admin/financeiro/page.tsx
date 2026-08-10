'use client';

import { useEffect, useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { CheckCircle2, Circle, Clock, ExternalLink, ShieldCheck, Upload, AlertTriangle } from 'lucide-react';
import { financialOnboardingApi } from '@/lib/api';
import type { CreatorFinancialOnboardingStatus, OnboardingStep, OnboardingDocument, StartFinancialOnboardingData } from '@/lib/api';

const COMPANY_TYPES: { value: string; label: string }[] = [
  { value: 'MEI', label: 'MEI (Microempreendedor Individual)' },
  { value: 'LIMITED', label: 'Limitada (LTDA)' },
  { value: 'INDIVIDUAL', label: 'Empresário Individual' },
  { value: 'ASSOCIATION', label: 'Associação / ONG' },
];

const onlyDigits = (value: string) => value.replace(/\D/g, '');

const inputClassName =
  'w-full px-3 py-2 border border-gray-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-300';

/**
 * "Financeiro -> Configurar recebimentos" do criador — jornada de onboarding financeiro via
 * subconta Asaas (BaaS) criada automaticamente pela própria Tuilow. Substitui a antiga tela de
 * "cole sua API Key aqui": o criador só informa dados pessoais/empresariais e envia documentos
 * quando pedido — a Tuilow cuida de criar a conta na Asaas, registrar o webhook e acompanhar a
 * aprovação. Em NENHUM momento esta tela menciona "API Key", "Wallet ID" ou "subconta" — só o
 * essencial para o criador entender em que etapa está e o que falta.
 *
 * Nota sobre a rota: esta página fica em (dashboard)/admin/financeiro por convenção já existente
 * neste repo -- a área "Administração" do menu do Criador usa o prefixo /admin (ver comentário em
 * (dashboard)/layout.tsx), distinta da área "Financeiro" do DONO da plataforma, que fica em
 * /plataforma/financeiro (ver AdminOnboardingsTab nessa outra página).
 */
export default function FinanceiroCriadorPage() {
  const queryClient = useQueryClient();

  const { data: status, isLoading } = useQuery<CreatorFinancialOnboardingStatus>({
    queryKey: ['financial-onboarding-status'],
    queryFn: () => financialOnboardingApi.getStatus().then(r => r.data),
    // Enquanto a conta está sendo criada na Asaas (ou os documentos ainda não voltaram da
    // análise), o status pode mudar sem nenhuma ação do criador nesta aba — reconsulta sozinho.
    refetchInterval: query => {
      const s = query.state.data?.status;
      return s === 'AccountCreationPending' || s === 'UnderReview' ? 8000 : false;
    },
  });

  const invalidateStatus = () => queryClient.invalidateQueries({ queryKey: ['financial-onboarding-status'] });

  if (isLoading) {
    return <p className="text-gray-400 text-sm">Carregando...</p>;
  }

  return (
    <div className="max-w-2xl">
      <div className="mb-1">
        <h1 className="text-2xl font-bold text-gray-800">Financeiro</h1>
      </div>
      <p className="text-sm text-gray-500 mb-6">
        Configure seus recebimentos para poder publicar e vender seus cursos. Leva só alguns
        minutos — cuidamos de toda a parte técnica pra você.
      </p>

      {status && <StepIndicator steps={status.steps} />}

      {status?.friendlyMessage && (
        <div className="my-6 flex items-start gap-2 p-4 rounded-lg bg-orange-50 border border-orange-200 text-orange-800 text-sm">
          <AlertTriangle className="w-4 h-4 flex-shrink-0 mt-0.5" />
          <p>{status.friendlyMessage}</p>
        </div>
      )}

      <div className="mt-6">
        {/* O status bruto do backend (não o array de steps, que só alimenta os chips acima)
            decide o que renderizar -- ver CreatorOnboardingStatus no backend para a lista
            completa de valores possíveis. */}
        {status?.canSell ? (
          <ReadyToSellCard />
        ) : status?.status === 'NotStarted' || status?.status === 'CollectingData' ? (
          <DataStep previousData={status?.previousData ?? null} onSubmitted={invalidateStatus} />
        ) : status?.status === 'AccountCreationPending' ? (
          <ProcessingCard />
        ) : status?.status === 'AccountCreated' || status?.status === 'DocumentsPending' ? (
          <DocumentsStep onAllSubmitted={invalidateStatus} />
        ) : status?.status === 'UnderReview' ? (
          <UnderReviewCard />
        ) : status?.status === 'Rejected' ? (
          // A subconta já existe na Asaas nesse ponto -- não há mais formulário de dados pra
          // reenviar (ver StartCollectingData no backend, guardado por AsaasAccountId != null);
          // o caminho real de correção é reenviar os documentos pendentes/rejeitados.
          <DocumentsStep onAllSubmitted={invalidateStatus} />
        ) : status?.status === 'Blocked' ? (
          <BlockedCard />
        ) : (
          <ProcessingCard />
        )}
      </div>
    </div>
  );
}

function StepIndicator({ steps }: { steps: OnboardingStep[] }) {
  return (
    <div className="flex items-center gap-1.5 overflow-x-auto pb-1">
      {steps.map((step, idx) => (
        <div key={step.key} className="flex items-center gap-1.5 flex-shrink-0">
          <div
            className={
              'flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium ' +
              (step.state === 'done'
                ? 'bg-green-50 text-green-700 border border-green-200'
                : step.state === 'current'
                  ? 'bg-brand-50 text-brand-700 border border-brand-300'
                  : step.state === 'blocked'
                    ? 'bg-red-50 text-red-700 border border-red-200'
                    : 'bg-gray-50 text-gray-400 border border-gray-200')
            }
          >
            {step.state === 'done' ? (
              <CheckCircle2 className="w-3.5 h-3.5" />
            ) : step.state === 'current' ? (
              <Clock className="w-3.5 h-3.5" />
            ) : (
              <Circle className="w-3.5 h-3.5" />
            )}
            {step.title}
          </div>
          {idx < steps.length - 1 && <div className="w-3 h-px bg-gray-200" />}
        </div>
      ))}
    </div>
  );
}

function ReadyToSellCard() {
  return (
    <div className="card border-green-200 bg-green-50/40 text-center py-10">
      <CheckCircle2 className="w-10 h-10 text-green-600 mx-auto mb-3" />
      <p className="font-semibold text-gray-800 mb-1">Sua conta financeira está pronta!</p>
      <p className="text-gray-500 text-sm">Você já pode publicar e vender seus cursos.</p>
    </div>
  );
}

function ProcessingCard() {
  return (
    <div className="card text-center py-10">
      <Clock className="w-8 h-8 text-brand-500 mx-auto mb-3 animate-pulse" />
      <p className="font-semibold text-gray-800 mb-1">Estamos processando seus dados</p>
      <p className="text-gray-500 text-sm">Isso costuma levar só alguns instantes. Esta página atualiza sozinha.</p>
    </div>
  );
}

function UnderReviewCard() {
  return (
    <div className="card text-center py-10">
      <ShieldCheck className="w-8 h-8 text-brand-500 mx-auto mb-3" />
      <p className="font-semibold text-gray-800 mb-1">Seus dados estão em análise</p>
      <p className="text-gray-500 text-sm">
        Assim que a análise terminar, você recebe a confirmação por aqui automaticamente.
      </p>
    </div>
  );
}

function BlockedCard() {
  return (
    <div className="card text-center py-10">
      <AlertTriangle className="w-8 h-8 text-red-500 mx-auto mb-3" />
      <p className="font-semibold text-gray-800 mb-1">Conta financeira bloqueada</p>
      <p className="text-gray-500 text-sm">Fale com o nosso suporte para reativar sua conta.</p>
    </div>
  );
}

interface DataStepProps {
  previousData: CreatorFinancialOnboardingStatus['previousData'];
  onSubmitted: () => void;
}

function DataStep({ previousData, onSubmitted }: DataStepProps) {
  const [form, setForm] = useState<StartFinancialOnboardingData>({
    legalName: '', cpfCnpj: '', birthDate: '', companyType: '',
    email: '', mobilePhone: '', phone: '', incomeValue: 0,
    address: '', addressNumber: '', addressComplement: '', province: '', postalCode: '',
  });

  // Preenche com o que o criador já digitou antes (ex.: reenvio após uma rejeição) para não
  // obrigá-lo a redigitar tudo — ver PreviousOnboardingDataResponse no backend.
  useEffect(() => {
    if (!previousData) return;
    setForm({
      legalName: previousData.legalName,
      cpfCnpj: previousData.cpfCnpj,
      birthDate: previousData.birthDate ?? '',
      companyType: previousData.companyType ?? '',
      email: previousData.email,
      mobilePhone: previousData.mobilePhone,
      phone: previousData.phone ?? '',
      incomeValue: previousData.incomeValue,
      address: previousData.address,
      addressNumber: previousData.addressNumber,
      addressComplement: previousData.addressComplement ?? '',
      province: previousData.province,
      postalCode: previousData.postalCode,
    });
  }, [previousData]);

  const cpfCnpjDigits = onlyDigits(form.cpfCnpj);
  const isCompany = cpfCnpjDigits.length === 14;
  const isPerson = cpfCnpjDigits.length === 11;

  const startMutation = useMutation({
    mutationFn: () => financialOnboardingApi.start({
      ...form,
      cpfCnpj: cpfCnpjDigits,
      mobilePhone: onlyDigits(form.mobilePhone),
      phone: form.phone ? onlyDigits(form.phone) : undefined,
      postalCode: onlyDigits(form.postalCode),
      birthDate: isPerson ? form.birthDate : undefined,
      companyType: isCompany ? form.companyType : undefined,
      incomeValue: Number(form.incomeValue),
    }),
    onSuccess: (res) => {
      if (res.data?.success === false) {
        toast.error(res.data?.errorMessage ?? 'Não foi possível continuar. Revise os dados e tente novamente.');
        return;
      }
      toast.success('Dados enviados! Estamos criando sua conta financeira.');
      onSubmitted();
    },
    onError: (err: unknown) => {
      const message = (err as { response?: { data?: { message?: string; detail?: string; errors?: Record<string, string[]> } } })
        ?.response?.data;
      const validationMessage = message?.errors ? Object.values(message.errors).flat()[0] : undefined;
      toast.error(validationMessage ?? message?.detail ?? message?.message ?? 'Não foi possível enviar seus dados.');
    },
  });

  const set = <K extends keyof StartFinancialOnboardingData>(key: K, value: StartFinancialOnboardingData[K]) =>
    setForm(prev => ({ ...prev, [key]: value }));

  const isValid =
    form.legalName.trim().length > 1 &&
    (isCompany || isPerson) &&
    (isPerson ? !!form.birthDate : !!form.companyType) &&
    /\S+@\S+\.\S+/.test(form.email) &&
    onlyDigits(form.mobilePhone).length >= 10 &&
    Number(form.incomeValue) > 0 &&
    form.address.trim().length > 2 &&
    form.addressNumber.trim().length > 0 &&
    form.province.trim().length > 1 &&
    onlyDigits(form.postalCode).length === 8;

  return (
    <div className="card border-gray-200 space-y-5">
      <div className="flex items-start gap-2 p-3 rounded-lg bg-brand-50 border border-brand-200 text-brand-700 text-xs">
        <ShieldCheck className="w-4 h-4 flex-shrink-0 mt-0.5" />
        <p>Seus dados são usados só para configurar sua conta de recebimentos — nunca são compartilhados publicamente.</p>
      </div>

      <div className="grid grid-cols-2 gap-4">
        <Field label="Nome completo / Razão social" className="col-span-2">
          <input className={inputClassName} value={form.legalName} onChange={e => set('legalName', e.target.value)} placeholder="Como no seu documento" />
        </Field>

        <Field label="CPF ou CNPJ">
          <input className={inputClassName} value={form.cpfCnpj} onChange={e => set('cpfCnpj', e.target.value)} placeholder="Somente números" />
        </Field>

        {isPerson && (
          <Field label="Data de nascimento">
            <input type="date" className={inputClassName} value={form.birthDate} onChange={e => set('birthDate', e.target.value)} />
          </Field>
        )}

        {isCompany && (
          <Field label="Tipo de empresa">
            <select className={inputClassName} value={form.companyType} onChange={e => set('companyType', e.target.value)}>
              <option value="">Selecione</option>
              {COMPANY_TYPES.map(ct => <option key={ct.value} value={ct.value}>{ct.label}</option>)}
            </select>
          </Field>
        )}

        <Field label="E-mail">
          <input type="email" className={inputClassName} value={form.email} onChange={e => set('email', e.target.value)} />
        </Field>

        <Field label="Celular">
          <input className={inputClassName} value={form.mobilePhone} onChange={e => set('mobilePhone', e.target.value)} placeholder="(00) 00000-0000" />
        </Field>

        <Field label="Telefone fixo (opcional)">
          <input className={inputClassName} value={form.phone} onChange={e => set('phone', e.target.value)} placeholder="(00) 0000-0000" />
        </Field>

        <Field label="Faturamento/renda mensal (R$)">
          <input type="number" min={0} step="0.01" className={inputClassName} value={form.incomeValue || ''} onChange={e => set('incomeValue', Number(e.target.value))} />
        </Field>

        <Field label="CEP">
          <input className={inputClassName} value={form.postalCode} onChange={e => set('postalCode', e.target.value)} placeholder="00000-000" />
        </Field>

        <Field label="Bairro">
          <input className={inputClassName} value={form.province} onChange={e => set('province', e.target.value)} />
        </Field>

        <Field label="Endereço" className="col-span-2">
          <input className={inputClassName} value={form.address} onChange={e => set('address', e.target.value)} placeholder="Rua, avenida..." />
        </Field>

        <Field label="Número">
          <input className={inputClassName} value={form.addressNumber} onChange={e => set('addressNumber', e.target.value)} />
        </Field>

        <Field label="Complemento (opcional)">
          <input className={inputClassName} value={form.addressComplement} onChange={e => set('addressComplement', e.target.value)} />
        </Field>
      </div>

      <button
        onClick={() => startMutation.mutate()}
        disabled={!isValid || startMutation.isPending}
        className="btn-primary w-full py-2.5 disabled:opacity-50"
      >
        {startMutation.isPending ? 'Enviando...' : 'Continuar'}
      </button>
    </div>
  );
}

function Field({ label, className, children }: { label: string; className?: string; children: React.ReactNode }) {
  return (
    <div className={className}>
      <label className="block text-sm font-medium text-gray-700 mb-1.5">{label}</label>
      {children}
    </div>
  );
}

function DocumentsStep({ onAllSubmitted }: { onAllSubmitted: () => void }) {
  const queryClient = useQueryClient();

  const { data: documents, isLoading } = useQuery<OnboardingDocument[]>({
    queryKey: ['financial-onboarding-documents'],
    queryFn: () => financialOnboardingApi.getDocuments().then(r => r.data),
  });

  const uploadMutation = useMutation({
    mutationFn: ({ documentId, file }: { documentId: string; file: File }) =>
      financialOnboardingApi.uploadDocument(documentId, file),
    onSuccess: () => {
      toast.success('Documento enviado! Estamos conferindo.');
      queryClient.invalidateQueries({ queryKey: ['financial-onboarding-documents'] });
      onAllSubmitted();
    },
    onError: () => toast.error('Não foi possível enviar este documento. Tente novamente.'),
  });

  if (isLoading) return <p className="text-gray-400 text-sm">Carregando documentos...</p>;

  if (!documents?.length) {
    return (
      <div className="card text-center py-10">
        <ShieldCheck className="w-8 h-8 text-brand-500 mx-auto mb-3" />
        <p className="font-semibold text-gray-800 mb-1">Nenhum documento pendente por enquanto</p>
        <p className="text-gray-500 text-sm">Assim que houver algo pra enviar, aparece aqui.</p>
      </div>
    );
  }

  return (
    <div className="space-y-3">
      <p className="text-sm text-gray-500 mb-2">Envie os documentos abaixo para seguir com a análise.</p>
      {documents.map(doc => (
        <div key={doc.id} className="card border-gray-200 flex items-center justify-between gap-4">
          <div>
            <p className="font-medium text-gray-800 text-sm">{doc.title}</p>
            {doc.description && <p className="text-gray-400 text-xs mt-0.5">{doc.description}</p>}
            <span className={
              doc.status === 'Approved' ? 'badge-green mt-1.5 inline-block'
                : doc.status === 'Rejected' ? 'badge bg-red-50 text-red-700 border border-red-200 mt-1.5 inline-block'
                  : doc.status === 'AwaitingApproval' ? 'badge-yellow mt-1.5 inline-block'
                    : 'badge bg-gray-100 text-gray-500 border border-gray-200 mt-1.5 inline-block'
            }>
              {doc.status === 'Approved' ? 'Aprovado'
                : doc.status === 'Rejected' ? 'Rejeitado — envie novamente'
                  : doc.status === 'AwaitingApproval' ? 'Em análise'
                    : 'Pendente'}
            </span>
          </div>

          {doc.status !== 'Approved' && doc.status !== 'AwaitingApproval' && (
            doc.onboardingUrl ? (
              <a
                href={doc.onboardingUrl}
                target="_blank"
                rel="noopener noreferrer"
                className="btn-secondary flex items-center gap-1.5 whitespace-nowrap text-xs px-3 py-2"
              >
                Enviar <ExternalLink className="w-3.5 h-3.5" />
              </a>
            ) : (
              <label className="btn-secondary flex items-center gap-1.5 whitespace-nowrap text-xs px-3 py-2 cursor-pointer">
                <Upload className="w-3.5 h-3.5" /> Enviar arquivo
                <input
                  type="file"
                  className="hidden"
                  onChange={e => {
                    const file = e.target.files?.[0];
                    if (file) uploadMutation.mutate({ documentId: doc.id, file });
                    e.target.value = '';
                  }}
                />
              </label>
            )
          )}
        </div>
      ))}
    </div>
  );
}
