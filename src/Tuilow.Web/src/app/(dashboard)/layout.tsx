'use client';

import { useEffect, useState } from 'react';
import { useRouter, usePathname } from 'next/navigation';
import { useQuery } from '@tanstack/react-query';
import Link from 'next/link';
import { authApi } from '@/lib/api';

// "Cursos" e "Assinatura" foram removidos do menu (Sprint Item 5): a plataforma não é uma
// vitrine pública de cursos — o acesso é sempre por link/canal/página de vendas de um Creator
// específico, então não faz sentido oferecer navegação pra "ver todos os cursos" ou "gerenciar
// assinaturas" no menu principal. As páginas em si continuam existindo (/cursos, /assinatura)
// e seguem acessíveis pelos CTAs do dashboard e do histórico — só o item de menu saiu.
const navItems = [
  { href: '/dashboard', label: 'Início',      icon: '🏠' },
  { href: '/historico', label: 'Histórico',   icon: '🕒' },
];

const adminNavItems = [
  { href: '/admin/produtos', label: 'Meus Produtos',  icon: '🚀' },
  { href: '/admin/cursos',   label: 'Gerenciar Cursos', icon: '🎬' },
  { href: '/canal',          label: 'Meu Canal',       icon: '📡' },
  { href: '/estudio',        label: 'Estúdio do Criador', icon: '🎙️' },
];

// Painel do dono da plataforma — distinto da área "Administração" do Criador acima (que apesar
// do nome é a área de gestão de produtos do próprio Criador, não do dono do Tuilow).
const platformNavItems = [
  { href: '/plataforma',          label: 'Visão Geral', icon: '📊' },
  { href: '/plataforma/usuarios', label: 'Usuários',    icon: '👥' },
];

export default function DashboardLayout({ children }: { children: React.ReactNode }) {
  const router   = useRouter();
  const pathname = usePathname();
  const [sidebarOpen, setSidebar] = useState(false);

  // Mesma query key usada em /dashboard — compartilhar o cache evita uma segunda chamada
  // independente a /auth/me a cada navegação (eram duas requisições concorrentes, uma daqui
  // e outra da página) e faz o sidebar reagir na hora a mudanças de role (ex.: virar Creator
  // via queryClient.invalidateQueries), sem precisar de reload de página inteira.
  const { data: user, isError } = useQuery({
    queryKey: ['user-profile'],
    queryFn: () => authApi.me().then(res => res.data),
  });

  useEffect(() => {
    if (isError) router.push('/login');
  }, [isError, router]);

  const logout = () => {
    localStorage.removeItem('access_token');
    localStorage.removeItem('refresh_token');
    router.push('/login');
  };

  const initials = user
    ? `${user.firstName?.[0] ?? ''}${user.lastName?.[0] ?? ''}`.toUpperCase()
    : '?';

  // Separado em duas flags: isCreator controla a área "Administração" (produtos/cursos do
  // próprio Criador, nome legado); isPlatformAdmin controla a nova área "Plataforma" (painel do
  // dono do Tuilow — gestão de todos os usuários). Antes eram conflados em um único isAdmin,
  // o que fazia um Criador comum ver conteúdo que deveria ser exclusivo do dono da plataforma.
  const isCreator = user?.roles?.includes('Creator');
  const isPlatformAdmin = user?.roles?.includes('Admin');

  const NavLink = ({ href, label, icon }: { href: string; label: string; icon: string }) => {
    const active = pathname === href || pathname.startsWith(href + '/');
    return (
      <Link
        href={href}
        onClick={() => setSidebar(false)}
        className={`
          flex items-center gap-3 px-4 py-2.5 rounded-lg text-sm font-medium
          transition-all duration-150
          ${active
            ? 'bg-brand-50 text-brand-700 border border-brand-200'
            : 'text-gray-500 hover:text-gray-800 hover:bg-gray-100'}
        `}
      >
        <span className="text-base">{icon}</span>
        {label}
      </Link>
    );
  };

  return (
    <div className="min-h-screen bg-white flex">

      {/* Mobile overlay */}
      {sidebarOpen && (
        <div className="fixed inset-0 bg-black/60 z-20 md:hidden"
             onClick={() => setSidebar(false)} />
      )}

      {/* Sidebar */}
      <aside className={`
        fixed md:static inset-y-0 left-0 z-30
        w-64 bg-white border-r border-gray-200
        flex flex-col
        transition-transform duration-200
        ${sidebarOpen ? 'translate-x-0' : '-translate-x-full md:translate-x-0'}
      `}>
        {/* Logo */}
        <div className="h-16 px-6 flex items-center border-b border-gray-200">
          <Link href="/dashboard" className="text-lg font-bold gradient-text">
            🎓 Tuilow
          </Link>
        </div>

        {/* Nav */}
        <nav className="flex-1 p-4 space-y-1 overflow-y-auto">
          {navItems.map(item => (
            <NavLink key={item.href} {...item} />
          ))}

          {/* Admin section (Criador) */}
          {isCreator && (
            <>
              <div className="pt-4 pb-1">
                <p className="px-4 text-xs font-semibold text-gray-400 uppercase tracking-wider">
                  Administração
                </p>
              </div>
              {adminNavItems.map(item => (
                <NavLink key={item.href} {...item} />
              ))}
            </>
          )}

          {/* Platform section (dono da plataforma) */}
          {isPlatformAdmin && (
            <>
              <div className="pt-4 pb-1">
                <p className="px-4 text-xs font-semibold text-gray-400 uppercase tracking-wider">
                  Plataforma
                </p>
              </div>
              {platformNavItems.map(item => (
                <NavLink key={item.href} {...item} />
              ))}
            </>
          )}
        </nav>

        {/* User footer */}
        <div className="p-4 border-t border-gray-200">
          <div className="flex items-center gap-3 mb-3">
            <div className="w-8 h-8 rounded-full bg-gradient-to-br from-brand-500 to-accent-500
                            flex items-center justify-center text-white font-bold text-xs flex-shrink-0">
              {initials}
            </div>
            <div className="flex-1 min-w-0">
              <p className="text-sm font-medium text-gray-800 truncate">
                {user?.firstName} {user?.lastName}
              </p>
              <p className="text-xs text-gray-400 truncate">{user?.email}</p>
            </div>
          </div>
          {(isCreator || isPlatformAdmin) && (
            <p className="text-xs text-accent-600 font-medium px-1 mb-2">
              {isPlatformAdmin ? '👑 Administrador' : '🎬 Criador de conteúdo'}
            </p>
          )}
          <button
            onClick={logout}
            className="w-full text-left text-xs text-red-400 hover:text-red-300 font-medium
                       transition-colors px-1 py-1"
          >
            Sair da conta →
          </button>
        </div>
      </aside>

      {/* Main content */}
      <div className="flex-1 flex flex-col min-w-0">
        {/* Mobile topbar */}
        <header className="md:hidden h-16 bg-white border-b border-gray-200 px-4
                           flex items-center justify-between">
          <button onClick={() => setSidebar(true)} className="text-gray-500 hover:text-gray-800 p-2">
            ☰
          </button>
          <span className="font-bold gradient-text text-sm">🎓 Tuilow</span>
          <div className="w-8 h-8 rounded-full bg-gradient-to-br from-brand-500 to-accent-500
                          flex items-center justify-center text-white font-bold text-xs">
            {initials}
          </div>
        </header>

        <main className="flex-1 p-6 md:p-8 overflow-auto animate-fade-in">
          {children}
        </main>
      </div>
    </div>
  );
}
