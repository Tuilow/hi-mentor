'use client';

import { useEffect, useRef, useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import { useQuery } from '@tanstack/react-query';
import Link from 'next/link';
import toast from 'react-hot-toast';
import { AxiosError } from 'axios';
import { coursesApi, coursePurchasesApi, courseSubscriptionPlansApi, enrollmentsApi, subscriptionsApi } from '@/lib/api';
import { commercializationLabel, isFreeState } from '@/lib/courseCommercialization';
import type { ProductDetail, InstructorCourseSummary } from '@/types';

const levelLabel: Record<string, string> = {
  Beginner: 'Iniciante',
  Intermediate: 'Intermediário',
  Advanced: 'Avançado',
};

// Mesmos rótulos usados no seletor de Recorrência do assistente de criação
// (admin/produtos/novo/page.tsx), só que como sufixo pra exibir "R$200/mês" etc.
const billingCycleSuffix: Record<string, string> = {
  Monthly: '/mês',
  Quarterly: '/trimestre',
  Semiannual: '/semestre',
  Annual: '/ano',
};

function formatPrice(price: number): string {
  return price === 0
    ? 'Grátis'
    : price.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

/**
 * Converte a URL original colada pelo criador (YouTube/Vimeo) numa URL de embed própria da
 * plataforma — mesma lógica de (dashboard)/cursos/[slug]/[lessonId], aqui só para o vídeo de
 * apresentação da página de vendas (não para aulas do curso).
 */
function toEmbedUrl(url: string): string | null {
  const youtubeMatch = url.match(/(?:youtube\.com\/watch\?v=|youtu\.be\/|youtube\.com\/embed\/)([a-zA-Z0-9_-]{6,})/);
  if (youtubeMatch) return `https://www.youtube.com/embed/${youtubeMatch[1]}`;

  const vimeoMatch = url.match(/vimeo\.com\/(?:video\/)?(\d+)/);
  if (vimeoMatch) return `https://player.vimeo.com/video/${vimeoMatch[1]}`;

  return null;
}

interface CheckoutData {
  customerName: string;
  customerEmail: string;
  cpfCnpj: string;
  phone?: string;
}

/**
 * Checkout embutido na própria Landing Page pública — o visitante compra sem sair da página e
 * sem precisar de conta prévia. A conta é localizada ou criada automaticamente pelo e-mail
 * informado aqui (checkout anônimo — ver PurchaseCourseCommandHandler); o acesso pós-pagamento
 * chega por Magic Link no e-mail informado, sem senha. Mesmo formulário/validação de
 * (dashboard)/cursos/[slug].
 */
function CheckoutModal({
  title, priceLabel, onClose, onConfirm, loading,
}: {
  title: string;
  priceLabel: string;
  onClose: () => void;
  onConfirm: (data: CheckoutData) => void;
  loading: boolean;
}) {
  const [form, setForm] = useState<CheckoutData>({
    customerName: '', customerEmail: '', cpfCnpj: '', phone: '',
  });
  const [errors, setErrors] = useState<Partial<CheckoutData>>({});

  const validate = () => {
    const e: Partial<CheckoutData> = {};
    if (!form.customerName.trim()) e.customerName = 'Nome obrigatório';
    if (!form.customerEmail.includes('@')) e.customerEmail = 'E-mail inválido';
    const digits = form.cpfCnpj.replace(/\D/g, '');
    if (digits.length !== 11 && digits.length !== 14)
      e.cpfCnpj = 'CPF (11 dígitos) ou CNPJ (14 dígitos)';
    setErrors(e);
    return Object.keys(e).length === 0;
  };

  const formatDoc = (v: string) => {
    const d = v.replace(/\D/g, '').slice(0, 14);
    if (d.length <= 3) return d;
    if (d.length <= 6) return `${d.slice(0,3)}.${d.slice(3)}`;
    if (d.length <= 9) return `${d.slice(0,3)}.${d.slice(3,6)}.${d.slice(6)}`;
    if (d.length <= 11) return `${d.slice(0,3)}.${d.slice(3,6)}.${d.slice(6,9)}-${d.slice(9)}`;
    if (d.length <= 12) return `${d.slice(0,2)}.${d.slice(2,5)}.${d.slice(5,8)}/${d.slice(8)}`;
    if (d.length <= 13) return `${d.slice(0,2)}.${d.slice(2,5)}.${d.slice(5,8)}/${d.slice(8,12)}-${d.slice(12)}`;
    return `${d.slice(0,2)}.${d.slice(2,5)}.${d.slice(5,8)}/${d.slice(8,12)}-${d.slice(12,14)}`;
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (validate()) onConfirm({ ...form, cpfCnpj: form.cpfCnpj.replace(/\D/g, '') });
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm">
      <div className="relative w-full max-w-md bg-white border border-gray-200 rounded-2xl shadow-2xl animate-fade-in">
        <div className="flex items-center justify-between p-6 border-b border-gray-200">
          <div>
            <h2 className="text-lg font-bold text-gray-800">{title}</h2>
            <p className="text-sm text-gray-500 mt-0.5">{priceLabel}</p>
          </div>
          <button onClick={onClose} className="text-gray-400 hover:text-gray-600 transition-colors">✕</button>
        </div>
        <form onSubmit={handleSubmit} className="p-6 space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-600 mb-1.5">Nome completo *</label>
            <input className="input-field" placeholder="João da Silva"
              value={form.customerName}
              onChange={e => setForm(f => ({ ...f, customerName: e.target.value }))} />
            {errors.customerName && <p className="text-red-400 text-xs mt-1">{errors.customerName}</p>}
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-600 mb-1.5">E-mail *</label>
            <input type="email" className="input-field" placeholder="joao@email.com"
              value={form.customerEmail}
              onChange={e => setForm(f => ({ ...f, customerEmail: e.target.value }))} />
            {errors.customerEmail && <p className="text-red-400 text-xs mt-1">{errors.customerEmail}</p>}
            <p className="text-xs text-gray-400 mt-1">Seu acesso ao curso chega direto neste e-mail, sem precisar de senha.</p>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-600 mb-1.5">CPF ou CNPJ *</label>
            <input className="input-field" placeholder="000.000.000-00"
              value={formatDoc(form.cpfCnpj)}
              onChange={e => setForm(f => ({ ...f, cpfCnpj: e.target.value }))} />
            {errors.cpfCnpj && <p className="text-red-400 text-xs mt-1">{errors.cpfCnpj}</p>}
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-600 mb-1.5">Telefone (opcional)</label>
            <input className="input-field" placeholder="(11) 99999-9999"
              value={form.phone}
              onChange={e => setForm(f => ({ ...f, phone: e.target.value }))} />
          </div>
          <div className="flex items-center gap-3 py-2">
            <span className="text-xs text-gray-400">Formas de pagamento:</span>
            <span className="text-xs font-medium text-gray-600 bg-gray-100 px-2 py-0.5 rounded">PIX</span>
            <span className="text-xs font-medium text-gray-600 bg-gray-100 px-2 py-0.5 rounded">Cartão</span>
            <span className="text-xs font-medium text-gray-600 bg-gray-100 px-2 py-0.5 rounded">Boleto</span>
          </div>
          <button type="submit" disabled={loading} className="btn-primary w-full py-3 mt-2">
            {loading ? 'Processando...' : 'Ir para pagamento →'}
          </button>
          <p className="text-center text-xs text-gray-400">Processado com segurança via Asaas.</p>
        </form>
      </div>
    </div>
  );
}

interface FreeEnrollData {
  customerName: string;
  customerEmail: string;
}

/**
 * Achado B2 da avaliação de UX: até aqui, curso grátis mandava o visitante pra /registro —
 * nome, sobrenome, e-mail, senha, confirmar senha, e ainda esperar um e-mail de confirmação —
 * enquanto o curso PAGO tinha um checkout anônimo embutido na própria página (só nome/e-mail).
 * Curso grátis devia ser o caminho de MENOR fricção da plataforma, não o de maior. Este modal
 * espelha o CheckoutModal acima (mesmo visual, embutido na página, sem navegação extra), mas
 * pede só nome e e-mail — nem CPF/telefone, que só fazem sentido quando há cobrança. A conta é
 * localizada ou criada automaticamente pelo e-mail informado, sem senha, e o acesso chega por
 * Magic Link — ver EnrollFreeCourseAnonymousCommandHandler (Learning.Application).
 */
function FreeEnrollModal({
  title, onClose, onConfirm, loading,
}: {
  title: string;
  onClose: () => void;
  onConfirm: (data: FreeEnrollData) => void;
  loading: boolean;
}) {
  const [form, setForm] = useState<FreeEnrollData>({ customerName: '', customerEmail: '' });
  const [errors, setErrors] = useState<Partial<FreeEnrollData>>({});

  const validate = () => {
    const e: Partial<FreeEnrollData> = {};
    if (!form.customerName.trim()) e.customerName = 'Nome obrigatório';
    if (!form.customerEmail.includes('@')) e.customerEmail = 'E-mail inválido';
    setErrors(e);
    return Object.keys(e).length === 0;
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (validate()) onConfirm(form);
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm">
      <div className="relative w-full max-w-md bg-white border border-gray-200 rounded-2xl shadow-2xl animate-fade-in">
        <div className="flex items-center justify-between p-6 border-b border-gray-200">
          <div>
            <h2 className="text-lg font-bold text-gray-800">{title}</h2>
            <p className="text-sm text-gray-500 mt-0.5">Grátis</p>
          </div>
          <button onClick={onClose} className="text-gray-400 hover:text-gray-600 transition-colors">✕</button>
        </div>
        <form onSubmit={handleSubmit} className="p-6 space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-600 mb-1.5">Nome completo *</label>
            <input className="input-field" placeholder="João da Silva"
              value={form.customerName}
              onChange={e => setForm(f => ({ ...f, customerName: e.target.value }))} />
            {errors.customerName && <p className="text-red-400 text-xs mt-1">{errors.customerName}</p>}
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-600 mb-1.5">E-mail *</label>
            <input type="email" className="input-field" placeholder="joao@email.com"
              value={form.customerEmail}
              onChange={e => setForm(f => ({ ...f, customerEmail: e.target.value }))} />
            {errors.customerEmail && <p className="text-red-400 text-xs mt-1">{errors.customerEmail}</p>}
            <p className="text-xs text-gray-400 mt-1">Seu acesso ao curso chega direto neste e-mail, sem precisar de senha.</p>
          </div>
          <button type="submit" disabled={loading} className="btn-primary w-full py-3 mt-2">
            {loading ? 'Matriculando...' : 'Começar agora, grátis →'}
          </button>
        </form>
      </div>
    </div>
  );
}

/**
 * Página de Vendas Pública (Kit de Divulgação) — não exige login. É para onde apontam o link
 * direto, o QR Code, o embed e o botão HTML gerados na aba "Divulgar" do dashboard do produto.
 * Usa os mesmos dados já persistidos em Course (SalesPageHeadline/Benefits/FaqItems) que a IA
 * de página de vendas já gera — nenhum dado novo, só uma leitura pública deles.
 */
export default function PublicSalesPage() {
  const { slug } = useParams<{ slug: string }>();
  const router = useRouter();
  // Tri-state (em vez de boolean simples): `null` = ainda não checou o localStorage (primeiro
  // render/SSR), só depois vira `true`/`false`. Sem isso a checagem de matrícula abaixo correria
  // o risco de tratar "ainda não sei se está logado" como "não está logado".
  const [isLoggedIn, setIsLoggedIn] = useState<boolean | null>(null);
  const viewRecordedRef = useRef(false);
  const [checkoutOpen, setCheckoutOpen] = useState(false);
  const [processingCheckout, setProcessingCheckout] = useState(false);
  // `kind` distingue compra avulsa de assinatura de produto: cada uma tem seu próprio endpoint
  // de simulação de pagamento (ver handleSimulatePayment) — sem isso não haveria como saber qual
  // dos dois chamar depois que o pedido já foi criado.
  const [createdOrder, setCreatedOrder] = useState<{ id: string; kind: 'purchase' | 'subscription'; paymentUrl?: string } | null>(null);
  const [simulating, setSimulating] = useState(false);
  // Curso grátis: evita chamar POST /enrollments mais de uma vez (efeito abaixo) enquanto a
  // matrícula automática está em andamento ou já foi tentada nesta sessão da página.
  const [enrolling, setEnrolling] = useState(false);
  const autoEnrollAttemptedRef = useRef(false);
  // Achado B2 da avaliação de UX: modal embutido de matrícula grátis (visitante SEM sessão) —
  // mesmo padrão do checkoutOpen/createdOrder acima, só que sem pagamento. freeEnrollSent
  // controla o card "verifique seu e-mail" pós-matrícula (equivalente ao createdOrder do pago).
  const [freeEnrollOpen, setFreeEnrollOpen] = useState(false);
  const [processingFreeEnroll, setProcessingFreeEnroll] = useState(false);
  const [freeEnrollSent, setFreeEnrollSent] = useState(false);
  // Endpoint de simulação só existe no backend em Development (404 em produção) — este check
  // evita mostrar o atalho de sandbox à toa fora de ambiente de dev.
  const isDevEnv = process.env.NODE_ENV !== 'production';

  useEffect(() => {
    setIsLoggedIn(!!localStorage.getItem('access_token'));
  }, []);

  const { data: course, isLoading } = useQuery<ProductDetail>({
    queryKey: ['public-course', slug],
    queryFn: () => coursesApi.getBySlug(slug).then(r => r.data),
    enabled: !!slug,
  });

  // Curso no modo "Assinatura" (ver admin/produtos/novo/page.tsx) grava Course.Price como 0 — o
  // preço real só existe no Plan, no módulo Sales. Sem esta busca, a página achava que o curso
  // era grátis (Price === 0) mesmo quando o criador configurou uma assinatura paga.
  const { data: plans } = useQuery({
    queryKey: ['course-subscription-plan', course?.id],
    queryFn: () => courseSubscriptionPlansApi.getByCourse(course!.id).then(r => r.data),
    enabled: !!course?.id,
  });
  const activePlan: { price: number; billingCycle: string } | undefined = plans?.[0];
  // Fonte de verdade sobre o curso ser gratuito de fato: vem pronta do backend
  // (course.commercializationState — ver CourseCommercializationResolver), não é mais derivada
  // aqui cruzando course.isFree com a query de planos acima (essa combinação local era
  // exatamente o tipo de regra duplicada no front-end que o backend agora resolve uma única vez).
  const isFree = isFreeState(course?.commercializationState, !!course?.isFree);

  // Esta é a página pública de divulgação (link direto/QR Code/embed da aba "Divulgar") — quem
  // já é aluno matriculado pode cair aqui de novo (favorito antigo, busca, e-mail) e não deve ver
  // a página de vendas/checkout de novo. GET /enrollments/courses/{id} devolve a matrícula se
  // existir, ou erro (tratado como "não matriculado") caso contrário — mesmo padrão já usado em
  // (dashboard)/cursos/[slug]/page.tsx.
  const { data: enrollment, isLoading: enrollmentLoading } = useQuery({
    queryKey: ['enrollment-progress', course?.id],
    queryFn: () => enrollmentsApi.getProgress(course!.id).then(r => r.data).catch(() => null),
    enabled: !!course?.id && isLoggedIn === true,
  });
  const isEnrolled = !!enrollment;

  useEffect(() => {
    if (isEnrolled) router.replace(`/cursos/${slug}`);
  }, [isEnrolled, router, slug]);

  // Curso grátis: fluxo pedido é "a pessoa se loga e se matricula automaticamente no curso" —
  // sem clique extra em "Matricular-se". Cobre quem já chega logado nesta página pública (ex.:
  // aluno com conta navegando por um link direto/QR Code) e, com a correção do achado B2, também
  // quem acabou de completar o FreeEnrollModal enquanto já tinha uma sessão ativa noutra aba —
  // em ambos os casos a checagem de matrícula acima resolve "não matriculado" e este efeito
  // matricula e redireciona para o curso, sem exigir clique extra.
  useEffect(() => {
    if (
      isFree && isLoggedIn === true && !enrollmentLoading && !isEnrolled &&
      !autoEnrollAttemptedRef.current && course
    ) {
      autoEnrollAttemptedRef.current = true;
      attemptAutoEnroll();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isFree, isLoggedIn, enrollmentLoading, isEnrolled, course]);

  // Registra a visualização uma única vez (alimenta o card "Views" do dashboard do criador).
  useEffect(() => {
    if (course && !viewRecordedRef.current) {
      viewRecordedRef.current = true;
      coursesApi.recordView(slug).catch(() => { /* best-effort, não bloqueia a página */ });
    }
  }, [course, slug]);

  const { data: otherCourses = [] } = useQuery<InstructorCourseSummary[]>({
    queryKey: ['instructor-courses', course?.instructorId, course?.id],
    queryFn: () => coursesApi.getByInstructor(course!.instructorId, course!.id).then(r => r.data),
    enabled: !!course?.instructorId,
  });

  const errorMessage = (e: unknown, fallback: string) =>
    (e as AxiosError<{ title?: string; detail?: string; message?: string }>)
      .response?.data?.detail
    ?? (e as AxiosError<{ title?: string; detail?: string; message?: string }>).response?.data?.title
    ?? (e as AxiosError<{ title?: string; detail?: string; message?: string }>).response?.data?.message
    ?? fallback;

  // Matrícula automática do curso grátis (visitante JÁ logado) — chamada tanto pelo efeito acima
  // quanto por um clique manual no CTA como rede de segurança. Note que isto chama
  // enrollmentsApi.enroll (endpoint autenticado, [Authorize]), não enrollFreeAnonymous — quem já
  // tem sessão não precisa do fluxo anônimo do FreeEnrollModal, só matricular direto.
  const attemptAutoEnroll = async () => {
    if (!course) return;
    setEnrolling(true);
    try {
      await enrollmentsApi.enroll(course.id);
      router.replace(`/cursos/${slug}`);
    } catch (e: unknown) {
      toast.error(errorMessage(e, 'Erro ao se matricular.'));
      setEnrolling(false);
    }
  };

  // Achado B2 da avaliação de UX (CORRIGIDO): antes, grátis levava para /registro (cadastro
  // completo com senha) enquanto pago tinha checkout anônimo embutido na página — o caminho que
  // deveria ter MENOS fricção tinha mais. Agora os dois fluxos abrem um modal embutido na própria
  // página, sem navegação extra: FreeEnrollModal (só nome/e-mail) para grátis, CheckoutModal
  // (+ CPF/telefone, exigidos pelo pagamento) para pago. Usa `isFree` (que já descarta o caso de
  // assinatura ativa), não `course.isFree` puro.
  const handleCtaClick = () => {
    if (!course) return;
    if (isFree) {
      // Já logado: o efeito acima normalmente já disparou a matrícula automática antes mesmo
      // do usuário ter chance de clicar; este clique manual é só uma rede de segurança.
      if (isLoggedIn === true) {
        attemptAutoEnroll();
        return;
      }
      setFreeEnrollSent(false);
      setFreeEnrollOpen(true);
      return;
    }
    setCreatedOrder(null);
    setCheckoutOpen(true);
  };

  const handleFreeEnroll = async (data: FreeEnrollData) => {
    if (!course) return;
    setProcessingFreeEnroll(true);
    try {
      await enrollmentsApi.enrollFreeAnonymous(course.id, data);
      setFreeEnrollOpen(false);
      setFreeEnrollSent(true);
      toast.success('Matrícula confirmada! Verifique o e-mail informado: chegou um link de acesso direto (Magic Link). 🎉');
    } catch (e: unknown) {
      toast.error(errorMessage(e, 'Erro ao se matricular.'));
    } finally {
      setProcessingFreeEnroll(false);
    }
  };

  const handleCheckout = async (data: CheckoutData) => {
    if (!course) return;
    setProcessingCheckout(true);
    try {
      // Curso no modo "Assinatura" (activePlan presente) usa o endpoint de assinatura por
      // produto em vez de compra avulsa — antes disso o checkout sempre chamava o endpoint de
      // compra avulsa, que rejeitava qualquer curso com Course.IsFree=true (sempre o caso para
      // cursos de assinatura, já que o preço mora no Plan, não no Course).
      const kind: 'purchase' | 'subscription' = activePlan ? 'subscription' : 'purchase';
      const { data: order } = activePlan
        ? await subscriptionsApi.subscribeToCourse(course.id, data)
        : await coursePurchasesApi.purchase({ courseId: course.id, ...data });
      const orderId: string = activePlan ? order.subscriptionId : order.coursePurchaseId;

      setCheckoutOpen(false);
      setCreatedOrder({ id: orderId, kind, paymentUrl: order.paymentUrl });
      if (order.paymentUrl) {
        toast.success('Pedido criado! Redirecionando para pagamento...');
        setTimeout(() => window.open(order.paymentUrl, '_blank'), 800);
      } else {
        toast.success('Pedido criado! Você receberá o link de pagamento por e-mail.');
      }

      // Sandbox/dev: confirma o pagamento automaticamente assim que o pedido é criado, sem
      // exigir o clique manual no botão de simulação — fecha o loop "clica em pagar → já ganha
      // acesso" pedido pelo fluxo "Quero começar agora". Passa orderId/kind explicitamente (não
      // depende do estado createdOrder, que só seria atualizado no próximo render). O botão
      // manual abaixo continua disponível como fallback.
      if (isDevEnv) await handleSimulatePayment(orderId, kind);
    } catch (e: unknown) {
      toast.error(errorMessage(e, 'Erro ao processar a compra.'));
    } finally {
      setProcessingCheckout(false);
    }
  };

  // SANDBOX/DEV: substitui o webhook do Asaas (que não alcança localhost), disponível mesmo
  // sem login — fecha o loop compra/assinatura anônima → pagamento confirmado → conta criada →
  // Magic Link por e-mail, tudo testável direto da própria Landing Page. Aceita orderId/kind
  // explícitos (chamada automática logo após handleCheckout) ou usa o estado createdOrder
  // (clique manual no botão de fallback, quando a simulação automática falhou ou está desligada).
  const handleSimulatePayment = async (orderId?: string, kind?: 'purchase' | 'subscription') => {
    const id = orderId ?? createdOrder?.id;
    const orderKind = kind ?? createdOrder?.kind;
    if (!id || !orderKind) return;
    setSimulating(true);
    try {
      if (orderKind === 'subscription') {
        await subscriptionsApi.simulateSubscriptionPayment(id);
      } else {
        await coursePurchasesApi.simulatePayment(id);
      }
      // Já logado nesta mesma sessão do navegador (ex.: quem tinha conta e comprou de novo):
      // não precisa esperar o e-mail com Magic Link, entra direto — mesmo destino de quem se
      // matricula num curso grátis.
      if (isLoggedIn === true) {
        toast.success('Pagamento confirmado! 🎉');
        router.replace(`/cursos/${slug}`);
        return;
      }
      toast.success('Pagamento confirmado! Verifique o e-mail informado: chegou um link de acesso direto (Magic Link). 🎉');
    } catch (e: unknown) {
      toast.error(errorMessage(e, 'Erro ao simular pagamento.'));
    } finally {
      setSimulating(false);
    }
  };

  if (isLoading) {
    return (
      <div className="min-h-screen bg-white flex items-center justify-center">
        <p className="text-gray-400 text-sm">Carregando...</p>
      </div>
    );
  }

  if (!course) {
    return (
      <div className="min-h-screen bg-white flex flex-col items-center justify-center gap-3">
        <p className="text-4xl">😕</p>
        <p className="text-lg font-medium text-gray-700">Curso não encontrado</p>
        <Link href="/" className="text-brand-600 hover:underline text-sm">← Voltar para o início</Link>
      </div>
    );
  }

  // Aluno já matriculado (ou matrícula automática do curso grátis em andamento): em vez de
  // mostrar a página de vendas (mesmo que só por um instante enquanto o redirect dispara),
  // mostra um loading dedicado. Evita o "flash" do botão Comprar/checkout pra quem já é aluno.
  if (isLoggedIn === true && (enrollmentLoading || isEnrolled || enrolling)) {
    return (
      <div className="min-h-screen bg-white flex flex-col items-center justify-center gap-3">
        <svg className="animate-spin h-8 w-8 text-brand-600" fill="none" viewBox="0 0 24 24">
          <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
          <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
        </svg>
        <p className="text-gray-500 text-sm">Redirecionando para o seu curso...</p>
      </div>
    );
  }

  const totalLessons = course.modules.reduce((acc, m) => acc + m.lessons.length, 0);
  const headline = course.salesPageHeadline || course.title;
  const subheadline = course.salesPageSubheadline || course.shortDescription;
  // "Quero começar agora" para os dois fluxos (grátis e pago) — o que muda é o que acontece
  // depois do clique, não o texto do botão. `salesPageCtaText` (gerado pela IA de página de
  // vendas) continua tendo prioridade quando o criador personalizou.
  const ctaText = course.salesPageCtaText || 'Quero começar agora';

  return (
    <div className="min-h-screen bg-white">
      {/* Topo minimalista — sem sidebar, visitante não está logado */}
      <header className="border-b border-gray-200">
        <div className="max-w-4xl mx-auto px-4 h-16 flex items-center justify-between">
          <Link href="/" className="text-lg font-bold gradient-text">🎓 Tuilow</Link>
          <Link href={isLoggedIn ? '/dashboard' : '/login'}
            className="text-sm text-gray-500 hover:text-gray-800 transition-colors">
            {isLoggedIn ? 'Ir para o painel →' : 'Entrar'}
          </Link>
        </div>
      </header>

      <div className="max-w-4xl mx-auto px-4 py-10">
        {/* Hero */}
        <div className="text-center max-w-2xl mx-auto mb-8">
          <div className="flex items-center justify-center gap-2 mb-4">
            <span className="badge-purple">{levelLabel[course.level] ?? course.level}</span>
            {isFree && <span className="badge-green">Grátis</span>}
          </div>
          <h1 className="text-3xl font-bold text-gray-800 mb-3">{headline}</h1>
          {subheadline && <p className="text-gray-500 text-lg">{subheadline}</p>}
        </div>

        {course.salesPageVideoUrl && toEmbedUrl(course.salesPageVideoUrl) ? (
          <div className="w-full aspect-video rounded-2xl overflow-hidden mb-8 bg-black">
            <iframe
              src={toEmbedUrl(course.salesPageVideoUrl)!}
              className="w-full h-full"
              allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
              allowFullScreen
            />
          </div>
        ) : course.thumbnailUrl ? (
          <img src={course.thumbnailUrl} alt={course.title}
            className="w-full h-64 md:h-96 object-cover rounded-2xl mb-8" />
        ) : (
          <div className="w-full h-64 md:h-96 bg-gradient-to-br from-gray-100 to-gray-50
                          rounded-2xl mb-8 flex items-center justify-center text-7xl">
            📚
          </div>
        )}

        <div className="flex flex-col items-center gap-2 mb-10">
          <p className="text-3xl font-bold gradient-text">
            {activePlan
              ? `${formatPrice(activePlan.price)}${billingCycleSuffix[activePlan.billingCycle] ?? ''}`
              : formatPrice(course.price)}
          </p>
          <button onClick={handleCtaClick} className="btn-primary px-8 py-3 text-base">{ctaText} →</button>
          <p className="text-xs text-gray-400">
            {isFree ? 'Sem cartão, sem senha — só nome e e-mail.' : 'Pagamento seguro via PIX, cartão ou boleto.'}
          </p>
          {(course.guaranteeDays || course.guaranteeText) && (
            <p className="text-xs text-emerald-600 font-medium flex items-center gap-1">
              🛡️ {course.guaranteeText || `Garantia de ${course.guaranteeDays} dias — satisfação ou seu dinheiro de volta`}
            </p>
          )}

          {freeEnrollSent && (
            <div className="w-full max-w-sm mt-4 card border-emerald-200 bg-emerald-50/40 text-center">
              <p className="text-sm font-medium text-gray-700">✅ Matrícula confirmada</p>
              <p className="text-xs text-gray-500 mt-1">
                Enviamos um Magic Link para o e-mail informado — clique nele para entrar direto no curso, sem senha.
              </p>
            </div>
          )}

          {createdOrder && (
            <div className="w-full max-w-sm mt-4 card border-brand-200 bg-brand-50/40 text-center">
              <p className="text-sm font-medium text-gray-700">
                {createdOrder.paymentUrl ? '⏳ Aguardando confirmação do pagamento' : '⏳ Pedido criado'}
              </p>
              <p className="text-xs text-gray-500 mt-1">
                Assim que o pagamento for confirmado, você recebe um Magic Link por e-mail e entra direto no curso, sem senha.
              </p>
              {createdOrder.paymentUrl && (
                <a href={createdOrder.paymentUrl} target="_blank" rel="noopener noreferrer"
                  className="text-xs text-brand-600 hover:underline mt-2 inline-block">
                  Abrir pagamento novamente →
                </a>
              )}
              {isDevEnv && (
                <button
                  onClick={() => handleSimulatePayment()}
                  disabled={simulating}
                  className="btn-primary text-xs py-2 px-4 mt-3 disabled:opacity-50"
                >
                  {simulating ? 'Simulando...' : '🧪 Simular pagamento confirmado (sandbox)'}
                </button>
              )}
            </div>
          )}
        </div>

        {/* Benefícios */}
        {course.salesPageBenefits.length > 0 && (
          <div className="card border-gray-200 mb-8">
            <h2 className="text-lg font-semibold text-gray-800 mb-4">O que você vai levar</h2>
            <ul className="grid sm:grid-cols-2 gap-3">
              {course.salesPageBenefits.map((benefit, i) => (
                <li key={i} className="flex items-start gap-2 text-sm text-gray-600">
                  <span className="text-emerald-500 flex-shrink-0">✓</span>
                  {benefit}
                </li>
              ))}
            </ul>
          </div>
        )}

        {/* Depoimentos */}
        {course.testimonials.length > 0 && (
          <div className="card border-gray-200 mb-8">
            <h2 className="text-lg font-semibold text-gray-800 mb-4">O que os alunos dizem</h2>
            <div className="grid sm:grid-cols-2 gap-4">
              {course.testimonials.map((t, i) => (
                <div key={i} className="border border-gray-200 rounded-xl p-4">
                  <p className="text-sm text-gray-600 leading-relaxed">&ldquo;{t.quote}&rdquo;</p>
                  <div className="flex items-center gap-2 mt-3">
                    {t.avatarUrl ? (
                      <img src={t.avatarUrl} alt={t.authorName} className="w-8 h-8 rounded-full object-cover" />
                    ) : (
                      <div className="w-8 h-8 rounded-full bg-gradient-to-br from-brand-500 to-accent-500
                                      flex items-center justify-center text-white text-xs font-bold">
                        {t.authorName[0]?.toUpperCase()}
                      </div>
                    )}
                    <div>
                      <p className="text-xs font-semibold text-gray-800">{t.authorName}</p>
                      {t.authorRole && <p className="text-xs text-gray-400">{t.authorRole}</p>}
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* Conteúdo */}
        <div className="card border-gray-200 mb-8">
          <h2 className="text-lg font-semibold text-gray-800 mb-1">Conteúdo do curso</h2>
          <p className="text-sm text-gray-400 mb-4">
            {course.modules.length} módulos · {totalLessons} aulas
          </p>
          <div className="space-y-2">
            {course.modules
              .slice()
              .sort((a, b) => a.order - b.order)
              .map(module => (
                <div key={module.id} className="border border-gray-200 rounded-lg px-4 py-3">
                  <p className="text-sm font-semibold text-gray-800">{module.title}</p>
                  <p className="text-xs text-gray-400 mt-0.5">{module.lessons.length} aulas</p>
                </div>
              ))}
          </div>
        </div>

        {/* Sobre o professor */}
        {course.instructorName && (
          <div className="card border-gray-200 mb-8">
            <h2 className="text-lg font-semibold text-gray-800 mb-4">Sobre o professor</h2>
            <div className="flex items-start gap-4">
              {course.instructorAvatarUrl ? (
                <img src={course.instructorAvatarUrl} alt={course.instructorName}
                  className="w-14 h-14 rounded-full object-cover flex-shrink-0" />
              ) : (
                <div className="w-14 h-14 rounded-full bg-gradient-to-br from-brand-500 to-accent-500
                                flex items-center justify-center text-white font-bold flex-shrink-0">
                  {course.instructorName[0]?.toUpperCase()}
                </div>
              )}
              <div>
                <p className="font-semibold text-gray-800">{course.instructorName}</p>
                {course.instructorBio && (
                  <p className="text-sm text-gray-500 mt-1">{course.instructorBio}</p>
                )}
              </div>
            </div>
          </div>
        )}

        {/* FAQ */}
        {course.faqItems.length > 0 && (
          <div className="card border-gray-200 mb-8">
            <h2 className="text-lg font-semibold text-gray-800 mb-4">Perguntas frequentes</h2>
            <div className="space-y-4">
              {course.faqItems
                .slice()
                .sort((a, b) => (a.order ?? 0) - (b.order ?? 0))
                .map(faq => (
                  <div key={faq.id} className="border-b border-gray-200/60 pb-4 last:border-0 last:pb-0">
                    <p className="text-sm font-semibold text-gray-800">{faq.question}</p>
                    <p className="text-sm text-gray-500 mt-1">{faq.answer}</p>
                  </div>
                ))}
            </div>
          </div>
        )}

        {/* Cross-sell */}
        {otherCourses.length > 0 && (
          <div className="mb-10">
            <h2 className="text-lg font-semibold text-gray-800 mb-4">Mais cursos deste professor</h2>
            <div className="grid sm:grid-cols-2 gap-4">
              {otherCourses.map(c => (
                <Link key={c.id} href={`/c/${c.slug}`}
                  className="card border-gray-200 hover:border-brand-300 transition-colors p-0 overflow-hidden flex gap-3">
                  {c.thumbnailUrl ? (
                    <img src={c.thumbnailUrl} alt={c.title} className="w-24 h-24 object-cover flex-shrink-0" />
                  ) : (
                    <div className="w-24 h-24 bg-gray-100 flex items-center justify-center text-2xl flex-shrink-0">📚</div>
                  )}
                  <div className="py-3 pr-3 min-w-0">
                    <p className="text-sm font-semibold text-gray-800 truncate">{c.title}</p>
                    <p className="text-xs text-gray-400 mt-1">
                      {commercializationLabel(c.commercializationState, c.price, c.isFree)}
                    </p>
                  </div>
                </Link>
              ))}
            </div>
          </div>
        )}

        <div className="text-center pb-6">
          <button onClick={handleCtaClick} className="btn-primary px-8 py-3 text-base">{ctaText} →</button>
        </div>
      </div>

      {checkoutOpen && course && (
        <CheckoutModal
          title={`Comprar ${course.title}`}
          priceLabel={activePlan
            ? `${formatPrice(activePlan.price)}${billingCycleSuffix[activePlan.billingCycle] ?? ''}`
            : formatPrice(course.price)}
          onClose={() => setCheckoutOpen(false)}
          onConfirm={handleCheckout}
          loading={processingCheckout}
        />
      )}

      {freeEnrollOpen && course && (
        <FreeEnrollModal
          title={`Matricular-se em ${course.title}`}
          onClose={() => setFreeEnrollOpen(false)}
          onConfirm={handleFreeEnroll}
          loading={processingFreeEnroll}
        />
      )}
    </div>
  );
}
