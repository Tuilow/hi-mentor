'use client';

import { useEffect, useRef, useState } from 'react';
import { createPortal } from 'react-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import toast from 'react-hot-toast';
import {
  Plus, MoreHorizontal, Pencil, Copy, Archive, Trash2, Rocket, BarChart3, Megaphone, Users, Calendar,
} from 'lucide-react';
import { creatorStudioApi, coursesApi } from '@/lib/api';
import { ProductListItem, ProductStatus } from '@/types';
import { Card, Badge, EmptyState } from '@/components/ui/design-system';

const statusLabel: Record<ProductStatus, string> = {
  Draft: 'Rascunho',
  InReview: 'Em análise',
  Published: 'Publicado',
  Archived: 'Arquivado',
};

const statusVariant: Record<ProductStatus, 'draft' | 'warning' | 'active' | 'neutral'> = {
  Draft: 'draft',
  InReview: 'warning',
  Published: 'active',
  Archived: 'neutral',
};

// Gradientes de capa (a Hi Mentor não tem foto de capa para produtos hoje — ProductListItem
// não traz thumbnailUrl — então em vez de inventar uma imagem, cada card recebe um dos
// gradientes abaixo, alternados por posição, igual ao tratamento que o próprio protótipo
// Figma Make já dá a programas "rascunho" sem capa, ver hi-mentor/src/pages/MentorProgramas.tsx).
const coverGradients = [
  'linear-gradient(135deg, oklch(45% 0.2 293) 0%, oklch(35% 0.22 280) 100%)',
  'linear-gradient(135deg, oklch(45% 0.2 160) 0%, oklch(35% 0.18 180) 100%)',
  'linear-gradient(135deg, oklch(55% 0.16 75) 0%, oklch(45% 0.18 40) 100%)',
  'linear-gradient(135deg, oklch(40% 0.18 250) 0%, oklch(50% 0.22 293) 100%)',
];

const currency = (v: number) =>
  v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });

export default function MeusProdutosPage() {
  const router = useRouter();
  const queryClient = useQueryClient();
  const [openMenu, setOpenMenu] = useState<{ id: string; top: number; left: number } | null>(null);
  const menuRef = useRef<HTMLDivElement | null>(null);
  const menuButtonRefs = useRef<Record<string, HTMLButtonElement | null>>({});

  // O menu "⋮" é renderizado num portal (fora do grid) porque cada card já usa overflow-hidden
  // na capa — um dropdown absolute dentro dele ficava cortado. Fecha ao clicar fora ou rolar a
  // página (a posição é calculada uma vez, em pixels da viewport, e não acompanha scroll — mais
  // simples e seguro do que recalcular, já que é raro rolar com o menu aberto).
  useEffect(() => {
    if (!openMenu) return;

    const handleClickOutside = (e: MouseEvent) => {
      const button = menuButtonRefs.current[openMenu.id];
      if (menuRef.current?.contains(e.target as Node)) return;
      if (button?.contains(e.target as Node)) return;
      setOpenMenu(null);
    };
    const handleScroll = () => setOpenMenu(null);

    document.addEventListener('mousedown', handleClickOutside);
    window.addEventListener('scroll', handleScroll, true);
    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
      window.removeEventListener('scroll', handleScroll, true);
    };
  }, [openMenu]);

  const toggleMenu = (productId: string) => {
    if (openMenu?.id === productId) {
      setOpenMenu(null);
      return;
    }
    const button = menuButtonRefs.current[productId];
    if (!button) return;
    const rect = button.getBoundingClientRect();
    // w-48 = 12rem = 192px — alinha a borda direita do menu com a borda direita do botão.
    setOpenMenu({ id: productId, top: rect.bottom + 4, left: rect.right - 192 });
  };

  const { data: products = [], isLoading, error } = useQuery<ProductListItem[]>({
    queryKey: ['my-products'],
    queryFn: () => creatorStudioApi.myProducts().then(r => r.data),
  });

  const archiveMutation = useMutation({
    mutationFn: (id: string) => coursesApi.archive(id),
    onSuccess: () => {
      toast.success('Programa arquivado.');
      queryClient.invalidateQueries({ queryKey: ['my-products'] });
    },
    onError: () => toast.error('Não foi possível arquivar o programa.'),
  });

  const duplicateMutation = useMutation({
    mutationFn: (id: string) => coursesApi.duplicate(id),
    onSuccess: () => {
      toast.success('Programa duplicado como novo rascunho.');
      queryClient.invalidateQueries({ queryKey: ['my-products'] });
    },
    onError: () => toast.error('Não foi possível duplicar o programa.'),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => coursesApi.deleteCourse(id),
    onSuccess: () => {
      toast.success('Programa excluído.');
      queryClient.invalidateQueries({ queryKey: ['my-products'] });
    },
    onError: (err: unknown) => {
      const anyErr = err as { response?: { data?: { message?: string } } };
      toast.error(anyErr.response?.data?.message ?? 'Não foi possível excluir o programa.');
    },
  });

  const handleDelete = (product: ProductListItem) => {
    if (product.status !== 'Draft' && product.status !== 'InReview') {
      toast.error('Só é possível excluir programas em rascunho. Publicados devem ser arquivados.');
      return;
    }
    if (confirm(`Excluir o rascunho "${product.name}"? Essa ação não pode ser desfeita.`)) {
      deleteMutation.mutate(product.id);
    }
  };

  return (
    <div className="max-w-6xl mx-auto">
      <div className="flex items-center justify-between mb-8 flex-wrap gap-4">
        <div>
          <h1 className="text-3xl font-bold text-ink mb-1" style={{ letterSpacing: '-0.03em' }}>Meus Programas</h1>
          <p className="text-ink-3 text-sm">
            Crie e gerencie seus programas de mentoria em um só lugar.
          </p>
        </div>
        <button
          onClick={() => router.push('/admin/produtos/novo')}
          className="btn-primary flex items-center gap-2"
        >
          <Plus className="w-4 h-4" /> Criar Programa
        </button>
      </div>

      {isLoading && <p className="text-ink-3 text-sm">Carregando programas...</p>}
      {!!error && <p className="text-red-500 text-sm">Erro ao carregar seus programas.</p>}

      {!isLoading && products.length === 0 && (
        <EmptyState
          icon={<Rocket size={28} />}
          title="Você ainda não tem nenhum programa"
          description="Publique seu primeiro programa de mentoria em menos de 15 minutos com o assistente guiado."
          action={{ label: 'Criar Programa', onClick: () => router.push('/admin/produtos/novo') }}
        />
      )}

      {products.length > 0 && (
        <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-5">
          {products.map((product, idx) => (
            <Card key={product.id} padding="none" className="overflow-hidden flex flex-col">
              {/* Capa (gradiente — ver comentário em coverGradients acima) */}
              <div className="h-28 relative overflow-hidden" style={{ background: coverGradients[idx % coverGradients.length] }}>
                <div className="absolute inset-0 flex items-end p-4">
                  <div className="flex items-center gap-1.5 bg-black/25 backdrop-blur-sm rounded-full px-3 py-1">
                    <Calendar size={12} className="text-white/80" />
                    <span className="text-xs font-semibold text-white">
                      {new Date(product.createdAt).toLocaleDateString('pt-BR')}
                    </span>
                  </div>
                </div>
                <button
                  ref={el => { menuButtonRefs.current[product.id] = el; }}
                  onClick={() => toggleMenu(product.id)}
                  className="absolute top-3 right-3 w-8 h-8 bg-black/20 hover:bg-black/40 backdrop-blur-sm rounded-lg flex items-center justify-center transition-all cursor-pointer"
                >
                  <MoreHorizontal size={16} className="text-white" />
                </button>
              </div>

              {/* Conteúdo */}
              <div className="p-5 flex flex-col flex-1">
                <div className="flex items-start justify-between gap-2 mb-3">
                  <h3 className="font-bold text-ink text-base leading-snug">{product.name}</h3>
                  <Badge variant={statusVariant[product.status]} dot>{statusLabel[product.status]}</Badge>
                </div>

                <div className="flex items-center gap-3 mb-4 text-xs text-ink-3 flex-wrap">
                  {product.category && <span>{product.category}</span>}
                  <span>{product.productType}</span>
                </div>

                <div className="flex items-center gap-4 mb-4 text-xs text-ink-3">
                  <div className="flex items-center gap-1.5">
                    <Users size={12} />
                    <span>{product.totalSales} vendas</span>
                  </div>
                  <div className="flex items-center gap-1.5 font-semibold text-ink-2">
                    {currency(product.revenueGenerated)}
                  </div>
                </div>

                <div className="mt-auto pt-3 border-t border-line-light flex items-center justify-between gap-2">
                  {/* Atalho direto pra aba "Divulgar" do dashboard do produto (Kit de
                      Divulgação + Central de Divulgação por IA) — antes só dava pra chegar lá
                      clicando em "Dashboard" no menu "⋮" e depois trocando de aba manualmente.
                      Só existe pra produtos Publicados: rascunho/arquivado não tem link público
                      de vendas ativo pra divulgar (mesma regra do item "Dashboard" no menu "⋮"). */}
                  {product.status === 'Published' ? (
                    <Link
                      href={`/admin/produtos/${product.id}/dashboard?tab=divulgar`}
                      className="inline-flex items-center gap-1.5 text-xs font-semibold text-violet-600
                                 border border-violet-200 hover:bg-violet-50 hover:border-violet-300
                                 rounded-lg px-3 py-1.5 transition-colors whitespace-nowrap"
                    >
                      <Megaphone className="w-3.5 h-3.5" /> Divulgar
                    </Link>
                  ) : <span />}
                </div>
              </div>
            </Card>
          ))}
        </div>
      )}

      <p className="text-xs text-ink-4 mt-6 flex items-center gap-1.5">
        <Rocket className="w-3.5 h-3.5" /> Publique programas ilimitados, sem mensalidade — a Hi Mentor só cobra uma comissão sobre cada venda.
      </p>

      {openMenu && typeof document !== 'undefined' && createPortal(
        (() => {
          const product = products.find(p => p.id === openMenu.id);
          if (!product) return null;
          return (
            <div
              ref={menuRef}
              style={{ position: 'fixed', top: openMenu.top, left: openMenu.left }}
              className="z-50 w-48 bg-surface border border-line rounded-xl shadow-lg py-1"
            >
              <Link
                href={`/admin/produtos/novo?courseId=${product.id}`}
                onClick={() => setOpenMenu(null)}
                className="flex items-center gap-2 px-4 py-2 text-sm text-ink hover:bg-subtle"
              >
                <Pencil className="w-3.5 h-3.5" /> Editar
              </Link>
              {product.status === 'Published' && (
                <Link
                  href={`/admin/produtos/${product.id}/dashboard`}
                  onClick={() => setOpenMenu(null)}
                  className="flex items-center gap-2 px-4 py-2 text-sm text-ink hover:bg-subtle"
                >
                  <BarChart3 className="w-3.5 h-3.5" /> Dashboard
                </Link>
              )}
              <button
                onClick={() => { duplicateMutation.mutate(product.id); setOpenMenu(null); }}
                className="w-full flex items-center gap-2 px-4 py-2 text-sm text-ink hover:bg-subtle"
              >
                <Copy className="w-3.5 h-3.5" /> Duplicar
              </button>
              {product.status !== 'Archived' && (
                <button
                  onClick={() => { archiveMutation.mutate(product.id); setOpenMenu(null); }}
                  className="w-full flex items-center gap-2 px-4 py-2 text-sm text-ink hover:bg-subtle"
                >
                  <Archive className="w-3.5 h-3.5" /> Arquivar
                </button>
              )}
              <button
                onClick={() => { handleDelete(product); setOpenMenu(null); }}
                className="w-full flex items-center gap-2 px-4 py-2 text-sm text-red-500 hover:bg-red-50"
              >
                <Trash2 className="w-3.5 h-3.5" /> Excluir
              </button>
            </div>
          );
        })(),
        document.body
      )}
    </div>
  );
}
