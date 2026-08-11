'use client';

import { useState } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { authApi, enrollmentsApi, setAccessToken } from '@/lib/api';
import type { MyEnrollment, ContinueWatching } from '@/types';
import Link from 'next/link';
import toast from 'react-hot-toast';
import { Play, BookOpen, CheckCircle2, TrendingUp, Rocket } from 'lucide-react';
import { Card, StatCard, Progress, Button } from '@/components/ui/design-system';

// Redesign "Hi Mentor" (ago/2026): anatomia (cabeçalho + "continuar assistindo" +
// stat cards) seguindo hi-mentor/src/pages/MentorDashboard.tsx e MentoradoHome.tsx —
// mas só as seções com dado real hoje. O protótipo também tem um feed de "atividade
// recente", agenda de "próximos encontros" e lista de "mentorados em risco"; nenhum
// desses dados existe no backend do Tuilow ainda, então essas seções ficam de fora
// (ver RELATORIO_REDESIGN_HI_MENTOR.md) em vez de mostrar números inventados.
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

  // "Continuar assistindo" — última aula tocada, entre todos os programas matriculados.
  const { data: continueWatching } = useQuery<ContinueWatching | null>({
    queryKey: ['continue-watching'],
    queryFn: () => enrollmentsApi.getContinueWatching().then(r => r.data).catch(() => null),
  });

  const totalCompletedLessons = myEnrollments.reduce((acc, e) => acc + e.completedLessonsCount, 0);
  const avgProgress = myEnrollments.length > 0
    ? Math.round(myEnrollments.reduce((acc, e) => acc + e.progressPercentage, 0) / myEnrollments.length)
    : 0;
  // Achado M7 da avaliação: chamava-se "certificados conquistados", mas nenhum certificado é
  // emitido de fato (ver achado A4) — é só a contagem de matrículas com status Completed. Manter
  // o rótulo antigo prometia um documento que o aluno não consegue baixar. Renomeado para
  // "programas concluídos" (a métrica em si está correta, só o nome estava errado) até a emissão
  // real de certificados existir.
  const completedCoursesCount = myEnrollments.filter(e => e.status === 'Completed').length;

  const isCreator = user?.roles?.includes('Creator') || user?.roles?.includes('Admin');

  const handleBecomeCreator = async () => {
    setBecomingCreator(true);
    try {
      // O backend já devolve um access token novo (com o claim de role "Creator") e regrava o
      // cookie HttpOnly de refresh token — evita uma chamada extra a /auth/refresh-token logo em
      // seguida, que competia pelo refresh token de uso único e causava um erro intermitente aqui.
      // Nota: refresh token não é mais gravado aqui — ele só existe no cookie HttpOnly (achado
      // C1), nunca no corpo da resposta nem em localStorage.
      const { data } = await authApi.becomeCreator();
      setAccessToken(data.accessToken);

      // Invalida o cache em vez de recarregar a página inteira: sidebar e conteúdo atualizam
      // na hora com os novos recursos de mentor (sem repetir toda a dança de autenticação de
      // um reload completo, que era o que deixava lento e, ocasionalmente, deslogava o usuário
      // se algum token estivesse perto de expirar durante o reload).
      await queryClient.invalidateQueries({ queryKey: ['user-profile'] });
      toast.success('Você agora é um mentor na Hi Mentor! Veja "Meus Programas" no menu.');
    } catch {
      toast.error('Não foi possível ativar o modo mentor. Tente novamente.');
    } finally {
      setBecomingCreator(false);
    }
  };

  const stats = [
    { label: 'Programas', value: String(myEnrollments.length), icon: <BookOpen size={20} className="text-violet-600" />, iconBg: 'bg-violet-50' },
    { label: 'Aulas concluídas', value: String(totalCompletedLessons), icon: <CheckCircle2 size={20} className="text-emerald-600" />, iconBg: 'bg-emerald-50' },
    { label: 'Progresso médio', value: `${avgProgress}%`, icon: <TrendingUp size={20} className="text-indigo-600" />, iconBg: 'bg-indigo-50' },
    { label: 'Programas concluídos', value: String(completedCoursesCount), icon: <Rocket size={20} className="text-amber-600" />, iconBg: 'bg-amber-50' },
  ];

  return (
    <div className="max-w-4xl mx-auto">
      {/* Header */}
      <div className="mb-8">
        <h1 className="text-2xl font-bold text-ink mb-1" style={{ letterSpacing: '-0.025em' }}>
          Olá, {user?.firstName ?? '...'}! 👋
        </h1>
        <p className="text-ink-3">Continue de onde parou ou explore novos programas.</p>
      </div>

      {/* Continuar assistindo — última aula tocada, entre todos os programas matriculados */}
      {continueWatching && (
        <Link href={`/cursos/${continueWatching.courseSlug}/${continueWatching.lessonId}`}>
          <Card hover className="mb-6 flex items-center gap-4">
            <div className="w-20 h-14 rounded-xl bg-subtle shrink-0 overflow-hidden flex items-center justify-center">
              {continueWatching.thumbnailUrl ? (
                // eslint-disable-next-line @next/next/no-img-element
                <img src={continueWatching.thumbnailUrl} alt="" className="w-full h-full object-cover" />
              ) : (
                <Play size={20} className="text-ink-3" fill="currentColor" />
              )}
            </div>
            <div className="flex-1 min-w-0">
              <p className="text-xs text-ink-3 uppercase tracking-wider font-semibold">Continuar assistindo</p>
              <p className="font-semibold text-ink truncate">{continueWatching.lessonTitle}</p>
              <p className="text-xs text-ink-3 truncate">{continueWatching.courseTitle}</p>
            </div>
            <div className="shrink-0 flex items-center gap-3">
              <div className="w-24 hidden sm:block">
                <Progress value={continueWatching.courseProgressPercentage} size="xs" />
              </div>
              <Button size="sm" icon={<Play size={14} fill="currentColor" />}>Continuar</Button>
            </div>
          </Card>
        </Link>
      )}

      {/* Torne-se mentor — plataforma aberta: qualquer pessoa pode publicar programas */}
      {!isCreator && (
        <Card className="mb-6 bg-violet-50/60 border-violet-100 flex items-center justify-between gap-4 flex-wrap">
          <div>
            <p className="font-semibold text-ink">Quer publicar seus próprios programas de mentoria?</p>
            <p className="text-sm text-ink-3 mt-0.5">
              Torne-se um mentor Hi Mentor: sem mensalidade, sem aprovação. Você só paga uma comissão quando vende.
            </p>
          </div>
          <Button onClick={handleBecomeCreator} loading={becomingCreator} className="shrink-0">
            {becomingCreator ? 'Ativando...' : 'Tornar-se mentor'}
          </Button>
        </Card>
      )}

      {/* Stats */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        {stats.map(s => (
          <StatCard key={s.label} {...s} />
        ))}
      </div>
    </div>
  );
}
