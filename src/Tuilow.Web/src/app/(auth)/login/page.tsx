'use client';

import { Suspense, useState } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import Link from 'next/link';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import toast from 'react-hot-toast';
import { AxiosError } from 'axios';
import { GoogleLogin, CredentialResponse } from '@react-oauth/google';
import { authApi } from '@/lib/api';
import { PasswordInput } from '@/components/ui/PasswordInput';

// Mesmo padrão usado em app/(auth)/registro/page.tsx — extrai a mensagem real que o back-end
// devolveu (ex.: "Credenciais inválidas.", vinda do ExceptionHandlingMiddleware) em vez de exibir
// sempre um texto genérico fixo, que escondia a causa real do erro.
const errorMessage = (e: unknown, fallback: string) => {
  const data = (e as AxiosError<{ title?: string; detail?: string; message?: string; errors?: Record<string, string[]> }>)
    .response?.data;
  const firstFieldError = data?.errors && Object.values(data.errors).flat()[0];
  return firstFieldError ?? data?.detail ?? data?.title ?? data?.message ?? fallback;
};

const schema = z.object({
  email: z.string().email('E-mail inválido'),
  password: z.string().min(6, 'Mínimo 6 caracteres'),
});

type FormData = z.infer<typeof schema>;

function LoginForm() {
  const router = useRouter();
  const searchParams = useSearchParams();
  // Vindo da página de vendas pública (/c/[slug]) — depois de logar, volta direto pra lá em vez
  // de cair no dashboard genérico, sem o que o visitante perdia o produto que veio comprar.
  const returnUrl = searchParams.get('returnUrl');
  const [loading, setLoading] = useState(false);
  const [googleLoading, setGoogleLoading] = useState(false);

  const { register, handleSubmit, formState: { errors } } = useForm<FormData>({
    resolver: zodResolver(schema),
  });

  // Criador loga e cai direto em "Meus Produtos" (era /dashboard genérico pra todo mundo, mas
  // pra quem já tem produto cadastrado o painel de aluno não é a tela útil no dia a dia). Aluno
  // sem papel de Criador continua caindo em /dashboard, sem mudança nenhuma. returnUrl (voltar
  // pra uma página de vendas específica) sempre tem prioridade sobre os dois casos.
  const goHome = async () => {
    if (returnUrl) {
      router.push(returnUrl);
      return;
    }
    try {
      const { data: profile } = await authApi.me();
      router.push(profile?.roles?.includes('Creator') ? '/admin/produtos' : '/dashboard');
    } catch {
      router.push('/dashboard');
    }
  };

  const onSubmit = async (data: FormData) => {
    try {
      setLoading(true);
      const res = await authApi.login(data);
      // Achado C1: o refresh token não volta mais no corpo (vive só no cookie HttpOnly setado
      // pelo backend nesta mesma resposta) — access_token continua em localStorage por enquanto.
      localStorage.setItem('access_token', res.data.accessToken);
      toast.success('Bem-vindo de volta!');
      await goHome();
    } catch (err) {
      toast.error(errorMessage(err, 'E-mail ou senha incorretos.'));
    } finally {
      setLoading(false);
    }
  };

  const handleGoogleSuccess = async (credentialResponse: CredentialResponse) => {
    if (!credentialResponse.credential) {
      toast.error('Falha ao obter token do Google.');
      return;
    }
    try {
      setGoogleLoading(true);
      const res = await authApi.googleLogin(credentialResponse.credential);
      localStorage.setItem('access_token', res.data.accessToken);
      toast.success('Bem-vindo!');
      await goHome();
    } catch {
      toast.error('Falha no login com Google. Tente novamente.');
    } finally {
      setGoogleLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-white flex items-center justify-center px-4">
      {/* Glow */}
      <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2
                      w-96 h-96 bg-brand-700/10 rounded-full blur-3xl pointer-events-none" />

      <div className="relative w-full max-w-md animate-fade-in">
        <div className="text-center mb-8">
          <Link href="/" className="inline-block">
            <h1 className="text-2xl font-bold gradient-text">🎓 Tuilow</h1>
          </Link>
          <p className="text-gray-500 mt-2 text-sm">Entre na sua conta</p>
        </div>

        <div className="card border-gray-200">
          {/* Google Login */}
          {googleLoading ? (
            <div className="w-full py-3 flex items-center justify-center gap-2 rounded-lg
                            border border-gray-300 bg-gray-100/50 text-gray-500 text-sm">
              <svg className="animate-spin h-4 w-4" fill="none" viewBox="0 0 24 24">
                <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                <path className="opacity-75" fill="currentColor"
                  d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
              </svg>
              Conectando ao Google...
            </div>
          ) : (
            <div className="flex justify-center">
              <GoogleLogin
                onSuccess={handleGoogleSuccess}
                onError={() => toast.error('Login com Google cancelado.')}
                theme="filled_black"
                size="large"
                width="368"
                text="signin_with"
                locale="pt-BR"
              />
            </div>
          )}

          <div className="flex items-center gap-3 my-5">
            <div className="flex-1 h-px bg-gray-100" />
            <span className="text-xs text-gray-400">ou continue com e-mail</span>
            <div className="flex-1 h-px bg-gray-100" />
          </div>

          <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
            <div>
              <label className="block text-sm font-medium text-gray-600 mb-1.5">E-mail</label>
              <input {...register('email')} type="email" placeholder="seu@email.com"
                className="input-field" />
              {errors.email && <p className="text-red-400 text-xs mt-1.5">{errors.email.message}</p>}
            </div>

            <div>
              <div className="flex items-center justify-between mb-1.5">
                <label className="block text-sm font-medium text-gray-600">Senha</label>
                <Link href="/esqueci-senha"
                  className="text-xs text-brand-600 hover:text-brand-700 transition-colors">
                  Esqueceu a senha?
                </Link>
              </div>
              <PasswordInput {...register('password')} placeholder="••••••••"
                className="input-field" />
              {errors.password && <p className="text-red-400 text-xs mt-1.5">{errors.password.message}</p>}
            </div>

            <button type="submit" disabled={loading} className="btn-primary w-full py-3">
              {loading ? 'Entrando...' : 'Entrar'}
            </button>
          </form>

          <div className="divider mt-6 pt-6">
            <p className="text-center text-sm text-gray-400">
              Não tem conta?{' '}
              <Link href={returnUrl ? `/registro?returnUrl=${encodeURIComponent(returnUrl)}` : '/registro'}
                className="text-brand-600 font-medium hover:text-brand-700 transition-colors">
                Cadastre-se grátis
              </Link>
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}

// useSearchParams exige um limite de Suspense (retornUrl vindo de /c/[slug]) — sem isso o
// Next.js falha o build estático desta página.
export default function LoginPage() {
  return (
    <Suspense fallback={null}>
      <LoginForm />
    </Suspense>
  );
}
