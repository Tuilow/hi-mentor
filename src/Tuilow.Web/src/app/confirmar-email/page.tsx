'use client';

import { Suspense, useEffect, useState } from 'react';
import { useSearchParams } from 'next/navigation';
import Link from 'next/link';
import { authApi } from '@/lib/api';

type Status = 'loading' | 'success' | 'error';

/**
 * Consumo do link de confirmação de e-mail enviado no cadastro (ver EmailService.SendWelcomeAsync
 * e RegisterUserCommandHandler). Diferente do Magic Link (app/acesso), este endpoint não devolve
 * tokens de sessão — só marca o e-mail como confirmado (POST /auth/confirm-email com userId+token,
 * ambos exigidos pelo backend). Reaproveita o padrão visual de app/acesso/page.tsx.
 */
function EmailConfirmer() {
  const searchParams = useSearchParams();
  const userId = searchParams.get('userId');
  const token = searchParams.get('token');
  const [status, setStatus] = useState<Status>('loading');
  const [isLoggedIn, setIsLoggedIn] = useState(false);

  useEffect(() => {
    setIsLoggedIn(!!localStorage.getItem('access_token'));
  }, []);

  useEffect(() => {
    if (!userId || !token) {
      setStatus('error');
      return;
    }
    authApi.confirmEmail(userId, token)
      .then(() => setStatus('success'))
      .catch(() => setStatus('error'));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [userId, token]);

  if (status === 'loading') {
    return (
      <div className="min-h-screen bg-white flex flex-col items-center justify-center gap-3">
        <svg className="animate-spin h-8 w-8 text-brand-600" fill="none" viewBox="0 0 24 24">
          <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
          <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
        </svg>
        <p className="text-gray-500 text-sm">Confirmando seu e-mail...</p>
      </div>
    );
  }

  if (status === 'error') {
    return (
      <div className="min-h-screen bg-white flex flex-col items-center justify-center gap-3 px-4 text-center">
        <p className="text-4xl">🔗</p>
        <p className="text-lg font-medium text-gray-700">Não foi possível confirmar seu e-mail</p>
        <p className="text-sm text-gray-400 max-w-xs">
          Este link pode ter expirado, já ter sido usado, ou o endereço está incompleto.
          Isso não impede você de acessar sua conta normalmente.
        </p>
        <Link href={isLoggedIn ? '/dashboard' : '/login'} className="btn-primary px-6 py-2.5 mt-2">
          {isLoggedIn ? 'Ir para o painel →' : 'Ir para o login →'}
        </Link>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-white flex flex-col items-center justify-center gap-3 px-4 text-center">
      <p className="text-4xl">🎉</p>
      <p className="text-lg font-medium text-gray-700">E-mail confirmado com sucesso!</p>
      <p className="text-sm text-gray-400 max-w-xs">
        Sua conta está pronta. Acesse sua área de aluno agora — e, quando quiser, torne-se
        criador de cursos direto por lá.
      </p>
      <Link href={isLoggedIn ? '/dashboard' : '/login'} className="btn-primary px-6 py-2.5 mt-2">
        {isLoggedIn ? 'Acessar minha área →' : 'Ir para o login →'}
      </Link>
    </div>
  );
}

export default function ConfirmarEmailPage() {
  return (
    <Suspense fallback={null}>
      <EmailConfirmer />
    </Suspense>
  );
}
