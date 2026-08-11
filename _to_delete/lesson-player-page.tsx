'use client';

import { useEffect, useRef, useState } from 'react';
import { useParams } from 'next/navigation';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { coursesApi, enrollmentsApi } from '@/lib/api';
import Link from 'next/link';
import Hls from 'hls.js';
import { Lock, CheckCircle2, Circle, Play, ChevronLeft, ChevronRight } from 'lucide-react';
import { Badge } from '@/components/ui/design-system';

// Redesign "Hi Mentor" (ago/2026): reskin visual seguindo hi-mentor/src/pages/PlayerPage.tsx —
// só classNames/ícones foram trocados no JSX abaixo (estrutura do DOM idêntica à anterior);
// TODA a lógica acima (refs, effects de HLS/YouTube/Vimeo, salvamento/retomada de progresso,
// paywall) permanece exatamente a mesma, sem nenhuma alteração.

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
 *
 * Achado B10 da avaliação (2ª parte — a 1ª, sobre fechar a aba sem pausar, já foi corrigida
 * abaixo em flushOnExit): YouTube e Vimeo TÊM API de controle de embed via postMessage (YouTube
 * IFrame API / Vimeo Player.js), carregada sob demanda pelos loaders abaixo — dá pra ler
 * currentTime, retomar e salvar progresso periodicamente, como um <video> comum (ver useEffect
 * "Player de embed" mais abaixo). Google Drive continua sem API pública de progresso — o preview
 * embutido do Drive não expõe currentTime nem aceita comandos de seek de fora; nesse caso
 * mostramos um aviso na tela em vez de fingir que o progresso é salvo.
 */
interface EmbedInfo {
  provider: 'youtube' | 'vimeo' | 'drive';
  embedId: string;
  src: string;
}

function resolveEmbed(url: string): EmbedInfo | null {
  const youtubeMatch = url.match(/(?:youtube\.com\/watch\?v=|youtu\.be\/|youtube\.com\/embed\/)([a-zA-Z0-9_-]{6,})/);
  if (youtubeMatch) {
    const id = youtubeMatch[1];
    // enablejsapi=1 + origin habilitam o controle via postMessage pela YouTube IFrame API
    // (loadYouTubeIframeApi abaixo) — sem eles a API carrega mas o player recusa comandos.
    const origin = typeof window !== 'undefined' ? window.location.origin : '';
    return { provider: 'youtube', embedId: id, src: `https://www.youtube.com/embed/${id}?enablejsapi=1&origin=${encodeURIComponent(origin)}` };
  }

  const vimeoMatch = url.match(/vimeo\.com\/(?:video\/)?(\d+)/);
  if (vimeoMatch) {
    const id = vimeoMatch[1];
    return { provider: 'vimeo', embedId: id, src: `https://player.vimeo.com/video/${id}?api=1` };
  }

  const driveMatch = url.match(/drive\.google\.com\/file\/d\/([a-zA-Z0-9_-]+)/);
  if (driveMatch) {
    const id = driveMatch[1];
    return { provider: 'drive', embedId: id, src: `https://drive.google.com/file/d/${id}/preview` };
  }

  return null;
}

/** Manifesto HLS do Cloudflare Stream — toca numa <video> comum via hls.js (ver useEffect abaixo). */
function isHlsUrl(url: string): boolean {
  return url.includes('.m3u8');
}

// ─── Loaders dos SDKs de embed (carregados sob demanda, uma única vez por sessão de página) ───
// Script tag global em vez de pacote npm: evita adicionar dependência só para isto, e as duas
// APIs (YouTube/Vimeo) são desenhadas justamente para serem carregadas assim, via <script> solto.

let youTubeApiPromise: Promise<void> | null = null;
function loadYouTubeIframeApi(): Promise<void> {
  if (typeof window === 'undefined') return Promise.resolve();
  const w = window as unknown as { YT?: { Player?: unknown }; onYouTubeIframeAPIReady?: () => void };
  if (w.YT?.Player) return Promise.resolve();
  if (youTubeApiPromise) return youTubeApiPromise;
  youTubeApiPromise = new Promise((resolve) => {
    const previous = w.onYouTubeIframeAPIReady;
    w.onYouTubeIframeAPIReady = () => {
      previous?.();
      resolve();
    };
    const script = document.createElement('script');
    script.src = 'https://www.youtube.com/iframe_api';
    document.head.appendChild(script);
  });
  return youTubeApiPromise;
}

let vimeoApiPromise: Promise<void> | null = null;
function loadVimeoPlayerApi(): Promise<void> {
  if (typeof window === 'undefined') return Promise.resolve();
  const w = window as unknown as { Vimeo?: { Player?: unknown } };
  if (w.Vimeo?.Player) return Promise.resolve();
  if (vimeoApiPromise) return vimeoApiPromise;
  vimeoApiPromise = new Promise((resolve, reject) => {
    const script = document.createElement('script');
    script.src = 'https://player.vimeo.com/api/player.js';
    script.onload = () => resolve();
    script.onerror = () => reject(new Error('Falha ao carregar o SDK do Vimeo Player.'));
    document.head.appendChild(script);
  });
  return vimeoApiPromise;
}

// Tipagem mínima das duas APIs — só os métodos realmente usados abaixo, os SDKs completos não
// têm @types oficiais leves o bastante pra valer a pena instalar só por causa deste player.
interface YouTubePlayerLike {
  getCurrentTime(): number;
  getDuration(): number;
  seekTo(seconds: number, allowSeekAhead: boolean): void;
  destroy(): void;
}
interface VimeoPlayerLike {
  getCurrentTime(): Promise<number>;
  getDuration(): Promise<number>;
  setCurrentTime(seconds: number): Promise<number>;
  on(event: string, cb: (data: { seconds: number; duration: number }) => void): void;
  destroy(): Promise<void>;
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
  // Achado B10 (2ª parte) — player de embed YouTube/Vimeo controlado via postMessage. embedFrameRef
  // aponta pro <iframe> renderizado; ytPlayerRef/vimeoPlayerRef guardam a instância do SDK depois
  // de inicializada (só uma delas fica preenchida por vez, conforme o provider da aula atual).
  const embedFrameRef = useRef<HTMLIFrameElement | null>(null);
  const ytPlayerRef = useRef<YouTubePlayerLike | null>(null);
  const vimeoPlayerRef = useRef<VimeoPlayerLike | null>(null);
  const embedSaveIntervalRef = useRef<ReturnType<typeof setInterval> | null>(null);
  const embedLastSavedAtRef = useRef(0);
  const embedHasResumedRef = useRef(false);

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
  // outra o próximo <video> (ou player de embed) nunca seria posicionado (o ref ficava true da
  // aula anterior).
  useEffect(() => {
    hasResumedRef.current = false;
    lastSavedAtRef.current = 0;
    embedHasResumedRef.current = false;
    embedLastSavedAtRef.current = 0;
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
      // Achado M6 da avaliação: instante em que o CLIENTE leu o currentTime (não quando a
      // requisição chega no servidor) — deixa o backend distinguir "evento antigo chegando
      // atrasado" de "evento realmente mais novo", em vez de só aplicar quem chegar por último.
      clientCapturedAt: new Date().toISOString(),
    }).catch(() => { /* não trava o player por causa disso — tenta de novo no próximo intervalo */ });
  };

  // Achado B10 da avaliação: até aqui só salvava a cada 10s e em pause/ended — fechar a aba sem
  // pausar (ex.: Ctrl+W, fechar o navegador) podia perder até ~10s de progresso. visibilitychange
  // dispara de forma confiável em troca de aba/minimizar/fechar (inclusive mobile); pagehide cobre
  // navegação/fechamento em navegadores que não disparam visibilitychange a tempo. keepalive
  // mantém a requisição viva mesmo com a página sendo descartada — sem ele, o navegador cancelaria
  // um fetch/XHR comum no meio da saída da página.
  useEffect(() => {
    // Extraído de dentro de flushOnExit (era só o corpo do <video>) para ser reaproveitado pelo
    // player do YouTube também — ver ramo `yt` abaixo.
    const sendBeacon = (watchedSeconds: number, totalSeconds: number) => {
      if (!totalSeconds || !enrollmentProgress?.enrollmentId) return;
      const token = typeof window !== 'undefined' ? localStorage.getItem('access_token') : null;
      const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5000';
      try {
        fetch(`${apiUrl}/api/v1/enrollments/${enrollmentProgress.enrollmentId}/progress`, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            ...(token ? { Authorization: `Bearer ${token}` } : {}),
          },
          body: JSON.stringify({
            lessonId,
            watchedSeconds: Math.floor(watchedSeconds),
            totalSeconds: Math.floor(totalSeconds),
            clientCapturedAt: new Date().toISOString(),
          }),
          keepalive: true,
        }).catch(() => { /* best-effort — não pode travar o fechamento da aba */ });
      } catch {
        /* keepalive/fetch podem não existir em navegadores muito antigos — ignora */
      }
    };

    const flushOnExit = () => {
      const video = videoRef.current;
      if (video && video.duration) {
        sendBeacon(video.currentTime, video.duration);
        return;
      }
      // YouTube: getCurrentTime()/getDuration() são síncronos (valor já mantido em cache local
      // pela IFrame API, sem round-trip de postMessage) — dá pra ler e mandar o beacon mesmo na
      // saída abrupta da aba, igual ao <video> nativo acima.
      const yt = ytPlayerRef.current;
      if (yt) {
        try {
          const current = yt.getCurrentTime();
          const duration = yt.getDuration();
          if (current && duration) sendBeacon(current, duration);
        } catch { /* player pode já ter sido destruído nesse instante — ignora */ }
      }
      // Vimeo NÃO entra aqui: toda a API é assíncrona (Promise sobre postMessage), sem garantia
      // de round-trip a tempo do fechamento da aba — fica só com os saves periódicos de
      // timeupdate/pause do player de embed (ver useEffect "Player de embed" abaixo).
    };

    const handleVisibilityChange = () => {
      if (document.visibilityState === 'hidden') flushOnExit();
    };

    document.addEventListener('visibilitychange', handleVisibilityChange);
    window.addEventListener('pagehide', flushOnExit);

    return () => {
      document.removeEventListener('visibilitychange', handleVisibilityChange);
      window.removeEventListener('pagehide', flushOnExit);
    };
  }, [enrollmentProgress?.enrollmentId, lessonId]);

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

  const embed = playData ? resolveEmbed(playData.playbackUrl) : null;

  // Achado B10 da avaliação (2ª parte): player de embed YouTube/Vimeo — mesmo papel dos 4
  // handlers do <video> acima (retomar, salvar periodicamente, salvar em pause/ended), só que
  // via SDK de cada plataforma em vez de eventos DOM nativos, porque o player de fato roda dentro
  // de um iframe de outro domínio. Drive fica de fora (sem API de progresso — ver resolveEmbed).
  useEffect(() => {
    if (!embed || embed.provider === 'drive') return;
    if (!embedFrameRef.current || !enrollmentProgress?.enrollmentId) return;

    let cancelled = false;

    const markCompletionSideEffects = () => {
      qc.invalidateQueries({ queryKey: ['enrollment-progress', courseId] });
      qc.invalidateQueries({ queryKey: ['my-enrollments'] });
    };

    if (embed.provider === 'youtube') {
      loadYouTubeIframeApi().then(() => {
        if (cancelled || !embedFrameRef.current) return;
        const YT = (window as unknown as { YT: { Player: new (el: HTMLIFrameElement, opts: unknown) => YouTubePlayerLike; PlayerState: { PLAYING: number; PAUSED: number; ENDED: number } } }).YT;
        const player = new YT.Player(embedFrameRef.current, {
          events: {
            onReady: () => {
              if (embedHasResumedRef.current) return;
              embedHasResumedRef.current = true;
              if (resumeSeconds > 5) {
                try {
                  const duration = player.getDuration();
                  if (!duration || resumeSeconds < duration - 5) player.seekTo(resumeSeconds, true);
                } catch { /* player ainda não pronto pra seek — ignora, aula toca do início */ }
              }
            },
            onStateChange: (e: { data: number }) => {
              if (embedSaveIntervalRef.current) {
                clearInterval(embedSaveIntervalRef.current);
                embedSaveIntervalRef.current = null;
              }
              if (e.data === YT.PlayerState.PLAYING) {
                // getCurrentTime/getDuration são síncronos — polling local, sem custo de rede
                // extra; só bate na API quando passam PROGRESS_SAVE_INTERVAL_SECONDS (igual ao
                // handleTimeUpdate do <video>).
                embedSaveIntervalRef.current = setInterval(() => {
                  try {
                    const current = player.getCurrentTime();
                    const duration = player.getDuration();
                    if (current - embedLastSavedAtRef.current >= PROGRESS_SAVE_INTERVAL_SECONDS) {
                      embedLastSavedAtRef.current = current;
                      saveProgress(current, duration);
                    }
                  } catch { /* ignora tick perdido — o próximo intervalo tenta de novo */ }
                }, 1000);
              } else if (e.data === YT.PlayerState.PAUSED || e.data === YT.PlayerState.ENDED) {
                try {
                  const current = player.getCurrentTime();
                  const duration = player.getDuration();
                  embedLastSavedAtRef.current = current;
                  saveProgress(current, duration);
                  markCompletionSideEffects();
                } catch { /* ignora — próxima interação salva de novo */ }
              }
            },
          },
        });
        ytPlayerRef.current = player;
      });
    } else if (embed.provider === 'vimeo') {
      loadVimeoPlayerApi().then(() => {
        if (cancelled || !embedFrameRef.current) return;
        const Vimeo = (window as unknown as { Vimeo: { Player: new (el: HTMLIFrameElement) => VimeoPlayerLike } }).Vimeo;
        const player = new Vimeo.Player(embedFrameRef.current);
        vimeoPlayerRef.current = player;

        if (!embedHasResumedRef.current) {
          embedHasResumedRef.current = true;
          if (resumeSeconds > 5) {
            player.getDuration()
              .then((duration) => {
                if (resumeSeconds < duration - 5) return player.setCurrentTime(resumeSeconds);
              })
              .catch(() => { /* SDK pode não estar pronto ainda — aula toca do início */ });
          }
        }

        // timeupdate do Vimeo Player.js dispara ~4x/s — throttlado pro mesmo intervalo do resto
        // do player, pra não bater na API de progresso a cada evento.
        player.on('timeupdate', (data) => {
          if (data.seconds - embedLastSavedAtRef.current >= PROGRESS_SAVE_INTERVAL_SECONDS) {
            embedLastSavedAtRef.current = data.seconds;
            saveProgress(data.seconds, data.duration);
          }
        });
        const flushVimeo = (data: { seconds: number; duration: number }) => {
          embedLastSavedAtRef.current = data.seconds;
          saveProgress(data.seconds, data.duration);
          markCompletionSideEffects();
        };
        player.on('pause', flushVimeo);
        player.on('ended', flushVimeo);
      });
    }

    return () => {
      cancelled = true;
      if (embedSaveIntervalRef.current) {
        clearInterval(embedSaveIntervalRef.current);
        embedSaveIntervalRef.current = null;
      }
      try { ytPlayerRef.current?.destroy(); } catch { /* iframe já pode ter sido desmontado */ }
      try { vimeoPlayerRef.current?.destroy(); } catch { /* iframe já pode ter sido desmontado */ }
      ytPlayerRef.current = null;
      vimeoPlayerRef.current = null;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [embed?.provider, embed?.embedId, enrollmentProgress?.enrollmentId, lessonId]);

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
      <div className="flex items-center gap-2 text-sm text-ink-3 mb-6">
        <Link href="/cursos" className="hover:text-ink-2">Programas</Link>
        <span>›</span>
        <Link href={`/cursos/${slug}`} className="hover:text-ink-2 truncate max-w-[200px]">
          {course?.title ?? slug}
        </Link>
        <span>›</span>
        <span className="text-ink-2 truncate">{playData?.title ?? 'Aula'}</span>
      </div>

      <div className="flex gap-6 flex-col lg:flex-row">

        {/* ── Player principal ── */}
        <div className="flex-1 min-w-0">
          {/* Paywall */}
          {isPaywalled && (
            <div className="card flex flex-col items-center justify-center
                            py-16 text-center gap-4">
              <div className="w-16 h-16 bg-amber-50 rounded-2xl flex items-center justify-center">
                <Lock size={28} className="text-amber-500" />
              </div>
              <div>
                <h2 className="text-xl font-bold text-ink mb-2">
                  Conteúdo pago
                </h2>
                <p className="text-ink-3 text-sm max-w-sm mx-auto leading-relaxed">
                  Esta aula faz parte do conteúdo pago deste programa.
                  Adquira acesso na página do programa para continuar assistindo.
                </p>
              </div>
              <Link href={`/cursos/${slug}`} className="btn-primary mt-2">
                Ver opções de acesso →
              </Link>
            </div>
          )}

          {/* Loading */}
          {!isPaywalled && isLoading && (
            <div className="w-full aspect-video bg-surface rounded-2xl animate-pulse flex
                            items-center justify-center">
              <p className="text-ink-3 text-sm">Carregando vídeo...</p>
            </div>
          )}

          {/* Player */}
          {!isPaywalled && !isLoading && playData && (
            <>
              <div className="w-full aspect-video rounded-2xl overflow-hidden bg-black">
                {(() => {
                  if (embed) {
                    // YouTube/Vimeo importados sem download — tocam via iframe de embed, com
                    // save/resume real por cima da API própria de cada plataforma (achado B10, 2ª
                    // parte — ver useEffect "Player de embed" acima e resolveEmbed no topo do
                    // arquivo). Drive não tem API de progresso, por isso o aviso abaixo do player.
                    // Vídeos baixados e hospedados no Cloudflare Stream NÃO caem aqui — vão pelo
                    // caso da <video> comum abaixo (manifesto HLS via hls.js).
                    return (
                      <iframe
                        ref={embedFrameRef}
                        src={embed.src}
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
                      <div className="w-full h-full flex flex-col items-center justify-center gap-3 text-white/60">
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

              {/* Achado B10 da avaliação (2ª parte): Google Drive não tem API de progresso —
                  avisa em vez de fingir que a posição é salva (ver resolveEmbed no topo do
                  arquivo). YouTube/Vimeo não caem aqui: já têm save/resume real. */}
              {embed?.provider === 'drive' && (
                <p className="text-xs text-ink-3 mt-2">
                  ℹ️ Vídeo hospedado no Google Drive: sua posição não pode ser salva automaticamente por aqui.
                </p>
              )}

              {/* Título e badge */}
              <div className="mt-4 flex items-start justify-between gap-3">
                <div>
                  <h1 className="text-xl font-bold text-ink" style={{ letterSpacing: '-0.02em' }}>{playData.title}</h1>
                  <div className="flex items-center gap-3 mt-1.5 flex-wrap">
                    {playData.durationSeconds && (
                      <p className="text-sm text-ink-3">
                        {Math.floor(playData.durationSeconds / 60)}min{' '}
                        {playData.durationSeconds % 60}s
                      </p>
                    )}
                    {myLessonProgress?.isCompleted && (
                      <span className="inline-flex items-center gap-1 text-xs text-emerald-600 font-semibold">
                        <CheckCircle2 size={13} /> Aula concluída
                      </span>
                    )}
                    {!myLessonProgress?.isCompleted && resumeSeconds > 5 && (
                      <span className="text-xs text-ink-3">
                        Retomando de {Math.floor(resumeSeconds / 60)}min {resumeSeconds % 60}s
                      </span>
                    )}
                  </div>
                </div>
                {playData.isPreview && (
                  <Badge variant="active" className="shrink-0">Preview gratuito</Badge>
                )}
              </div>
            </>
          )}

          {/* Erro genérico */}
          {!isPaywalled && error && !isLoading && (
            <div className="card border-red-200 bg-red-50/40 text-center py-10">
              <p className="text-red-600 font-semibold">Erro ao carregar o vídeo.</p>
              <p className="text-sm text-ink-3 mt-1">Tente novamente em instantes.</p>
            </div>
          )}

          {/* Navegação entre aulas */}
          {(prevLesson || nextLesson) && (
            <div className="flex items-center justify-between mt-6 pt-6 border-t border-line">
              {prevLesson ? (
                <Link
                  href={`/cursos/${slug}/${prevLesson.id}`}
                  className="flex items-center gap-1.5 text-sm font-semibold text-ink-2
                             hover:text-ink transition-colors group"
                >
                  <ChevronLeft size={16} className="group-hover:-translate-x-0.5 transition-transform" />
                  <span className="truncate max-w-[180px]">{prevLesson.title}</span>
                </Link>
              ) : <div />}

              {nextLesson && (
                <Link
                  href={`/cursos/${slug}/${nextLesson.id}`}
                  className="flex items-center gap-1.5 text-sm font-semibold text-violet-600
                             hover:text-violet-700 transition-colors group ml-auto"
                >
                  <span className="truncate max-w-[180px]">{nextLesson.title}</span>
                  <ChevronRight size={16} className="group-hover:translate-x-0.5 transition-transform" />
                </Link>
              )}
            </div>
          )}
        </div>

        {/* ── Sidebar com lista de aulas ── */}
        <aside className="w-full lg:w-72 flex-shrink-0">
          <div className="card p-0 overflow-hidden sticky top-6">
            <div className="px-4 py-3.5 border-b border-line-light">
              <p className="text-sm font-bold text-ink">Aulas do programa</p>
            </div>
            <div className="overflow-y-auto max-h-[70vh]">
              {course?.modules
                .sort((a, b) => a.order - b.order)
                .map(module => (
                  <div key={module.id} className="border-b border-line-light last:border-0">
                    <div className="px-4 py-2.5 bg-surface-2">
                      <p className="text-xs font-semibold text-ink-3 uppercase tracking-wider">
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
                              flex items-start gap-3 px-4 py-2.5 transition-all
                              ${isCurrent
                                ? 'bg-violet-50 border-l-2 border-l-violet-500'
                                : 'hover:bg-subtle border-l-2 border-l-transparent'}
                            `}
                          >
                            <span className="mt-0.5 shrink-0">
                              {isCurrent
                                ? <Play size={14} fill="currentColor" className="text-violet-600" />
                                : isLocked
                                  ? <Lock size={13} className="text-ink-4" />
                                  : isCompleted
                                    ? <CheckCircle2 size={14} className="text-emerald-500" />
                                    : <Circle size={14} className="text-ink-4" />}
                            </span>
                            <div className="min-w-0">
                              <p className={`text-xs font-medium leading-snug truncate
                                ${isCurrent ? 'text-violet-700 font-semibold' : 'text-ink-2'}`}>
                                {lesson.title}
                              </p>
                              {lesson.durationSeconds && (
                                <p className="text-xs text-ink-4 mt-0.5">
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
