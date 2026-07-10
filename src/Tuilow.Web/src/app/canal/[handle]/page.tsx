'use client';

import { useEffect, useState } from 'react';
import { useParams } from 'next/navigation';
import { useQuery } from '@tanstack/react-query';
import Link from 'next/link';
import { channelApi } from '@/lib/api';
import type { PublicChannel } from '@/types';

function formatPrice(price: number): string {
  return price === 0
    ? 'Grátis'
    : price.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

const platformIcon: Record<string, string> = {
  instagram: '📷',
  youtube: '▶️',
  tiktok: '🎵',
  whatsapp: '💬',
  site: '🔗',
  twitter: '🐦',
  x: '🐦',
  facebook: '📘',
  linkedin: '💼',
};

/**
 * Canal do Criador — vitrine pública em tuilow.com/canal/{handle} com todos os cursos
 * publicados do criador. Cursos grátis aparecem liberados para qualquer um; cursos pagos
 * mostram cadeado até o visitante logar e comprar/matricular (mesma regra do backend,
 * ver GetPublicChannelQueryHandler.IsUnlocked).
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
      <div className="min-h-screen bg-white flex items-center justify-center">
        <p className="text-gray-400 text-sm">Carregando...</p>
      </div>
    );
  }

  if (!channel) {
    return (
      <div className="min-h-screen bg-white flex flex-col items-center justify-center gap-3">
        <p className="text-4xl">😕</p>
        <p className="text-lg font-medium text-gray-700">Canal não encontrado</p>
        <Link href="/" className="text-brand-600 hover:underline text-sm">← Voltar para o início</Link>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-white">
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
        {/* Perfil do canal */}
        <div className="flex flex-col items-center text-center mb-8">
          {channel.avatarUrl ? (
            <img src={channel.avatarUrl} alt={channel.displayName}
              className="w-24 h-24 rounded-full object-cover mb-4" />
          ) : (
            <div className="w-24 h-24 rounded-full bg-gradient-to-br from-brand-500 to-accent-500
                            flex items-center justify-center text-white text-3xl font-bold mb-4">
              {channel.displayName[0]?.toUpperCase()}
            </div>
          )}
          <h1 className="text-2xl font-bold text-gray-800">{channel.displayName}</h1>
          <p className="text-sm text-gray-400 mt-0.5">@{channel.handle}</p>
          {channel.bio && <p className="text-gray-600 mt-3 max-w-lg">{channel.bio}</p>}

          {channel.socialLinks.length > 0 && (
            <div className="flex items-center gap-3 mt-4">
              {channel.socialLinks.map((link, i) => (
                <a key={i} href={link.url} target="_blank" rel="noopener noreferrer"
                  className="w-10 h-10 rounded-full bg-gray-100 hover:bg-gray-200 transition-colors
                             flex items-center justify-center text-lg" title={link.platform}>
                  {platformIcon[link.platform.toLowerCase()] ?? '🔗'}
                </a>
              ))}
            </div>
          )}
        </div>

        {/* Catálogo de cursos */}
        <h2 className="text-lg font-semibold text-gray-800 mb-4">
          Cursos {channel.courses.length > 0 && `(${channel.courses.length})`}
        </h2>

        {channel.courses.length === 0 ? (
          <div className="card border-gray-200 text-center py-10">
            <p className="text-gray-400 text-sm">Nenhum curso publicado ainda.</p>
          </div>
        ) : (
          <div className="grid sm:grid-cols-2 gap-4">
            {channel.courses.map(course => (
              <Link key={course.id} href={`/c/${course.slug}`}
                className="card border-gray-200 hover:border-brand-300 transition-colors p-0 overflow-hidden">
                <div className="relative">
                  {course.thumbnailUrl ? (
                    <img src={course.thumbnailUrl} alt={course.title}
                      className="w-full h-36 object-cover" />
                  ) : (
                    <div className="w-full h-36 bg-gradient-to-br from-gray-100 to-gray-50
                                    flex items-center justify-center text-4xl">📚</div>
                  )}
                  {!course.isUnlocked && (
                    <div className="absolute top-2 right-2 w-7 h-7 rounded-full bg-black/60
                                    flex items-center justify-center text-white text-xs">🔒</div>
                  )}
                </div>
                <div className="p-4">
                  <p className="text-sm font-semibold text-gray-800 line-clamp-2">{course.title}</p>
                  <p className="text-sm gradient-text font-bold mt-2">{formatPrice(course.price)}</p>
                </div>
              </Link>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
