'use client';

import { useEffect, useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import { useQuery } from '@tanstack/react-query';
import { coursesApi, subscriptionsApi } from '@/lib/api';
import Link from 'next/link';

interface PlayUrlResponse {
  lessonId: string;
  title: string;
  isPreview: boolean;
  playbackUrl: string;
  durationSeconds?: number;
  thumbnailUrl?: string;
}

/**
 * Vídeos importados (YouTube/Vimeo/Drive/...) chegam do backend com a URL ORIGINAL colada pelo
 * criador (ex.: https://www.youtube.com/watch?v=abc123) — essa URL não é um arquivo de vídeo e
 * não funciona numa tag <video src="...">, precisa virar uma URL de EMBED (iframe) própria da
 * plataforma. Cloudflare Stream já vem pronto para iframe (ver checagem abaixo); os demais
 * precisam dessa conversão no front. Retorna null quando não há embed conhecido (Dropbox/
 * OneDrive não têm formato de embed de vídeo universal sem uma chamada de API adicional) — nesse
 * caso mostramos um link para abrir o vídeo na plataforma original em vez de tentar embutir.
 */
function toEmbedUrl(url: string): string | null {
  const youtubeMatch = url.match(/(?:youtube\.com\/watch\?v=|youtu\.be\/|youtube\.com\/embed\/)([a-zA-Z0-9_-]{6,})/);
  if (youtubeMatch) return `https://www.youtube.com/embed/${youtubeMatch[1]}`;

  const vimeoMatch = url.match(/vimeo\.com\/(?:video\/)?(\d+)/);
  if (vimeoMatch) return `https://player.vimeo.com/video/${vimeoMatch[1]}`;

  const driveMatch = url.match(/drive\.google\.com\/file\/d\/([a-zA-Z0-9_-]+)/);
  if (driveMatch) return `https://drive.google.com/file/d/${driveMatch[1]}/preview`;

  if (url.includes('cloudflarestream.com') || url.includes('videodelivery.net')) return url;

  return null;
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

export default function LessonPlayerPage() {
  const { slug, lessonId } = useParams<{ slug: string; lessonId: string }>();
  const router = useRouter();
  const [courseId, setCourseId] = useState<string | null>(null);

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

  // 3. Verifica assinatura para o paywall
  const { data: subscription } = useQuery({
    queryKey: ['my-subscription'],
    queryFn: () => subscriptionsApi.getMySubscription().then(r => r.data).catch(() => null),
  });

  // Status do erro (403 = sem assinatura, 401 = não logado)
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
                  Conteúdo exclusivo para assinantes
                </h2>
                <p className="text-gray-500 text-sm max-w-sm mx-auto">
                  Esta aula faz parte do conteúdo pago.
                  Assine um plano para ter acesso ilimitado a todos os cursos.
                </p>
              </div>
              <Link href="/assinatura" className="btn-primary mt-2">
                Ver planos →
              </Link>
              <Link href={`/cursos/${slug}`}
                className="text-sm text-gray-400 hover:text-gray-600 transition-colors">
                ← Voltar ao curso
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
                    // YouTube/Vimeo/Drive/Cloudflare Stream — todos tocam via iframe de embed
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
                  // Arquivo de vídeo direto (upload — Cloudflare Stream real ou mock local)
                  return (
                    <video
                      src={playData.playbackUrl}
                      controls
                      autoPlay
                      className="w-full h-full"
                      title={playData.title}
                    />
                  );
                })()}
              </div>

              {/* Título e badge */}
              <div className="mt-4 flex items-start justify-between gap-3">
                <div>
                  <h1 className="text-xl font-bold text-gray-800">{playData.title}</h1>
                  {playData.durationSeconds && (
                    <p className="text-sm text-gray-400 mt-1">
                      ⏱ {Math.floor(playData.durationSeconds / 60)}min{' '}
                      {playData.durationSeconds % 60}s
                    </p>
                  )}
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
                        const isLocked = !lesson.isPreview;
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
                              {isCurrent ? '▶' : isLocked ? '🔒' : '▷'}
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
                              {lesson.isPreview && (
                                <span className="text-[10px] text-green-400">Preview</span>
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
