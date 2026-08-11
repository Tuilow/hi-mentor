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
import { authApi, setAccessToken } from '@/lib/api';
import { PasswordInput } from '@/components/ui/PasswordInput';
import Logo from '@/components/brand/Logo';

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
  // Achado em teste manual: cadastro com e-mail já existente devolvia só um toast de erro
  // genérico (o texto real do backend nem chegava a aparecer, ver fix no
  // RegisterUserCommandHandler) e a pessoa ficava sem saber que já tinha conta. 422 é o único
  // status possível nesse endpoint pra esse caso (erros de campo saem como 400).
  const [duplicateEmail, setDuplicateEmail] = useState<string | null>(null);

  const { register, handleSubmit, formState: { errors } } = useForm<FormData>({
    resolver: zodResolver(schema),
  });

  const onSubmit = async ({ confirmPassword: _, ...data }: FormData) => {
    setDuplicateEmail(null);
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
      if (err instanceof AxiosError && err.response?.status === 422) {
        setDuplicateEmail(data.email);
        toast.error(errorMessage(err, 'Este e-mail já está cadastrado.'));
        return;
      }
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
      setAccessToken(res.data.accessToken);
      toast.success('Conta criada com Google!');
      router.push(returnUrl || '/dashboard');
    } catch (err) {
      toast.error(errorMessage(err, 'Falha no cadastro com Google. Tente novamente.'));
    } finally {
      setGoogleLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex">
      {/* Painel esquerdo — marca (mesmo padrão visual do login, ver (auth)/login/page.tsx) */}
      <div
        className="hidden lg:flex flex-col justify-between w-[46%] p-12 relative overflow-hidden"
        style={{
          background: 'linear-gradient(145deg, oklch(30% 0.18 293) 0%, oklch(44% 0.26 293) 45%, oklch(36% 0.2 280) 100%)',
        }}
      >
        <div className="absolute inset-0 pointer-events-none">
          <div
            className="absolute top-[-80px] right-[-80px] w-96 h-96 rounded-full opacity-20"
            style={{ background: 'radial-gradient(circle, oklch(80% 0.15 75) 0%, transparent 70%)' }}
          />
          <div
            className="absolute bottom-[-60px] left-[-60px] w-80 h-80 rounded-full opacity-15"
            style={{ background: 'radial-gradient(circle, oklch(75% 0.15 75) 0%, transparent 70%)' }}
          />
        </div>
        <div className="relative">
          <Logo size="lg" variant="full" onDark />
        </div>
        <div className="relative">
          <h1 className="text-4xl font-bold text-white leading-tight mb-4" style={{ letterSpacing: '-0.03em' }}>
            Comece sua<br />jornada de mentoria.
          </h1>
          <p className="text-violet-200 text-lg leading-relaxed max-w-sm">
            Publique programas, acompanhe mentorados e receba pelas suas mentorias — sem mensalidade.
          </p>
        </div>
        <p className="relative text-violet-400 text-sm">
          © {new Date().getFullYear()} Hi Mentor. Todos os direitos reservados.
        </p>
      </div>

      {/* Painel direito — formulário */}
      <div className="flex-1 flex items-center justify-center bg-canvas px-6 py-12">
        <div className="w-full max-w-[400px]">
          <div className="lg:hidden mb-8 flex justify-center">
            <Logo size="lg" variant="full" />
          </div>

          <div className="mb-8">
            <h2 className="text-2xl font-bold text-ink mb-1" style={{ letterSpacing: '-0.025em' }}>
              Crie sua conta
            </h2>
            <p className="text-ink-3 text-sm">Comece sua jornada no Hi Mentor</p>
          </div>

          <div className="card border-line">
            {googleLoading ? (
              <div className="w-full py-3 flex items-center justify-center gap-2 rounded-xl
                              border border-line bg-subtle/60 text-ink-3 text-sm">
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

            <div className="relative flex items-center gap-3 py-1 my-5">
              <hr className="flex-1 border-line" />
              <span className="text-xs text-ink-4 font-medium shrink-0">ou cadastre-se com e-mail</span>
              <hr className="flex-1 border-line" />
            </div>

            {duplicateEmail && (
              <div className="mb-4 p-3 rounded-xl bg-amber-50 border border-amber-200 text-sm text-amber-800">
                <p className="mb-2">Este e-mail já tem uma conta na Hi Mentor.</p>
                <div className="flex gap-2">
                  <Link href={`/login?email=${encodeURIComponent(duplicateEmail)}`}
                    className="btn-primary px-4 py-2 text-xs">
                    Entrar
                  </Link>
                  <Link href={`/esqueci-senha?email=${encodeURIComponent(duplicateEmail)}`}
                    className="px-4 py-2 text-xs border border-amber-300 rounded-xl hover:bg-amber-100 transition-colors">
                    Esqueci minha senha
                  </Link>
                </div>
              </div>
            )}

            <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-semibold text-ink-2 mb-1.5">Nome</label>
                  <input {...register('firstName')} placeholder="João" className="input-field" />
                  {errors.firstName && <p className="text-red-500 text-xs mt-1">{errors.firstName.message}</p>}
                </div>
                <div>
                  <label className="block text-sm font-semibold text-ink-2 mb-1.5">Sobrenome</label>
                  <input {...register('lastName')} placeholder="Silva" className="input-field" />
                  {errors.lastName && <p className="text-red-500 text-xs mt-1">{errors.lastName.message}</p>}
                </div>
              </div>

              <div>
                <label className="block text-sm font-semibold text-ink-2 mb-1.5">E-mail</label>
                <input {...register('email')} type="email" placeholder="seu@email.com" className="input-field" />
                {errors.email && <p className="text-red-500 text-xs mt-1">{errors.email.message}</p>}
              </div>

              <div>
                <label className="block text-sm font-semibold text-ink-2 mb-1.5">Senha</label>
                <PasswordInput {...register('password')} placeholder="Mínimo 8 caracteres" className="input-field" />
                {errors.password && <p className="text-red-500 text-xs mt-1">{errors.password.message}</p>}
              </div>

              <div>
                <label className="block text-sm font-semibold text-ink-2 mb-1.5">Confirmar senha</label>
                <PasswordInput {...register('confirmPassword')} placeholder="••••••••" className="input-field" />
                {errors.confirmPassword && <p className="text-red-500 text-xs mt-1">{errors.confirmPassword.message}</p>}
              </div>

              <button type="submit" disabled={loading} className="btn-primary w-full py-3 mt-2">
                {loading ? 'Criando conta...' : 'Criar conta gratuita'}
              </button>
            </form>

            <div className="divider mt-6 pt-6">
              <p className="text-center text-sm text-ink-3">
                Já tem conta?{' '}
                <Link href={returnUrl ? `/login?returnUrl=${encodeURIComponent(returnUrl)}` : '/login'}
                  className="font-semibold text-violet-600 hover:text-violet-700 transition-colors">
                  Entrar
                </Link>
              </p>
            </div>
          </div>

          <p className="text-center text-xs text-ink-4 mt-6">
            Ao criar uma conta você concorda com nossos Termos de Uso.
          </p>
        </div>
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
