'use client';

import { Suspense, useState } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import Link from 'next/link';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import toast from 'react-hot-toast';
import { AxiosError } from 'axios';
import { authApi } from '@/lib/api';

// Mesmo padrão de app/(auth)/registro/page.tsx: o backend (ExceptionHandlingMiddleware) devolve
// a mensagem real do erro em "title" — aqui é como a tela sabe dizer "link expirado" em vez de
// um genérico "ocorreu um erro" (ver ResetPasswordCommandHandler / User.ResetPassword).
const errorMessage = (e: unknown, fallback: string) => {
  const data = (e as AxiosError<{ title?: string; detail?: string; message?: string; errors?: Record<string, string[]> }>)
    .response?.data;
  const firstFieldError = data?.errors && Object.values(data.errors).flat()[0];
  return firstFieldError ?? data?.detail ?? data?.title ?? data?.message ?? fallback;
};

const schema = z.object({
  password: z.string().min(8, 'Mínimo 8 caracteres'),
  confirmPassword: z.string(),
}).refine(d => d.password === d.confirmPassword, {
  message: 'Senhas não coincidem',
  path: ['confirmPassword'],
});

type FormData = z.infer<typeof schema>;

/**
 * Consumo do link de redefinição de senha enviado por e-mail (ver
 * EmailService.SendPasswordResetAsync: "${_frontendUrl}/redefinir-senha?token=..."). Fora do
 * grupo (auth) de propósito — mesma decisão já tomada em app/confirmar-email, página avulsa
 * acessada só a partir do link do e-mail, sem layout de dashboard.
 */
function ResetPasswordForm() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const token = searchParams.get('token');
  const [loading, setLoading] = useState(false);
  const [done, setDone] = useState(false);

  const { register, handleSubmit, formState: { errors } } = useForm<FormData>({
    resolver: zodResolver(schema),
  });

  const onSubmit = async (data: FormData) => {
    if (!token) return;
    try {
      setLoading(true);
      await authApi.resetPassword(token, data.password);
      setDone(true);
      toast.success('Senha redefinida com sucesso!');
      setTimeout(() => router.push('/login'), 1500);
    } catch (err) {
      toast.error(errorMessage(err, 'Não foi possível redefinir sua senha. Tente novamente.'));
    } finally {
      setLoading(false);
    }
  };

  if (!token) {
    return (
      <div className="min-h-screen bg-white flex flex-col items-center justify-center gap-3 px-4 text-center">
        <p className="text-4xl">🔗</p>
        <p className="text-lg font-medium text-gray-700">Link inválido</p>
        <p className="text-sm text-gray-400 max-w-xs">
          Este link de redefinição de senha está incompleto ou não foi aberto corretamente.
        </p>
        <Link href="/esqueci-senha" className="btn-primary px-6 py-2.5 mt-2">
          Solicitar novo link →
        </Link>
      </div>
    );
  }

  if (done) {
    return (
      <div className="min-h-screen bg-white flex flex-col items-center justify-center gap-3 px-4 text-center">
        <p className="text-4xl">🎉</p>
        <p className="text-lg font-medium text-gray-700">Senha redefinida com sucesso!</p>
        <p className="text-sm text-gray-400 max-w-xs">Redirecionando para o login...</p>
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
          <p className="text-gray-500 mt-2 text-sm">Escolha sua nova senha</p>
        </div>

        <div className="card border-gray-200">
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
            <div>
              <label className="block text-sm font-medium text-gray-600 mb-1.5">Nova senha</label>
              <input {...register('password')} type="password" placeholder="••••••••"
                className="input-field" autoFocus />
              {errors.password && <p className="text-red-400 text-xs mt-1.5">{errors.password.message}</p>}
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-600 mb-1.5">Confirmar nova senha</label>
              <input {...register('confirmPassword')} type="password" placeholder="••••••••"
                className="input-field" />
              {errors.confirmPassword && <p className="text-red-400 text-xs mt-1.5">{errors.confirmPassword.message}</p>}
            </div>

            <button type="submit" disabled={loading} className="btn-primary w-full py-3">
              {loading ? 'Redefinindo...' : 'Redefinir senha'}
            </button>
          </form>
        </div>
      </div>
    </div>
  );
}

// useSearchParams exige um limite de Suspense (mesmo caso de app/(auth)/login/page.tsx e
// app/confirmar-email/page.tsx) — sem isso o Next.js falha o build estático desta página.
export default function RedefinirSenhaPage() {
  return (
    <Suspense fallback={null}>
      <ResetPasswordForm />
    </Suspense>
  );
}
