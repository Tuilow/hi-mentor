'use client';

import { useEffect, useRef, useState } from 'react';
import { useParams } from 'next/navigation';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { coursesApi, enrollmentsApi } from '@/lib/api';
import Link from 'next/link';
import toast from 'react-hot-toast';
import Hls from 'hls.js';
import {
  Lock, CheckCircle2, Circle, Play, ChevronLeft, ChevronRight,
  BookOpen, Download, MessageSquare,
} from 'lucide-react';
import { Badge } from '@/components/ui/design-system';

// Redesign "Hi Mentor" (ago/2026, 2ª rodada): reskin visual completo seguindo
// hi-mentor/src/pages/PlayerPage.tsx — player full-bleed escuro, barra superior própria
// (Voltar/breadcrumb/"Marcar como concluído"/toggle "Jornada"), abas abaixo do player
// (Descrição/Materiais/Anotações/Discussão) e barra lateral "Sua jornada" com progresso real.
// TODA a lógica de reprodução/progresso abaixo (refs, effects de HLS/YouTube/Vimeo,
// salvamento/retomada, paywall) permanece exatamente a mesma da 1ª rodada do redesign — só o
// `return` final e um punhado de estado de UI nova (activeTab/journeyOpen/markingComplete) foram
// adicionados. "Materiais"/"Anotações"/"Discussão" não têm contrapartida real hoje — Materiais
// JÁ EXISTE como dado real no domínio (Lesson.Attachments, endpoint de download protegido em
// MaterialsUploadController), mas o DTO que esta página consome (CourseDetailResponse/
// LessonResponse, GetCourseBySlugQueryHandler) ainda não devolve a lista de anexos por aula —
// por isso, e não por falta de feature no backend, a aba fica "Em breve" nesta rodada (gap
// pequeno e pontual, plausível de fechar numa próxima passada). Anotações/Discussão não têm
// NENHUM modelo de dado no backend (sem entidade Nota/Comentário) — "Em breve" de verdade.
// Sem player custom (barra de progresso/volume/velocidade toda refeita do zero): o protótipo tem
// controles próprios, mas eles não funcionariam de forma uniforme para os 4 casos reais que este
// player já suporta (arquivo de vídeo via <video>, iframe do YouTube, iframe do Vimeo, iframe do
// Drive) — um iframe de terceiro não pode ser controlado por uma barra de progresso HTML externa.
// Mantidos os controles nativos (<video controls>/UI própria de cada iframe), só restilizados ao
// redor (fundo escuro full-bleed) — trocar isso arriscaria quebrar o salvamento/retomada de
// progresso, que é a funcionalidade mais importante desta tela.

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
  // Já vem de GetCourseBySlugQueryHandler hoje — só não estava declarado aqui porque esta página
  // não usava ainda (mesmo dado já usado em (dashboard)/cursos/[slug]/page.tsx).
  instructorName?: string;
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
      // Idem — LessonResponse.Description já existe no backend.
      description?: string;
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
  // Estado de UI nova do redesign (protótipo PlayerPage.tsx) — sem nenhum efeito sobre a lógica
  // de reprodução/progresso acima.
  const [journeyOpen, setJourneyOpen] = useState(true);
  const [activeTab, setActiveTab] = useState<'descricao' | 'materiais' | 'anotacoes' | 'discussao'>('descricao');
  const [markingComplete, setMarkingComplete] = useState(false);
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

  // "Módulo N · Aula M" (protótipo PlayerPage.tsx) — cópias não-mutantes (.slice().sort()) pra
  // não repetir o sort() in-place de `allLessons` acima sobre o mesmo array do cache do React Query.
  const sortedModulesForLabel = course?.modules.slice().sort((a, b) => a.order - b.order)
    .map(m => ({ ...m, lessons: m.lessons.slice().sort((a, b) => a.order - b.order) })) ?? [];
  let currentModuleNumber: number | null = null;
  let currentLessonNumberInModule: number | null = null;
  sortedModulesForLabel.forEach((m, mi) => {
    const li = m.lessons.findIndex(l => l.id === lessonId);
    if (li !== -1) {
      currentModuleNumber = mi + 1;
      currentLessonNumberInModule = li + 1;
    }
  });

  const currentLessonDescription = allLessons.find(l => l.id === lessonId)?.description;

  // "Marcar como concluído" (protótipo PlayerPage.tsx) — não é um endpoint novo: reusa o mesmo
  // POST /enrollments/{id}/progress que o player já chama a cada 10s (saveProgress acima),
  // simplesmente reportando watchedSeconds = totalSeconds conhecidos. O backend já considera
  // concluída qualquer aula com >= 90% assistido (Enrollment/LessonProgress.UpdateProgress),
  // então isto não precisa (nem deveria) de uma regra nova — só aciona a mesma regra real na
  // hora, em vez de esperar o aluno assistir até o fim.
  const handleMarkComplete = async () => {
    if (!enrollmentProgress?.enrollmentId || myLessonProgress?.isCompleted) return;
    const total = videoRef.current?.duration || playData?.durationSeconds;
    if (!total) {
      toast.error('Não foi possível concluir: duração da aula ainda não carregou.');
      return;
    }
    setMarkingComplete(true);
    try {
      await enrollmentsApi.trackProgress(enrollmentProgress.enrollmentId, {
        lessonId,
        watchedSeconds: Math.floor(total),
        totalSeconds: Math.floor(total),
        clientCapturedAt: new Date().toISOString(),
      });
      qc.invalidateQueries({ queryKey: ['enrollment-progress', courseId] });
      qc.invalidateQueries({ queryKey: ['my-enrollments'] });
      toast.success('Aula marcada como concluída!');
    } catch {
      toast.error('Erro ao marcar aula como concluída.');
    } finally {
      setMarkingComplete(false);
    }
  };

  // Escape do sidebar do dashboard (mesma técnica de ../MentoradoJourneyView.tsx): esta rota
  // vive dentro do grupo (dashboard) e por isso herdaria o sidebar fixo se não fosse isto — um
  // overlay "fixed inset-0" cobre o sidebar (z-30) e o topbar mobile inteiros, replicando o
  // protótipo (que não tem sidebar nesta tela).
  return (
    <div className="fixed inset-0 z-40 bg-ink flex flex-col overflow-hidden">
      {/* Top bar */}
      <div className="flex items-center justify-between gap-4 px-5 py-3.5 border-b border-white/10 bg-ink shrink-0">
        <div className="flex items-center gap-3 min-w-0">
          <Link href={`/cursos/${slug}`} className="flex items-center gap-2 text-white/60 hover:text-white transition-colors text-sm font-medium shrink-0">
            <ChevronLeft size={18} />
            <span className="hidden sm:block">Voltar</span>
          </Link>
          <div className="w-px h-4 bg-white/20 shrink-0" />
          <div className="min-w-0">
            <p className="text-white/50 text-xs truncate">
              {currentModuleNumber ? `Módulo ${currentModuleNumber} · ${course?.title ?? ''}` : (course?.title ?? '')}
            </p>
            <p className="text-white font-semibold text-sm truncate">{playData?.title ?? 'Aula'}</p>
          </div>
        </div>
        <div className="flex items-center gap-3 shrink-0">
          {isEnrolled && playData && (
            <button
              onClick={handleMarkComplete}
              disabled={myLessonProgress?.isCompleted || markingComplete}
              className={`flex items-center gap-1.5 text-xs font-semibold px-3 py-1.5 rounded-lg border transition-all ${
                myLessonProgress?.isCompleted
                  ? 'bg-emerald-500/15 border-emerald-400/30 text-emerald-300 cursor-default'
                  : 'bg-white/10 border-white/20 text-white hover:bg-white/20 disabled:opacity-50 disabled:cursor-not-allowed'
              }`}
            >
              <CheckCircle2 size={14} />
              <span className="hidden sm:inline">
                {myLessonProgress?.isCompleted ? 'Aula concluída' : markingComplete ? 'Salvando...' : 'Marcar como concluído'}
              </span>
            </button>
          )}
          <button
            onClick={() => setJourneyOpen(o => !o)}
            className="text-white/60 hover:text-white text-xs font-semibold transition-colors flex items-center gap-1.5 shrink-0"
          >
            <BookOpen size={15} />
            <span className="hidden sm:block">Jornada</span>
          </button>
        </div>
      </div>

      {/* Main area */}
      <div className="flex flex-1 overflow-hidden">

        {/* Player column */}
        <div className="flex flex-col flex-1 overflow-y-auto">

          {/* Paywall */}
          {isPaywalled && (
            <div className="bg-black flex flex-col items-center justify-center py-16 text-center gap-4"
                 style={{ aspectRatio: '16/9', maxHeight: '60vh' }}>
              <div className="w-16 h-16 bg-amber-400/10 rounded-2xl flex items-center justify-center">
                <Lock size={28} className="text-amber-400" />
              </div>
              <div>
                <h2 className="text-xl font-bold text-white mb-2">Conteúdo pago</h2>
                <p className="text-white/50 text-sm max-w-sm mx-auto leading-relaxed">
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
            <div className="w-full bg-black animate-pulse flex items-center justify-center"
                 style={{ aspectRatio: '16/9', maxHeight: '60vh' }}>
              <p className="text-white/40 text-sm">Carregando vídeo...</p>
            </div>
          )}

          {/* Player */}
          {!isPaywalled && !isLoading && playData && (
            <div className="relative bg-black" style={{ aspectRatio: '16/9', maxHeight: '60vh' }}>
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
                // não pela prop — por isso fica undefined nesse caso. Mantidos os controles
                // nativos do navegador (ver nota no topo do arquivo sobre não reimplementar um
                // player custom) — só o fundo ao redor virou full-bleed escuro.
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
          )}

          {/* Erro genérico */}
          {!isPaywalled && error && !isLoading && (
            <div className="bg-black flex flex-col items-center justify-center gap-1 py-16"
                 style={{ aspectRatio: '16/9', maxHeight: '60vh' }}>
              <p className="text-red-400 font-semibold">Erro ao carregar o vídeo.</p>
              <p className="text-sm text-white/40">Tente novamente em instantes.</p>
            </div>
          )}

          {/* Abaixo do player */}
          <div className="bg-canvas flex-1 px-6 py-6">

            {/* Achado B10 da avaliação (2ª parte): Google Drive não tem API de progresso —
                avisa em vez de fingir que a posição é salva (ver resolveEmbed no topo do
                arquivo). YouTube/Vimeo não caem aqui: já têm save/resume real. */}
            {embed?.provider === 'drive' && (
              <p className="text-xs text-ink-3 mb-4">
                ℹ️ Vídeo hospedado no Google Drive: sua posição não pode ser salva automaticamente por aqui.
              </p>
            )}

            {/* Título + navegação */}
            <div className="flex items-start justify-between gap-4 mb-5 flex-wrap">
              <div className="min-w-0">
                <p className="text-xs font-semibold text-ink-3 mb-1">
                  {currentModuleNumber && currentLessonNumberInModule
                    ? `Módulo ${currentModuleNumber} · Aula ${currentLessonNumberInModule}`
                    : 'Aula'}
                </p>
                <h1 className="text-xl font-bold text-ink" style={{ letterSpacing: '-0.02em' }}>
                  {playData?.title ?? '...'}
                </h1>
                <div className="flex items-center gap-3 mt-1.5 flex-wrap text-sm">
                  {course?.instructorName && (
                    <span className="text-ink-3">{course.instructorName}</span>
                  )}
                  {playData?.durationSeconds && (
                    <span className="text-ink-3">
                      {course?.instructorName && '· '}
                      {Math.floor(playData.durationSeconds / 60)}min {playData.durationSeconds % 60}s
                    </span>
                  )}
                  {playData?.isPreview && <Badge variant="active">Preview gratuito</Badge>}
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
              {(prevLesson || nextLesson) && (
                <div className="flex gap-2 shrink-0">
                  {prevLesson && (
                    <Link
                      href={`/cursos/${slug}/${prevLesson.id}`}
                      className="flex items-center gap-1.5 text-sm font-semibold text-ink-2 hover:text-ink px-3 py-1.5 rounded-lg hover:bg-subtle transition-all"
                    >
                      <ChevronLeft size={16} /> Anterior
                    </Link>
                  )}
                  {nextLesson && (
                    <Link
                      href={`/cursos/${slug}/${nextLesson.id}`}
                      className="flex items-center gap-1.5 text-sm font-semibold text-violet-600 hover:text-violet-700 px-3 py-1.5 rounded-lg hover:bg-violet-50 transition-all"
                    >
                      Próxima <ChevronRight size={16} />
                    </Link>
                  )}
                </div>
              )}
            </div>

            {/* Abas — Materiais/Anotações/Discussão sem contrapartida real hoje (ver nota no topo
                do arquivo), marcadas com selo "Em breve" em vez de fingir que funcionam. */}
            <div className="flex gap-1 border-b border-line mb-5 overflow-x-auto">
              {([
                ['descricao', 'Descrição', false],
                ['materiais', 'Materiais', true],
                ['anotacoes', 'Anotações', true],
                ['discussao', 'Discussão', true],
              ] as const).map(([tab, label, soon]) => (
                <button
                  key={tab}
                  onClick={() => setActiveTab(tab)}
                  className={`flex items-center gap-1.5 px-4 py-2.5 text-sm font-semibold border-b-2 -mb-px transition-all shrink-0 ${
                    activeTab === tab ? 'border-violet-600 text-violet-600' : 'border-transparent text-ink-3 hover:text-ink'
                  }`}
                >
                  {label}
                  {soon && (
                    <span className="inline-flex items-center px-1.5 py-0.5 rounded-full bg-amber-50 text-amber-700 text-[10px] font-bold border border-amber-200">
                      Em breve
                    </span>
                  )}
                </button>
              ))}
            </div>

            {activeTab === 'descricao' && (
              <div className="max-w-2xl">
                {currentLessonDescription ? (
                  <p className="text-sm text-ink-2 leading-relaxed mb-4 whitespace-pre-line">
                    {currentLessonDescription}
                  </p>
                ) : (
                  <p className="text-sm text-ink-3 mb-4">Esta aula ainda não tem descrição.</p>
                )}
                {nextLesson && (
                  <div className="p-4 bg-violet-50 border border-violet-100 rounded-xl">
                    <p className="text-sm font-bold text-violet-800 mb-1">Próxima aula</p>
                    <p className="text-sm text-violet-600">
                      <span className="font-semibold">{nextLesson.title}</span>
                    </p>
                  </div>
                )}
              </div>
            )}

            {activeTab === 'materiais' && (
              <div className="max-w-lg text-center py-10">
                <Download size={28} className="mx-auto mb-3 text-ink-4" />
                <p className="font-semibold text-ink-2 mb-1">Em breve</p>
                <p className="text-sm text-ink-3">
                  Materiais de apoio desta aula vão aparecer aqui assim que a plataforma passar a exibi-los.
                </p>
              </div>
            )}

            {activeTab === 'anotacoes' && (
              <div className="max-w-lg">
                <textarea
                  disabled
                  className="w-full h-40 bg-subtle border border-line rounded-xl px-4 py-3 text-sm text-ink-3 placeholder:text-ink-4 resize-none cursor-not-allowed"
                  placeholder="Em breve você vai poder escrever anotações sobre esta aula aqui."
                />
              </div>
            )}

            {activeTab === 'discussao' && (
              <div className="max-w-lg">
                <div className="text-center py-12 text-ink-3">
                  <MessageSquare size={32} className="mx-auto mb-3 opacity-40" />
                  <p className="font-semibold text-ink-2 mb-1">Em breve</p>
                  <p className="text-sm">Em breve você vai poder discutir esta aula com outros mentorados aqui.</p>
                </div>
              </div>
            )}
          </div>
        </div>

        {/* Journey sidebar */}
        {journeyOpen && (
          <aside className="w-72 border-l border-white/10 bg-[oklch(13%_0.018_280)] flex-col shrink-0 overflow-y-auto hidden lg:flex">
            <div className="px-4 py-4 border-b border-white/10">
              <p className="text-white font-bold text-sm">Sua jornada</p>
              {enrollmentProgress && (
                <div className="flex items-center gap-2 mt-2">
                  <div className="flex-1 bg-white/10 rounded-full overflow-hidden h-1">
                    <div
                      className="h-full bg-violet-500 rounded-full"
                      style={{ width: `${Math.min(100, Math.max(0, enrollmentProgress.progressPercentage))}%` }}
                    />
                  </div>
                  <span className="text-white/50 text-xs font-semibold shrink-0">
                    {Math.round(enrollmentProgress.progressPercentage)}%
                  </span>
                </div>
              )}
            </div>

            <div className="flex-1">
              {sortedModulesForLabel.map(module => (
                <div key={module.id} className="border-b border-white/5">
                  <div className="px-4 py-3">
                    <p className="text-white/40 text-xs font-semibold uppercase tracking-wider">{module.title}</p>
                  </div>
                  <div className="pb-2">
                    {module.lessons.map(lesson => {
                      const isCurrent = lesson.id === lessonId;
                      // Mesma regra real de acesso já usada nesta página antes do redesign — só
                      // é "locked" pra quem NÃO está matriculado (preview parcial); matriculado
                      // nunca vê cadeado, porque não existe trava sequencial real no backend.
                      const isLocked = !lesson.isPreview && !isEnrolled;
                      const isCompleted = enrollmentProgress?.lessonProgress
                        .find(lp => lp.lessonId === lesson.id)?.isCompleted ?? false;
                      const disabled = isLocked;
                      const content = (
                        <>
                          <div className="shrink-0 mt-0.5">
                            {isCurrent
                              ? <Play size={14} fill="currentColor" className="text-violet-400 ml-0.5" />
                              : isLocked
                                ? <Lock size={13} className="text-white/30" />
                                : isCompleted
                                  ? <CheckCircle2 size={14} className="text-emerald-400" />
                                  : <Circle size={14} className="text-white/30" />}
                          </div>
                          <div className="flex-1 min-w-0">
                            <p className={`text-xs font-medium leading-snug truncate ${
                              isCurrent ? 'text-violet-200 font-semibold' : isLocked ? 'text-white/30' : 'text-white/60'
                            }`}>
                              {lesson.title}
                            </p>
                            {lesson.durationSeconds && (
                              <p className="text-[10px] text-white/30 mt-0.5">
                                {Math.floor(lesson.durationSeconds / 60)}min
                              </p>
                            )}
                          </div>
                        </>
                      );
                      const className = `w-full flex items-start gap-3 px-4 py-2.5 text-left transition-all ${
                        isCurrent
                          ? 'bg-violet-600/20 border-l-2 border-violet-400'
                          : isLocked
                            ? 'opacity-70 cursor-default border-l-2 border-transparent'
                            : 'hover:bg-white/5 border-l-2 border-transparent'
                      }`;
                      return disabled ? (
                        <div key={lesson.id} className={className}>{content}</div>
                      ) : (
                        <Link key={lesson.id} href={`/cursos/${slug}/${lesson.id}`} className={className}>
                          {content}
                        </Link>
                      );
                    })}
                  </div>
                </div>
              ))}
            </div>
          </aside>
        )}
      </div>
    </div>
  );
}
