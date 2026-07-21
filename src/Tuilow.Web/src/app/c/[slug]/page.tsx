'use client';

import { useEffect, useRef, useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import { useQuery } from '@tanstack/react-query';
import Link from 'next/link';
import toast from 'react-hot-toast';
import { AxiosError } from 'axios';
import { coursesApi, coursePurchasesApi, enrollmentsApi } from '@/lib/api';
import type { ProductDetail, InstructorCourseSummary } from '@/types';

const levelLabel: Record<string, string> = {
  Beginner: 'Iniciante',
  Intermediate: 'Intermediário',
  Advanced: 'Avançado',
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
  const [createdPurchase, setCreatedPurchase] = useState<{ id: string; paymentUrl?: string } | null>(null);
  const [simulating, setSimulating] = useState(false);
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

  // Grátis continua exigindo conta (matrícula é um endpoint autenticado) — leva para o
  // cadastro, que já volta pra cá com returnUrl. Pago: checkout embutido, sem sair da página
  // e sem precisar de conta prévia (o diferencial da Tuilow — ver PurchaseCourseCommandHandler).
  const ctaHref = course && course.isFree
    ? (isLoggedIn ? `/cursos/${slug}` : `/registro?returnUrl=${encodeURIComponent(`/cursos/${slug}`)}`)
    : undefined;

  const handleCtaClick = () => {
    if (!course) return;
    if (course.isFree) return; // navegação normal via <a href>
    setCreatedPurchase(null);
    setCheckoutOpen(true);
  };

  const handleCheckout = async (data: CheckoutData) => {
    if (!course) return;
    setProcessingCheckout(true);
    try {
      const { data: purchase } = await coursePurchasesApi.purchase({ courseId: course.id, ...data });
      setCheckoutOpen(false);
      setCreatedPurchase({ id: purchase.coursePurchaseId, paymentUrl: purchase.paymentUrl });
      if (purchase.paymentUrl) {
        toast.success('Pedido criado! Redirecionando para pagamento...');
        setTimeout(() => window.open(purchase.paymentUrl, '_blank'), 800);
      } else {
        toast.success('Pedido criado! Você receberá o link de pagamento por e-mail.');
      }
    } catch (e: unknown) {
      toast.error(errorMessage(e, 'Erro ao processar a compra.'));
    } finally {
      setProcessingCheckout(false);
    }
  };

  // SANDBOX/DEV: substitui o webhook do Asaas (que não alcança localhost), disponível mesmo
  // sem login — fecha o loop compra anônima → pagamento confirmado → conta criada → Magic Link
  // por e-mail, tudo testável direto da própria Landing Page.
  const handleSimulatePayment = async () => {
    if (!createdPurchase) return;
    setSimulating(true);
    try {
      await coursePurchasesApi.simulatePayment(createdPurchase.id);
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

  // Aluno já matriculado: em vez de mostrar a página de vendas (mesmo que só por um instante
  // enquanto o redirect do useEffect acima dispara), mostra um loading dedicado. Evita o "flash"
  // do botão Comprar/checkout pra quem já é aluno.
  if (isLoggedIn === true && (enrollmentLoading || isEnrolled)) {
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
  const ctaText = course.salesPageCtaText || (course.isFree ? 'Começar grátis' : 'Quero este curso');

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
            {course.isFree && <span className="badge-green">Grátis</span>}
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
          <p className="text-3xl font-bold gradient-text">{formatPrice(course.price)}</p>
          {ctaHref ? (
            <a href={ctaHref} className="btn-primary px-8 py-3 text-base">{ctaText} →</a>
          ) : (
            <button onClick={handleCtaClick} className="btn-primary px-8 py-3 text-base">{ctaText} →</button>
          )}
          <p className="text-xs text-gray-400">Pagamento seguro via PIX, cartão ou boleto.</p>
          {(course.guaranteeDays || course.guaranteeText) && (
            <p className="text-xs text-emerald-600 font-medium flex items-center gap-1">
              🛡️ {course.guaranteeText || `Garantia de ${course.guaranteeDays} dias — satisfação ou seu dinheiro de volta`}
            </p>
          )}

          {createdPurchase && (
            <div className="w-full max-w-sm mt-4 card border-brand-200 bg-brand-50/40 text-center">
              <p className="text-sm font-medium text-gray-700">
                {createdPurchase.paymentUrl ? '⏳ Aguardando confirmação do pagamento' : '⏳ Pedido criado'}
              </p>
              <p className="text-xs text-gray-500 mt-1">
                Assim que o pagamento for confirmado, você recebe um Magic Link por e-mail e entra direto no curso, sem senha.
              </p>
              {createdPurchase.paymentUrl && (
                <a href={createdPurchase.paymentUrl} target="_blank" rel="noopener noreferrer"
                  className="text-xs text-brand-600 hover:underline mt-2 inline-block">
                  Abrir pagamento novamente →
                </a>
              )}
              {isDevEnv && (
                <button
                  onClick={handleSimulatePayment}
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
                    <p className="text-xs text-gray-400 mt-1">{formatPrice(c.price)}</p>
                  </div>
                </Link>
              ))}
            </div>
          </div>
        )}

        <div className="text-center pb-6">
          {ctaHref ? (
            <a href={ctaHref} className="btn-primary px-8 py-3 text-base">{ctaText} →</a>
          ) : (
            <button onClick={handleCtaClick} className="btn-primary px-8 py-3 text-base">{ctaText} →</button>
          )}
        </div>
      </div>

      {checkoutOpen && course && (
        <CheckoutModal
          title={`Comprar ${course.title}`}
          priceLabel={formatPrice(course.price)}
          onClose={() => setCheckoutOpen(false)}
          onConfirm={handleCheckout}
          loading={processingCheckout}
        />
      )}
    </div>
  );
}
