'use client';

import { Suspense, useEffect, useState } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import Link from 'next/link';
import { authApi } from '@/lib/api';

type Status = 'loading' | 'error';

/**
 * Consumo do Magic Link — para onde apontam os links enviados por e-mail/WhatsApp após a
 * confirmação de pagamento (ver EmailService.SendMagicLinkAccessAsync). Troca o token por um
 * login completo (POST /auth/magic-link/consume) sem exigir senha, e cai direto no curso
 * comprado (?redirect=/cursos/slug) — ou no dashboard, se não houver redirect.
 */
function MagicLinkConsumer() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const token = searchParams.get('token');
  const redirect = searchParams.get('redirect');
  const [status, setStatus] = useState<Status>('loading');

  useEffect(() => {
    if (!token) {
      setStatus('error');
      return;
    }

    authApi.consumeMagicLink(token)
      .then(res => {
        localStorage.setItem('access_token', res.data.accessToken);
        router.push(redirect || '/dashboard');
      })
      .catch(() => setStatus('error'));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [token]);

  if (status === 'error') {
    return (
      <div className="min-h-screen bg-white flex flex-col items-center justify-center gap-3 px-4 text-center">
        <p className="text-4xl">🔗</p>
        <p className="text-lg font-medium text-gray-700">Este link de acesso expirou ou já foi usado</p>
        <p className="text-sm text-gray-400 max-w-xs">
          Magic Links valem por 48 horas e só podem ser usados uma vez. Entre normalmente com e-mail e senha.
        </p>
        <Link href="/login" className="btn-primary px-6 py-2.5 mt-2">Ir para o login →</Link>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-white flex flex-col items-center justify-center gap-3">
      <svg className="animate-spin h-8 w-8 text-brand-600" fill="none" viewBox="0 0 24 24">
        <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
        <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
      </svg>
      <p className="text-gray-500 text-sm">Liberando seu acesso...</p>
    </div>
  );
}

export default function AcessoPage() {
  return (
    <Suspense fallback={null}>
      <MagicLinkConsumer />
    </Suspense>
  );
}
