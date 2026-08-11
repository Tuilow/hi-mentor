'use client';

import { useQuery } from '@tanstack/react-query';
import { enrollmentsApi } from '@/lib/api';
import { MyEnrollment } from '@/types';
import Link from 'next/link';
import { GraduationCap, BookOpen } from 'lucide-react';
import { Card, Badge, Progress, EmptyState } from '@/components/ui/design-system';

const levelLabel: Record<string, string> = {
  Beginner: 'Iniciante',
  Intermediate: 'Intermediário',
  Advanced: 'Avançado',
};

const levelVariant: Record<string, 'active' | 'done' | 'warning'> = {
  Beginner: 'active',
  Intermediate: 'done',
  Advanced: 'warning',
};

/**
 * "Minha Jornada" — Problema 2 da sprint de regras de negócio do Catálogo/Biblioteca. Separação
 * COMPLETA de conceitos: esta tela NUNCA mistura vitrine/catálogo público com o que o mentorado
 * realmente possui — mostra exclusivamente os programas aos quais ele tem acesso (compra,
 * matrícula, assinatura ativa ou liberação manual futura), a mesma lista já usada pela aba
 * "Meus cursos" de /cursos (GET /enrollments/me — GetMyEnrollmentsQueryHandler), aqui como
 * destino próprio (sem abas, sem catálogo, sem CTA de "explorar"), para que o link do menu
 * "Minha Jornada" (ver (dashboard)/layout.tsx) leve direto para uma experiência "só o que posso
 * assistir", nos moldes de uma biblioteca estilo Netflix.
 *
 * Nenhuma regra de acesso nova aqui: Enrollment já É o registro de acesso da plataforma (ver
 * IUserCourseAccessService, SharedKernel.Application) — esta tela só lista, nunca decide acesso.
 *
 * Redesign "Hi Mentor" (ago/2026): cards seguindo o padrão visual de
 * hi-mentor/src/pages/MentoradoHome.tsx — mesma query única (my-enrollments), sem lógica nova.
 */
export default function MyCoursesLibraryPage() {
  const { data: myEnrollments = [], isLoading } = useQuery<MyEnrollment[]>({
    queryKey: ['my-enrollments'],
    queryFn: () => enrollmentsApi.getMyEnrollments().then(r => r.data),
  });

  return (
    <div className="max-w-5xl mx-auto">
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-ink mb-1" style={{ letterSpacing: '-0.03em' }}>Minha Jornada</h1>
        <p className="text-ink-3">Os programas aos quais você tem acesso — só o que você pode assistir.</p>
      </div>

      {isLoading && (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
          {[1, 2, 3].map(i => (
            <Card key={i} className="animate-pulse">
              <div className="w-full h-40 bg-subtle rounded-xl mb-4" />
              <div className="h-4 bg-subtle rounded w-1/3 mb-3" />
              <div className="h-5 bg-subtle rounded w-3/4 mb-2" />
            </Card>
          ))}
        </div>
      )}

      {!isLoading && myEnrollments.length === 0 && (
        <EmptyState
          icon={<GraduationCap size={28} />}
          title="Você ainda não tem acesso a nenhum programa"
          description="Programas comprados, matriculados ou assinados aparecem aqui assim que liberados."
        />
      )}

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
        {myEnrollments.map((e) => (
          <Link key={e.enrollmentId} href={`/cursos/${e.slug}`}>
            <Card hover padding="none" className="overflow-hidden flex flex-col h-full">
              <div className="h-40 relative overflow-hidden bg-subtle">
                {e.thumbnailUrl ? (
                  // eslint-disable-next-line @next/next/no-img-element
                  <img src={e.thumbnailUrl} alt={e.title} className="absolute inset-0 w-full h-full object-cover" />
                ) : (
                  <div className="absolute inset-0 flex items-center justify-center text-ink-3">
                    <BookOpen size={28} />
                  </div>
                )}
              </div>
              <div className="p-4 flex flex-col flex-1">
                <div className="flex items-center gap-2 mb-3 flex-wrap">
                  <Badge variant={levelVariant[e.level] ?? 'neutral'}>{levelLabel[e.level] ?? e.level}</Badge>
                  {e.status === 'Completed' && <Badge variant="active">Concluído</Badge>}
                </div>

                <h3 className="font-bold text-ink mb-1 line-clamp-2 flex-1">{e.title}</h3>

                <div className="mt-auto pt-4 border-t border-line-light">
                  <div className="flex items-center justify-between text-xs text-ink-3 mb-1.5">
                    <span>Progresso</span>
                    <span>{Math.round(e.progressPercentage)}%</span>
                  </div>
                  <Progress value={e.progressPercentage} size="sm" variant={e.status === 'Completed' ? 'success' : 'primary'} />
                </div>
              </div>
            </Card>
          </Link>
        ))}
      </div>
    </div>
  );
}
