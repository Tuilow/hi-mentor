'use client';

import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { coursesApi, enrollmentsApi } from '@/lib/api';
import { Course } from '@/types';
import toast from 'react-hot-toast';
import Link from 'next/link';

export default function CoursesPage() {
  const [search, setSearch] = useState('');
  const [level, setLevel] = useState('');

  const { data, isLoading } = useQuery({
    queryKey: ['courses', search, level],
    queryFn: () => coursesApi.list({ search, level: level || undefined }).then(r => r.data),
  });

  const enroll = async (courseId: string) => {
    try {
      await enrollmentsApi.enroll(courseId);
      toast.success('Matrícula realizada com sucesso!');
    } catch (e: any) {
      if (e.response?.status === 402)
        toast.error('É necessário ter uma assinatura ativa para se matricular.');
      else
        toast.error('Erro ao realizar matrícula.');
    }
  };

  return (
    <div className="max-w-5xl mx-auto">
      <h1 className="text-2xl font-bold text-gray-900 mb-6">Cursos</h1>

      {/* Filtros */}
      <div className="flex gap-4 mb-8">
        <input
          value={search}
          onChange={e => setSearch(e.target.value)}
          placeholder="Buscar cursos..."
          className="input-field max-w-sm"
        />
        <select value={level} onChange={e => setLevel(e.target.value)} className="input-field max-w-xs">
          <option value="">Todos os níveis</option>
          <option value="Beginner">Iniciante</option>
          <option value="Intermediate">Intermediário</option>
          <option value="Advanced">Avançado</option>
        </select>
      </div>

      {isLoading && <p className="text-gray-500">Carregando cursos...</p>}

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {data?.items?.map((course: Course) => (
          <div key={course.id} className="card hover:shadow-md transition-shadow">
            {course.thumbnailUrl && (
              <img src={course.thumbnailUrl} alt={course.title}
                className="w-full h-40 object-cover rounded-lg mb-4" />
            )}
            <div className="flex items-center gap-2 mb-2">
              <span className="text-xs bg-brand-50 text-brand-700 px-2 py-1 rounded font-medium">
                {course.level === 'Beginner' ? 'Iniciante' :
                 course.level === 'Intermediate' ? 'Intermediário' : 'Avançado'}
              </span>
              {course.isFree && (
                <span className="text-xs bg-green-50 text-green-700 px-2 py-1 rounded font-medium">Grátis</span>
              )}
            </div>
            <h3 className="font-semibold text-gray-900 mb-1">{course.title}</h3>
            <p className="text-sm text-gray-500 mb-4 line-clamp-2">{course.shortDescription}</p>
            <div className="flex items-center justify-between">
              <span className="text-sm text-gray-500">{course.totalLessons} aulas</span>
              {course.isEnrolled ? (
                <Link href={`/cursos/${course.slug}`} className="text-sm text-brand-600 font-medium hover:underline">
                  Continuar →
                </Link>
              ) : (
                <button onClick={() => enroll(course.id)} className="text-sm btn-primary py-2 px-4">
                  Matricular
                </button>
              )}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
