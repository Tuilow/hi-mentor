'use client';

import { useQuery } from '@tanstack/react-query';
import Link from 'next/link';
import { adminUsersApi } from '@/lib/api';

interface PlatformStats {
  totalUsers: number;
  activeUsers: number;
  suspendedUsers: number;
  totalCreators: number;
  activeLast24h: number;
  activeLast7d: number;
  totalPublishedCourses: number;
  totalVideos: number;
}

// Painel do dono da plataforma — visão geral. Distinto de /admin/produtos e /admin/cursos
// (área do Criador, apesar do nome "admin"). Só visível a quem tem o role Admin — ver
// isPlatformAdmin em (dashboard)/layout.tsx.
export default function PlataformaPage() {
  const { data: stats, isLoading } = useQuery<PlatformStats>({
    queryKey: ['platform-stats'],
    queryFn: () => adminUsersApi.getStats().then(r => r.data),
  });

  const cards = [
    { label: 'Usuários',    value: stats?.totalUsers,            sub: 'cadastrados' },
    { label: 'Ativos',      value: stats?.activeUsers,           sub: 'contas ativas' },
    { label: 'Suspensos',   value: stats?.suspendedUsers,        sub: 'contas suspensas' },
    { label: 'Criadores',   value: stats?.totalCreators,         sub: 'de conteúdo' },
    { label: 'Últimas 24h', value: stats?.activeLast24h,         sub: 'usuários ativos' },
    { label: 'Últimos 7d',  value: stats?.activeLast7d,          sub: 'usuários ativos' },
    { label: 'Cursos',      value: stats?.totalPublishedCourses, sub: 'publicados' },
    { label: 'Vídeos',      value: stats?.totalVideos,           sub: 'no total' },
  ];

  return (
    <div className="max-w-4xl mx-auto">
      {/* Header */}
      <div className="mb-8 flex items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-gray-800">Painel da Plataforma 👑</h1>
          <p className="text-gray-500 mt-1">Visão geral de usuários, criadores e conteúdo do HiMentor.</p>
        </div>
        <Link href="/plataforma/usuarios" className="btn-primary whitespace-nowrap flex-shrink-0">
          Gerenciar usuários →
        </Link>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        {cards.map(c => (
          <div key={c.label} className="card border-gray-200 text-center">
            <p className="text-2xl font-bold gradient-text">
              {isLoading || c.value === undefined ? '—' : c.value}
            </p>
            <p className="text-xs font-semibold text-gray-700 mt-1">{c.label}</p>
            <p className="text-[11px] text-gray-400">{c.sub}</p>
          </div>
        ))}
      </div>
    </div>
  );
}
