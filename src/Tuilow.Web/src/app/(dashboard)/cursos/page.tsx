'use client';

import { useState } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { coursesApi, enrollmentsApi } from '@/lib/api';
import { Course, MyEnrollment } from '@/types';
import { commercializationLabel, isFreeState } from '@/lib/courseCommercialization';
import toast from 'react-hot-toast';
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

type Tab = 'all' | 'enrolled';

export default function CoursesPage() {
  const qc = useQueryClient();
  const [search, setSearch] = useState('');
  const [level,  setLevel]  = useState('');
  const [tab, setTab] = useState<Tab>('all');

  const { data, isLoading } = useQuery({
    queryKey: ['courses', search, level],
    queryFn: () => coursesApi.list({ search, level: level || undefined }).then(r => r.data),
    enabled: tab === 'all',
  });

  // Fonte de verdade de "em quais cursos eu já estou matriculado" — o campo course.isEnrolled
  // do catálogo nunca é preenchido pelo backend (GET /courses não conhece o usuário logado),
  // então usamos GET /enrollments/me (Learning) tanto para marcar os cards do catálogo quanto
  // para popular a aba "Meus cursos".
  const { data: myEnrollments = [], isLoading: loadingEnrollments } = useQuery<MyEnrollment[]>({
    queryKey: ['my-enrollments'],
    queryFn: () => enrollmentsApi.getMyEnrollments().then(r => r.data),
  });
  const enrolledIds = new Set(myEnrollments.map(e => e.courseId));

  // Matrícula direta só se aplica a cursos Grátis. Para cursos pagos (Compra Única ou
  // Assinatura), o botão leva para a página do curso — só lá dá pra saber qual dos dois
  // modelos se aplica (existência de Plan.CourseId) e mostrar o botão certo (Comprar/Assinar),
  // sem chutar/assumir que todo curso pago exige assinatura.
  const enroll = async (courseId: string) => {
    try {
      await enrollmentsApi.enroll(courseId);
      toast.success('Matrícula realizada! Bom estudo! 🎓');
      // Sem isso, o card continuava mostrando "Matricular" até um refresh manual da página —
      // era exatamente o "não aparece que estou matriculado" relatado.
      qc.invalidateQueries({ queryKey: ['my-enrollments'] });
      qc.invalidateQueries({ queryKey: ['courses'] });
    } catch {
      toast.error('Erro ao realizar matrícula.');
    }
  };

  return (
    <div className="max-w-5xl mx-auto">
      <div className="mb-8">
        <h1 className="text-2xl font-bold text-gray-800">Cursos</h1>
        <p className="text-gray-500 mt-1">Explore nosso catálogo de cursos.</p>
      </div>

      {/* Abas: Todos os cursos x Meus cursos (matriculados) */}
      <div className="flex items-center gap-1 mb-6 border-b border-gray-200">
        {([
          { id: 'all' as const, label: 'Todos os cursos' },
          { id: 'enrolled' as const, label: `Meus cursos${myEnrollments.length > 0 ? ` (${myEnrollments.length})` : ''}` },
        ]).map(t => (
          <button
            key={t.id}
            onClick={() => setTab(t.id)}
            className={`px-4 py-2.5 text-sm font-medium border-b-2 -mb-px transition-colors
              ${tab === t.id
                ? 'border-brand-600 text-brand-700'
                : 'border-transparent text-gray-400 hover:text-gray-600'}`}
          >
            {t.label}
          </button>
        ))}
      </div>

      {tab === 'all' && (
        <>
          {/* Filtros */}
          <div className="flex flex-col sm:flex-row gap-3 mb-8">
            <input
              value={search}
              onChange={e => setSearch(e.target.value)}
              placeholder="Buscar cursos..."
              className="input-field sm:max-w-xs"
            />
            <select
              value={level}
              onChange={e => setLevel(e.target.value)}
              className="input-field sm:max-w-[180px]"
            >
              <option value="">Todos os níveis</option>
              <option value="Beginner">Iniciante</option>
              <option value="Intermediate">Intermediário</option>
              <option value="Advanced">Avançado</option>
            </select>
          </div>

          {isLoading && (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
              {[1,2,3,4,5,6].map(i => (
                <div key={i} className="card border-gray-200 animate-pulse">
                  <div className="w-full h-40 bg-gray-100 rounded-lg mb-4" />
                  <div className="h-4 bg-gray-100 rounded w-1/3 mb-3" />
                  <div className="h-5 bg-gray-100 rounded w-3/4 mb-2" />
                  <div className="h-4 bg-gray-100 rounded w-full" />
                </div>
              ))}
            </div>
          )}

          {!isLoading && data?.items?.length === 0 && (
            <div className="text-center py-16 text-gray-400">
              <p className="text-4xl mb-4">📚</p>
              <p className="text-lg font-medium">Nenhum curso encontrado</p>
              <p className="text-sm mt-1">Tente ajustar os filtros de busca.</p>
            </div>
          )}

          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
            {data?.items?.map((course: Course) => {
              const isEnrolled = enrolledIds.has(course.id);
              // Nunca deriva "grátis" de course.isFree isolado — ver courseCommercialization.ts.
              const isCourseFree = isFreeState(course.commercializationState, course.isFree);
              return (
                <div key={course.id}
                  className="card border-gray-200 hover:border-gray-300 transition-all duration-200 flex flex-col">
                  {course.thumbnailUrl ? (
                    <img src={course.thumbnailUrl} alt={course.title}
                      className="w-full h-40 object-cover rounded-lg mb-4" />
                  ) : (
                    <div className="w-full h-40 bg-gray-100 rounded-lg mb-4 flex items-center justify-center text-4xl">
                      📚
                    </div>
                  )}

                  <div className="flex items-center gap-2 mb-3">
                    <span className={levelColor[course.level] || 'badge-purple'}>
                      {levelLabel[course.level] ?? course.level}
                    </span>
                    {isCourseFree && <span className="badge-green">Grátis</span>}
                    {isEnrolled && <span className="badge-green">Matriculado</span>}
                  </div>

                  <h3 className="font-semibold text-gray-800 mb-1 line-clamp-2">{course.title}</h3>
                  <p className="text-sm text-gray-500 mb-4 line-clamp-2 flex-1">{course.shortDescription}</p>

                  <div className="flex items-center justify-between mt-auto pt-4 border-t border-gray-200">
                    <span className="text-xs text-gray-400">
                      {course.totalLessons} aulas
                      {!isCourseFree && (
                        <> · {commercializationLabel(course.commercializationState, course.price, course.isFree)}</>
                      )}
                    </span>
                    {isEnrolled ? (
                      <Link href={`/cursos/${course.slug}`}
                        className="text-sm text-brand-600 font-medium hover:text-brand-700 transition-colors">
                        Continuar →
                      </Link>
                    ) : isCourseFree ? (
                      <button onClick={() => enroll(course.id)} className="btn-primary text-xs py-1.5 px-4">
                        Matricular
                      </button>
                    ) : (
                      <Link href={`/cursos/${course.slug}`} className="btn-primary text-xs py-1.5 px-4">
                        Ver curso
                      </Link>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        </>
      )}

      {tab === 'enrolled' && (
        <>
          {loadingEnrollments && (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
              {[1,2,3].map(i => (
                <div key={i} className="card border-gray-200 animate-pulse">
                  <div className="w-full h-40 bg-gray-100 rounded-lg mb-4" />
                  <div className="h-4 bg-gray-100 rounded w-1/3 mb-3" />
                  <div className="h-5 bg-gray-100 rounded w-3/4 mb-2" />
                </div>
              ))}
            </div>
          )}

          {!loadingEnrollments && myEnrollments.length === 0 && (
            <div className="text-center py-16 text-gray-400">
              <p className="text-4xl mb-4">🎓</p>
              <p className="text-lg font-medium">Você ainda não está matriculado em nenhum curso</p>
              <button onClick={() => setTab('all')} className="text-brand-600 hover:underline text-sm mt-2">
                Explorar catálogo →
              </button>
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
        </>
      )}
    </div>
  );
}
