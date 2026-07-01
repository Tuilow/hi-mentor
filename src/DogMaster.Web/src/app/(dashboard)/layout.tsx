'use client';

import { useEffect, useState } from 'react';
import { useRouter, usePathname } from 'next/navigation';
import Link from 'next/link';
import { authApi } from '@/lib/api';
import { UserProfile } from '@/types';

const navItems = [
  { href: '/dashboard', label: 'Início',      icon: '🏠' },
  { href: '/cursos',    label: 'Cursos',      icon: '📚' },
  { href: '/cachorros', label: 'Meus Cães',   icon: '🐕' },
  { href: '/assinatura',label: 'Assinatura',  icon: '💎' },
];

export default function DashboardLayout({ children }: { children: React.ReactNode }) {
  const router   = useRouter();
  const pathname = usePathname();
  const [user, setUser]           = useState<UserProfile | null>(null);
  const [sidebarOpen, setSidebar] = useState(false);

  useEffect(() => {
    authApi.me()
      .then(res => setUser(res.data))
      .catch(() => router.push('/login'));
  }, [router]);

  const logout = () => {
    localStorage.removeItem('access_token');
    localStorage.removeItem('refresh_token');
    router.push('/login');
  };

  const initials = user
    ? `${user.firstName?.[0] ?? ''}${user.lastName?.[0] ?? ''}`.toUpperCase()
    : '?';

  return (
    <div className="min-h-screen bg-zinc-950 flex">

      {/* Mobile overlay */}
      {sidebarOpen && (
        <div className="fixed inset-0 bg-black/60 z-20 md:hidden"
             onClick={() => setSidebar(false)} />
      )}

      {/* Sidebar */}
      <aside className={`
        fixed md:static inset-y-0 left-0 z-30
        w-64 bg-zinc-900 border-r border-zinc-800
        flex flex-col
        transition-transform duration-200
        ${sidebarOpen ? 'translate-x-0' : '-translate-x-full md:translate-x-0'}
      `}>
        {/* Logo */}
        <div className="h-16 px-6 flex items-center border-b border-zinc-800">
          <Link href="/dashboard" className="text-lg font-bold gradient-text">
            🐕 DogMaster Pro
          </Link>
        </div>

        {/* Nav */}
        <nav className="flex-1 p-4 space-y-1 overflow-y-auto">
          {navItems.map(item => {
            const active = pathname === item.href;
            return (
              <Link
                key={item.href}
                href={item.href}
                onClick={() => setSidebar(false)}
                className={`
                  flex items-center gap-3 px-4 py-2.5 rounded-lg text-sm font-medium
                  transition-all duration-150
                  ${active
                    ? 'bg-brand-600/20 text-brand-300 border border-brand-700/40'
                    : 'text-zinc-400 hover:text-zinc-100 hover:bg-zinc-800'}
                `}
              >
                <span className="text-base">{item.icon}</span>
                {item.label}
              </Link>
            );
          })}
        </nav>

        {/* User footer */}
        <div className="p-4 border-t border-zinc-800">
          <div className="flex items-center gap-3 mb-3">
            <div className="w-8 h-8 rounded-full bg-gradient-to-br from-brand-500 to-purple-600
                            flex items-center justify-center text-white font-bold text-xs flex-shrink-0">
              {initials}
            </div>
            <div className="flex-1 min-w-0">
              <p className="text-sm font-medium text-zinc-100 truncate">
                {user?.firstName} {user?.lastName}
              </p>
              <p className="text-xs text-zinc-500 truncate">{user?.email}</p>
            </div>
          </div>
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
        <header className="md:hidden h-16 bg-zinc-900 border-b border-zinc-800 px-4
                           flex items-center justify-between">
          <button onClick={() => setSidebar(true)} className="text-zinc-400 hover:text-zinc-100 p-2">
            ☰
          </button>
          <span className="font-bold gradient-text text-sm">🐕 DogMaster Pro</span>
          <div className="w-8 h-8 rounded-full bg-gradient-to-br from-brand-500 to-purple-600
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
