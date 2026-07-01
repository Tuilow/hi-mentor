'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import toast from 'react-hot-toast';
import { authApi } from '@/lib/api';

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

export default function RegisterPage() {
  const router = useRouter();
  const [loading, setLoading] = useState(false);

  const { register, handleSubmit, formState: { errors } } = useForm<FormData>({
    resolver: zodResolver(schema),
  });

  const onSubmit = async ({ confirmPassword: _, ...data }: FormData) => {
    try {
      setLoading(true);
      const res = await authApi.register(data);
      localStorage.setItem('access_token', res.data.accessToken);
      localStorage.setItem('refresh_token', res.data.refreshToken);
      toast.success('Conta criada! Verifique seu e-mail.');
      router.push('/dashboard');
    } catch {
      toast.error('Erro ao criar conta. Verifique os dados.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-zinc-950 flex items-center justify-center px-4 py-12">
      <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2
                      w-96 h-96 bg-purple-700/10 rounded-full blur-3xl pointer-events-none" />

      <div className="relative w-full max-w-md animate-fade-in">
        <div className="text-center mb-8">
          <Link href="/" className="inline-block">
            <h1 className="text-2xl font-bold gradient-text">🐕 DogMaster Pro</h1>
          </Link>
          <p className="text-zinc-400 mt-2 text-sm">Crie sua conta gratuita</p>
        </div>

        <div className="card border-zinc-800">
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-zinc-300 mb-1.5">Nome</label>
                <input {...register('firstName')} placeholder="João" className="input-field" />
                {errors.firstName && <p className="text-red-400 text-xs mt-1">{errors.firstName.message}</p>}
              </div>
              <div>
                <label className="block text-sm font-medium text-zinc-300 mb-1.5">Sobrenome</label>
                <input {...register('lastName')} placeholder="Silva" className="input-field" />
                {errors.lastName && <p className="text-red-400 text-xs mt-1">{errors.lastName.message}</p>}
              </div>
            </div>

            <div>
              <label className="block text-sm font-medium text-zinc-300 mb-1.5">E-mail</label>
              <input {...register('email')} type="email" placeholder="seu@email.com" className="input-field" />
              {errors.email && <p className="text-red-400 text-xs mt-1">{errors.email.message}</p>}
            </div>

            <div>
              <label className="block text-sm font-medium text-zinc-300 mb-1.5">Senha</label>
              <input {...register('password')} type="password" placeholder="Mínimo 8 caracteres" className="input-field" />
              {errors.password && <p className="text-red-400 text-xs mt-1">{errors.password.message}</p>}
            </div>

            <div>
              <label className="block text-sm font-medium text-zinc-300 mb-1.5">Confirmar senha</label>
              <input {...register('confirmPassword')} type="password" placeholder="••••••••" className="input-field" />
              {errors.confirmPassword && <p className="text-red-400 text-xs mt-1">{errors.confirmPassword.message}</p>}
            </div>

            <button type="submit" disabled={loading} className="btn-primary w-full py-3 mt-2">
              {loading ? 'Criando conta...' : 'Criar conta gratuita'}
            </button>
          </form>

          <div className="divider mt-6 pt-6">
            <p className="text-center text-sm text-zinc-500">
              Já tem conta?{' '}
              <Link href="/login" className="text-brand-400 font-medium hover:text-brand-300 transition-colors">
                Entrar
              </Link>
            </p>
          </div>
        </div>

        <p className="text-center text-xs text-zinc-600 mt-6">
          Ao criar uma conta você concorda com nossos Termos de Uso.
        </p>
      </div>
    </div>
  );
}
