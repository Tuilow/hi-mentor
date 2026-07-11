'use client';

import { useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { coursesApi, enrollmentsApi, courseSubscriptionPlansApi, coursePurchasesApi, subscriptionsApi } from '@/lib/api';
import type { CoursePlan, InstructorCourseSummary } from '@/types';
import Link from 'next/link';
import toast from 'react-hot-toast';
import { AxiosError } from 'axios';

const levelLabel: Record<string, string> = {
  Beginner: 'Iniciante',
  Intermediate: 'Intermediário',
  Advanced: 'Avançado',
};

interface Lesson {
  id: string;
  title: string;
  description?: string;
  order: number;
  durationSeconds?: number;
  isPreview: boolean;
  hasVideo: boolean;
}

interface Module {
  id: string;
  title: string;
  description?: string;
  order: number;
  lessons: Lesson[];
}

interface CourseDetail {
  id: string;
  title: string;
  slug: string;
  description?: string;
  shortDescription?: string;
  thumbnailUrl?: string;
  price: number;
  isFree: boolean;
  level: string;
  totalDurationMinutes?: number;
  publishedAt?: string;
  modules: Module[];
  instructorId: string;
}

// GET /enrollments/courses/{courseId} — 200 com esse shape se o aluno já está matriculado,
// 404 caso contrário. Usado tanto para o progresso quanto como checagem de "já estou matriculado".
interface EnrollmentProgressDto {
  enrollmentId: string;
  courseId: string;
  status: string;
  progressPercentage: number;
}

// GET /course-purchases/me — histórico de compras avulsas do aluno (qualquer curso).
// Usado para saber se já existe uma compra "Pending" deste curso aguardando confirmação
// de pagamento (webhook do Asaas), e mostrar isso na tela em vez de simplesmente nada.
interface CoursePurchaseDto {
  id: string;
  courseId: string;
  amount: number;
  status: string;
  confirmedAt?: string;
  createdAt: string;
}

function formatDuration(seconds?: number): string {
  if (!seconds) return '';
  const m = Math.floor(seconds / 60);
  const s = seconds % 60;
  return m > 0 ? `${m}min${s > 0 ? ` ${s}s` : ''}` : `${s}s`;
}

function formatPrice(price: number): string {
  return price === 0
    ? 'Grátis'
    : price.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

interface CheckoutData {
  customerName: string;
  customerEmail: string;
  cpfCnpj: string;
  phone?: string;
}

/**
 * Modal de checkout genérico — usado tanto para compra avulsa (Compra Única) quanto para
 * assinatura de um produto específico (Assinatura). O rótulo/preço mudam conforme o modo;
 * o formulário e a validação de CPF/CNPJ são os mesmos usados em /assinatura.
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

export default function CourseDetailPage() {
  const { slug } = useParams<{ slug: string }>();
  const router = useRouter();
  const qc = useQueryClient();
  const [openModules, setOpenModules] = useState<Set<string>>(new Set());
  const [enrolling, setEnrolling] = useState(false);
  const [checkoutOpen, setCheckoutOpen] = useState(false);
  const [processingCheckout, setProcessingCheckout] = useState(false);

  const { data: course, isLoading } = useQuery<CourseDetail>({
    queryKey: ['course', slug],
    queryFn: () => coursesApi.getBySlug(slug).then(r => r.data),
    enabled: !!slug,
  });

  // "Estou matriculado neste curso?" — GET /enrollments/courses/{id} retorna 404 quando não há
  // matrícula, então tratamos qualquer erro como "não matriculado" em vez de propagar.
  const { data: enrollment } = useQuery<EnrollmentProgressDto | null>({
    queryKey: ['enrollment-progress', course?.id],
    queryFn: () => enrollmentsApi.getProgress(course!.id).then(r => r.data).catch(() => null),
    enabled: !!course?.id,
  });
  const isEnrolled = !!enrollment;

  // Única fonte de verdade sobre o modelo de monetização do produto: existe (ou não) um Plan
  // vinculado a este curso (CourseId = course.id). Se não existir plano, o produto é Grátis
  // (price 0) ou Compra Única (price > 0) — nunca exige assinatura. Ver
  // Tuilow.Sales.Domain.Entities.Plan.CourseId e SubscriptionRepository.GetPlansByCourseAsync.
  const { data: coursePlans = [] } = useQuery<CoursePlan[]>({
    queryKey: ['course-plans', course?.id],
    queryFn: () => courseSubscriptionPlansApi.getByCourse(course!.id).then(r => r.data),
    enabled: !!course?.id && !course.isFree,
  });
  const subscriptionPlan = coursePlans[0] ?? null;
  const isSubscriptionProduct = !!subscriptionPlan;

  // Compra avulsa (Compra Única) pendente: existe quando o aluno já iniciou uma compra deste
  // curso mas o pagamento ainda não foi confirmado (aguardando webhook do Asaas). É o que faltava
  // pra "clico em Comprar e não aparece nada" — sem isso a compra desaparecia no limbo.
  const { data: myPurchases = [] } = useQuery<CoursePurchaseDto[]>({
    queryKey: ['my-course-purchases'],
    queryFn: () => coursePurchasesApi.getMyPurchases().then(r => r.data),
    enabled: !!course?.id && !course.isFree && !isSubscriptionProduct && !isEnrolled,
  });
  const pendingPurchase = myPurchases.find(p => p.courseId === course?.id && p.status === 'Pending');

  const [simulating, setSimulating] = useState(false);
  // Endpoint de simulação só existe no backend em Development (404 em produção) — este check
  // evita mostrar o botão à toa fora de ambiente de dev/sandbox.
  const isDevEnv = process.env.NODE_ENV !== 'production';

  // Cross-sell: outros cursos publicados deste mesmo criador — audiência própria em vez de
  // curso isolado. Público no backend, mas só faz sentido mostrar depois que o curso carregou.
  const { data: otherCourses = [] } = useQuery<InstructorCourseSummary[]>({
    queryKey: ['instructor-courses', course?.instructorId, course?.id],
    queryFn: () => coursesApi.getByInstructor(course!.instructorId, course!.id).then(r => r.data),
    enabled: !!course?.instructorId,
  });

  const toggleModule = (id: string) =>
    setOpenModules(prev => {
      const next = new Set(prev);
      next.has(id) ? next.delete(id) : next.add(id);
      return next;
    });

  const errorMessage = (e: unknown, fallback: string) =>
    (e as AxiosError<{ title?: string; detail?: string; message?: string }>)
      .response?.data?.detail
    ?? (e as AxiosError<{ title?: string; detail?: string; message?: string }>).response?.data?.title
    ?? (e as AxiosError<{ title?: string; detail?: string; message?: string }>).response?.data?.message
    ?? fallback;

  // Produto Grátis: libera acesso imediatamente, sem checkout.
  const handleEnroll = async () => {
    if (!course) return;
    setEnrolling(true);
    try {
      await enrollmentsApi.enroll(course.id);
      toast.success('Matrícula realizada! Bom estudo! 🎓');
      // router.refresh() sozinho não bastava: os dados desta página vêm do React Query
      // (cache no cliente), então sem invalidar as queries o botão continuava mostrando
      // "Matricular agora" até um F5 manual — era o "não aparece que estou matriculado".
      qc.invalidateQueries({ queryKey: ['enrollment-progress', course.id] });
      qc.invalidateQueries({ queryKey: ['my-enrollments'] });
      qc.invalidateQueries({ queryKey: ['courses'] });
      router.refresh();
    } catch (e: unknown) {
      toast.error(errorMessage(e, 'Erro ao realizar matrícula.'));
    } finally {
      setEnrolling(false);
    }
  };

  // Produto Compra Única ou Assinatura: abre o checkout e envia para o fluxo correto
  // (compra avulsa do curso vs. assinatura do plano do produto). Nunca mistura os dois.
  const handleCheckout = async (data: { customerName: string; customerEmail: string; cpfCnpj: string; phone?: string }) => {
    if (!course) return;
    setProcessingCheckout(true);
    try {
      const paymentUrl = isSubscriptionProduct && subscriptionPlan
        ? (await subscriptionsApi.subscribe({ planId: subscriptionPlan.id, ...data })).data?.paymentUrl
        : (await coursePurchasesApi.purchase({ courseId: course.id, ...data })).data?.paymentUrl;

      setCheckoutOpen(false);
      // Sem isso o pedido "Pending" recém-criado só aparecia depois de um F5 — mesma causa raiz
      // do bug de matrícula (cache do React Query não sabia que algo mudou no servidor).
      qc.invalidateQueries({ queryKey: ['my-course-purchases'] });
      if (paymentUrl) {
        toast.success('Pedido criado! Redirecionando para pagamento...');
        setTimeout(() => window.open(paymentUrl, '_blank'), 800);
      } else {
        toast.success('Pedido criado! Você receberá o link de pagamento por e-mail.');
      }
    } catch (e: unknown) {
      toast.error(errorMessage(
        e, isSubscriptionProduct ? 'Erro ao criar assinatura.' : 'Erro ao processar a compra.'
      ));
    } finally {
      setProcessingCheckout(false);
    }
  };

  // SANDBOX/DEV: substitui o webhook do Asaas (que não alcança localhost) para fechar o loop
  // compra → pagamento confirmado → matrícula automática sem depender de infra externa.
  const handleSimulatePayment = async () => {
    if (!pendingPurchase) return;
    setSimulating(true);
    try {
      await coursePurchasesApi.simulatePayment(pendingPurchase.id);
      toast.success('Pagamento simulado! Acesso liberado. 🎉');
      qc.invalidateQueries({ queryKey: ['enrollment-progress', course!.id] });
      qc.invalidateQueries({ queryKey: ['my-enrollments'] });
      qc.invalidateQueries({ queryKey: ['courses'] });
      qc.invalidateQueries({ queryKey: ['my-course-purchases'] });
    } catch (e: unknown) {
      toast.error(errorMessage(e, 'Erro ao simular pagamento.'));
    } finally {
      setSimulating(false);
    }
  };

  if (isLoading) {
    return (
      <div className="max-w-4xl mx-auto animate-pulse space-y-6">
        <div className="h-8 bg-gray-100 rounded w-2/3" />
        <div className="h-56 bg-gray-100 rounded-xl" />
        <div className="space-y-3">
          {[1,2,3].map(i => <div key={i} className="h-14 bg-gray-100 rounded-lg" />)}
        </div>
      </div>
    );
  }

  if (!course) {
    return (
      <div className="text-center py-20 text-gray-400">
        <p className="text-4xl mb-3">😕</p>
        <p className="text-lg font-medium">Curso não encontrado</p>
        <Link href="/cursos" className="text-brand-600 hover:underline text-sm mt-2 inline-block">
          ← Voltar para cursos
        </Link>
      </div>
    );
  }

  const totalLessons = course.modules.reduce((acc, m) => acc + m.lessons.length, 0);

  return (
    <div className="max-w-4xl mx-auto">
      {/* Breadcrumb */}
      <div className="flex items-center gap-2 text-sm text-gray-400 mb-6">
        <Link href="/cursos" className="hover:text-gray-600 transition-colors">Cursos</Link>
        <span>›</span>
        <span className="text-gray-600 truncate">{course.title}</span>
      </div>

      {/* Hero */}
      <div className="card border-gray-200 mb-6 overflow-hidden p-0">
        {course.thumbnailUrl ? (
          <img src={course.thumbnailUrl} alt={course.title}
            className="w-full h-56 object-cover" />
        ) : (
          <div className="w-full h-56 bg-gradient-to-br from-gray-100 to-gray-50
                          flex items-center justify-center text-6xl">
            📚
          </div>
        )}
        <div className="p-6">
          <div className="flex flex-wrap gap-2 mb-3">
            <span className="badge-purple">{levelLabel[course.level] ?? course.level}</span>
            {course.isFree && <span className="badge-green">Grátis</span>}
          </div>
          <h1 className="text-2xl font-bold text-gray-800 mb-2">{course.title}</h1>
          {course.description && (
            <p className="text-gray-500 text-sm leading-relaxed mb-4">{course.description}</p>
          )}

          <div className="flex flex-wrap items-center gap-6 pt-4 border-t border-gray-200">
            <div className="text-center">
              <p className="text-lg font-bold text-gray-800">{totalLessons}</p>
              <p className="text-xs text-gray-400">Aulas</p>
            </div>
            <div className="text-center">
              <p className="text-lg font-bold text-gray-800">{course.modules.length}</p>
              <p className="text-xs text-gray-400">Módulos</p>
            </div>
            {course.totalDurationMinutes && (
              <div className="text-center">
                <p className="text-lg font-bold text-gray-800">{course.totalDurationMinutes}min</p>
                <p className="text-xs text-gray-400">Duração total</p>
              </div>
            )}
            <div className="ml-auto">
              <p className="text-2xl font-bold gradient-text">{formatPrice(course.price)}</p>
            </div>
          </div>

          <div className="mt-4">
            {/* Já matriculado tem prioridade sobre qualquer CTA de venda — nunca mostra
                Matricular/Comprar/Assinar de novo para quem já tem acesso. */}
            {isEnrolled ? (
              <div className="flex flex-wrap items-center gap-3">
                <span className="badge-green">✓ Você está matriculado</span>
                {course.modules[0]?.lessons[0] && (
                  <Link
                    href={`/cursos/${slug}/${course.modules
                      .slice().sort((a, b) => a.order - b.order)[0]
                      .lessons.slice().sort((a, b) => a.order - b.order)[0].id}`}
                    className="btn-primary text-sm py-2"
                  >
                    Continuar assistindo →
                  </Link>
                )}
              </div>
            ) : course.isFree ? (
              /* Grátis → matrícula direta; Compra Única → Comprar; Assinatura → Assinar.
                 Nunca mistura os três modelos de monetização. */
              <button
                onClick={handleEnroll}
                disabled={enrolling}
                className="btn-primary w-full sm:w-auto disabled:opacity-50"
              >
                {enrolling ? 'Matriculando...' : 'Matricular agora →'}
              </button>
            ) : pendingPurchase ? (
              /* Compra Única com pagamento pendente: em vez de sumir sem feedback, mostra o
                 status real e — só em dev/sandbox — o atalho pra simular a confirmação, já que
                 o webhook do Asaas não alcança localhost. */
              <div className="flex flex-wrap items-center gap-3">
                <span className="badge-yellow">⏳ Pagamento pendente</span>
                {isDevEnv && (
                  <button
                    onClick={handleSimulatePayment}
                    disabled={simulating}
                    className="btn-primary text-sm py-2 disabled:opacity-50"
                  >
                    {simulating ? 'Simulando...' : '🧪 Simular pagamento confirmado (sandbox)'}
                  </button>
                )}
                <button
                  onClick={() => setCheckoutOpen(true)}
                  className="text-xs text-gray-400 hover:text-gray-600 underline"
                >
                  Comprar novamente
                </button>
              </div>
            ) : (
              <button
                onClick={() => setCheckoutOpen(true)}
                className="btn-primary w-full sm:w-auto"
              >
                {isSubscriptionProduct ? 'Assinar plano →' : 'Comprar agora →'}
              </button>
            )}
          </div>
        </div>
      </div>

      {/* Conteúdo do curso */}
      <div>
        <h2 className="text-lg font-semibold text-gray-800 mb-4">Conteúdo do curso</h2>
        <div className="space-y-2">
          {course.modules
            .sort((a, b) => a.order - b.order)
            .map(module => {
              const isOpen = openModules.has(module.id);
              const previewCount = module.lessons.filter(l => l.isPreview).length;
              return (
                <div key={module.id} className="card border-gray-200 p-0 overflow-hidden">
                  {/* Módulo header */}
                  <button
                    onClick={() => toggleModule(module.id)}
                    className="w-full flex items-center justify-between px-5 py-4
                               hover:bg-gray-100/50 transition-colors text-left"
                  >
                    <div className="flex items-center gap-3">
                      <span className="text-xs font-medium text-gray-400 w-5">
                        {module.order}
                      </span>
                      <div>
                        <p className="text-sm font-semibold text-gray-800">{module.title}</p>
                        <p className="text-xs text-gray-400 mt-0.5">
                          {module.lessons.length} aulas
                          {previewCount > 0 && ` · ${previewCount} gratuita${previewCount > 1 ? 's' : ''}`}
                        </p>
                      </div>
                    </div>
                    <span className="text-gray-400 transition-transform duration-200"
                          style={{ transform: isOpen ? 'rotate(180deg)' : 'rotate(0)' }}>
                      ▾
                    </span>
                  </button>

                  {/* Aulas */}
                  {isOpen && (
                    <div className="border-t border-gray-200">
                      {module.lessons
                        .sort((a, b) => a.order - b.order)
                        .map((lesson, idx) => (
                          <div key={lesson.id}
                            className="flex items-center gap-3 px-5 py-3
                                       border-b border-gray-200/50 last:border-0
                                       hover:bg-gray-100/30 transition-colors group">
                            <span className="text-xs text-gray-400 w-5 flex-shrink-0">
                              {idx + 1}
                            </span>
                            <span className="text-base flex-shrink-0">
                              {lesson.hasVideo
                                ? (lesson.isPreview || isEnrolled ? '▶️' : '🔒')
                                : '📝'}
                            </span>
                            <div className="flex-1 min-w-0">
                              <p className="text-sm text-gray-700 truncate">{lesson.title}</p>
                              {lesson.durationSeconds && (
                                <p className="text-xs text-gray-400">
                                  {formatDuration(lesson.durationSeconds)}
                                </p>
                              )}
                            </div>
                            {(lesson.isPreview || isEnrolled) && lesson.hasVideo && (
                              <Link
                                href={`/cursos/${slug}/${lesson.id}`}
                                className="text-xs text-brand-600 font-medium
                                           opacity-0 group-hover:opacity-100
                                           transition-opacity hover:text-brand-700"
                              >
                                Assistir →
                              </Link>
                            )}
                            {!lesson.isPreview && !isEnrolled && (
                              <span className="text-xs text-gray-400">Conteúdo bloqueado</span>
                            )}
                          </div>
                        ))}
                    </div>
                  )}
                </div>
              );
            })}
        </div>
      </div>

      {/* Cross-sell: audiência própria do criador, não curso isolado */}
      {otherCourses.length > 0 && (
        <div className="mt-8">
          <h2 className="text-lg font-semibold text-gray-800 mb-4">Mais cursos deste professor</h2>
          <div className="grid sm:grid-cols-2 gap-4">
            {otherCourses.map(c => (
              <Link key={c.id} href={`/cursos/${c.slug}`}
                className="card border-gray-200 hover:border-brand-300 transition-colors p-0 overflow-hidden flex gap-3">
                {c.thumbnailUrl ? (
                  <img src={c.thumbnailUrl} alt={c.title} className="w-24 h-24 object-cover flex-shrink-0" />
                ) : (
                  <div className="w-24 h-24 bg-gray-100 flex items-center justify-center text-2xl flex-shrink-0">📚</div>
                )}
                <div className="py-3 pr-3 min-w-0">
                  <p className="text-sm font-semibold text-gray-800 truncate">{c.title}</p>
                  <p className="text-xs text-gray-400 mt-1">{formatPrice(c.price)}</p>
                </div>
              </Link>
            ))}
          </div>
        </div>
      )}

      {checkoutOpen && course && (
        <CheckoutModal
          title={isSubscriptionProduct ? `Assinar ${course.title}` : `Comprar ${course.title}`}
          priceLabel={
            isSubscriptionProduct && subscriptionPlan
              ? `R$ ${subscriptionPlan.price.toFixed(2).replace('.', ',')}${
                  subscriptionPlan.billingCycle === 'Monthly' ? '/mês'
                  : subscriptionPlan.billingCycle === 'Quarterly' ? '/trimestre'
                  : subscriptionPlan.billingCycle === 'Semiannual' ? '/semestre'
                  : '/ano'}`
              : formatPrice(course.price)
          }
          onClose={() => setCheckoutOpen(false)}
          onConfirm={handleCheckout}
          loading={processingCheckout}
        />
      )}
    </div>
  );
}
