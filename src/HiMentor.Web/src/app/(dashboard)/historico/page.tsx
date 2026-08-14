'use client';

import { useQuery } from '@tanstack/react-query';
import { enrollmentsApi } from '@/lib/api';
import type { LessonHistoryItem } from '@/types';
import Link from 'next/link';

function formatDate(iso: string) {
  return new Date(iso).toLocaleString('pt-BR', {
    day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit',
  });
}

export default function LessonHistoryPage() {
  const { data: history = [], isLoading } = useQuery<LessonHistoryItem[]>({
    queryKey: ['lesson-history'],
    queryFn: () => enrollmentsApi.getLessonHistory().then(r => r.data),
  });

  return (
    <div className="max-w-4xl mx-auto">
      <div className="mb-8">
        <h1 className="text-2xl font-bold text-gray-800">Histórico de aulas</h1>
        <p className="text-gray-500 mt-1">Tudo o que você assistiu, entre todos os cursos matriculados.</p>
      </div>

      {isLoading && (
        <div className="space-y-3">
          {[1, 2, 3, 4].map(i => (
            <div key={i} className="card border-gray-200 animate-pulse flex items-center gap-4">
              <div className="w-16 h-16 bg-gray-100 rounded-lg flex-shrink-0" />
              <div className="flex-1 space-y-2">
                <div className="h-4 bg-gray-100 rounded w-1/2" />
                <div className="h-3 bg-gray-100 rounded w-1/3" />
              </div>
            </div>
          ))}
        </div>
      )}

      {!isLoading && history.length === 0 && (
        <div className="text-center py-16 text-gray-400">
          <p className="text-4xl mb-4">🕒</p>
          <p className="text-lg font-medium">Você ainda não assistiu nenhuma aula</p>
          <Link href="/cursos" className="text-brand-600 hover:underline text-sm mt-2 inline-block">
            Explorar catálogo →
          </Link>
        </div>
      )}

      <div className="space-y-3">
        {history.map(item => (
          <Link
            key={`${item.lessonId}-${item.lastWatchedAt}`}
            href={`/cursos/${item.courseSlug}/${item.lessonId}`}
            className="card border-gray-200 hover:border-gray-300 transition-all duration-200 flex items-center gap-4"
          >
            <div className="w-16 h-16 rounded-lg bg-gray-100 flex-shrink-0 overflow-hidden flex items-center justify-center">
              {item.thumbnailUrl ? (
                // eslint-disable-next-line @next/next/no-img-element
                <img src={item.thumbnailUrl} alt="" className="w-full h-full object-cover" />
              ) : (
                <span className="text-2xl">🎬</span>
              )}
            </div>

            <div className="flex-1 min-w-0">
              <p className="font-semibold text-gray-800 truncate">{item.lessonTitle}</p>
              <p className="text-xs text-gray-500 truncate">{item.courseTitle}</p>
              <p className="text-xs text-gray-400 mt-1">{formatDate(item.lastWatchedAt)}</p>
            </div>

            {item.isCompleted && <span className="badge-green flex-shrink-0">Concluída</span>}
          </Link>
        ))}
      </div>
    </div>
  );
}
