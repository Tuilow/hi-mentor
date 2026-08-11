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
import { Rocket, Users, Sparkles } from 'lucide-react';
import { authApi, setAccessToken } from '@/lib/api';
import { PasswordInput } from '@/components/ui/PasswordInput';
import Logo from '@/components/brand/Logo';

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
  // Preenchido quando a pessoa vem do aviso de "e-mail já cadastrado" no formulário de
  // registro (ver app/(auth)/registro/page.tsx) — poupa digitar o e-mail de novo.
  const prefillEmail = searchParams.get('email') ?? '';
  const [loading, setLoading] = useState(false);
  const [googleLoading, setGoogleLoading] = useState(false);

  const { register, handleSubmit, formState: { errors } } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: { email: prefillEmail },
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
      // setAccessToken também grava o cookie "has_session" (ver lib/api.ts) que o middleware
      // usa pra liberar as rotas protegidas — precisa existir ANTES do goHome() abaixo.
      setAccessToken(res.data.accessToken);
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
      setAccessToken(res.data.accessToken);
      toast.success('Bem-vindo!');
      await goHome();
    } catch {
      toast.error('Falha no login com Google. Tente novamente.');
    } finally {
      setGoogleLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex">
      {/* Painel esquerdo — marca */}
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

        <div className="relative space-y-8">
          <div>
            <h1 className="text-4xl font-bold text-white leading-tight mb-4" style={{ letterSpacing: '-0.03em' }}>
              Mentoria que<br />transforma carreiras.
            </h1>
            <p className="text-violet-200 text-lg leading-relaxed max-w-sm">
              Estruture programas, acompanhe o progresso e crie conexões reais com seus mentorados.
            </p>
          </div>

          <div className="flex gap-6">
            {[
              { icon: Rocket, label: 'Publique sem mensalidade' },
              { icon: Users, label: 'Acompanhe seus mentorados' },
              { icon: Sparkles, label: 'Receba pelos seus programas' },
            ].map(({ icon: Icon, label }) => (
              <div key={label} className="flex flex-col items-center text-center gap-2 max-w-[90px]">
                <div className="w-9 h-9 rounded-xl bg-white/10 flex items-center justify-center">
                  <Icon size={16} className="text-white" />
                </div>
                <span className="text-xs text-violet-200 leading-snug">{label}</span>
              </div>
            ))}
          </div>
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
              Bom ter você de volta
            </h2>
            <p className="text-ink-3 text-sm">Entre com sua conta para continuar</p>
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
                  onError={() => toast.error('Login com Google cancelado.')}
                  theme="filled_black"
                  size="large"
                  width="368"
                  text="signin_with"
                  locale="pt-BR"
                />
              </div>
            )}

            <div className="relative flex items-center gap-3 py-1 my-5">
              <hr className="flex-1 border-line" />
              <span className="text-xs text-ink-4 font-medium shrink-0">ou continue com e-mail</span>
              <hr className="flex-1 border-line" />
            </div>

            <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
              <div>
                <label className="block text-sm font-semibold text-ink-2 mb-1.5">E-mail</label>
                <input {...register('email')} type="email" placeholder="voce@email.com"
                  className="input-field" />
                {errors.email && <p className="text-red-500 text-xs mt-1.5">{errors.email.message}</p>}
              </div>

              <div>
                <div className="flex items-center justify-between mb-1.5">
                  <label className="block text-sm font-semibold text-ink-2">Senha</label>
                  <Link href="/esqueci-senha"
                    className="text-xs font-medium text-violet-600 hover:text-violet-700 transition-colors">
                    Esqueceu a senha?
                  </Link>
                </div>
                <PasswordInput {...register('password')} placeholder="••••••••"
                  className="input-field" />
                {errors.password && <p className="text-red-500 text-xs mt-1.5">{errors.password.message}</p>}
              </div>

              <button type="submit" disabled={loading} className="btn-primary w-full py-3">
                {loading ? 'Entrando...' : 'Entrar'}
              </button>
            </form>

            <div className="divider mt-6 pt-6">
              <p className="text-center text-sm text-ink-3">
                Não tem conta?{' '}
                <Link href={returnUrl ? `/registro?returnUrl=${encodeURIComponent(returnUrl)}` : '/registro'}
                  className="font-semibold text-violet-600 hover:text-violet-700 transition-colors">
                  Cadastre-se grátis
                </Link>
              </p>
            </div>
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
