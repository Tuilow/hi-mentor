'use client';

import { useEffect, useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { Check, Clock, ExternalLink, ShieldCheck, Upload, AlertTriangle, CheckCircle2 } from 'lucide-react';
import { financialOnboardingApi } from '@/lib/api';
import type { CreatorFinancialOnboardingStatus, OnboardingStep, OnboardingDocument, StartFinancialOnboardingData } from '@/lib/api';
import { Card, Button, Badge } from '@/components/ui/design-system';

const COMPANY_TYPES: { value: string; label: string }[] = [
  { value: 'MEI', label: 'MEI (Microempreendedor Individual)' },
  { value: 'LIMITED', label: 'Limitada (LTDA)' },
  { value: 'INDIVIDUAL', label: 'Empresário Individual' },
  { value: 'ASSOCIATION', label: 'Associação / ONG' },
];

const onlyDigits = (value: string) => value.replace(/\D/g, '');

/**
 * "Financeiro -> Configurar recebimentos" do mentor — jornada de onboarding financeiro via
 * subconta Asaas (BaaS) criada automaticamente pela própria Hi Mentor. Substitui a antiga tela
 * de "cole sua API Key aqui": o mentor só informa dados pessoais/empresariais e envia documentos
 * quando pedido — a Hi Mentor cuida de criar a conta na Asaas, registrar o webhook e acompanhar
 * a aprovação. Em NENHUM momento esta tela menciona "API Key", "Wallet ID" ou "subconta" — só o
 * essencial para o mentor entender em que etapa está e o que falta.
 *
 * Redesign "Hi Mentor" (ago/2026): visual seguindo hi-mentor/src/pages/FinanceiroPage.tsx
 * (stepper numerado + cards por etapa) — todos os campos, mutations e o polling automático
 * (refetchInterval) continuam exatamente os mesmos de antes, só o JSX/classNames mudaram.
 *
 * Nota sobre a rota: esta página fica em (dashboard)/admin/financeiro por convenção já existente
 * neste repo -- a área "Administração" do menu do Mentor usa o prefixo /admin (ver comentário em
 * (dashboard)/layout.tsx), distinta da área "Financeiro" do DONO da plataforma, que fica em
 * /plataforma/financeiro (ver AdminOnboardingsTab nessa outra página).
 */
export default function FinanceiroCriadorPage() {
  const queryClient = useQueryClient();

  const { data: status, isLoading } = useQuery<CreatorFinancialOnboardingStatus>({
    queryKey: ['financial-onboarding-status'],
    queryFn: () => financialOnboardingApi.getStatus().then(r => r.data),
    // Enquanto a conta está sendo criada na Asaas (ou os documentos ainda não voltaram da
    // análise), o status pode mudar sem nenhuma ação do mentor nesta aba — reconsulta sozinho.
    refetchInterval: query => {
      const s = query.state.data?.status;
      return s === 'AccountCreationPending' || s === 'UnderReview' ? 8000 : false;
    },
  });

  const invalidateStatus = () => queryClient.invalidateQueries({ queryKey: ['financial-onboarding-status'] });

  if (isLoading) {
    return <p className="text-ink-3 text-sm">Carregando...</p>;
  }

  return (
    <div className="max-w-2xl">
      <div className="mb-1">
        <h1 className="text-3xl font-bold text-ink mb-1" style={{ letterSpacing: '-0.03em' }}>Financeiro</h1>
      </div>
      <p className="text-sm text-ink-3 mb-8">
        Configure seus recebimentos para poder publicar e vender seus programas. Leva só alguns
        minutos — cuidamos de toda a parte técnica pra você.
      </p>

      {status && <StepIndicator steps={status.steps} />}

      {status?.friendlyMessage && (
        <div className="my-6 flex items-start gap-2 p-4 rounded-xl bg-amber-50 border border-amber-200 text-amber-800 text-sm">
          <AlertTriangle className="w-4 h-4 shrink-0 mt-0.5" />
          <p>{status.friendlyMessage}</p>
        </div>
      )}

      <div className="mt-8">
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
    <div className="flex items-center gap-0">
      {steps.map((step, idx) => {
        const done = step.state === 'done';
        const current = step.state === 'current';
        const blocked = step.state === 'blocked';
        return (
          <div key={step.key} className="flex items-center flex-1 last:flex-none">
            <div className="flex flex-col items-center gap-1.5">
              <div
                className={`w-10 h-10 rounded-full flex items-center justify-center border-2 transition-all ${
                  done
                    ? 'bg-emerald-500 border-emerald-500 text-white'
                    : current
                      ? 'bg-violet-600 border-violet-600 text-white shadow-lg shadow-violet-600/30'
                      : blocked
                        ? 'bg-red-50 border-red-300 text-red-500'
                        : 'bg-surface border-line text-ink-4'
                }`}
              >
                {done ? <Check size={16} /> : blocked ? <AlertTriangle size={15} /> : <Clock size={16} />}
              </div>
              <span className={`text-xs font-semibold hidden sm:block whitespace-nowrap ${
                current ? 'text-violet-600' : done ? 'text-emerald-600' : blocked ? 'text-red-500' : 'text-ink-4'
              }`}>
                {step.title}
              </span>
            </div>
            {idx < steps.length - 1 && (
              <div className={`flex-1 h-0.5 mx-1 mt-[-18px] transition-all ${done ? 'bg-emerald-400' : 'bg-line'}`} />
            )}
          </div>
        );
      })}
    </div>
  );
}

function ReadyToSellCard() {
  return (
    <Card className="bg-emerald-50/40 border-emerald-200 text-center py-12">
      <CheckCircle2 className="w-10 h-10 text-emerald-600 mx-auto mb-3" />
      <p className="font-bold text-ink mb-1">Sua conta financeira está pronta!</p>
      <p className="text-ink-3 text-sm">Você já pode publicar e vender seus programas.</p>
    </Card>
  );
}

function ProcessingCard() {
  return (
    <Card className="text-center py-12">
      <Clock className="w-8 h-8 text-violet-500 mx-auto mb-3 animate-pulse" />
      <p className="font-bold text-ink mb-1">Estamos processando seus dados</p>
      <p className="text-ink-3 text-sm">Isso costuma levar só alguns instantes. Esta página atualiza sozinha.</p>
    </Card>
  );
}

function UnderReviewCard() {
  return (
    <Card className="text-center py-12">
      <ShieldCheck className="w-8 h-8 text-violet-500 mx-auto mb-3" />
      <p className="font-bold text-ink mb-1">Seus dados estão em análise</p>
      <p className="text-ink-3 text-sm max-w-xs mx-auto leading-relaxed">
        Assim que a análise terminar, você recebe a confirmação por aqui automaticamente.
      </p>
    </Card>
  );
}

function BlockedCard() {
  return (
    <Card className="text-center py-12">
      <AlertTriangle className="w-8 h-8 text-red-500 mx-auto mb-3" />
      <p className="font-bold text-ink mb-1">Conta financeira bloqueada</p>
      <p className="text-ink-3 text-sm">Fale com o nosso suporte para reativar sua conta.</p>
    </Card>
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

  // Preenche com o que o mentor já digitou antes (ex.: reenvio após uma rejeição) para não
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
    <Card className="space-y-5">
      <div className="flex items-start gap-2 p-3 rounded-xl bg-violet-50 border border-violet-200 text-violet-700 text-xs">
        <ShieldCheck className="w-4 h-4 shrink-0 mt-0.5" />
        <p>Seus dados são usados só para configurar sua conta de recebimentos — nunca são compartilhados publicamente.</p>
      </div>

      <div className="grid grid-cols-2 gap-4">
        <Field label="Nome completo / Razão social" className="col-span-2">
          <input className="input-field" value={form.legalName} onChange={e => set('legalName', e.target.value)} placeholder="Como no seu documento" />
        </Field>

        <Field label="CPF ou CNPJ">
          <input className="input-field" value={form.cpfCnpj} onChange={e => set('cpfCnpj', e.target.value)} placeholder="Somente números" />
        </Field>

        {isPerson && (
          <Field label="Data de nascimento">
            <input type="date" className="input-field" value={form.birthDate} onChange={e => set('birthDate', e.target.value)} />
          </Field>
        )}

        {isCompany && (
          <Field label="Tipo de empresa">
            <select className="input-field" value={form.companyType} onChange={e => set('companyType', e.target.value)}>
              <option value="">Selecione</option>
              {COMPANY_TYPES.map(ct => <option key={ct.value} value={ct.value}>{ct.label}</option>)}
            </select>
          </Field>
        )}

        <Field label="E-mail">
          <input type="email" className="input-field" value={form.email} onChange={e => set('email', e.target.value)} />
        </Field>

        <Field label="Celular">
          <input className="input-field" value={form.mobilePhone} onChange={e => set('mobilePhone', e.target.value)} placeholder="(00) 00000-0000" />
        </Field>

        <Field label="Telefone fixo (opcional)">
          <input className="input-field" value={form.phone} onChange={e => set('phone', e.target.value)} placeholder="(00) 0000-0000" />
        </Field>

        <Field label="Faturamento/renda mensal (R$)">
          <input type="number" min={0} step="0.01" className="input-field" value={form.incomeValue || ''} onChange={e => set('incomeValue', Number(e.target.value))} />
        </Field>

        <Field label="CEP">
          <input className="input-field" value={form.postalCode} onChange={e => set('postalCode', e.target.value)} placeholder="00000-000" />
        </Field>

        <Field label="Bairro">
          <input className="input-field" value={form.province} onChange={e => set('province', e.target.value)} />
        </Field>

        <Field label="Endereço" className="col-span-2">
          <input className="input-field" value={form.address} onChange={e => set('address', e.target.value)} placeholder="Rua, avenida..." />
        </Field>

        <Field label="Número">
          <input className="input-field" value={form.addressNumber} onChange={e => set('addressNumber', e.target.value)} />
        </Field>

        <Field label="Complemento (opcional)">
          <input className="input-field" value={form.addressComplement} onChange={e => set('addressComplement', e.target.value)} />
        </Field>
      </div>

      <Button
        onClick={() => startMutation.mutate()}
        disabled={!isValid}
        loading={startMutation.isPending}
        className="w-full"
        size="lg"
      >
        {startMutation.isPending ? 'Enviando...' : 'Continuar'}
      </Button>
    </Card>
  );
}

function Field({ label, className, children }: { label: string; className?: string; children: React.ReactNode }) {
  return (
    <div className={className}>
      <label className="block text-sm font-semibold text-ink-2 mb-1.5">{label}</label>
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

  if (isLoading) return <p className="text-ink-3 text-sm">Carregando documentos...</p>;

  if (!documents?.length) {
    return (
      <Card className="text-center py-12">
        <ShieldCheck className="w-8 h-8 text-violet-500 mx-auto mb-3" />
        <p className="font-bold text-ink mb-1">Nenhum documento pendente por enquanto</p>
        <p className="text-ink-3 text-sm">Assim que houver algo pra enviar, aparece aqui.</p>
      </Card>
    );
  }

  return (
    <div className="space-y-3">
      <p className="text-sm text-ink-3 mb-2">Envie os documentos abaixo para seguir com a análise.</p>
      {documents.map(doc => (
        <Card key={doc.id} className="flex items-center justify-between gap-4" padding="sm">
          <div>
            <p className="font-semibold text-ink text-sm">{doc.title}</p>
            {doc.description && <p className="text-ink-3 text-xs mt-0.5">{doc.description}</p>}
            <Badge
              variant={doc.status === 'Approved' ? 'active' : doc.status === 'Rejected' ? 'risk' : doc.status === 'AwaitingApproval' ? 'warning' : 'neutral'}
              className="mt-1.5"
            >
              {doc.status === 'Approved' ? 'Aprovado'
                : doc.status === 'Rejected' ? 'Rejeitado — envie novamente'
                  : doc.status === 'AwaitingApproval' ? 'Em análise'
                    : 'Pendente'}
            </Badge>
          </div>

          {doc.status !== 'Approved' && doc.status !== 'AwaitingApproval' && (
            doc.onboardingUrl ? (
              <a href={doc.onboardingUrl} target="_blank" rel="noopener noreferrer">
                <Button variant="secondary" size="xs" icon={<ExternalLink className="w-3.5 h-3.5" />}>
                  Enviar
                </Button>
              </a>
            ) : (
              <label className="inline-flex items-center justify-center gap-1.5 bg-surface border border-line text-ink hover:bg-subtle font-semibold rounded-xl px-3 py-1 text-xs whitespace-nowrap cursor-pointer transition-all">
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
        </Card>
      ))}
    </div>
  );
}
