'use client';

import { Suspense, useState } from 'react';
import { useSearchParams } from 'next/navigation';
import Link from 'next/link';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import toast from 'react-hot-toast';
import { AxiosError } from 'axios';
import { authApi } from '@/lib/api';

// Mesmo padrão de app/(auth)/registro/page.tsx: o backend (ExceptionHandlingMiddleware) devolve
// a mensagem real do erro em "title" (e validações de campo em "errors").
const errorMessage = (e: unknown, fallback: string) => {
  const data = (e as AxiosError<{ title?: string; detail?: string; message?: string; errors?: Record<string, string[]> }>)
    .response?.data;
  const firstFieldError = data?.errors && Object.values(data.errors).flat()[0];
  return firstFieldError ?? data?.detail ?? data?.title ?? data?.message ?? fallback;
};

const schema = z.object({
  email: z.string().email('E-mail inválido'),
});

type FormData = z.infer<typeof schema>;

function EsqueciSenhaForm() {
  const searchParams = useSearchParams();
  // Preenchido quando a pessoa vem do aviso de "e-mail já cadastrado" no formulário de
  // registro (ver app/(auth)/registro/page.tsx) — poupa digitar o e-mail de novo.
  const prefillEmail = searchParams.get('email') ?? '';
  const [loading, setLoading] = useState(false);
  const [sent, setSent] = useState(false);

  const { register, handleSubmit, formState: { errors } } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: { email: prefillEmail },
  });

  const onSubmit = async (data: FormData) => {
    try {
      setLoading(true);
      // O backend nunca revela se o e-mail existe (ForgotPasswordCommandHandler) — por isso a
      // tela sempre mostra a mesma confirmação genérica, mesmo que a chamada falhe por outro
      // motivo (ex.: rede). Isso é intencional (evita enumeração de contas).
      await authApi.forgotPassword(data.email);
      setSent(true);
    } catch (err) {
      toast.error(errorMessage(err, 'Não foi possível enviar o e-mail agora. Tente novamente.'));
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-white flex items-center justify-center px-4">
      <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2
                      w-96 h-96 bg-brand-700/10 rounded-full blur-3xl pointer-events-none" />

      <div className="relative w-full max-w-md animate-fade-in">
        <div className="text-center mb-8">
          <Link href="/" className="inline-block">
            <h1 className="text-2xl font-bold gradient-text">🎓 Tuilow</h1>
          </Link>
          <p className="text-gray-500 mt-2 text-sm">Recuperar acesso</p>
        </div>

        <div className="card border-gray-200">
          {sent ? (
            <div className="text-center py-2">
              <p className="text-4xl mb-3">📧</p>
              <p className="text-gray-700 font-medium">Se este e-mail existe, você receberá as instruções em breve.</p>
              <p className="text-sm text-gray-400 mt-2">
                Verifique sua caixa de entrada (e o spam). O link expira em 1 hora.
              </p>
              <Link href="/login" className="btn-primary inline-block mt-6 px-6 py-2.5">
                Voltar para o login
              </Link>
            </div>
          ) : (
            <>
              <p className="text-sm text-gray-500 mb-5">
                Informe o e-mail da sua conta e enviaremos um link para redefinir sua senha.
              </p>
              <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
                <div>
                  <label className="block text-sm font-medium text-gray-600 mb-1.5">E-mail</label>
                  <input {...register('email')} type="email" placeholder="seu@email.com"
                    className="input-field" autoFocus />
                  {errors.email && <p className="text-red-400 text-xs mt-1.5">{errors.email.message}</p>}
                </div>

                <button type="submit" disabled={loading} className="btn-primary w-full py-3">
                  {loading ? 'Enviando...' : 'Enviar link de redefinição'}
                </button>
              </form>

              <div className="divider mt-6 pt-6">
                <p className="text-center text-sm text-gray-400">
                  Lembrou a senha?{' '}
                  <Link href="/login" className="text-brand-600 font-medium hover:text-brand-700 transition-colors">
                    Voltar para o login
                  </Link>
                </p>
              </div>
            </>
          )}
        </div>
      </div>
    </div>
  );
}

export default function EsqueciSenhaPage() {
  return (
    <Suspense fallback={null}>
      <EsqueciSenhaForm />
    </Suspense>
  );
}
