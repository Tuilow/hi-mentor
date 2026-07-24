'use client';

import { useQuery } from '@tanstack/react-query';
import { enrollmentsApi } from '@/lib/api';
import { MyEnrollment } from '@/types';
import Link from 'next/link';

const levelLabel: Record<string, string> = {
  Beginner: 'Iniciante',
  Intermediate: 'Intermediário',
  Advanced: 'Avançado',
};

const levelColor: Record<string, string> = {
  Beginner: 'badge-green',
  Intermediate: 'badge-purple',
  Advanced: 'bg-accent-50 text-accent-700 border border-accent-200 badge',
};

/**
 * Biblioteca do usuário ("Meus Cursos") — Problema 2 da sprint de regras de negócio do
 * Catálogo/Biblioteca. Separação COMPLETA de conceitos: esta tela NUNCA mistura vitrine/catálogo
 * público com o que o usuário realmente possui — mostra exclusivamente os cursos aos quais ele
 * tem acesso (compra, matrícula, assinatura ativa ou liberação manual futura), a mesma lista já
 * usada pela aba "Meus cursos" de /cursos (GET /enrollments/me — GetMyEnrollmentsQueryHandler),
 * aqui como destino próprio (sem abas, sem catálogo, sem CTA de "explorar"), para que o link do
 * menu "Meus Cursos" (ver (dashboard)/layout.tsx) leve direto para uma experiência "só o que
 * posso assistir", nos moldes de uma biblioteca estilo Netflix.
 *
 * Nenhuma regra de acesso nova aqui: Enrollment já É o registro de acesso da plataforma (ver
 * IUserCourseAccessService, SharedKernel.Application) — esta tela só lista, nunca decide acesso.
 */
export default function MyCoursesLibraryPage() {
  const { data: myEnrollments = [], isLoading } = useQuery<MyEnrollment[]>({
    queryKey: ['my-enrollments'],
    queryFn: () => enrollmentsApi.getMyEnrollments().then(r => r.data),
  });

  return (
    <div className="max-w-5xl mx-auto">
      <div className="mb-8">
        <h1 className="text-2xl font-bold text-gray-800">Meus Cursos</h1>
        <p className="text-gray-500 mt-1">Os cursos aos quais você tem acesso — só o que você pode assistir.</p>
      </div>

      {isLoading && (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
          {[1, 2, 3].map(i => (
            <div key={i} className="card border-gray-200 animate-pulse">
              <div className="w-full h-40 bg-gray-100 rounded-lg mb-4" />
              <div className="h-4 bg-gray-100 rounded w-1/3 mb-3" />
              <div className="h-5 bg-gray-100 rounded w-3/4 mb-2" />
            </div>
          ))}
        </div>
      )}

      {!isLoading && myEnrollments.length === 0 && (
        <div className="text-center py-16 text-gray-400">
          <p className="text-4xl mb-4">🎓</p>
          <p className="text-lg font-medium">Você ainda não tem acesso a nenhum curso</p>
          <p className="text-sm mt-1">Cursos comprados, matriculados ou assinados aparecem aqui assim que liberados.</p>
        </div>
      )}

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
        {myEnrollments.map((e) => (
          <Link key={e.enrollmentId} href={`/cursos/${e.slug}`}
            className="card border-gray-200 hover:border-gray-300 transition-all duration-200 flex flex-col">
            {e.thumbnailUrl ? (
              <img src={e.thumbnailUrl} alt={e.title}
                className="w-full h-40 object-cover rounded-lg mb-4" />
            ) : (
              <div className="w-full h-40 bg-gray-100 rounded-lg mb-4 flex items-center justify-center text-4xl">
                📚
              </div>
            )}

            <div className="flex items-center gap-2 mb-3">
              <span className={levelColor[e.level] || 'badge-purple'}>
                {levelLabel[e.level] ?? e.level}
              </span>
              {e.status === 'Completed' && <span className="badge-green">Concluído</span>}
            </div>

            <h3 className="font-semibold text-gray-800 mb-1 line-clamp-2">{e.title}</h3>

            <div className="mt-auto pt-4 border-t border-gray-200">
              <div className="flex items-center justify-between text-xs text-gray-400 mb-1.5">
                <span>Progresso</span>
                <span>{Math.round(e.progressPercentage)}%</span>
              </div>
              <div className="w-full h-1.5 bg-gray-100 rounded-full overflow-hidden">
                <div className="h-full bg-brand-600 rounded-full"
                  style={{ width: `${Math.min(100, Math.max(0, e.progressPercentage))}%` }} />
              </div>
            </div>
          </Link>
        ))}
      </div>
    </div>
  );
}
