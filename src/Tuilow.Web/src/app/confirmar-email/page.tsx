'use client';

import { Suspense, useEffect, useState, FormEvent } from 'react';
import { useSearchParams } from 'next/navigation';
import Link from 'next/link';
import toast from 'react-hot-toast';
import { AxiosError } from 'axios';
import { authApi } from '@/lib/api';

// Mesmo padrão usado em app/(auth)/registro/page.tsx.
const errorMessage = (e: unknown, fallback: string) => {
  const data = (e as AxiosError<{ title?: string; detail?: string; message?: string; errors?: Record<string, string[]> }>)
    .response?.data;
  const firstFieldError = data?.errors && Object.values(data.errors).flat()[0];
  return firstFieldError ?? data?.detail ?? data?.title ?? data?.message ?? fallback;
};

/**
 * Confirmação de e-mail via código de 6 dígitos (Sprint Item 4 — dupla checagem antes de permitir
 * login; ver User.Register/RegisterUserCommandHandler/LoginUserCommandHandler). Cadastro não faz
 * mais login automático, então esta tela é o próximo passo obrigatório depois de /registro.
 * Aceita tanto o clique no link do e-mail (email+code já vêm na URL e confirma sozinho, mesma
 * ideia do fluxo anterior baseado em userId+token) quanto a digitação manual do código, já que o
 * e-mail agora também exibe o código em destaque para quem preferir não clicar em nada.
 */
function EmailConfirmer() {
  const searchParams = useSearchParams();
  const returnUrl = searchParams.get('returnUrl');
  const emailFromUrl = searchParams.get('email') ?? '';
  const codeFromUrl = searchParams.get('code') ?? '';

  const [email, setEmail] = useState(emailFromUrl);
  const [code, setCode] = useState(codeFromUrl);
  const [loading, setLoading] = useState(false);
  const [autoConfirming, setAutoConfirming] = useState(!!(emailFromUrl && codeFromUrl));
  const [confirmed, setConfirmed] = useState(false);

  const confirm = async (emailToConfirm: string, codeToConfirm: string) => {
    try {
      await authApi.confirmEmail(emailToConfirm, codeToConfirm);
      setConfirmed(true);
      toast.success('E-mail confirmado! Agora você já pode entrar.');
    } catch (err) {
      toast.error(errorMessage(err, 'Código inválido ou expirado.'));
    } finally {
      setLoading(false);
      setAutoConfirming(false);
    }
  };

  useEffect(() => {
    if (emailFromUrl && codeFromUrl) confirm(emailFromUrl, codeFromUrl);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const onSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (!email || !code) {
      toast.error('Preencha e-mail e código.');
      return;
    }
    setLoading(true);
    await confirm(email, code);
  };

  const loginHref = returnUrl ? `/login?returnUrl=${encodeURIComponent(returnUrl)}` : '/login';

  if (autoConfirming) {
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

  if (confirmed) {
    return (
      <div className="min-h-screen bg-white flex flex-col items-center justify-center gap-3 px-4 text-center">
        <p className="text-4xl">🎉</p>
        <p className="text-lg font-medium text-gray-700">E-mail confirmado com sucesso!</p>
        <p className="text-sm text-gray-400 max-w-xs">
          Sua conta está pronta. Entre agora com seu e-mail e senha.
        </p>
        <Link href={loginHref} className="btn-primary px-6 py-2.5 mt-2">
          Ir para o login →
        </Link>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-white flex items-center justify-center px-4">
      <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2
                      w-96 h-96 bg-brand-700/10 rounded-full blur-3xl pointer-events-none" />

      <div className="relative w-full max-w-md animate-fade-in">
        <div className="text-center mb-8">
          <Link href="/" className="inline-block">
            <h1 className="text-2xl font-bold gradient-text">🎓 Tuilow</h1>
          </Link>
          <p className="text-gray-500 mt-2 text-sm">Confirme seu e-mail</p>
        </div>

        <div className="card border-gray-200">
          <p className="text-sm text-gray-500 mb-5">
            Enviamos um código de 6 dígitos para o seu e-mail. Digite-o abaixo para ativar sua conta.
          </p>

          <form onSubmit={onSubmit} className="space-y-5">
            <div>
              <label className="block text-sm font-medium text-gray-600 mb-1.5">E-mail</label>
              <input
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                type="email"
                placeholder="seu@email.com"
                className="input-field"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-600 mb-1.5">Código de confirmação</label>
              <input
                value={code}
                onChange={(e) => setCode(e.target.value.replace(/\D/g, '').slice(0, 6))}
                inputMode="numeric"
                placeholder="000000"
                maxLength={6}
                className="input-field text-center text-2xl tracking-[0.5em]"
              />
            </div>

            <button type="submit" disabled={loading} className="btn-primary w-full py-3">
              {loading ? 'Confirmando...' : 'Confirmar e-mail'}
            </button>
          </form>

          <div className="divider mt-6 pt-6">
            <p className="text-center text-sm text-gray-400">
              Já confirmou?{' '}
              <Link href={loginHref} className="text-brand-600 font-medium hover:text-brand-700 transition-colors">
                Ir para o login
              </Link>
            </p>
          </div>
        </div>
      </div>
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
