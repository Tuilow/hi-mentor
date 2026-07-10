'use client';

import { useEffect, useRef, useState } from 'react';
import { useParams } from 'next/navigation';
import { useQuery } from '@tanstack/react-query';
import Link from 'next/link';
import { coursesApi } from '@/lib/api';
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
 * Página de Vendas Pública (Kit de Divulgação) — não exige login. É para onde apontam o link
 * direto, o QR Code, o embed e o botão HTML gerados na aba "Divulgar" do dashboard do produto.
 * Usa os mesmos dados já persistidos em Course (SalesPageHeadline/Benefits/FaqItems) que a IA
 * de página de vendas já gera — nenhum dado novo, só uma leitura pública deles.
 */
export default function PublicSalesPage() {
  const { slug } = useParams<{ slug: string }>();
  const [isLoggedIn, setIsLoggedIn] = useState(false);
  const viewRecordedRef = useRef(false);

  useEffect(() => {
    setIsLoggedIn(!!localStorage.getItem('access_token'));
  }, []);

  const { data: course, isLoading } = useQuery<ProductDetail>({
    queryKey: ['public-course', slug],
    queryFn: () => coursesApi.getBySlug(slug).then(r => r.data),
    enabled: !!slug,
  });

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

  // Checkout/matrícula exigem conta — o visitante anônimo é levado a criar conta (ou logar) e
  // volta automaticamente para a página autenticada do curso, que já tem todo o fluxo de
  // Matricular/Comprar/Assinar pronto. Não duplicamos checkout aqui.
  const ctaHref = course
    ? (isLoggedIn ? `/cursos/${slug}` : `/registro?returnUrl=${encodeURIComponent(`/cursos/${slug}`)}`)
    : '#';

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

        {course.thumbnailUrl ? (
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
          <a href={ctaHref} className="btn-primary px-8 py-3 text-base">{ctaText} →</a>
          <p className="text-xs text-gray-400">Pagamento seguro via PIX, cartão ou boleto.</p>
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
          <a href={ctaHref} className="btn-primary px-8 py-3 text-base">{ctaText} →</a>
        </div>
      </div>
    </div>
  );
}
