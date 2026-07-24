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

// Mesmo padrão usado em outras páginas (ex: cursos/[slug]/page.tsx): o backend
// (ExceptionHandlingMiddleware) devolve a mensagem real do erro em "title" (e
// validações de campo em "errors"); "detail"/"message" cobrem outros formatos
// (ex: ProblemDetails padrão do ASP.NET), caso apareçam.
const errorMessage = (e: unknown, fallback: string) => {
  const data = (e as AxiosError<{ title?: string; detail?: string; message?: string; errors?: Record<string, string[]> }>)
    .response?.data;
  const firstFieldError = data?.errors && Object.values(data.errors).flat()[0];
  return firstFieldError ?? data?.detail ?? data?.title ?? data?.message ?? fallback;
};

const schema = z.object({
  firstName: z.string().min(2, 'Nome muito curto'),
  lastName: z.string().min(2, 'Sobrenome muito curto'),
  email: z.string().email('E-mail inválido'),
  password: z.string().min(8, 'Mínimo 8 caracteres'),
  confirmPassword: z.string(),
}).refine(d => d.password === d.confirmPassword, {
  message: 'Senhas não coincidem',
  path: ['confirmPassword'],
});

type FormData = z.infer<typeof schema>;

function RegisterForm() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const returnUrl = searchParams.get('returnUrl');
  const [loading, setLoading] = useState(false);
  const [googleLoading, setGoogleLoading] = useState(false);

  const { register, handleSubmit, formState: { errors } } = useForm<FormData>({
    resolver: zodResolver(schema),
  });

  const onSubmit = async ({ confirmPassword: _, ...data }: FormData) => {
    try {
      setLoading(true);
      await authApi.register(data);
      toast.success('Conta criada! Enviamos um código de confirmação para o seu e-mail.');
      // Sprint Item 4: cadastro não faz mais login automático — a conta só é liberada pra
      // login depois que o código enviado por e-mail é confirmado (ver confirmar-email/page.tsx).
      const params = new URLSearchParams({ email: data.email });
      if (returnUrl) params.set('returnUrl', returnUrl);
      router.push(`/confirmar-email?${params.toString()}`);
    } catch (err) {
      toast.error(errorMessage(err, 'Erro ao criar conta. Verifique os dados.'));
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
      localStorage.setItem('refresh_token', res.data.refreshToken);
      toast.success('Conta criada com Google!');
      router.push(returnUrl || '/dashboard');
    } catch (err) {
      toast.error(errorMessage(err, 'Falha no cadastro com Google. Tente novamente.'));
    } finally {
      setGoogleLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-white flex items-center justify-center px-4 py-12">
      <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2
                      w-96 h-96 bg-accent-400/10 rounded-full blur-3xl pointer-events-none" />

      <div className="relative w-full max-w-md animate-fade-in">
        <div className="text-center mb-8">
          <Link href="/" className="inline-block">
            <h1 className="text-2xl font-bold gradient-text">🎓 Tuilow</h1>
          </Link>
          <p className="text-gray-500 mt-2 text-sm">Crie sua conta gratuita</p>
        </div>

        <div className="card border-gray-200">
          {/* Google Signup */}
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
                onError={() => toast.error('Cadastro com Google cancelado.')}
                theme="filled_black"
                size="large"
                width="368"
                text="signup_with"
                locale="pt-BR"
              />
            </div>
          )}

          <div className="flex items-center gap-3 my-5">
            <div className="flex-1 h-px bg-gray-100" />
            <span className="text-xs text-gray-400">ou cadastre-se com e-mail</span>
            <div className="flex-1 h-px bg-gray-100" />
          </div>

          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-600 mb-1.5">Nome</label>
                <input {...register('firstName')} placeholder="João" className="input-field" />
                {errors.firstName && <p className="text-red-400 text-xs mt-1">{errors.firstName.message}</p>}
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-600 mb-1.5">Sobrenome</label>
                <input {...register('lastName')} placeholder="Silva" className="input-field" />
                {errors.lastName && <p className="text-red-400 text-xs mt-1">{errors.lastName.message}</p>}
              </div>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-600 mb-1.5">E-mail</label>
              <input {...register('email')} type="email" placeholder="seu@email.com" className="input-field" />
              {errors.email && <p className="text-red-400 text-xs mt-1">{errors.email.message}</p>}
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-600 mb-1.5">Senha</label>
              <PasswordInput {...register('password')} placeholder="Mínimo 8 caracteres" className="input-field" />
              {errors.password && <p className="text-red-400 text-xs mt-1">{errors.password.message}</p>}
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-600 mb-1.5">Confirmar senha</label>
              <PasswordInput {...register('confirmPassword')} placeholder="••••••••" className="input-field" />
              {errors.confirmPassword && <p className="text-red-400 text-xs mt-1">{errors.confirmPassword.message}</p>}
            </div>

            <button type="submit" disabled={loading} className="btn-primary w-full py-3 mt-2">
              {loading ? 'Criando conta...' : 'Criar conta gratuita'}
            </button>
          </form>

          <div className="divider mt-6 pt-6">
            <p className="text-center text-sm text-gray-400">
              Já tem conta?{' '}
              <Link href={returnUrl ? `/login?returnUrl=${encodeURIComponent(returnUrl)}` : '/login'}
                className="text-brand-600 font-medium hover:text-brand-700 transition-colors">
                Entrar
              </Link>
            </p>
          </div>
        </div>

        <p className="text-center text-xs text-gray-400 mt-6">
          Ao criar uma conta você concorda com nossos Termos de Uso.
        </p>
      </div>
    </div>
  );
}

export default function RegisterPage() {
  return (
    <Suspense fallback={null}>
      <RegisterForm />
    </Suspense>
  );
}
