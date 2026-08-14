'use client';

import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { Copy, Ticket } from 'lucide-react';
import { adminAccessCodesApi, coursesApi } from '@/lib/api';
import { AccessCodeAdminItem, Course } from '@/types';

// Mesmo padrão visual de (dashboard)/plataforma/usuarios/page.tsx (classes utilitárias globais
// .card/.input-field/.btn-primary/.badge-*, não os componentes Card/Button do design system
// "Hi Mentor") — /plataforma/* ficou fora do escopo do redesign de ago/2026, então esta página
// nova segue o padrão das irmãs já existentes aqui, não o do dashboard do aluno.

async function copyToClipboard(text: string, label: string) {
  try {
    await navigator.clipboard.writeText(text);
    toast.success(`${label} copiado!`);
  } catch {
    toast.error('Não foi possível copiar. Selecione o texto manualmente.');
  }
}

function statusOf(code: AccessCodeAdminItem): { label: string; className: string } {
  if (!code.isActive) return { label: 'Inativo', className: 'badge bg-gray-100 text-gray-500 border border-gray-200' };
  if (code.expiresAt && new Date(code.expiresAt) <= new Date())
    return { label: 'Expirado', className: 'badge bg-red-50 text-red-700 border border-red-200' };
  if (code.maxUses != null && code.usesCount >= code.maxUses)
    return { label: 'Esgotado', className: 'badge-yellow' };
  return { label: 'Ativo', className: 'badge-green' };
}

export default function CodigosAcessoPage() {
  const queryClient = useQueryClient();
  const [courseSearch, setCourseSearch] = useState('');
  const [selectedCourseId, setSelectedCourseId] = useState('');
  const [maxUses, setMaxUses] = useState('');
  const [expiresAt, setExpiresAt] = useState('');

  const { data: codes = [], isLoading, error } = useQuery<AccessCodeAdminItem[]>({
    queryKey: ['admin-access-codes'],
    queryFn: () => adminAccessCodesApi.list().then(r => r.data),
  });

  // Busca de programa publicado por nome — não é um seletor de "todos os cursos" (a plataforma
  // não tem essa tela ainda), só o suficiente para achar o programa certo antes de gerar o
  // código. Só dispara com pelo menos 2 caracteres, pra não listar tudo à toa.
  const { data: courseResults } = useQuery<{ items: Course[] }>({
    queryKey: ['admin-access-codes-course-search', courseSearch],
    queryFn: () => coursesApi.list({ search: courseSearch, pageSize: 20 }).then(r => r.data),
    enabled: courseSearch.trim().length >= 2,
  });
  const courseOptions = courseResults?.items ?? [];

  const generateMutation = useMutation({
    mutationFn: () => adminAccessCodesApi.generate({
      courseId: selectedCourseId,
      maxUses: maxUses ? Number(maxUses) : undefined,
      expiresAt: expiresAt ? new Date(`${expiresAt}T23:59:59`).toISOString() : undefined,
    }),
    onSuccess: (res) => {
      queryClient.invalidateQueries({ queryKey: ['admin-access-codes'] });
      copyToClipboard(res.data.code, 'Código de acesso');
      setSelectedCourseId('');
      setCourseSearch('');
      setMaxUses('');
      setExpiresAt('');
    },
    onError: (err: unknown) => {
      const anyErr = err as { response?: { data?: { title?: string; detail?: string; message?: string } } };
      toast.error(
        anyErr.response?.data?.title ?? anyErr.response?.data?.detail ?? anyErr.response?.data?.message
        ?? 'Não foi possível gerar o código.'
      );
    },
  });

  const handleGenerate = (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedCourseId) {
      toast.error('Selecione o programa que o código vai liberar.');
      return;
    }
    generateMutation.mutate();
  };

  return (
    <div className="max-w-5xl mx-auto">
      <div className="mb-8">
        <h1 className="text-2xl font-bold text-gray-800">Códigos de acesso</h1>
        <p className="text-gray-500 text-sm mt-1">
          Gere códigos para liberar acesso manual a um programa — o aluno ativa em &quot;Tenho um código de acesso&quot; no dashboard dele.
        </p>
      </div>

      {/* Gerar código */}
      <form onSubmit={handleGenerate} className="card mb-8">
        <h2 className="text-sm font-semibold text-gray-700 mb-3">Gerar novo código</h2>
        <div className="flex flex-wrap items-end gap-3">
          <div className="flex-1 min-w-[240px]">
            <label className="block text-xs font-medium text-gray-500 mb-1">Programa (publicado)</label>
            <input
              value={selectedCourseId ? courseOptions.find(c => c.id === selectedCourseId)?.title ?? courseSearch : courseSearch}
              onChange={e => { setCourseSearch(e.target.value); setSelectedCourseId(''); }}
              placeholder="Buscar programa por nome..."
              className="input-field w-full"
            />
            {courseSearch.trim().length >= 2 && !selectedCourseId && courseOptions.length > 0 && (
              <div className="mt-1 border border-gray-200 rounded-xl bg-white shadow-sm max-h-48 overflow-y-auto">
                {courseOptions.map(c => (
                  <button
                    key={c.id}
                    type="button"
                    onClick={() => { setSelectedCourseId(c.id); setCourseSearch(c.title); }}
                    className="w-full text-left px-3 py-2 text-sm text-gray-700 hover:bg-gray-50"
                  >
                    {c.title}
                  </button>
                ))}
              </div>
            )}
          </div>
          <div className="w-36">
            <label className="block text-xs font-medium text-gray-500 mb-1">Máx. de usos</label>
            <input
              type="number"
              min={1}
              value={maxUses}
              onChange={e => setMaxUses(e.target.value)}
              placeholder="Ilimitado"
              className="input-field w-full"
            />
          </div>
          <div className="w-44">
            <label className="block text-xs font-medium text-gray-500 mb-1">Expira em</label>
            <input
              type="date"
              value={expiresAt}
              onChange={e => setExpiresAt(e.target.value)}
              className="input-field w-full"
            />
          </div>
          <button
            type="submit"
            disabled={generateMutation.isPending}
            className="btn-primary flex items-center gap-2 whitespace-nowrap disabled:opacity-50"
          >
            <Ticket className="w-4 h-4" />
            {generateMutation.isPending ? 'Gerando...' : 'Gerar código'}
          </button>
        </div>
        <p className="text-xs text-gray-400 mt-2">
          Deixe &quot;Máx. de usos&quot; e &quot;Expira em&quot; em branco para um código ilimitado e sem validade.
        </p>
      </form>

      {isLoading && <p className="text-gray-400 text-sm">Carregando códigos...</p>}
      {!!error && <p className="text-red-500 text-sm">Erro ao carregar os códigos de acesso.</p>}

      {!isLoading && codes.length === 0 && (
        <div className="card text-center py-16">
          <p className="text-4xl mb-3">🎟️</p>
          <p className="font-semibold text-gray-800 mb-1">Nenhum código gerado ainda</p>
          <p className="text-gray-500 text-sm">Gere o primeiro código acima para liberar acesso a um programa.</p>
        </div>
      )}

      {codes.length > 0 && (
        <div className="card overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="text-left text-gray-400 text-xs uppercase tracking-wider border-b border-gray-100">
                <th className="pb-3 pr-4 font-medium">Código</th>
                <th className="pb-3 pr-4 font-medium">Programa</th>
                <th className="pb-3 pr-4 font-medium">Usos</th>
                <th className="pb-3 pr-4 font-medium">Expira em</th>
                <th className="pb-3 pr-4 font-medium">Status</th>
                <th className="pb-3 font-medium">Criado em</th>
              </tr>
            </thead>
            <tbody>
              {codes.map(code => {
                const status = statusOf(code);
                return (
                  <tr key={code.id} className="border-b border-gray-50 last:border-0">
                    <td className="py-3 pr-4">
                      <button
                        onClick={() => copyToClipboard(code.code, 'Código')}
                        className="flex items-center gap-1.5 font-mono font-semibold text-gray-800 hover:text-violet-600"
                      >
                        {code.code} <Copy className="w-3.5 h-3.5 text-gray-400" />
                      </button>
                    </td>
                    <td className="py-3 pr-4 text-gray-600">{code.courseTitle}</td>
                    <td className="py-3 pr-4 text-gray-500">
                      {code.usesCount}{code.maxUses != null ? ` / ${code.maxUses}` : ' (ilimitado)'}
                    </td>
                    <td className="py-3 pr-4 text-gray-500">
                      {code.expiresAt ? new Date(code.expiresAt).toLocaleDateString('pt-BR') : '—'}
                    </td>
                    <td className="py-3 pr-4">
                      <span className={status.className}>{status.label}</span>
                    </td>
                    <td className="py-3 text-gray-500">
                      {new Date(code.createdAt).toLocaleDateString('pt-BR')}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
