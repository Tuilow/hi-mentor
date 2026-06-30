'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { authApi } from '@/lib/api';
import { UserProfile } from '@/types';

const navItems = [
  { href: '/dashboard', label: 'Início', icon: '🏠' },
  { href: '/cursos', label: 'Cursos', icon: '📚' },
  { href: '/cachorros', label: 'Meus Cães', icon: '🐕' },
  { href: '/assinatura', label: 'Assinatura', icon: '💎' },
];

export default function DashboardLayout({ children }: { children: React.ReactNode }) {
  const router = useRouter();
  const [user, setUser] = useState<UserProfile | null>(null);

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

  return (
    <div className="min-h-screen bg-gray-50 flex">
      {/* Sidebar */}
      <aside className="w-64 bg-white border-r border-gray-200 flex flex-col">
        <div className="p-6 border-b border-gray-200">
          <h1 className="text-xl font-bold text-brand-700">🐕 DogMaster Pro</h1>
        </div>

        <nav className="flex-1 p-4 space-y-1">
          {navItems.map(item => (
            <Link key={item.href} href={item.href}
              className="flex items-center gap-3 px-4 py-3 rounded-lg text-gray-700
                         hover:bg-brand-50 hover:text-brand-700 transition-colors font-medium">
              <span>{item.icon}</span>
              {item.label}
            </Link>
          ))}
        </nav>

        <div className="p-4 border-t border-gray-200">
          <div className="flex items-center gap-3 mb-3">
            <div className="w-8 h-8 bg-brand-600 rounded-full flex items-center justify-center text-white font-bold text-sm">
              {user?.firstName?.[0]}
            </div>
            <div className="flex-1 min-w-0">
              <p className="text-sm font-medium text-gray-900 truncate">
                {user?.firstName} {user?.lastName}
              </p>
              <p className="text-xs text-gray-500 truncate">{user?.email}</p>
            </div>
          </div>
          <button onClick={logout}
            className="w-full text-left text-sm text-red-500 hover:text-red-700 font-medium">
            Sair
          </button>
        </div>
      </aside>

      {/* Content */}
      <main className="flex-1 p-8 overflow-auto">{children}</main>
    </div>
  );
}
