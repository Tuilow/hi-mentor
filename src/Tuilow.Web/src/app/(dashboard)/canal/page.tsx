'use client';

import { useEffect, useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { Plus, Trash2, ExternalLink } from 'lucide-react';
import { authApi, channelApi } from '@/lib/api';
import type { MyChannel, SocialLinkItem } from '@/types';

interface MeResponse {
  firstName: string;
  lastName: string;
  avatarUrl?: string;
  bio?: string;
  phone?: string;
  birthDate?: string;
}

const platforms = ['Instagram', 'YouTube', 'TikTok', 'WhatsApp', 'Site', 'Twitter', 'Facebook', 'LinkedIn'];

/**
 * Tela "Meu Canal" — o criador define seu @handle público, redes sociais, avatar e bio.
 * Bio/avatar não pertencem a este agregado (vêm de IdentidadeAcesso.UserProfile, editados
 * aqui via PUT /auth/me) — só o @handle e as redes sociais são dados próprios do Canal
 * (PUT /channel/me). O front trata isso como um único formulário; o backend mantém a separação.
 */
export default function MeuCanalPage() {
  const queryClient = useQueryClient();

  const { data: me } = useQuery<MeResponse>({
    queryKey: ['user-profile'],
    queryFn: () => authApi.me().then(r => r.data),
  });

  const { data: channel, isLoading } = useQuery<MyChannel | null>({
    queryKey: ['my-channel'],
    queryFn: () => channelApi.getMy().then(r => r.data),
  });

  const [handle, setHandle] = useState('');
  const [bio, setBio] = useState('');
  const [avatarUrl, setAvatarUrl] = useState('');
  const [links, setLinks] = useState<SocialLinkItem[]>([]);
  const [hydrated, setHydrated] = useState(false);

  useEffect(() => {
    if (hydrated) return;
    if (channel) {
      setHandle(channel.handle);
      setLinks(channel.socialLinks);
    }
    if (me) {
      setBio(me.bio ?? '');
      setAvatarUrl(me.avatarUrl ?? '');
    }
    if (channel !== undefined && me !== undefined) setHydrated(true);
  }, [channel, me, hydrated]);

  const saveMutation = useMutation({
    mutationFn: async () => {
      await channelApi.upsert({
        handle,
        socialLinks: links.filter(l => l.platform && l.url),
      });
      if (me) {
        // Update é um "overwrite" completo no backend (não faz merge parcial) — precisamos
        // reenviar phone/birthDate como vieram do GET, senão a edição de bio/avatar aqui
        // apagaria silenciosamente esses dois campos do perfil.
        await authApi.updateProfile({
          firstName: me.firstName,
          lastName: me.lastName,
          phone: me.phone,
          birthDate: me.birthDate,
          bio,
          avatarUrl,
        });
      }
    },
    onSuccess: () => {
      toast.success('Canal atualizado com sucesso!');
      queryClient.invalidateQueries({ queryKey: ['my-channel'] });
      queryClient.invalidateQueries({ queryKey: ['user-profile'] });
    },
    onError: (err: unknown) => {
      const message = (err as { response?: { data?: { message?: string } } })?.response?.data?.message;
      toast.error(message ?? 'Não foi possível salvar. Verifique o @ escolhido.');
    },
  });

  const addLink = () => setLinks([...links, { platform: 'Instagram', url: '' }]);
  const removeLink = (i: number) => setLinks(links.filter((_, idx) => idx !== i));
  const updateLink = (i: number, field: keyof SocialLinkItem, value: string) =>
    setLinks(links.map((l, idx) => (idx === i ? { ...l, [field]: value } : l)));

  if (isLoading || !hydrated) {
    return <p className="text-gray-400 text-sm">Carregando...</p>;
  }

  return (
    <div className="max-w-2xl">
      <div className="flex items-center justify-between mb-1">
        <h1 className="text-2xl font-bold text-gray-800">Meu Canal</h1>
      </div>
      <p className="text-sm text-gray-500 mb-6">
        Sua vitrine pública com todos os cursos publicados — divulgue um único link e reúna sua audiência.
      </p>

      {channel && (
        <a href={`/canal/${channel.handle}`} target="_blank" rel="noopener noreferrer"
          className="inline-flex items-center gap-1.5 text-sm text-brand-600 hover:underline mb-6">
          tuilow.com/canal/{channel.handle} <ExternalLink className="w-3.5 h-3.5" />
        </a>
      )}

      <div className="card border-gray-200 space-y-5">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1.5">@ do canal</label>
          <div className="flex items-center">
            <span className="px-3 py-2 bg-gray-50 border border-r-0 border-gray-200 rounded-l-lg text-gray-400 text-sm">
              tuilow.com/canal/
            </span>
            <input
              value={handle}
              onChange={e => setHandle(e.target.value)}
              placeholder="seunome"
              className="flex-1 px-3 py-2 border border-gray-200 rounded-r-lg text-sm
                         focus:outline-none focus:ring-2 focus:ring-brand-300"
            />
          </div>
          <p className="text-xs text-gray-400 mt-1">3-30 caracteres: letras, números ou _.</p>
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1.5">Foto de perfil (URL)</label>
          <input
            value={avatarUrl}
            onChange={e => setAvatarUrl(e.target.value)}
            placeholder="https://..."
            className="w-full px-3 py-2 border border-gray-200 rounded-lg text-sm
                       focus:outline-none focus:ring-2 focus:ring-brand-300"
          />
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1.5">Bio</label>
          <textarea
            value={bio}
            onChange={e => setBio(e.target.value)}
            rows={3}
            placeholder="Conte em poucas palavras quem você é e o que ensina."
            className="w-full px-3 py-2 border border-gray-200 rounded-lg text-sm resize-none
                       focus:outline-none focus:ring-2 focus:ring-brand-300"
          />
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1.5">Redes sociais</label>
          <div className="space-y-2">
            {links.map((link, i) => (
              <div key={i} className="flex items-center gap-2">
                <select
                  value={link.platform}
                  onChange={e => updateLink(i, 'platform', e.target.value)}
                  className="px-2 py-2 border border-gray-200 rounded-lg text-sm w-32 flex-shrink-0
                             focus:outline-none focus:ring-2 focus:ring-brand-300"
                >
                  {platforms.map(p => <option key={p} value={p}>{p}</option>)}
                </select>
                <input
                  value={link.url}
                  onChange={e => updateLink(i, 'url', e.target.value)}
                  placeholder="https://..."
                  className="flex-1 px-3 py-2 border border-gray-200 rounded-lg text-sm
                             focus:outline-none focus:ring-2 focus:ring-brand-300"
                />
                <button onClick={() => removeLink(i)}
                  className="p-2 text-gray-400 hover:text-red-500 transition-colors flex-shrink-0">
                  <Trash2 className="w-4 h-4" />
                </button>
              </div>
            ))}
          </div>
          <button onClick={addLink}
            className="flex items-center gap-1.5 text-sm text-brand-600 hover:underline mt-2">
            <Plus className="w-4 h-4" /> Adicionar rede social
          </button>
        </div>

        <button
          onClick={() => saveMutation.mutate()}
          disabled={!handle || saveMutation.isPending}
          className="btn-primary w-full py-2.5 disabled:opacity-50"
        >
          {saveMutation.isPending ? 'Salvando...' : 'Salvar canal'}
        </button>
      </div>
    </div>
  );
}
