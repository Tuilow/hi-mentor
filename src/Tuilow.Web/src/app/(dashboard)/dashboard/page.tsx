'use client';

import { useState } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { authApi, enrollmentsApi } from '@/lib/api';
import type { MyEnrollment, ContinueWatching } from '@/types';
import Link from 'next/link';
import toast from 'react-hot-toast';

const quickActions = [
  { href: '/cursos',  icon: '📚', title: 'Explorar Cursos', desc: 'Encontre o curso ideal para você',   color: 'from-blue-50 to-blue-100/60' },
  { href: '/assinatura', icon: '💎', title: 'Assinatura',   desc: 'Gerencie seu plano atual',            color: 'from-orange-50 to-rose-50' },
];

export default function DashboardPage() {
  const [becomingCreator, setBecomingCreator] = useState(false);
  const queryClient = useQueryClient();

  // Mesma query key usada no sidebar (DashboardLayout) — um único cache compartilhado evita
  // duas chamadas redundantes a /auth/me a cada carregamento de página (eram duas requisições
  // concorrentes independentes, o que deixava o carregamento mais lento e multiplicava as
  // chances de uma delas cair num 401 por token perto de expirar) e permite atualizar sidebar
  // + conteúdo juntos com um simples invalidateQueries, sem reload de página.
  const { data: user } = useQuery({
    queryKey: ['user-profile'],
    queryFn: () => authApi.me().then(r => r.data),
  });

  // Mesma query key usada em /cursos (filtro "Matriculados") — um único cache alimenta os dois
  // lugares. Antes os 4 cards abaixo eram só '—' fixo, nunca conectados a dado nenhum.
  const { data: myEnrollments = [] } = useQuery<MyEnrollment[]>({
    queryKey: ['my-enrollments'],
    queryFn: () => enrollmentsApi.getMyEnrollments().then(r => r.data),
  });

  // "Continuar assistindo" — última aula tocada, entre todos os cursos matriculados.
  const { data: continueWatching } = useQuery<ContinueWatching | null>({
    queryKey: ['continue-watching'],
    queryFn: () => enrollmentsApi.getContinueWatching().then(r => r.data).catch(() => null),
  });

  const totalCompletedLessons = myEnrollments.reduce((acc, e) => acc + e.completedLessonsCount, 0);
  const avgProgress = myEnrollments.length > 0
    ? Math.round(myEnrollments.reduce((acc, e) => acc + e.progressPercentage, 0) / myEnrollments.length)
    : 0;
  const certificatesCount = myEnrollments.filter(e => e.status === 'Completed').length;

  const isCreator = user?.roles?.includes('Creator') || user?.roles?.includes('Admin');

  const handleBecomeCreator = async () => {
    setBecomingCreator(true);
    try {
      // O backend já devolve um access token novo (com o claim de role "Creator") e regrava o
      // cookie HttpOnly de refresh token — evita uma chamada extra a /auth/refresh-token logo em
      // seguida, que competia pelo refresh token de uso único e causava um erro intermitente aqui.
      const { data } = await authApi.becomeCreator();
      localStorage.setItem('access_token', data.accessToken);

      // Invalida o cache em vez de recarregar a página inteira: sidebar e conteúdo atualizam
      // na hora com os novos recursos de criador (sem repetir toda a dança de autenticação de
      // um reload completo, que era o que deixava lento e, ocasionalmente, deslogava o usuário
      // se algum token estivesse perto de expirar durante o reload).
      await queryClient.invalidateQueries({ queryKey: ['user-profile'] });
      toast.success('Você agora é um criador de conteúdo! Veja "Gerenciar Cursos" no menu.');
    } catch {
      toast.error('Não foi possível ativar o modo criador. Tente novamente.');
    } finally {
      setBecomingCreator(false);
    }
  };

  return (
    <div className="max-w-4xl mx-auto">
      {/* Header */}
      <div className="mb-8">
        <h1 className="text-2xl font-bold text-gray-800">
          Olá, {user?.firstName ?? '...'}! 👋
        </h1>
        <p className="text-gray-500 mt-1">Continue de onde parou ou explore novos cursos.</p>
      </div>

      {/* Continuar assistindo — última aula tocada, entre todos os cursos matriculados */}
      {continueWatching && (
        <Link
          href={`/cursos/${continueWatching.courseSlug}/${continueWatching.lessonId}`}
          className="card border-gray-200 hover:border-blue-200 mb-8 flex items-center gap-4 group transition-all duration-200"
        >
          <div className="w-20 h-14 rounded-lg bg-gray-100 flex-shrink-0 overflow-hidden flex items-center justify-center">
            {continueWatching.thumbnailUrl ? (
              // eslint-disable-next-line @next/next/no-img-element
              <img src={continueWatching.thumbnailUrl} alt="" className="w-full h-full object-cover" />
            ) : (
              <span className="text-xl">▶️</span>
            )}
          </div>
          <div className="flex-1 min-w-0">
            <p className="text-xs text-gray-500 uppercase tracking-wider font-semibold">Continuar assistindo</p>
            <p className="font-semibold text-gray-800 truncate group-hover:text-blue-700">
              {continueWatching.lessonTitle}
            </p>
            <p className="text-xs text-gray-500 truncate">{continueWatching.courseTitle}</p>
          </div>
          <div className="flex-shrink-0 flex items-center gap-3">
            <div className="w-24 h-1.5 rounded-full bg-gray-200 overflow-hidden hidden sm:block">
              <div
                className="h-full bg-blue-500 rounded-full"
                style={{ width: `${Math.min(100, Math.round(continueWatching.courseProgressPercentage))}%` }}
              />
            </div>
            <span className="btn-primary whitespace-nowrap">Continuar</span>
          </div>
        </Link>
      )}

      {/* Become a Creator banner — plataforma aberta: qualquer pessoa pode publicar cursos */}
      {!isCreator && (
        <div className="card border-violet-100 bg-violet-50/60 mb-8 flex items-center justify-between gap-4">
          <div>
            <p className="font-semibold text-gray-800">Quer publicar seus próprios cursos?</p>
            <p className="text-sm text-gray-500 mt-0.5">
              Torne-se um criador Tuilow: sem mensalidade, sem aprovação. Você só paga uma comissão quando vende.
            </p>
          </div>
          <button
            onClick={handleBecomeCreator}
            disabled={becomingCreator}
            className="btn-primary whitespace-nowrap flex-shrink-0 disabled:opacity-60"
          >
            {becomingCreator ? 'Ativando...' : 'Tornar-se criador 🎬'}
          </button>
        </div>
      )}

      {/* Stats */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-8">
        {[
          { label: 'Cursos',       value: String(myEnrollments.length), sub: 'matriculados' },
          { label: 'Aulas',        value: String(totalCompletedLessons), sub: 'concluídas' },
          { label: 'Progresso',    value: `${avgProgress}%`, sub: 'médio' },
          { label: 'Certificados', value: String(certificatesCount), sub: 'conquistados' },
        ].map(s => (
          <div key={s.label} className="card border-gray-200 text-center">
            <p className="text-2xl font-bold gradient-text">{s.value}</p>
            <p className="text-xs text-gray-500 mt-1">{s.sub}</p>
          </div>
        ))}
      </div>

      {/* Quick actions */}
      <h2 className="text-sm font-semibold text-gray-500 uppercase tracking-wider mb-4">Acesso rápido</h2>
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {quickActions.map(action => (
          <Link
            key={action.href}
            href={action.href}
            className={`card border-gray-200 hover:border-blue-200 bg-gradient-to-br ${action.color}
                        transition-all duration-200 group cursor-pointer`}
          >
            <div className="text-2xl mb-3 group-hover:scale-110 transition-transform duration-200">
              {action.icon}
            </div>
            <h3 className="font-semibold text-gray-800 group-hover:text-blue-700">{action.title}</h3>
            <p className="text-xs text-gray-500 mt-1">{action.desc}</p>
          </Link>
        ))}
      </div>
    </div>
  );
}
