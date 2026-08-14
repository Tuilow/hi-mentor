'use client';

// Redesign "Hi Mentor" (ago/2026) — tela de jornada do mentorado. Renderizada por
// ./page.tsx quando isEnrolled === true (aluno já matriculado neste curso): troca a tela de
// "vitrine/compra" pela experiência de curso comprado, seguindo o layout de
// hi-mentor's MentoradoHome.tsx (protótipo Figma Make) — menu superior próprio (em vez do
// sidebar do dashboard), hero com progresso, "Você parou em", "Seu próximo passo", "Meus
// programas" e o accordion "Sua jornada". O sidebar/menu do mentor (Meus Produtos etc.) não é
// tocado — esta view só aparece para quem está matriculado (nunca para o instrutor dono do
// curso, que não tem Enrollment do próprio produto).
//
// Todos os dados são reais: progresso por aula vem de GetEnrollmentProgressQueryHandler
// (lessonProgress[], já devolvido pelo backend hoje — só não estava tipado aqui antes, ver
// page.tsx), nome do instrutor vem de GetCourseBySlugQueryHandler (IInstructorLookup), e "Meus
// programas" reusa a mesma query GetMyEnrollments já usada em /meus-cursos. "Comunidade" e
// "Encontros" (nav) e "Próximos encontros" não têm contrapartida real no backend (sem sistema de
// posts nem agenda de encontros ao vivo) — mantidos visíveis com selo "Em breve" (decisão do
// usuário), sem nenhum dado fabricado (a seção de encontros nunca lista reuniões falsas).
//
// Escape do sidebar do dashboard: como esta rota vive dentro do grupo (dashboard) — e por isso
// herda o layout com sidebar fixo (ver (dashboard)/layout.tsx) — este componente usa um overlay
// "fixed inset-0" com z-index acima do sidebar (z-30) e do overlay mobile (z-20) para ocupar a
// tela inteira com o menu superior próprio, replicando fielmente o protótipo (que não tem
// sidebar nesta tela) sem precisar mover a rota para fora do grupo (o que quebraria toda a
// navegação do dashboard). Nenhum CSS ancestral usa transform, então "fixed" continua relativo à
// viewport normalmente.

import { useMemo, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import Link from 'next/link';
import toast from 'react-hot-toast';
import { Play, ChevronRight, BookOpen, ArrowRight, CheckCircle, Circle, Download, Loader2 } from 'lucide-react';
import { authApi, enrollmentsApi, certificatesApi } from '@/lib/api';
import type { MyEnrollment, MyCertificate } from '@/types';
import { Card, Progress, Avatar } from '@/components/ui/design-system';
import Logo from '@/components/brand/Logo';
import type { CourseDetail, EnrollmentProgressDto, LessonProgressDto } from './page';

function formatDuration(seconds?: number): string {
  if (!seconds || seconds <= 0) return '';
  const m = Math.floor(seconds / 60);
  const s = seconds % 60;
  return m > 0 ? `${m}min${s > 0 ? ` ${s}s` : ''}` : `${s}s`;
}

/** Selo "Em breve" — mesmo padrão adotado na home pública (SoonBadge) pra seções/itens que
 * existem no protótipo mas ainda não têm contrapartida real no backend. */
function SoonPill() {
  return (
    <span className="inline-flex items-center px-1.5 py-0.5 rounded-full bg-amber-50 text-amber-700 text-[10px] font-bold border border-amber-200">
      Em breve
    </span>
  );
}

interface FlatLesson {
  id: string;
  title: string;
  hasVideo: boolean;
  durationSeconds?: number;
  moduleTitle: string;
  moduleIndex: number;
  lessonIndex: number;
}

export default function MentoradoJourneyView({
  slug, course, enrollment,
}: {
  slug: string;
  course: CourseDetail;
  enrollment: EnrollmentProgressDto;
}) {
  // Mesmas query keys usadas em (dashboard)/layout.tsx e /meus-cursos — compartilham cache,
  // sem chamada nova redundante a /auth/me ou /enrollments/me.
  const { data: user } = useQuery({
    queryKey: ['user-profile'],
    queryFn: () => authApi.me().then(res => res.data),
  });

  const { data: myEnrollments = [] } = useQuery<MyEnrollment[]>({
    queryKey: ['my-enrollments'],
    queryFn: () => enrollmentsApi.getMyEnrollments().then(r => r.data),
  });

  const progressByLessonId = useMemo(() => {
    const map = new Map<string, LessonProgressDto>();
    (enrollment.lessonProgress ?? []).forEach(p => map.set(p.lessonId, p));
    return map;
  }, [enrollment.lessonProgress]);

  const orderedModules = useMemo(
    () => course.modules.slice().sort((a, b) => a.order - b.order).map(m => ({
      ...m,
      lessons: m.lessons.slice().sort((a, b) => a.order - b.order),
    })),
    [course.modules]
  );

  const flatLessons: FlatLesson[] = useMemo(() => {
    const flat: FlatLesson[] = [];
    orderedModules.forEach((m, mi) => {
      m.lessons.forEach((l, li) => {
        flat.push({
          id: l.id,
          title: l.title,
          hasVideo: l.hasVideo,
          durationSeconds: l.durationSeconds,
          moduleTitle: m.title,
          moduleIndex: mi + 1,
          lessonIndex: li + 1,
        });
      });
    });
    return flat;
  }, [orderedModules]);

  // Próxima aula não concluída, na ordem do curso — não existe nenhum bloqueio sequencial real
  // no backend hoje (uma vez matriculado, todas as aulas com vídeo já ficam acessíveis, ver
  // page.tsx), então "current" é só uma indicação de "por onde continuar", nunca um "locked".
  const nextLesson = flatLessons.find(l => !progressByLessonId.get(l.id)?.isCompleted);
  const isCompleted = !nextLesson && flatLessons.length > 0;

  // Feature 12/08/2026 ("Baixar certificado"): só busca depois que o curso está 100% concluído
  // (enabled: isCompleted) — antes disso o backend nunca emitiu o certificado (ver
  // CourseCompletedEventHandler), então a chamada seria sempre um 404 sem sentido. 404 aqui não é
  // erro (é o estado normal logo após a última aula ser marcada, antes do evento processar), por
  // isso não usamos `isError`/toast neste useQuery — o botão simplesmente não aparece até existir.
  const { data: certificate } = useQuery<MyCertificate | null>({
    queryKey: ['course-certificate', course.id],
    queryFn: () => certificatesApi.getForCourse(course.id).then(r => r.data).catch(() => null),
    enabled: isCompleted,
  });
  const [downloadingCertificate, setDownloadingCertificate] = useState(false);

  const handleDownloadCertificate = async () => {
    if (!certificate) return;
    setDownloadingCertificate(true);
    try {
      const { data } = await certificatesApi.download(certificate.certificateId);
      const blobUrl = URL.createObjectURL(data as Blob);
      const link = document.createElement('a');
      link.href = blobUrl;
      link.download = `certificado-${course.title}.pdf`;
      document.body.appendChild(link);
      link.click();
      link.remove();
      URL.revokeObjectURL(blobUrl);
    } catch {
      toast.error('Não foi possível baixar o certificado agora. Tente de novo em instantes.');
    } finally {
      setDownloadingCertificate(false);
    }
  };

  const lastWatched = useMemo(() => {
    const entries = enrollment.lessonProgress ?? [];
    if (entries.length === 0) return null;
    const latest = entries.slice().sort(
      (a, b) => new Date(b.lastWatchedAt).getTime() - new Date(a.lastWatchedAt).getTime()
    )[0];
    const lesson = flatLessons.find(l => l.id === latest.lessonId);
    return lesson ? { lesson, progress: latest } : null;
  }, [enrollment.lessonProgress, flatLessons]);

  const continueTarget = lastWatched?.lesson.id ?? nextLesson?.id ?? flatLessons[0]?.id;
  const otherEnrollments = myEnrollments.filter(e => e.courseId !== course.id);
  const userName = user ? `${user.firstName ?? ''} ${user.lastName ?? ''}`.trim() : '';

  const soon = () => toast('Em breve nesta plataforma. 🚧');

  return (
    <div className="fixed inset-0 z-40 bg-canvas overflow-y-auto">
      {/* Top nav */}
      <header className="bg-surface border-b border-line sticky top-0 z-10">
        <div className="max-w-5xl mx-auto px-6 py-3.5 flex items-center justify-between gap-4">
          <Logo size="sm" />
          <nav className="hidden md:flex items-center gap-6">
            <span className="text-sm font-semibold text-violet-600 cursor-default">Jornada</span>
            <Link href="/meus-cursos" className="text-sm font-medium text-ink-2 hover:text-ink transition-colors">
              Programas
            </Link>
            <button onClick={soon} className="flex items-center gap-1.5 text-sm font-medium text-ink-3 hover:text-ink transition-colors cursor-pointer">
              Comunidade <SoonPill />
            </button>
            <button onClick={soon} className="flex items-center gap-1.5 text-sm font-medium text-ink-3 hover:text-ink transition-colors cursor-pointer">
              Encontros <SoonPill />
            </button>
          </nav>
          <div className="flex items-center gap-3">
            <Link href="/dashboard" className="hidden md:block text-xs font-semibold text-ink-3 hover:text-ink transition-colors">
              Voltar ao painel
            </Link>
            <Avatar name={userName || user?.email || '?'} src={user?.avatarUrl} size="sm" />
          </div>
        </div>
      </header>

      {/* Hero */}
      <section
        className="relative overflow-hidden"
        style={{
          background: 'linear-gradient(135deg, oklch(22% 0.15 293) 0%, oklch(36% 0.24 293) 50%, oklch(28% 0.18 280) 100%)',
        }}
      >
        <div className="relative max-w-5xl mx-auto px-6 py-14 flex flex-col md:flex-row items-start md:items-center justify-between gap-8">
          <div className="flex-1 min-w-0">
            <span className="inline-flex mb-4 px-2.5 py-0.5 rounded-full text-xs font-semibold border border-white/20 text-white/70 bg-white/10">
              {isCompleted ? 'Programa concluído' : 'Continue sua jornada'}
            </span>
            <h1 className="text-3xl md:text-4xl font-bold text-white mb-2" style={{ letterSpacing: '-0.03em' }}>
              {course.title}
            </h1>
            {course.instructorName && (
              <p className="text-violet-200 mb-2 text-base font-medium">com {course.instructorName}</p>
            )}
            {(course.shortDescription || course.description) && (
              <p className="text-white/70 text-sm leading-relaxed max-w-lg mb-1">
                {course.shortDescription || course.description}
              </p>
            )}

            <div className="flex items-center gap-3 mb-6 mt-4">
              <div className="flex-1 max-w-xs bg-white/10 rounded-full overflow-hidden h-2">
                <div
                  className="h-full bg-amber-400 rounded-full"
                  style={{ width: `${Math.min(100, Math.max(0, enrollment.progressPercentage))}%` }}
                />
              </div>
              <span className="text-white font-bold text-sm">{Math.round(enrollment.progressPercentage)}% concluído</span>
            </div>

            {continueTarget && (
              <Link
                href={`/cursos/${slug}/${continueTarget}`}
                className="inline-flex items-center gap-2.5 bg-white text-violet-700 font-bold px-6 py-3 rounded-2xl hover:bg-violet-50 transition-all shadow-lg shadow-black/20 text-sm"
              >
                <Play size={18} fill="currentColor" />
                {lastWatched ? 'Continuar' : 'Começar'}
              </Link>
            )}
          </div>

          {lastWatched && (
            <div className="w-full md:w-72 bg-white/10 backdrop-blur-sm border border-white/20 rounded-2xl p-4 shrink-0">
              <p className="text-xs font-semibold text-white/50 uppercase tracking-wider mb-2">Você parou em</p>
              <Link
                href={`/cursos/${slug}/${lastWatched.lesson.id}`}
                className="w-full h-28 rounded-xl mb-3 flex items-center justify-center group relative overflow-hidden"
                style={{ background: 'linear-gradient(135deg, oklch(40% 0.22 270), oklch(30% 0.18 293))' }}
              >
                <div className="w-10 h-10 bg-white/20 group-hover:bg-white/30 rounded-full flex items-center justify-center transition-all">
                  <Play size={18} fill="white" className="text-white ml-0.5" />
                </div>
                {!!lastWatched.lesson.durationSeconds
                  && lastWatched.lesson.durationSeconds > lastWatched.progress.watchedSeconds && (
                  <div className="absolute bottom-2 right-2 bg-black/40 text-white text-xs font-medium px-2 py-0.5 rounded-md">
                    {formatDuration(lastWatched.lesson.durationSeconds - lastWatched.progress.watchedSeconds)} restantes
                  </div>
                )}
              </Link>
              <p className="text-xs text-white/50 mb-0.5">
                Módulo {lastWatched.lesson.moduleIndex} · Aula {lastWatched.lesson.lessonIndex}
              </p>
              <p className="text-sm font-bold text-white leading-snug">{lastWatched.lesson.title}</p>
            </div>
          )}
        </div>
      </section>

      <div className="max-w-5xl mx-auto px-6 py-8 space-y-10">

        {/* Próximo passo */}
        {nextLesson && (
          <section>
            <div className="bg-violet-600 rounded-2xl p-5 flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
              <div className="flex-1">
                <p className="text-violet-200 text-xs font-semibold uppercase tracking-wider mb-1">Seu próximo passo</p>
                <p className="text-white font-bold text-base leading-snug mb-1">
                  Assista &ldquo;<span className="text-amber-300">{nextLesson.title}</span>&rdquo;
                </p>
                <p className="text-violet-200 text-sm">{nextLesson.moduleTitle}</p>
              </div>
              <Link
                href={`/cursos/${slug}/${nextLesson.id}`}
                className="flex items-center gap-2 bg-white text-violet-700 font-bold text-sm px-4 py-2.5 rounded-xl hover:bg-violet-50 transition-all shrink-0"
              >
                Continuar jornada <ArrowRight size={16} />
              </Link>
            </div>
          </section>
        )}
        {isCompleted && (
          <section>
            <div className="bg-emerald-600 rounded-2xl p-5 flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
              <div>
                <p className="text-emerald-100 text-xs font-semibold uppercase tracking-wider mb-1">Programa concluído</p>
                <p className="text-white font-bold text-base leading-snug">Você concluiu todas as aulas deste programa. 🎉</p>
              </div>
              {/* Certificado é emitido automaticamente pelo backend ao concluir (ver
                  CourseCompletedEventHandler) — o botão só aparece depois que a busca acima
                  encontra o certificado, evitando um clique que dá 404 no instante exato da
                  conclusão da última aula. */}
              {certificate && (
                <button
                  type="button"
                  onClick={handleDownloadCertificate}
                  disabled={downloadingCertificate}
                  className="flex items-center gap-2 bg-white text-emerald-700 font-bold text-sm px-4 py-2.5 rounded-xl hover:bg-emerald-50 disabled:opacity-60 transition-all shrink-0"
                >
                  {downloadingCertificate ? (
                    <>
                      <Loader2 size={16} className="animate-spin" /> Gerando PDF...
                    </>
                  ) : (
                    <>
                      <Download size={16} /> Baixar certificado
                    </>
                  )}
                </button>
              )}
            </div>
          </section>
        )}

        {/* Meus programas — outros cursos em que o mentorado está matriculado (dado real,
            GetMyEnrollments). Só aparece se houver outro programa além deste. */}
        {otherEnrollments.length > 0 && (
          <section>
            <div className="flex items-center justify-between mb-4">
              <h2 className="text-lg font-bold text-ink">Meus programas</h2>
              <Link href="/meus-cursos" className="text-sm font-semibold text-violet-600 hover:text-violet-700">
                Ver todos
              </Link>
            </div>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {otherEnrollments.slice(0, 4).map(p => (
                <Link key={p.enrollmentId} href={`/cursos/${p.slug}`}>
                  <Card hover padding="none" className="overflow-hidden flex flex-col h-full">
                    <div className="h-28 relative overflow-hidden bg-violet-900">
                      {p.thumbnailUrl ? (
                        // eslint-disable-next-line @next/next/no-img-element
                        <img src={p.thumbnailUrl} alt={p.title} className="absolute inset-0 w-full h-full object-cover" />
                      ) : (
                        <div className="absolute inset-0 flex items-center justify-center text-white/60">
                          <BookOpen size={24} />
                        </div>
                      )}
                    </div>
                    <div className="p-4">
                      <p className="font-bold text-ink text-sm mb-2 line-clamp-1">{p.title}</p>
                      <div className="flex items-center gap-2">
                        <Progress value={p.progressPercentage} size="xs" />
                        <span className="text-xs font-semibold text-ink-3 shrink-0">{Math.round(p.progressPercentage)}%</span>
                      </div>
                    </div>
                  </Card>
                </Link>
              ))}
            </div>
          </section>
        )}

        {/* Próximos encontros — sem sistema real de agenda ao vivo hoje (sem contrapartida no
            backend); mantido visível com selo "Em breve" por escolha do usuário, sem inventar
            nenhuma reunião. */}
        <section>
          <div className="flex items-center gap-2 mb-4">
            <h2 className="text-lg font-bold text-ink">Próximos encontros</h2>
            <SoonPill />
          </div>
          <Card className="text-center py-8">
            <p className="text-sm text-ink-3">
              Em breve você vai poder ver e participar de encontros ao vivo com seu mentor por aqui.
            </p>
          </Card>
        </section>

        {/* Sua jornada */}
        <section>
          <h2 className="text-lg font-bold text-ink mb-4">Sua jornada</h2>
          <Card padding="none">
            {orderedModules.map((mod, mi) => (
              <div key={mod.id} className="border-b border-line-light last:border-0">
                <div className="w-full flex items-center gap-3 px-5 py-4">
                  <BookOpen size={15} className="text-ink-3 shrink-0" />
                  <span className="text-sm font-bold text-ink flex-1">
                    Módulo {mi + 1} — {mod.title}
                  </span>
                  <ChevronRight size={15} className="text-ink-4 shrink-0" />
                </div>
                <div className="px-5 pb-3 space-y-1">
                  {mod.lessons.map((lesson) => {
                    const progress = progressByLessonId.get(lesson.id);
                    const isDone = !!progress?.isCompleted;
                    const isCurrent = nextLesson?.id === lesson.id;
                    const stateIcon = isDone
                      ? <CheckCircle size={15} className="text-emerald-500 shrink-0" />
                      : isCurrent
                        ? <Play size={15} fill="currentColor" className="text-violet-600 shrink-0" />
                        : <Circle size={15} className="text-ink-4 shrink-0" />;
                    const className = `w-full flex items-center gap-3 px-3 py-2.5 rounded-xl text-left text-sm transition-all ${
                      isCurrent
                        ? 'bg-violet-50 font-semibold text-violet-700'
                        : isDone
                          ? 'text-ink-3 hover:bg-surface-2'
                          : 'text-ink-2 hover:bg-surface-2'
                    }`;
                    // Aulas sem vídeo (raras — ex.: material só em anexo) não têm player pra
                    // abrir; mesmo critério já usado na tela de vitrine (page.tsx).
                    return lesson.hasVideo ? (
                      <Link key={lesson.id} href={`/cursos/${slug}/${lesson.id}`} className={className}>
                        {stateIcon}
                        <span className="flex-1">{lesson.title}</span>
                      </Link>
                    ) : (
                      <div key={lesson.id} className={`${className} cursor-default`}>
                        {stateIcon}
                        <span className="flex-1">{lesson.title}</span>
                      </div>
                    );
                  })}
                </div>
              </div>
            ))}
          </Card>
        </section>
      </div>
    </div>
  );
}
