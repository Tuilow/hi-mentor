'use client';

import { useState } from 'react';
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
  const [openMenuId, setOpenMenuId] = useState<string | null>(null);

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
                      onClick={() => setOpenMenuId(openMenuId === product.id ? null : product.id)}
                      className="p-1.5 rounded-lg hover:bg-gray-100 text-gray-400"
                    >
                      <MoreVertical className="w-4 h-4" />
                    </button>
                    {openMenuId === product.id && (
                      <div
                        className="absolute right-0 top-8 z-10 w-48 bg-white border border-gray-200
                                   rounded-xl shadow-lg py-1"
                        onMouseLeave={() => setOpenMenuId(null)}
                      >
                        <Link
                          href={`/admin/produtos/novo?courseId=${product.id}`}
                          className="flex items-center gap-2 px-4 py-2 text-sm text-gray-700 hover:bg-gray-50"
                        >
                          <Pencil className="w-3.5 h-3.5" /> Editar
                        </Link>
                        {product.status === 'Published' && (
                          <Link
                            href={`/admin/produtos/${product.id}/dashboard`}
                            className="flex items-center gap-2 px-4 py-2 text-sm text-gray-700 hover:bg-gray-50"
                          >
                            <BarChart3 className="w-3.5 h-3.5" /> Dashboard
                          </Link>
                        )}
                        <button
                          onClick={() => { duplicateMutation.mutate(product.id); setOpenMenuId(null); }}
                          className="w-full flex items-center gap-2 px-4 py-2 text-sm text-gray-700 hover:bg-gray-50"
                        >
                          <Copy className="w-3.5 h-3.5" /> Duplicar
                        </button>
                        {product.status !== 'Archived' && (
                          <button
                            onClick={() => { archiveMutation.mutate(product.id); setOpenMenuId(null); }}
                            className="w-full flex items-center gap-2 px-4 py-2 text-sm text-gray-700 hover:bg-gray-50"
                          >
                            <Archive className="w-3.5 h-3.5" /> Arquivar
                          </button>
                        )}
                        <button
                          onClick={() => { handleDelete(product); setOpenMenuId(null); }}
                          className="w-full flex items-center gap-2 px-4 py-2 text-sm text-red-500 hover:bg-red-50"
                        >
                          <Trash2 className="w-3.5 h-3.5" /> Excluir
                        </button>
                      </div>
                    )}
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
    </div>
  );
}
