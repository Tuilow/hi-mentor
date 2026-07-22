'use client';

import { useEffect, useRef, useState } from 'react';
import { useParams } from 'next/navigation';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { coursesApi, enrollmentsApi } from '@/lib/api';
import Link from 'next/link';
import Hls from 'hls.js';

interface PlayUrlResponse {
  lessonId: string;
  title: string;
  isPreview: boolean;
  playbackUrl: string;
  durationSeconds?: number;
  thumbnailUrl?: string;
}

/**
 * Vídeos importados sem download (YouTube/Vimeo/Drive) chegam do backend com a URL ORIGINAL
 * colada pelo criador (ex.: https://www.youtube.com/watch?v=abc123) — essa URL não é um arquivo
 * de vídeo e não funciona numa tag <video src="...">, precisa virar uma URL de EMBED (iframe)
 * própria da plataforma. Retorna null quando não há embed conhecido (Dropbox/OneDrive não têm
 * formato de embed de vídeo universal sem uma chamada de API adicional) — nesse caso mostramos
 * um link para abrir o vídeo na plataforma original em vez de tentar embutir.
 *
 * Vídeos hospedados no Cloudflare Stream (upload direto ou baixado do YouTube) NÃO entram aqui
 * — o backend já devolve um manifesto HLS (.m3u8), tratado como <video> comum (ver isHlsUrl
 * abaixo), não como iframe — assim dá pra salvar/retomar o progresso do aluno.
 */
function toEmbedUrl(url: string): string | null {
  const youtubeMatch = url.match(/(?:youtube\.com\/watch\?v=|youtu\.be\/|youtube\.com\/embed\/)([a-zA-Z0-9_-]{6,})/);
  if (youtubeMatch) return `https://www.youtube.com/embed/${youtubeMatch[1]}`;

  const vimeoMatch = url.match(/vimeo\.com\/(?:video\/)?(\d+)/);
  if (vimeoMatch) return `https://player.vimeo.com/video/${vimeoMatch[1]}`;

  const driveMatch = url.match(/drive\.google\.com\/file\/d\/([a-zA-Z0-9_-]+)/);
  if (driveMatch) return `https://drive.google.com/file/d/${driveMatch[1]}/preview`;

  return null;
}

/** Manifesto HLS do Cloudflare Stream — toca numa <video> comum via hls.js (ver useEffect abaixo). */
function isHlsUrl(url: string): boolean {
  return url.includes('.m3u8');
}

interface CourseDetail {
  id: string;
  title: string;
  slug: string;
  modules: Array<{
    id: string;
    title: string;
    order: number;
    lessons: Array<{
      id: string;
      title: string;
      order: number;
      isPreview: boolean;
      hasVideo: boolean;
      durationSeconds?: number;
    }>;
  }>;
}

// GET /enrollments/courses/{courseId} — inclui o progresso salvo de CADA aula, usado aqui para
// retomar o vídeo de onde parou e marcar aulas concluídas na barra lateral.
interface LessonProgressDto {
  lessonId: string;
  watchedSeconds: number;
  isCompleted: boolean;
}
interface EnrollmentProgressDto {
  enrollmentId: string;
  courseId: string;
  status: string;
  progressPercentage: number;
  lessonProgress: LessonProgressDto[];
}

// Intervalo mínimo entre salvamentos automáticos de progresso durante a reprodução — evita
// bater na API a cada frame do evento timeupdate (que dispara várias vezes por segundo).
const PROGRESS_SAVE_INTERVAL_SECONDS = 10;

export default function LessonPlayerPage() {
  const { slug, lessonId } = useParams<{ slug: string; lessonId: string }>();
  const qc = useQueryClient();
  const [courseId, setCourseId] = useState<string | null>(null);
  const videoRef = useRef<HTMLVideoElement | null>(null);
  const lastSavedAtRef = useRef(0);
  const hasResumedRef = useRef(false);

  // 1. Busca o curso para obter o courseId e a lista de aulas (sidebar)
  const { data: course } = useQuery<CourseDetail>({
    queryKey: ['course', slug],
    queryFn: () => coursesApi.getBySlug(slug).then(r => r.data),
    enabled: !!slug,
  });

  useEffect(() => {
    if (course) setCourseId(course.id);
  }, [course]);

  // 2. Busca a URL de playback (com controle de acesso no backend)
  const {
    data: playData,
    isLoading,
    error,
  } = useQuery<PlayUrlResponse>({
    queryKey: ['lesson-play', courseId, lessonId],
    queryFn: () =>
      coursesApi.getLessonPlayUrl(courseId!, lessonId).then(r => r.data),
    enabled: !!courseId && !!lessonId,
    retry: false,
  });

  // 3. Progresso da matrícula — de onde retomar este vídeo e quais aulas já foram concluídas.
  // GET /enrollments/courses/{courseId} retorna 404 se o aluno não está matriculado (ex.: preview
  // gratuito sem matrícula); nesse caso não há progresso pra salvar/retomar, e tudo bem.
  const { data: enrollmentProgress } = useQuery<EnrollmentProgressDto | null>({
    queryKey: ['enrollment-progress', courseId],
    queryFn: () => enrollmentsApi.getProgress(courseId!).then(r => r.data).catch(() => null),
    enabled: !!courseId,
  });
  const isEnrolled = !!enrollmentProgress;
  const myLessonProgress = enrollmentProgress?.lessonProgress.find(lp => lp.lessonId === lessonId);
  const resumeSeconds = myLessonProgress?.watchedSeconds ?? 0;

  // Reseta o "já retomei este vídeo" ao trocar de aula — sem isso, ao navegar de uma aula pra
  // outra o próximo <video> nunca seria posicionado (o ref ficava true da aula anterior).
  useEffect(() => {
    hasResumedRef.current = false;
    lastSavedAtRef.current = 0;
  }, [lessonId]);

  // Cloudflare Stream devolve um manifesto HLS (.m3u8), não um arquivo de vídeo direto — a
  // maioria dos navegadores (Chrome, Firefox, Edge) não sabe tocar isso nativamente numa
  // <video src="...">, então usamos hls.js para decodificar o manifesto e alimentar a tag via
  // Media Source Extensions. Safari já toca HLS nativamente, então nesse caso só setamos o src
  // direto. Isso mantém o player como uma <video> comum (não um iframe), o que é o que permite
  // ler currentTime e salvar/retomar o progresso do aluno.
  useEffect(() => {
    const video = videoRef.current;
    const url = playData?.playbackUrl;
    if (!video || !url || !isHlsUrl(url)) return;

    if (video.canPlayType('application/vnd.apple.mpegurl')) {
      video.src = url;
      return;
    }

    if (Hls.isSupported()) {
      const hls = new Hls();
      hls.loadSource(url);
      hls.attachMedia(video);
      return () => hls.destroy();
    }
  }, [playData?.playbackUrl]);

  const saveProgress = (watchedSeconds: number, totalSeconds: number) => {
    if (!enrollmentProgress?.enrollmentId || !totalSeconds) return;
    enrollmentsApi.trackProgress(enrollmentProgress.enrollmentId, {
      lessonId, watchedSeconds: Math.floor(watchedSeconds), totalSeconds: Math.floor(totalSeconds),
    }).catch(() => { /* não trava o player por causa disso — tenta de novo no próximo intervalo */ });
  };

  // Ao carregar os metadados do vídeo, pula direto pra onde o aluno parou da última vez
  // (só uma vez por aula — depois disso o aluno pode navegar livremente sem ser "puxado de volta").
  const handleLoadedMetadata = (e: React.SyntheticEvent<HTMLVideoElement>) => {
    if (hasResumedRef.current) return;
    hasResumedRef.current = true;
    const video = e.currentTarget;
    if (resumeSeconds > 5 && resumeSeconds < video.duration - 5) {
      video.currentTime = resumeSeconds;
    }
  };

  const handleTimeUpdate = (e: React.SyntheticEvent<HTMLVideoElement>) => {
    const video = e.currentTarget;
    if (video.currentTime - lastSavedAtRef.current >= PROGRESS_SAVE_INTERVAL_SECONDS) {
      lastSavedAtRef.current = video.currentTime;
      saveProgress(video.currentTime, video.duration);
    }
  };

  // Pausa e fim de vídeo salvam na hora (não esperam o intervalo de 10s) e atualizam a barra
  // lateral / progresso da matrícula em outras telas (ex.: aba "Meus cursos" e o ✓ ao lado desta
  // aula), já que uma pausa costuma ser exatamente o momento em que o aluno vai embora.
  const handlePauseOrEnded = (e: React.SyntheticEvent<HTMLVideoElement>) => {
    const video = e.currentTarget;
    lastSavedAtRef.current = video.currentTime;
    saveProgress(video.currentTime, video.duration);
    qc.invalidateQueries({ queryKey: ['enrollment-progress', courseId] });
    qc.invalidateQueries({ queryKey: ['my-enrollments'] });
  };

  // Status do erro (403 = sem acesso pago a este curso, 401 = não logado). O motivo exato
  // (compra pendente vs. assinatura pendente) depende do modelo de monetização do PRODUTO, não
  // é algo genérico da plataforma — por isso o paywall abaixo manda o aluno de volta para a
  // página do curso, que já sabe (via Plan.CourseId) se este produto é Compra Única ou
  // Assinatura e mostra o botão certo (Comprar/Assinar). Ver cursos/[slug]/page.tsx.
  const errStatus = (error as { response?: { status?: number } })?.response?.status;
  const isPaywalled = errStatus === 403 || errStatus === 401;

  const allLessons = course?.modules
    .sort((a, b) => a.order - b.order)
    .flatMap(m => m.lessons.sort((a, b) => a.order - b.order)) ?? [];

  const currentIdx = allLessons.findIndex(l => l.id === lessonId);
  const prevLesson = currentIdx > 0 ? allLessons[currentIdx - 1] : null;
  const nextLesson = currentIdx >= 0 && currentIdx < allLessons.length - 1
    ? allLessons[currentIdx + 1]
    : null;

  return (
    <div className="max-w-6xl mx-auto">
      {/* Breadcrumb */}
      <div className="flex items-center gap-2 text-sm text-gray-400 mb-6">
        <Link href="/cursos" className="hover:text-gray-600">Cursos</Link>
        <span>›</span>
        <Link href={`/cursos/${slug}`} className="hover:text-gray-600 truncate max-w-[200px]">
          {course?.title ?? slug}
        </Link>
        <span>›</span>
        <span className="text-gray-600 truncate">{playData?.title ?? 'Aula'}</span>
      </div>

      <div className="flex gap-6 flex-col lg:flex-row">

        {/* ── Player principal ── */}
        <div className="flex-1 min-w-0">
          {/* Paywall */}
          {isPaywalled && (
            <div className="card border-gray-200 flex flex-col items-center justify-center
                            py-16 text-center gap-4">
              <div className="text-5xl">🔒</div>
              <div>
                <h2 className="text-xl font-bold text-gray-800 mb-2">
                  Conteúdo pago
                </h2>
                <p className="text-gray-500 text-sm max-w-sm mx-auto">
                  Esta aula faz parte do conteúdo pago deste curso.
                  Adquira acesso na página do curso para continuar assistindo.
                </p>
              </div>
              <Link href={`/cursos/${slug}`} className="btn-primary mt-2">
                Ver opções de acesso →
              </Link>
            </div>
          )}

          {/* Loading */}
          {!isPaywalled && isLoading && (
            <div className="w-full aspect-video bg-white rounded-xl animate-pulse flex
                            items-center justify-center">
              <p className="text-gray-400 text-sm">Carregando vídeo...</p>
            </div>
          )}

          {/* Player */}
          {!isPaywalled && !isLoading && playData && (
            <>
              <div className="w-full aspect-video rounded-xl overflow-hidden bg-black">
                {(() => {
                  const embedUrl = toEmbedUrl(playData.playbackUrl);
                  if (embedUrl) {
                    // YouTube/Vimeo/Drive importados sem download — tocam via iframe de embed.
                    // Não dá pra salvar/retomar posição aqui: o player fica isolado num outro
                    // domínio (iframe) e não expõe currentTime sem integrar a API própria de
                    // cada plataforma (YouTube IFrame API, Vimeo Player.js etc.). Vídeos baixados
                    // e hospedados no Cloudflare Stream NÃO caíem aqui — vão pelo caso da
                    // <video> comum abaixo (manifesto HLS via hls.js).
                    return (
                      <iframe
                        src={embedUrl}
                        allow="accelerometer; gyroscope; autoplay; encrypted-media; picture-in-picture"
                        allowFullScreen
                        className="w-full h-full border-0"
                        title={playData.title}
                      />
                    );
                  }
                  if (playData.playbackUrl.includes('dropbox.com') || playData.playbackUrl.includes('onedrive') || playData.playbackUrl.includes('1drv.ms')) {
                    // Sem formato de embed universal — abre na plataforma original
                    return (
                      <div className="w-full h-full flex flex-col items-center justify-center gap-3 text-gray-300">
                        <p className="text-sm">Este vídeo está hospedado externamente.</p>
                        <a href={playData.playbackUrl} target="_blank" rel="noopener noreferrer" className="btn-primary">
                          Abrir vídeo ↗
                        </a>
                      </div>
                    );
                  }
                  // Arquivo de vídeo direto (upload/download — Cloudflare Stream real via HLS,
                  // ou mock local servindo o arquivo puro). Único caso em que dá pra controlar
                  // currentTime diretamente — retoma de resumeSeconds ao carregar e salva a
                  // posição periodicamente. Para HLS o src é setado pelo useEffect (hls.js/Safari),
                  // não pela prop — por isso fica undefined nesse caso.
                  return (
                    <video
                      ref={videoRef}
                      src={isHlsUrl(playData.playbackUrl) ? undefined : playData.playbackUrl}
                      controls
                      autoPlay
                      className="w-full h-full"
                      title={playData.title}
                      onLoadedMetadata={handleLoadedMetadata}
                      onTimeUpdate={handleTimeUpdate}
                      onPause={handlePauseOrEnded}
                      onEnded={handlePauseOrEnded}
                    />
                  );
                })()}
              </div>

              {/* Título e badge */}
              <div className="mt-4 flex items-start justify-between gap-3">
                <div>
                  <h1 className="text-xl font-bold text-gray-800">{playData.title}</h1>
                  <div className="flex items-center gap-3 mt-1">
                    {playData.durationSeconds && (
                      <p className="text-sm text-gray-400">
                        ⏱ {Math.floor(playData.durationSeconds / 60)}min{' '}
                        {playData.durationSeconds % 60}s
                      </p>
                    )}
                    {myLessonProgress?.isCompleted && (
                      <span className="text-xs text-emerald-500 font-medium">✓ Aula concluída</span>
                    )}
                    {!myLessonProgress?.isCompleted && resumeSeconds > 5 && (
                      <span className="text-xs text-gray-400">
                        Retomando de {Math.floor(resumeSeconds / 60)}min {resumeSeconds % 60}s
                      </span>
                    )}
                  </div>
                </div>
                {playData.isPreview && (
                  <span className="badge-green flex-shrink-0">Preview gratuito</span>
                )}
              </div>
            </>
          )}

          {/* Erro genérico */}
          {!isPaywalled && error && !isLoading && (
            <div className="card border-red-900/40 bg-red-950/20 text-center py-10">
              <p className="text-red-400 font-medium">Erro ao carregar o vídeo.</p>
              <p className="text-sm text-gray-400 mt-1">Tente novamente em instantes.</p>
            </div>
          )}

          {/* Navegação entre aulas */}
          {(prevLesson || nextLesson) && (
            <div className="flex items-center justify-between mt-6 pt-6 border-t border-gray-200">
              {prevLesson ? (
                <Link
                  href={`/cursos/${slug}/${prevLesson.id}`}
                  className="flex items-center gap-2 text-sm text-gray-500
                             hover:text-gray-800 transition-colors group"
                >
                  <span className="group-hover:-translate-x-0.5 transition-transform">←</span>
                  <span className="truncate max-w-[180px]">{prevLesson.title}</span>
                </Link>
              ) : <div />}

              {nextLesson && (
                <Link
                  href={`/cursos/${slug}/${nextLesson.id}`}
                  className="flex items-center gap-2 text-sm text-gray-500
                             hover:text-gray-800 transition-colors group ml-auto"
                >
                  <span className="truncate max-w-[180px]">{nextLesson.title}</span>
                  <span className="group-hover:translate-x-0.5 transition-transform">→</span>
                </Link>
              )}
            </div>
          )}
        </div>

        {/* ── Sidebar com lista de aulas ── */}
        <aside className="w-full lg:w-72 flex-shrink-0">
          <div className="card border-gray-200 p-0 overflow-hidden sticky top-6">
            <div className="px-4 py-3 border-b border-gray-200">
              <p className="text-sm font-semibold text-gray-700">Aulas do curso</p>
            </div>
            <div className="overflow-y-auto max-h-[70vh]">
              {course?.modules
                .sort((a, b) => a.order - b.order)
                .map(module => (
                  <div key={module.id}>
                    <div className="px-4 py-2 bg-gray-100/60 border-b border-gray-200">
                      <p className="text-xs font-semibold text-gray-500 uppercase tracking-wide">
                        {module.title}
                      </p>
                    </div>
                    {module.lessons
                      .sort((a, b) => a.order - b.order)
                      .map(lesson => {
                        const isCurrent = lesson.id === lessonId;
                        const isLocked = !lesson.isPreview && !isEnrolled;
                        const isCompleted = enrollmentProgress?.lessonProgress
                          .find(lp => lp.lessonId === lesson.id)?.isCompleted ?? false;
                        return (
                          <Link
                            key={lesson.id}
                            href={`/cursos/${slug}/${lesson.id}`}
                            className={`
                              flex items-start gap-3 px-4 py-3 border-b border-gray-200/50
                              last:border-0 transition-colors
                              ${isCurrent
                                ? 'bg-brand-600/20 border-l-2 border-l-brand-500'
                                : 'hover:bg-gray-100/40'}
                            `}
                          >
                            <span className="text-base mt-0.5 flex-shrink-0">
                              {isCurrent ? '▶' : isLocked ? '🔒' : isCompleted ? '✅' : '▷'}
                            </span>
                            <div className="min-w-0">
                              <p className={`text-xs font-medium leading-snug truncate
                                ${isCurrent ? 'text-brand-600' : 'text-gray-700'}`}>
                                {lesson.title}
                              </p>
                              {lesson.durationSeconds && (
                                <p className="text-xs text-gray-400 mt-0.5">
                                  {Math.floor(lesson.durationSeconds / 60)}min
                                </p>
                              )}
                            </div>
                          </Link>
                        );
                      })}
                  </div>
                ))}
            </div>
          </div>
        </aside>
      </div>
    </div>
  );
}
