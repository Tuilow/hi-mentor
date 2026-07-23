'use client';

import { useEffect, useRef, useState } from 'react';
import { createPortal } from 'react-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import toast from 'react-hot-toast';
import {
  Plus, MoreVertical, Pencil, Copy, Archive, Trash2, Rocket, BarChart3,
} from 'lucide-react';
import { creatorStudioApi, coursesApi } from '@/lib/api';
import { ProductListItem, ProductStatus } from '@/types';

const statusLabel: Record<ProductStatus, string> = {
  Draft: 'Rascunho',
  InReview: 'Em análise',
  Published: 'Publicado',
  Archived: 'Arquivado',
};

const statusColor: Record<ProductStatus, string> = {
  Draft: 'badge-yellow',
  InReview: 'badge-purple',
  Published: 'badge-green',
  Archived: 'badge',
};

const currency = (v: number) =>
  v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });

export default function MeusProdutosPage() {
  const router = useRouter();
  const queryClient = useQueryClient();
  const [openMenu, setOpenMenu] = useState<{ id: string; top: number; left: number } | null>(null);
  const menuRef = useRef<HTMLDivElement | null>(null);
  const menuButtonRefs = useRef<Record<string, HTMLButtonElement | null>>({});

  // O menu "⋮" é renderizado num portal (fora da tabela) porque a tabela tem overflow-x-auto
  // (necessário pro scroll horizontal em telas estreitas) — um dropdown absolute dentro dela
  // ficava cortado/rolando junto com a tabela em vez de flutuar por cima. Fecha ao clicar fora
  // ou rolar a página (a posição é calculada uma vez, em pixels da viewport, e não acompanha
  // scroll — mais simples e seguro do que recalcular, já que é raro rolar com o menu aberto).
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
      toast.success('Produto arquivado.');
      queryClient.invalidateQueries({ queryKey: ['my-products'] });
    },
    onError: () => toast.error('Não foi possível arquivar o produto.'),
  });

  const duplicateMutation = useMutation({
    mutationFn: (id: string) => coursesApi.duplicate(id),
    onSuccess: () => {
      toast.success('Produto duplicado como novo rascunho.');
      queryClient.invalidateQueries({ queryKey: ['my-products'] });
    },
    onError: () => toast.error('Não foi possível duplicar o produto.'),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => coursesApi.deleteCourse(id),
    onSuccess: () => {
      toast.success('Produto excluído.');
      queryClient.invalidateQueries({ queryKey: ['my-products'] });
    },
    onError: (err: unknown) => {
      const anyErr = err as { response?: { data?: { message?: string } } };
      toast.error(anyErr.response?.data?.message ?? 'Não foi possível excluir o produto.');
    },
  });

  const handleDelete = (product: ProductListItem) => {
    if (product.status !== 'Draft' && product.status !== 'InReview') {
      toast.error('Só é possível excluir produtos em rascunho. Publicados devem ser arquivados.');
      return;
    }
    if (confirm(`Excluir o rascunho "${product.name}"? Essa ação não pode ser desfeita.`)) {
      deleteMutation.mutate(product.id);
    }
  };

  return (
    <div className="max-w-6xl mx-auto">
      <div className="flex items-center justify-between mb-8">
        <div>
          <h1 className="text-2xl font-bold text-gray-800">Meus Produtos</h1>
          <p className="text-gray-500 text-sm mt-1">
            Crie e gerencie seus cursos e produtos digitais em um só lugar.
          </p>
        </div>
        <button
          onClick={() => router.push('/admin/produtos/novo')}
          className="btn-primary flex items-center gap-2"
        >
          <Plus className="w-4 h-4" /> Criar Produto
        </button>
      </div>

      {isLoading && <p className="text-gray-400 text-sm">Carregando produtos...</p>}
      {!!error && <p className="text-red-500 text-sm">Erro ao carregar seus produtos.</p>}

      {!isLoading && products.length === 0 && (
        <div className="card text-center py-16">
          <p className="text-4xl mb-3">🚀</p>
          <p className="font-semibold text-gray-800 mb-1">Você ainda não tem nenhum produto</p>
          <p className="text-gray-500 text-sm mb-6">
            Publique seu primeiro curso em menos de 15 minutos com o assistente guiado.
          </p>
          <button onClick={() => router.push('/admin/produtos/novo')} className="btn-primary">
            + Criar Produto
          </button>
        </div>
      )}

      {products.length > 0 && (
        <div className="card overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="text-left text-gray-400 text-xs uppercase tracking-wider border-b border-gray-100">
                <th className="pb-3 pr-4 font-medium">Nome</th>
                <th className="pb-3 pr-4 font-medium">Categoria</th>
                <th className="pb-3 pr-4 font-medium">Tipo</th>
                <th className="pb-3 pr-4 font-medium">Status</th>
                <th className="pb-3 pr-4 font-medium">Criado em</th>
                <th className="pb-3 pr-4 font-medium text-right">Vendas</th>
                <th className="pb-3 pr-4 font-medium text-right">Receita</th>
                <th className="pb-3 font-medium"></th>
              </tr>
            </thead>
            <tbody>
              {products.map(product => (
                <tr key={product.id} className="border-b border-gray-50 last:border-0 hover:bg-gray-50/60">
                  <td className="py-3 pr-4">
                    <p className="font-medium text-gray-800">{product.name}</p>
                  </td>
                  <td className="py-3 pr-4 text-gray-500">{product.category ?? '—'}</td>
                  <td className="py-3 pr-4 text-gray-500">{product.productType}</td>
                  <td className="py-3 pr-4">
                    <span className={`badge ${statusColor[product.status]}`}>
                      {statusLabel[product.status]}
                    </span>
                  </td>
                  <td className="py-3 pr-4 text-gray-500">
                    {new Date(product.createdAt).toLocaleDateString('pt-BR')}
                  </td>
                  <td className="py-3 pr-4 text-right text-gray-700">{product.totalSales}</td>
                  <td className="py-3 pr-4 text-right text-gray-700">{currency(product.revenueGenerated)}</td>
                  <td className="py-3 relative">
                    <button
                      ref={el => { menuButtonRefs.current[product.id] = el; }}
                      onClick={() => toggleMenu(product.id)}
                      className="p-1.5 rounded-lg hover:bg-gray-100 text-gray-400"
                    >
                      <MoreVertical className="w-4 h-4" />
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <p className="text-xs text-gray-400 mt-6 flex items-center gap-1.5">
        <Rocket className="w-3.5 h-3.5" /> Publique produtos ilimitados, sem mensalidade — a Tuilow só cobra uma comissão sobre cada venda.
      </p>

      {openMenu && typeof document !== 'undefined' && createPortal(
        (() => {
          const product = products.find(p => p.id === openMenu.id);
          if (!product) return null;
          return (
            <div
              ref={menuRef}
              style={{ position: 'fixed', top: openMenu.top, left: openMenu.left }}
              className="z-50 w-48 bg-white border border-gray-200 rounded-xl shadow-lg py-1"
            >
              <Link
                href={`/admin/produtos/novo?courseId=${product.id}`}
                onClick={() => setOpenMenu(null)}
                className="flex items-center gap-2 px-4 py-2 text-sm text-gray-700 hover:bg-gray-50"
              >
                <Pencil className="w-3.5 h-3.5" /> Editar
              </Link>
              {product.status === 'Published' && (
                <Link
                  href={`/admin/produtos/${product.id}/dashboard`}
                  onClick={() => setOpenMenu(null)}
                  className="flex items-center gap-2 px-4 py-2 text-sm text-gray-700 hover:bg-gray-50"
                >
                  <BarChart3 className="w-3.5 h-3.5" /> Dashboard
                </Link>
              )}
              <button
                onClick={() => { duplicateMutation.mutate(product.id); setOpenMenu(null); }}
                className="w-full flex items-center gap-2 px-4 py-2 text-sm text-gray-700 hover:bg-gray-50"
              >
                <Copy className="w-3.5 h-3.5" /> Duplicar
              </button>
              {product.status !== 'Archived' && (
                <button
                  onClick={() => { archiveMutation.mutate(product.id); setOpenMenu(null); }}
                  className="w-full flex items-center gap-2 px-4 py-2 text-sm text-gray-700 hover:bg-gray-50"
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
