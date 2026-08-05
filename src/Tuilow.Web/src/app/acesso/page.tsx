'use client';

import { Suspense, useEffect, useState } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import Link from 'next/link';
import { authApi, setAccessToken } from '@/lib/api';

type Status = 'loading' | 'error';
type ResendStatus = 'idle' | 'sending' | 'sent';

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

  // Achado em teste manual: Magic Links valem só 48h e uso único -- quem perdia essa janela
  // caía direto na tela "link expirou" mandando entrar com e-mail e senha, só que a conta
  // criada no checkout anônimo nunca teve senha nenhuma (ver User.RegisterFromPurchase), um
  // beco sem saída self-service. Reenvia um novo Magic Link pro mesmo e-mail (POST
  // /auth/resend-access-link), preservando a experiência "sem senha" do produto.
  const [resendEmail, setResendEmail] = useState('');
  const [resendStatus, setResendStatus] = useState<ResendStatus>('idle');

  useEffect(() => {
    if (!token) {
      setStatus('error');
      return;
    }

    authApi.consumeMagicLink(token)
      .then(res => {
        setAccessToken(res.data.accessToken);
        router.push(redirect || '/dashboard');
      })
      .catch(() => setStatus('error'));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [token]);

  async function handleResend(e: React.FormEvent) {
    e.preventDefault();
    if (!resendEmail || resendStatus === 'sending') return;
    setResendStatus('sending');
    try {
      await authApi.resendAccessLink(resendEmail);
    } finally {
      // Mesma mensagem sempre (independente de sucesso/falha) -- mesmo padrão de privacidade
      // do backend (não revela se o e-mail existe na base).
      setResendStatus('sent');
    }
  }

  if (status === 'error') {
    return (
      <div className="min-h-screen bg-white flex flex-col items-center justify-center gap-3 px-4 text-center">
        <p className="text-4xl">🔗</p>
        <p className="text-lg font-medium text-gray-700">Este link de acesso expirou ou já foi usado</p>
        <p className="text-sm text-gray-400 max-w-xs">
          Magic Links valem por 48 horas e só podem ser usados uma vez.
        </p>

        {resendStatus === 'sent' ? (
          <p className="text-sm text-green-600 max-w-xs mt-2">
            Se esse e-mail tiver uma compra na Tuilow, mandamos um novo link de acesso agora. Confira sua caixa de entrada.
          </p>
        ) : (
          <form onSubmit={handleResend} className="flex flex-col gap-2 w-full max-w-xs mt-2">
            <input
              type="email"
              required
              placeholder="Seu e-mail da compra"
              value={resendEmail}
              onChange={e => setResendEmail(e.target.value)}
              className="border border-gray-300 rounded-md px-3 py-2 text-sm"
            />
            <button type="submit" disabled={resendStatus === 'sending'} className="btn-primary px-6 py-2.5">
              {resendStatus === 'sending' ? 'Enviando...' : 'Reenviar link de acesso'}
            </button>
          </form>
        )}

        <p className="text-xs text-gray-400 mt-4">
          Prefere usar senha? <Link href="/login" className="underline">Entrar com e-mail e senha</Link>
        </p>
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
