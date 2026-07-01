'use client';

import { useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import { useQuery } from '@tanstack/react-query';
import { coursesApi, enrollmentsApi } from '@/lib/api';
import Link from 'next/link';
import toast from 'react-hot-toast';

const levelLabel: Record<string, string> = {
  Beginner: 'Iniciante',
  Intermediate: 'Intermediário',
  Advanced: 'Avançado',
};

interface Lesson {
  id: string;
  title: string;
  description?: string;
  order: number;
  durationSeconds?: number;
  isPreview: boolean;
  hasVideo: boolean;
}

interface Module {
  id: string;
  title: string;
  description?: string;
  order: number;
  lessons: Lesson[];
}

interface CourseDetail {
  id: string;
  title: string;
  slug: string;
  description?: string;
  shortDescription?: string;
  thumbnailUrl?: string;
  price: number;
  isFree: boolean;
  level: string;
  totalDurationMinutes?: number;
  publishedAt?: string;
  modules: Module[];
}

function formatDuration(seconds?: number): string {
  if (!seconds) return '';
  const m = Math.floor(seconds / 60);
  const s = seconds % 60;
  return m > 0 ? `${m}min${s > 0 ? ` ${s}s` : ''}` : `${s}s`;
}

function formatPrice(price: number): string {
  return price === 0
    ? 'Grátis'
    : price.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

export default function CourseDetailPage() {
  const { slug } = useParams<{ slug: string }>();
  const router = useRouter();
  const [openModules, setOpenModules] = useState<Set<string>>(new Set());
  const [enrolling, setEnrolling] = useState(false);

  const { data: course, isLoading } = useQuery<CourseDetail>({
    queryKey: ['course', slug],
    queryFn: () => coursesApi.getBySlug(slug).then(r => r.data),
    enabled: !!slug,
  });

  const toggleModule = (id: string) =>
    setOpenModules(prev => {
      const next = new Set(prev);
      next.has(id) ? next.delete(id) : next.add(id);
      return next;
    });

  const handleEnroll = async () => {
    if (!course) return;
    setEnrolling(true);
    try {
      await enrollmentsApi.enroll(course.id);
      toast.success('Matrícula realizada! Bom estudo! 🎓');
      router.refresh();
    } catch (e: unknown) {
      const status = (e as { response?: { status?: number } })?.response?.status;
      if (status === 402 || status === 403)
        toast.error('Você precisa de uma assinatura ativa para este curso.');
      else
        toast.error('Erro ao realizar matrícula.');
    } finally {
      setEnrolling(false);
    }
  };

  if (isLoading) {
    return (
      <div className="max-w-4xl mx-auto animate-pulse space-y-6">
        <div className="h-8 bg-gray-100 rounded w-2/3" />
        <div className="h-56 bg-gray-100 rounded-xl" />
        <div className="space-y-3">
          {[1,2,3].map(i => <div key={i} className="h-14 bg-gray-100 rounded-lg" />)}
        </div>
      </div>
    );
  }

  if (!course) {
    return (
      <div className="text-center py-20 text-gray-400">
        <p className="text-4xl mb-3">😕</p>
        <p className="text-lg font-medium">Curso não encontrado</p>
        <Link href="/cursos" className="text-brand-600 hover:underline text-sm mt-2 inline-block">
          ← Voltar para cursos
        </Link>
      </div>
    );
  }

  const totalLessons = course.modules.reduce((acc, m) => acc + m.lessons.length, 0);

  return (
    <div className="max-w-4xl mx-auto">
      {/* Breadcrumb */}
      <div className="flex items-center gap-2 text-sm text-gray-400 mb-6">
        <Link href="/cursos" className="hover:text-gray-600 transition-colors">Cursos</Link>
        <span>›</span>
        <span className="text-gray-600 truncate">{course.title}</span>
      </div>

      {/* Hero */}
      <div className="card border-gray-200 mb-6 overflow-hidden p-0">
        {course.thumbnailUrl ? (
          <img src={course.thumbnailUrl} alt={course.title}
            className="w-full h-56 object-cover" />
        ) : (
          <div className="w-full h-56 bg-gradient-to-br from-gray-100 to-gray-50
                          flex items-center justify-center text-6xl">
            📚
          </div>
        )}
        <div className="p-6">
          <div className="flex flex-wrap gap-2 mb-3">
            <span className="badge-purple">{levelLabel[course.level] ?? course.level}</span>
            {course.isFree && <span className="badge-green">Grátis</span>}
          </div>
          <h1 className="text-2xl font-bold text-gray-800 mb-2">{course.title}</h1>
          {course.description && (
            <p className="text-gray-500 text-sm leading-relaxed mb-4">{course.description}</p>
          )}

          <div className="flex flex-wrap items-center gap-6 pt-4 border-t border-gray-200">
            <div className="text-center">
              <p className="text-lg font-bold text-gray-800">{totalLessons}</p>
              <p className="text-xs text-gray-400">Aulas</p>
            </div>
            <div className="text-center">
              <p className="text-lg font-bold text-gray-800">{course.modules.length}</p>
              <p className="text-xs text-gray-400">Módulos</p>
            </div>
            {course.totalDurationMinutes && (
              <div className="text-center">
                <p className="text-lg font-bold text-gray-800">{course.totalDurationMinutes}min</p>
                <p className="text-xs text-gray-400">Duração total</p>
              </div>
            )}
            <div className="ml-auto">
              <p className="text-2xl font-bold gradient-text">{formatPrice(course.price)}</p>
            </div>
          </div>

          <div className="mt-4">
            <button
              onClick={handleEnroll}
              disabled={enrolling}
              className="btn-primary w-full sm:w-auto disabled:opacity-50"
            >
              {enrolling ? 'Matriculando...' : 'Matricular agora →'}
            </button>
          </div>
        </div>
      </div>

      {/* Conteúdo do curso */}
      <div>
        <h2 className="text-lg font-semibold text-gray-800 mb-4">Conteúdo do curso</h2>
        <div className="space-y-2">
          {course.modules
            .sort((a, b) => a.order - b.order)
            .map(module => {
              const isOpen = openModules.has(module.id);
              const previewCount = module.lessons.filter(l => l.isPreview).length;
              return (
                <div key={module.id} className="card border-gray-200 p-0 overflow-hidden">
                  {/* Módulo header */}
                  <button
                    onClick={() => toggleModule(module.id)}
                    className="w-full flex items-center justify-between px-5 py-4
                               hover:bg-gray-100/50 transition-colors text-left"
                  >
                    <div className="flex items-center gap-3">
                      <span className="text-xs font-medium text-gray-400 w-5">
                        {module.order}
                      </span>
                      <div>
                        <p className="text-sm font-semibold text-gray-800">{module.title}</p>
                        <p className="text-xs text-gray-400 mt-0.5">
                          {module.lessons.length} aulas
                          {previewCount > 0 && ` · ${previewCount} gratuita${previewCount > 1 ? 's' : ''}`}
                        </p>
                      </div>
                    </div>
                    <span className="text-gray-400 transition-transform duration-200"
                          style={{ transform: isOpen ? 'rotate(180deg)' : 'rotate(0)' }}>
                      ▾
                    </span>
                  </button>

                  {/* Aulas */}
                  {isOpen && (
                    <div className="border-t border-gray-200">
                      {module.lessons
                        .sort((a, b) => a.order - b.order)
                        .map((lesson, idx) => (
                          <div key={lesson.id}
                            className="flex items-center gap-3 px-5 py-3
                                       border-b border-gray-200/50 last:border-0
                                       hover:bg-gray-100/30 transition-colors group">
                            <span className="text-xs text-gray-400 w-5 flex-shrink-0">
                              {idx + 1}
                            </span>
                            <span className="text-base flex-shrink-0">
                              {lesson.hasVideo
                                ? (lesson.isPreview ? '▶️' : '🔒')
                                : '📝'}
                            </span>
                            <div className="flex-1 min-w-0">
                              <p className="text-sm text-gray-700 truncate">{lesson.title}</p>
                              {lesson.durationSeconds && (
                                <p className="text-xs text-gray-400">
                                  {formatDuration(lesson.durationSeconds)}
                                </p>
                              )}
                            </div>
                            {lesson.isPreview && lesson.hasVideo && (
                              <Link
                                href={`/cursos/${slug}/${lesson.id}`}
                                className="text-xs text-brand-600 font-medium
                                           opacity-0 group-hover:opacity-100
                                           transition-opacity hover:text-brand-700"
                              >
                                Assistir →
                              </Link>
                            )}
                            {!lesson.isPreview && (
                              <span className="text-xs text-gray-400">Assinantes</span>
                            )}
                          </div>
                        ))}
                    </div>
                  )}
                </div>
              );
            })}
        </div>
      </div>
    </div>
  );
}
