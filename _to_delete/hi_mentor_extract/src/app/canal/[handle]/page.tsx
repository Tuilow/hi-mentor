'use client';

import { useEffect, useState } from 'react';
import { useParams } from 'next/navigation';
import { useQuery } from '@tanstack/react-query';
import Link from 'next/link';
import { Instagram, Youtube, MessageCircle, Globe, Twitter, Facebook, Linkedin, Music2, Lock, BookOpen, Play } from 'lucide-react';
import { channelApi } from '@/lib/api';
import type { PublicChannel } from '@/types';
import { commercializationLabel } from '@/lib/courseCommercialization';
import Logo from '@/components/brand/Logo';
import { Badge } from '@/components/ui/design-system';

/**
 * Converte a URL colada pelo mentor (YouTube/Vimeo) numa URL de embed — mesma lógica de
 * /c/[slug] (vídeo da página de vendas), aqui para o vídeo de apresentação do canal.
 */
function toEmbedUrl(url: string): string | null {
  const youtubeMatch = url.match(/(?:youtube\.com\/watch\?v=|youtu\.be\/|youtube\.com\/embed\/)([a-zA-Z0-9_-]{6,})/);
  if (youtubeMatch) return `https://www.youtube.com/embed/${youtubeMatch[1]}`;

  const vimeoMatch = url.match(/vimeo\.com\/(?:video\/)?(\d+)/);
  if (vimeoMatch) return `https://player.vimeo.com/video/${vimeoMatch[1]}`;

  return null;
}

const platformIcon: Record<string, React.ReactNode> = {
  instagram: <Instagram size={16} />,
  youtube: <Youtube size={16} />,
  tiktok: <Music2 size={16} />,
  whatsapp: <MessageCircle size={16} />,
  site: <Globe size={16} />,
  twitter: <Twitter size={16} />,
  x: <Twitter size={16} />,
  facebook: <Facebook size={16} />,
  linkedin: <Linkedin size={16} />,
};

/**
 * Canal do Mentor — vitrine pública em hi-mentor.com/canal/{handle} com todos os programas
 * publicados do mentor. Programas grátis aparecem liberados para qualquer um; programas pagos
 * mostram cadeado até o visitante logar e comprar/matricular (mesma regra do backend,
 * ver GetPublicChannelQueryHandler.IsUnlocked).
 *
 * Redesign "Hi Mentor" (ago/2026): banner + bloco de perfil + grid de programas seguindo
 * hi-mentor/src/pages/MentorChannelPage.tsx. O protótipo também tem abas "Sobre" (com bio
 * estendida/estatísticas) e "Comunidade" (posts) — como o backend só fornece bio/redes sociais
 * (já mostradas no bloco de perfil) e nenhum dado de posts/comunidade, essas abas não foram
 * recriadas aqui: uma navegação por abas com conteúdo inventado seria pior do que não ter abas
 * (ver RELATORIO_REDESIGN_HI_MENTOR.md).
 */
export default function PublicChannelPage() {
  const { handle } = useParams<{ handle: string }>();
  const [isLoggedIn, setIsLoggedIn] = useState(false);

  useEffect(() => {
    setIsLoggedIn(!!localStorage.getItem('access_token'));
  }, []);

  const { data: channel, isLoading } = useQuery<PublicChannel>({
    queryKey: ['public-channel', handle],
    queryFn: () => channelApi.getByHandle(handle).then(r => r.data),
    enabled: !!handle,
  });

  if (isLoading) {
    return (
      <div className="min-h-screen bg-canvas flex items-center justify-center">
        <p className="text-ink-3 text-sm">Carregando...</p>
      </div>
    );
  }

  if (!channel) {
    return (
      <div className="min-h-screen bg-canvas flex flex-col items-center justify-center gap-3">
        <p className="text-4xl">😕</p>
        <p className="text-lg font-medium text-ink">Canal não encontrado</p>
        <Link href="/" className="text-violet-600 hover:underline text-sm">← Voltar para o início</Link>
      </div>
    );
  }

  const embedUrl = channel.introVideoUrl ? toEmbedUrl(channel.introVideoUrl) : null;

  return (
    <div className="min-h-screen bg-canvas" style={{ fontFamily: 'var(--font-manrope)' }}>
      {/* Nav */}
      <header className="sticky top-0 z-30 bg-surface/90 backdrop-blur-md border-b border-line">
        <div className="max-w-5xl mx-auto px-5 h-14 flex items-center justify-between gap-4">
          <Link href="/"><Logo size="xs" /></Link>
          <Link href={isLoggedIn ? '/dashboard' : '/login'}
            className="text-sm font-semibold text-ink-2 hover:text-ink transition-colors">
            {isLoggedIn ? 'Ir para o painel →' : 'Entrar'}
          </Link>
        </div>
      </header>

      {/* Banner */}
      <div className="relative h-40 sm:h-56 overflow-hidden bg-violet-900">
        {channel.bannerUrl && (
          // eslint-disable-next-line @next/next/no-img-element
          <img src={channel.bannerUrl} alt="" className="w-full h-full object-cover" />
        )}
        <div className="absolute inset-0 bg-gradient-to-t from-black/50 via-black/10 to-transparent" />
      </div>

      <div className="max-w-5xl mx-auto px-5">
        {/* Perfil */}
        <div className="relative -mt-12 mb-8 flex flex-col sm:flex-row sm:items-end gap-4">
          {channel.avatarUrl ? (
            // eslint-disable-next-line @next/next/no-img-element
            <img src={channel.avatarUrl} alt={channel.displayName}
              className="w-24 h-24 rounded-2xl object-cover border-4 border-surface shadow-lg shrink-0 z-10" />
          ) : (
            <div className="w-24 h-24 rounded-2xl bg-violet-600 border-4 border-surface flex items-center justify-center text-white text-3xl font-bold shrink-0 shadow-lg z-10">
              {channel.displayName[0]?.toUpperCase()}
            </div>
          )}

          <div className="flex-1 sm:pb-1">
            <h1 className="text-2xl font-extrabold text-ink" style={{ letterSpacing: '-0.025em' }}>
              {channel.displayName}
            </h1>
            <p className="text-ink-3 text-sm mt-0.5">@{channel.handle}</p>
            {channel.bio && <p className="text-ink-2 text-sm mt-3 max-w-lg leading-relaxed">{channel.bio}</p>}

            {channel.socialLinks.length > 0 && (
              <div className="flex items-center gap-2 mt-4">
                {channel.socialLinks.map((link, i) => (
                  <a key={i} href={link.url} target="_blank" rel="noopener noreferrer"
                    className="w-9 h-9 rounded-full bg-subtle hover:bg-violet-50 hover:text-violet-600 text-ink-2 transition-colors
                               flex items-center justify-center" title={link.platform}>
                    {platformIcon[link.platform.toLowerCase()] ?? <Globe size={16} />}
                  </a>
                ))}
              </div>
            )}
          </div>
        </div>

        {/* Vídeo de apresentação */}
        {embedUrl && (
          <div className="mb-10 rounded-2xl overflow-hidden aspect-video bg-black">
            <iframe
              src={embedUrl}
              className="w-full h-full"
              allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
              allowFullScreen
            />
          </div>
        )}

        {/* Catálogo de programas */}
        <div className="pb-14">
          <p className="text-xs font-bold text-ink-3 uppercase tracking-widest mb-4">
            Programas {channel.courses.length > 0 && `(${channel.courses.length})`}
          </p>

          {channel.courses.length === 0 ? (
            <div className="card text-center py-10">
              <p className="text-ink-3 text-sm">Nenhum programa publicado ainda.</p>
            </div>
          ) : (
            <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-5">
              {/* Regra principal (Problema 3): "visualizar" (aparecer nesta grade) ≠ "possuir
                  acesso" — todos os programas publicados do mentor aparecem aqui, mas só quem
                  tem acesso de fato (course.isUnlocked, calculado no backend por
                  IUserCourseAccessService) pode assistir. Vai sempre para /c/{slug}: a própria
                  página de vendas já resolve "já matriculado → entra direto no programa" ou
                  "ainda não → matricula/paga", sem duplicar essa lógica aqui. */}
              {channel.courses.map(course => (
                <Link
                  key={course.id}
                  href={`/c/${course.slug}`}
                  className="bg-surface rounded-2xl border border-line overflow-hidden flex flex-col group hover:shadow-md hover:border-line-light transition-all"
                >
                  <div className="h-36 relative overflow-hidden bg-subtle">
                    {course.thumbnailUrl ? (
                      // eslint-disable-next-line @next/next/no-img-element
                      <img src={course.thumbnailUrl} alt={course.title}
                        className="w-full h-full object-cover transition-transform duration-300 group-hover:scale-105" />
                    ) : (
                      <div className="w-full h-full flex items-center justify-center text-ink-3">
                        <BookOpen size={28} />
                      </div>
                    )}
                    {!course.isUnlocked && (
                      <div className="absolute top-2.5 right-2.5 w-7 h-7 rounded-full bg-black/50 backdrop-blur-sm
                                      flex items-center justify-center text-white">
                        <Lock size={13} />
                      </div>
                    )}
                    {course.isUnlocked && (
                      <div className="absolute bottom-2.5 right-2.5">
                        <Badge variant="done" className="text-[10px]">Inscrito</Badge>
                      </div>
                    )}
                  </div>
                  <div className="p-4 flex flex-col flex-1">
                    <h3 className="font-bold text-ink text-sm leading-snug mb-2 line-clamp-2">{course.title}</h3>
                    <p className="text-sm font-bold text-violet-600 mt-auto">
                      {commercializationLabel(course.commercializationState, course.price, course.isFree)}
                    </p>
                    <p className="text-xs text-ink-3 mt-1 flex items-center gap-1">
                      {course.isUnlocked
                        ? <><Play size={11} fill="currentColor" /> Continuar assistindo</>
                        : <><Lock size={11} /> Comprar programa</>}
                    </p>
                  </div>
                </Link>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
