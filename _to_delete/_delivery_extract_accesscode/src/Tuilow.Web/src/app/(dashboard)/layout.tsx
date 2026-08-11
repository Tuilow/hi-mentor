'use client';

import { useEffect, useState } from 'react';
import { useRouter, usePathname } from 'next/navigation';
import { useQuery } from '@tanstack/react-query';
import Link from 'next/link';
import {
  Home, Clock, GraduationCap, Rocket, FileText, Radio, Mic, Wallet,
  LayoutDashboard, Users, CreditCard, LogOut, Menu, Crown, Ticket,
} from 'lucide-react';
import { authApi, enrollmentsApi, clearAccessToken } from '@/lib/api';
import { MyEnrollment } from '@/types';
import Logo from '@/components/brand/Logo';

// Redesign "Hi Mentor" (ago/2026): sidebar reestilizado seguindo hi-mentor/src/components/Sidebar.tsx
// (protótipo Figma Make) — mesma estrutura de navegação, mesmos hrefs e mesma lógica de
// gating (isCreator/isPlatformAdmin/hasAnyCourseAccess) de antes, só o visual e os rótulos
// mudaram para a terminologia "Hi Mentor" (Programas/Mentorados/Mentor).
//
// "Cursos" e "Assinatura" foram removidos do menu (Sprint Item 5): a plataforma não é uma
// vitrine pública de cursos — o acesso é sempre por link/canal/página de vendas de um Creator
// específico, então não faz sentido oferecer navegação pra "ver todos os cursos" ou "gerenciar
// assinaturas" no menu principal. As páginas em si continuam existindo (/cursos, /assinatura)
// e seguem acessíveis pelos CTAs do dashboard e do histórico — só o item de menu saiu.
const navItems = [
  { href: '/dashboard', label: 'Início', icon: Home },
  { href: '/historico', label: 'Histórico', icon: Clock },
];

const adminNavItems = [
  { href: '/admin/produtos', label: 'Meus Programas', icon: Rocket },
  { href: '/admin/cursos', label: 'Conteúdos', icon: FileText },
  { href: '/canal', label: 'Meu Canal', icon: Radio },
  { href: '/estudio', label: 'Estúdio do Mentor', icon: Mic },
  { href: '/admin/financeiro', label: 'Financeiro', icon: Wallet },
];

// Painel do dono da plataforma — distinto da área "Administração" do Mentor acima (que apesar
// do nome é a área de gestão de programas do próprio Mentor, não do dono da Hi Mentor). Sem
// contrapartida no protótipo Figma Make — rótulos mantidos como já eram.
const platformNavItems = [
  { href: '/plataforma', label: 'Visão Geral', icon: LayoutDashboard },
  { href: '/plataforma/usuarios', label: 'Usuários', icon: Users },
  { href: '/plataforma/codigos-acesso', label: 'Códigos de acesso', icon: Ticket },
  { href: '/plataforma/financeiro', label: 'Financeiro', icon: CreditCard },
];

export default function DashboardLayout({ children }: { children: React.ReactNode }) {
  const router = useRouter();
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

  // Problema 2 da sprint (Biblioteca do usuário): "Meus Cursos" só aparece no menu para quem já
  // possui pelo menos um curso com acesso — nenhum visitante recém-cadastrado vê um menu vazio.
  // Mesma query key usada em /cursos e /meus-cursos: compartilhar o cache evita uma chamada
  // redundante a GET /enrollments/me a cada navegação.
  const { data: myEnrollments = [] } = useQuery<MyEnrollment[]>({
    queryKey: ['my-enrollments'],
    queryFn: () => enrollmentsApi.getMyEnrollments().then(r => r.data),
    enabled: !!user,
  });
  const hasAnyCourseAccess = myEnrollments.length > 0;

  useEffect(() => {
    if (isError) router.push('/login');
  }, [isError, router]);

  // Achado C1 da avaliação www/app: antes só limpava o localStorage — o refresh token continuava
  // válido no servidor até expirar sozinho (30 dias). Agora chama /auth/logout para revogá-lo de
  // verdade e limpar o cookie HttpOnly; se a chamada falhar (ex.: rede fora), ainda assim limpa o
  // estado local e redireciona — não deixar o usuário preso na tela é mais importante do que
  // garantir a revogação no servidor neste caminho de erro.
  const logout = async () => {
    try {
      await authApi.logout();
    } catch {
      // Sessão local será limpa de qualquer forma — ver comentário acima.
    } finally {
      clearAccessToken();
      router.push('/login');
    }
  };

  const initials = user
    ? `${user.firstName?.[0] ?? ''}${user.lastName?.[0] ?? ''}`.toUpperCase()
    : '?';

  // Separado em duas flags: isCreator controla a área "Administração" (programas do próprio
  // Mentor, nome legado da role continua "Creator" no backend); isPlatformAdmin controla a área
  // "Plataforma" (painel do dono da Hi Mentor — gestão de todos os usuários). Antes eram
  // conflados em um único isAdmin, o que fazia um Mentor comum ver conteúdo que deveria ser
  // exclusivo do dono da plataforma.
  const isCreator = user?.roles?.includes('Creator');
  const isPlatformAdmin = user?.roles?.includes('Admin');

  const NavLink = ({ href, label, icon: Icon }: { href: string; label: string; icon: typeof Home }) => {
    const active = pathname === href || pathname.startsWith(href + '/');
    return (
      <Link
        href={href}
        onClick={() => setSidebar(false)}
        className={`w-full flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium transition-all duration-150 text-left group ${
          active
            ? 'bg-violet-50 text-violet-700 font-semibold'
            : 'text-ink-2 hover:bg-subtle hover:text-ink'
        }`}
      >
        <Icon size={18} className={`shrink-0 transition-colors ${active ? 'text-violet-600' : 'text-ink-3 group-hover:text-ink-2'}`} />
        <span>{label}</span>
      </Link>
    );
  };

  return (
    <div className="min-h-screen bg-canvas flex">

      {/* Mobile overlay */}
      {sidebarOpen && (
        <div className="fixed inset-0 bg-black/60 z-20 md:hidden"
             onClick={() => setSidebar(false)} />
      )}

      {/* Sidebar */}
      <aside className={`
        fixed md:static inset-y-0 left-0 z-30
        w-64 bg-surface border-r border-line
        flex flex-col
        transition-transform duration-200
        ${sidebarOpen ? 'translate-x-0' : '-translate-x-full md:translate-x-0'}
      `}>
        {/* Logo */}
        <div className="px-5 py-5 border-b border-line-light">
          <Link href="/dashboard">
            <Logo size="sm" />
          </Link>
        </div>

        {/* Nav */}
        <nav className="flex-1 px-3 py-4 flex flex-col gap-0.5 overflow-y-auto">
          {navItems.map(item => (
            <NavLink key={item.href} {...item} />
          ))}

          {/* "Minha Jornada" (biblioteca) só aparece para quem já tem acesso a pelo menos um
              programa — compra, matrícula, assinatura ativa ou liberação manual (a mesma lista
              de GET /enrollments/me, que É o registro de acesso da plataforma). Nunca mostra a
              vitrine/catálogo público aqui — ver (dashboard)/meus-cursos/page.tsx. */}
          {hasAnyCourseAccess && (
            <NavLink href="/meus-cursos" label="Minha Jornada" icon={GraduationCap} />
          )}

          {/* Área do Mentor */}
          {isCreator && (
            <>
              <div className="pt-4 pb-1">
                <p className="px-3 text-xs font-semibold text-ink-4 uppercase tracking-wider">
                  Administração
                </p>
              </div>
              {adminNavItems.map(item => (
                <NavLink key={item.href} {...item} />
              ))}
            </>
          )}

          {/* Painel do dono da plataforma */}
          {isPlatformAdmin && (
            <>
              <div className="pt-4 pb-1">
                <p className="px-3 text-xs font-semibold text-ink-4 uppercase tracking-wider">
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
        <div className="px-3 py-4 mt-2 border-t border-line-light space-y-1">
          {(isCreator || isPlatformAdmin) && (
            <p className="flex items-center gap-1.5 text-xs text-amber-600 font-semibold px-3 mb-1">
              {isPlatformAdmin ? <Crown size={13} /> : <Mic size={13} />}
              {isPlatformAdmin ? 'Administrador' : 'Mentor'}
            </p>
          )}
          <div className="w-full flex items-center gap-3 px-3 py-2 rounded-xl">
            <div className="w-8 h-8 rounded-full bg-violet-600 flex items-center justify-center text-white text-sm font-bold shrink-0">
              {initials}
            </div>
            <div className="flex-1 min-w-0">
              <p className="text-sm font-semibold text-ink truncate">
                {user?.firstName} {user?.lastName}
              </p>
              <p className="text-xs text-ink-3 truncate">{user?.email}</p>
            </div>
          </div>
          <button
            onClick={logout}
            className="w-full flex items-center gap-2 px-3 py-1.5 rounded-xl text-xs text-ink-3 hover:text-red-600 hover:bg-red-50 font-medium transition-all cursor-pointer text-left"
          >
            <LogOut size={13} />
            Sair da conta
          </button>
        </div>
      </aside>

      {/* Main content */}
      <div className="flex-1 flex flex-col min-w-0">
        {/* Mobile topbar */}
        <header className="md:hidden h-16 bg-surface border-b border-line px-4
                           flex items-center justify-between">
          <button onClick={() => setSidebar(true)} className="text-ink-2 hover:text-ink p-2">
            <Menu size={20} />
          </button>
          <Logo size="xs" />
          <div className="w-8 h-8 rounded-full bg-violet-600 flex items-center justify-center text-white font-bold text-xs">
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
