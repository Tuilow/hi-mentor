'use client';

import { useEffect, useRef, useState } from 'react';
import { createPortal } from 'react-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { MoreVertical, CheckCircle2, Ban, Trash2 } from 'lucide-react';
import { adminUsersApi } from '@/lib/api';

type UserStatus = 'PendingConfirmation' | 'Active' | 'Suspended' | 'Deleted';

interface UserSummary {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  status: UserStatus;
  roles: string[];
  createdAt: string;
  lastLoginAt: string | null;
}

interface PagedUsers {
  items: UserSummary[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

const statusLabel: Record<UserStatus, string> = {
  PendingConfirmation: 'Aguardando confirmação',
  Active: 'Ativo',
  Suspended: 'Suspenso',
  Deleted: 'Excluído',
};

// Sem badge vermelho pronto no design system (globals.css só tem purple/green/yellow) — compõe
// direto com utilitários Tailwind sobre a classe .badge, mesmo padrão das outras variantes.
const statusColor: Record<UserStatus, string> = {
  PendingConfirmation: 'badge-yellow',
  Active: 'badge-green',
  Suspended: 'badge bg-red-50 text-red-700 border border-red-200',
  Deleted: 'badge bg-gray-100 text-gray-500 border border-gray-200',
};

const roleOptions = ['Student', 'Creator', 'Admin', 'ChannelMember'];

const PAGE_SIZE = 20;

export default function PlataformaUsuariosPage() {
  const queryClient = useQueryClient();
  const [search, setSearch] = useState('');
  const [searchInput, setSearchInput] = useState('');
  const [roleFilter, setRoleFilter] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const [page, setPage] = useState(1);

  const [openMenu, setOpenMenu] = useState<{ id: string; top: number; left: number } | null>(null);
  const menuRef = useRef<HTMLDivElement | null>(null);
  const menuButtonRefs = useRef<Record<string, HTMLButtonElement | null>>({});

  // Mesmo padrão de admin/produtos/page.tsx: o menu "⋮" vai por portal porque a tabela tem
  // overflow-x-auto (scroll horizontal em telas estreitas), o que cortava/rolava um dropdown
  // absolute posicionado dentro dela.
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

  const toggleMenu = (userId: string) => {
    if (openMenu?.id === userId) {
      setOpenMenu(null);
      return;
    }
    const button = menuButtonRefs.current[userId];
    if (!button) return;
    const rect = button.getBoundingClientRect();
    setOpenMenu({ id: userId, top: rect.bottom + 4, left: rect.right - 192 });
  };

  const { data, isLoading, error } = useQuery<PagedUsers>({
    queryKey: ['platform-users', search, roleFilter, statusFilter, page],
    queryFn: () => adminUsersApi.list({
      search: search || undefined,
      role: roleFilter || undefined,
      status: statusFilter || undefined,
      page,
      pageSize: PAGE_SIZE,
    }).then(r => r.data),
  });

  const users = data?.items ?? [];

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['platform-users'] });
    queryClient.invalidateQueries({ queryKey: ['platform-stats'] });
  };

  const suspendMutation = useMutation({
    mutationFn: (id: string) => adminUsersApi.suspend(id),
    onSuccess: () => { toast.success('Usuário suspenso.'); invalidate(); },
    onError: () => toast.error('Não foi possível suspender o usuário.'),
  });

  const reactivateMutation = useMutation({
    mutationFn: (id: string) => adminUsersApi.reactivate(id),
    onSuccess: () => { toast.success('Usuário reativado.'); invalidate(); },
    onError: () => toast.error('Não foi possível reativar o usuário.'),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => adminUsersApi.delete(id),
    onSuccess: () => { toast.success('Usuário excluído.'); invalidate(); },
    onError: (err: unknown) => {
      const anyErr = err as { response?: { data?: { message?: string } } };
      toast.error(anyErr.response?.data?.message ?? 'Não foi possível excluir o usuário.');
    },
  });

  const handleDelete = (user: UserSummary) => {
    if (confirm(
      `Excluir a conta de "${user.firstName} ${user.lastName}" (${user.email})?\n\n` +
      'Isso vai apagar PERMANENTEMENTE todos os vídeos dessa pessoa e arquivar todos os cursos ' +
      'dela (o registro do curso continua existindo para quem já comprou, mas sem os vídeos). ' +
      'Essa ação não pode ser desfeita.'
    )) {
      deleteMutation.mutate(user.id);
    }
  };

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setPage(1);
    setSearch(searchInput.trim());
  };

  return (
    <div className="max-w-6xl mx-auto">
      <div className="mb-8">
        <h1 className="text-2xl font-bold text-gray-800">Usuários da Plataforma</h1>
        <p className="text-gray-500 text-sm mt-1">
          Veja quem se cadastrou, quem está criando conteúdo, e ative, suspenda ou exclua contas.
        </p>
      </div>

      {/* Filtros */}
      <form onSubmit={handleSearchSubmit} className="flex flex-wrap items-center gap-3 mb-6">
        <input
          value={searchInput}
          onChange={e => setSearchInput(e.target.value)}
          placeholder="Buscar por nome ou e-mail..."
          className="input-field flex-1 min-w-[220px]"
        />
        <select
          value={roleFilter}
          onChange={e => { setPage(1); setRoleFilter(e.target.value); }}
          className="input-field sm:max-w-[180px]"
        >
          <option value="">Todos os roles</option>
          {roleOptions.map(r => <option key={r} value={r}>{r}</option>)}
        </select>
        <select
          value={statusFilter}
          onChange={e => { setPage(1); setStatusFilter(e.target.value); }}
          className="input-field sm:max-w-[200px]"
        >
          <option value="">Todos os status</option>
          {Object.entries(statusLabel).map(([value, label]) => (
            <option key={value} value={value}>{label}</option>
          ))}
        </select>
        <button type="submit" className="btn-primary whitespace-nowrap">Buscar</button>
      </form>

      {isLoading && <p className="text-gray-400 text-sm">Carregando usuários...</p>}
      {!!error && <p className="text-red-500 text-sm">Erro ao carregar usuários.</p>}

      {!isLoading && users.length === 0 && (
        <div className="card text-center py-16">
          <p className="text-4xl mb-3">🔍</p>
          <p className="font-semibold text-gray-800 mb-1">Nenhum usuário encontrado</p>
          <p className="text-gray-500 text-sm">Tente ajustar a busca ou os filtros.</p>
        </div>
      )}

      {users.length > 0 && (
        <div className="card overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="text-left text-gray-400 text-xs uppercase tracking-wider border-b border-gray-100">
                <th className="pb-3 pr-4 font-medium">Nome</th>
                <th className="pb-3 pr-4 font-medium">E-mail</th>
                <th className="pb-3 pr-4 font-medium">Roles</th>
                <th className="pb-3 pr-4 font-medium">Status</th>
                <th className="pb-3 pr-4 font-medium">Cadastro</th>
                <th className="pb-3 pr-4 font-medium">Último login</th>
                <th className="pb-3 font-medium"></th>
              </tr>
            </thead>
            <tbody>
              {users.map(user => (
                <tr key={user.id} className="border-b border-gray-50 last:border-0 hover:bg-gray-50/60">
                  <td className="py-3 pr-4">
                    <p className="font-medium text-gray-800">{user.firstName} {user.lastName}</p>
                  </td>
                  <td className="py-3 pr-4 text-gray-500">{user.email}</td>
                  <td className="py-3 pr-4 text-gray-500">{user.roles.join(', ') || '—'}</td>
                  <td className="py-3 pr-4">
                    <span className={statusColor[user.status]}>{statusLabel[user.status]}</span>
                  </td>
                  <td className="py-3 pr-4 text-gray-500">
                    {new Date(user.createdAt).toLocaleDateString('pt-BR')}
                  </td>
                  <td className="py-3 pr-4 text-gray-500">
                    {user.lastLoginAt ? new Date(user.lastLoginAt).toLocaleDateString('pt-BR') : '—'}
                  </td>
                  <td className="py-3 relative">
                    <button
                      ref={el => { menuButtonRefs.current[user.id] = el; }}
                      onClick={() => toggleMenu(user.id)}
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

      {/* Paginação */}
      {data && data.totalPages > 1 && (
        <div className="flex items-center justify-between mt-6 text-sm text-gray-500">
          <p>Página {data.page} de {data.totalPages} — {data.total} usuários no total</p>
          <div className="flex gap-2">
            <button
              disabled={!data.hasPreviousPage}
              onClick={() => setPage(p => Math.max(1, p - 1))}
              className="px-3 py-1.5 rounded-lg border border-gray-200 disabled:opacity-40"
            >
              Anterior
            </button>
            <button
              disabled={!data.hasNextPage}
              onClick={() => setPage(p => p + 1)}
              className="px-3 py-1.5 rounded-lg border border-gray-200 disabled:opacity-40"
            >
              Próxima
            </button>
          </div>
        </div>
      )}

      {openMenu && typeof document !== 'undefined' && createPortal(
        (() => {
          const user = users.find(u => u.id === openMenu.id);
          if (!user) return null;
          return (
            <div
              ref={menuRef}
              style={{ position: 'fixed', top: openMenu.top, left: openMenu.left }}
              className="z-50 w-48 bg-white border border-gray-200 rounded-xl shadow-lg py-1"
            >
              {user.status !== 'Active' && (
                <button
                  onClick={() => { reactivateMutation.mutate(user.id); setOpenMenu(null); }}
                  className="w-full flex items-center gap-2 px-4 py-2 text-sm text-gray-700 hover:bg-gray-50"
                >
                  <CheckCircle2 className="w-3.5 h-3.5" /> Ativar
                </button>
              )}
              {user.status === 'Active' && (
                <button
                  onClick={() => { suspendMutation.mutate(user.id); setOpenMenu(null); }}
                  className="w-full flex items-center gap-2 px-4 py-2 text-sm text-gray-700 hover:bg-gray-50"
                >
                  <Ban className="w-3.5 h-3.5" /> Suspender
                </button>
              )}
              <button
                onClick={() => { handleDelete(user); setOpenMenu(null); }}
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
