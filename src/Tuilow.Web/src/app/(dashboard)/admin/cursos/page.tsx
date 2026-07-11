'use client';

import { useState, useRef, useCallback } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { coursesApi, api } from '@/lib/api';
import toast from 'react-hot-toast';
import { Plus, ChevronDown, ChevronRight, Upload, Check, Loader2,
         Video, BookOpen, Layers, Eye, EyeOff, X, Play } from 'lucide-react';
import Link from 'next/link';

// ─── API helpers ──────────────────────────────────────────────────────────────

const adminApi = {
  createCourse: (data: CreateCourseData) => api.post('/courses', data),
  addModule: (courseId: string, data: { title: string; description?: string }) =>
    api.post(`/courses/${courseId}/modules`, data),
  addLesson: (courseId: string, moduleId: string, data: { title: string; description?: string; isPreview?: boolean }) =>
    api.post(`/courses/${courseId}/modules/${moduleId}/lessons`, data),
  getUploadUrl: (courseId: string) => api.post('/videos/upload-url', { courseId }),
  linkVideo: (videoId: string, data: LinkLessonData) =>
    api.post(`/videos/${videoId}/link-lesson`, data),
};

interface CreateCourseData {
  title: string; description: string; shortDescription?: string;
  level: string; price: number;
}
interface LinkLessonData {
  courseId: string; moduleId: string; lessonId: string; isPreview: boolean;
}

// ─── Video upload via TUS (Cloudflare Stream) ─────────────────────────────────

async function uploadVideoToCloudflare(
  file: File,
  uploadUrl: string,
  onProgress: (pct: number) => void
): Promise<void> {
  const tus = await import('tus-js-client');

  return new Promise((resolve, reject) => {
    const upload = new tus.Upload(file, {
      uploadUrl,
      uploadLengthDeferred: true,          // mock pré-cria URL sem saber o tamanho
      chunkSize: 50 * 1024 * 1024,         // 50 MB por chunk
      retryDelays: [0, 3000, 5000, 10000],
      metadata: { filename: file.name, filetype: file.type },
      onError: reject,
      onProgress: (bytesUploaded, bytesTotal) => {
        onProgress(Math.round((bytesUploaded / bytesTotal) * 100));
      },
      onSuccess: () => resolve(),
    });
    upload.start();
  });
}

// ─── Components ───────────────────────────────────────────────────────────────

function Badge({ children, color = 'zinc' }: { children: React.ReactNode; color?: string }) {
  const colors: Record<string, string> = {
    zinc:   'bg-gray-100 text-gray-500',
    green:  'bg-emerald-950/60 text-emerald-400 border border-emerald-800/40',
    brand:  'bg-brand-50 text-brand-700 border border-brand-200',
    yellow: 'bg-yellow-950/60 text-yellow-400 border border-yellow-800/40',
  };
  return (
    <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${colors[color]}`}>
      {children}
    </span>
  );
}

// ─── Video Upload Component ───────────────────────────────────────────────────

interface VideoUploaderProps {
  courseId: string; moduleId: string; lessonId: string;
  onDone: () => void;
}

function VideoUploader({ courseId, moduleId, lessonId, onDone }: VideoUploaderProps) {
  const [file, setFile] = useState<File | null>(null);
  const [isPreview, setIsPreview] = useState(false);
  const [progress, setProgress] = useState(0);
  const [phase, setPhase] = useState<'idle' | 'uploading' | 'linking' | 'done'>('idle');
  const inputRef = useRef<HTMLInputElement>(null);

  const handleFile = (e: React.ChangeEvent<HTMLInputElement>) => {
    const f = e.target.files?.[0];
    if (f) setFile(f);
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    const f = e.dataTransfer.files[0];
    if (f && f.type.startsWith('video/')) setFile(f);
  };

  const handleUpload = async () => {
    if (!file) return;
    try {
      // 1. Pede URL de upload ao backend
      setPhase('uploading');
      const { data } = await adminApi.getUploadUrl(courseId);
      const { videoId, uploadUrl } = data;

      // 2. Faz upload direto para o Cloudflare via TUS
      await uploadVideoToCloudflare(file, uploadUrl, setProgress);

      // 3. Vincula o vídeo à aula
      setPhase('linking');
      await adminApi.linkVideo(videoId, { courseId, moduleId, lessonId, isPreview });

      setPhase('done');
      toast.success('Vídeo enviado e vinculado à aula!');
      setTimeout(onDone, 1200);
    } catch (err: unknown) {
      setPhase('idle');
      setProgress(0);
      const msg = err instanceof Error ? err.message : String(err);
      toast.error(`Erro no upload: ${msg}`);
      console.error('[VideoUpload] erro:', err);
    }
  };

  if (phase === 'done') {
    return (
      <div className="flex items-center gap-2 text-emerald-400 text-sm py-3">
        <Check className="w-4 h-4" /> Vídeo enviado com sucesso!
      </div>
    );
  }

  return (
    <div className="space-y-3">
      {/* Drop zone */}
      <div
        onDrop={handleDrop}
        onDragOver={e => e.preventDefault()}
        onClick={() => phase === 'idle' && inputRef.current?.click()}
        className={`border-2 border-dashed rounded-xl p-6 text-center transition-colors cursor-pointer
          ${file ? 'border-brand-600 bg-brand-50' : 'border-gray-300 hover:border-gray-400'}
          ${phase !== 'idle' ? 'pointer-events-none opacity-70' : ''}`}
      >
        <input ref={inputRef} type="file" accept="video/*" onChange={handleFile} className="hidden" />
        <Video className="w-8 h-8 mx-auto mb-2 text-gray-400" />
        {file ? (
          <>
            <p className="text-sm font-medium text-gray-700">{file.name}</p>
            <p className="text-xs text-gray-400 mt-1">{(file.size / 1024 / 1024).toFixed(1)} MB</p>
          </>
        ) : (
          <>
            <p className="text-sm text-gray-500">Arraste o vídeo ou clique para selecionar</p>
            <p className="text-xs text-gray-400 mt-1">MP4, MOV, WebM — máx. 1 hora</p>
          </>
        )}
      </div>

      {/* Preview toggle */}
      <label className="flex items-center gap-2 cursor-pointer select-none">
        <div
          onClick={() => setIsPreview(v => !v)}
          className={`w-9 h-5 rounded-full transition-colors relative ${isPreview ? 'bg-brand-600' : 'bg-gray-200'}`}
        >
          <span className={`absolute top-0.5 w-4 h-4 bg-white rounded-full transition-transform
            ${isPreview ? 'translate-x-4' : 'translate-x-0.5'}`} />
        </div>
        <span className="text-sm text-gray-600">
          {isPreview
            ? <><Eye className="inline w-3.5 h-3.5 mr-1 text-brand-600" />Preview gratuito</>
            : <><EyeOff className="inline w-3.5 h-3.5 mr-1 text-gray-400" />Requer assinatura</>
          }
        </span>
      </label>

      {/* Progress */}
      {phase === 'uploading' && (
        <div>
          <div className="flex justify-between text-xs text-gray-500 mb-1">
            <span>Enviando para Cloudflare Stream...</span>
            <span>{progress}%</span>
          </div>
          <div className="h-1.5 bg-gray-100 rounded-full overflow-hidden">
            <div
              className="h-full bg-brand-600 transition-all duration-300"
              style={{ width: `${progress}%` }}
            />
          </div>
        </div>
      )}

      {phase === 'linking' && (
        <div className="flex items-center gap-2 text-sm text-gray-500">
          <Loader2 className="w-4 h-4 animate-spin" /> Vinculando à aula...
        </div>
      )}

      <button
        onClick={handleUpload}
        disabled={!file || phase !== 'idle'}
        className="btn-primary w-full py-2 flex items-center justify-center gap-2 disabled:opacity-50 disabled:cursor-not-allowed"
      >
        <Upload className="w-4 h-4" />
        {phase === 'idle' ? 'Enviar vídeo' : 'Enviando...'}
      </button>
    </div>
  );
}

// ─── Main Page ────────────────────────────────────────────────────────────────

interface CourseItem { id: string; title: string; slug: string; level: string; status?: string; }
interface ModuleItem { id: string; title: string; description?: string; }
interface LessonItem { id: string; title: string; isPreview: boolean; hasVideo: boolean; }

export default function AdminCursosPage() {
  const qc = useQueryClient();
  const [expandedCourse, setExpandedCourse] = useState<string | null>(null);
  const [expandedModule, setExpandedModule] = useState<string | null>(null);
  const [uploadTarget, setUploadTarget] = useState<{ courseId: string; moduleId: string; lessonId: string } | null>(null);

  // Forms state
  const [showCourseForm, setShowCourseForm] = useState(false);
  const [showModuleForm, setShowModuleForm] = useState<string | null>(null);
  const [showLessonForm, setShowLessonForm] = useState<string | null>(null); // moduleId

  // ─── Queries ───────────────────────────────────────────────────────────────

  const { data: courses = [], isLoading, error: coursesError } = useQuery<CourseItem[]>({
    queryKey: ['admin-courses'],
    queryFn: () => api.get('/admin/courses').then(r => r.data),
  });

  const { data: courseDetail } = useQuery({
    queryKey: ['admin-course-detail', expandedCourse],
    queryFn: async () => {
      if (!expandedCourse) return null;
      return api.get(`/admin/courses/${expandedCourse}`).then(r => r.data);
    },
    enabled: !!expandedCourse,
  });

  // ─── Mutations ─────────────────────────────────────────────────────────────

  const createCourse = useMutation({
    mutationFn: (data: CreateCourseData) => adminApi.createCourse(data),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['admin-courses'] }); setShowCourseForm(false); toast.success('Curso criado!'); },
    onError: (e: unknown) => {
      const status = (e as { response?: { status?: number } })?.response?.status;
      if (status === 403 || status === 401)
        toast.error('Sem permissão. Certifique-se de estar logado como Admin ou Instrutor.');
      else
        toast.error('Erro ao criar curso. Verifique os dados e tente novamente.');
    },
  });

  const addModule = useMutation({
    mutationFn: ({ courseId, ...data }: { courseId: string; title: string; description?: string }) =>
      adminApi.addModule(courseId, data),
    onSuccess: (_, vars) => {
      qc.invalidateQueries({ queryKey: ['admin-course-detail', vars.courseId] });
      setShowModuleForm(null);
      toast.success('Módulo adicionado!');
    },
    onError: () => toast.error('Erro ao adicionar módulo.'),
  });

  const addLesson = useMutation({
    mutationFn: ({ courseId, moduleId, ...data }: { courseId: string; moduleId: string; title: string; description?: string; isPreview?: boolean }) =>
      adminApi.addLesson(courseId, moduleId, data),
    onSuccess: (_, vars) => {
      qc.invalidateQueries({ queryKey: ['admin-course-detail', vars.courseId] });
      setShowLessonForm(null);
      toast.success('Aula adicionada!');
    },
    onError: () => toast.error('Erro ao adicionar aula.'),
  });

  const publishCourse = useMutation({
    mutationFn: (courseId: string) => api.post(`/courses/${courseId}/publish`, {}),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['admin-courses'] }); toast.success('Curso publicado!'); },
    onError: () => toast.error('Erro ao publicar curso.'),
  });

  return (
    <div className="max-w-4xl mx-auto">
      <div className="flex items-center justify-between mb-8">
        <div>
          <h1 className="text-2xl font-bold text-gray-800">Gerenciar Cursos</h1>
          <p className="text-gray-500 mt-1 text-sm">Crie cursos, módulos, aulas e faça upload das videoaulas.</p>
        </div>
        <button onClick={() => setShowCourseForm(true)} className="btn-primary flex items-center gap-2">
          <Plus className="w-4 h-4" /> Novo curso
        </button>
      </div>

      {/* ─── Create Course Form ─────────────────────────────────────────────── */}
      {showCourseForm && (
        <CreateCourseForm
          onSubmit={(data) => createCourse.mutate(data)}
          onCancel={() => setShowCourseForm(false)}
          loading={createCourse.isPending}
        />
      )}

      {/* ─── Course List ────────────────────────────────────────────────────── */}
      {isLoading ? (
        <div className="space-y-3">
          {[1,2,3].map(i => <div key={i} className="card border-gray-200 animate-pulse h-16" />)}
        </div>
      ) : coursesError ? (
        <div className="card border-red-900/40 bg-red-950/20 text-center py-12">
          <p className="text-red-400 font-medium">Erro ao carregar cursos</p>
          <p className="text-xs text-gray-400 mt-2">
            {String((coursesError as { response?: { data?: { title?: string } } })?.response?.data?.title ?? coursesError)}
          </p>
          <p className="text-xs text-gray-400 mt-1">Verifique se a API está rodando e reinicie-a.</p>
        </div>
      ) : courses.length === 0 ? (
        <div className="card border-gray-200 text-center py-12">
          <BookOpen className="w-10 h-10 mx-auto text-gray-400 mb-3" />
          <p className="text-gray-500">Nenhum curso ainda. Crie o primeiro!</p>
        </div>
      ) : (
        <div className="space-y-3">
          {courses.map(course => (
            <div key={course.id} className="border border-gray-200 rounded-xl overflow-hidden bg-white">
              {/* Course header */}
              <div className="flex items-center gap-3 p-4">
                <button
                  onClick={() => setExpandedCourse(expandedCourse === course.id ? null : course.id)}
                  className="flex items-center gap-2 flex-1 text-left"
                >
                  {expandedCourse === course.id
                    ? <ChevronDown className="w-4 h-4 text-gray-400 flex-shrink-0" />
                    : <ChevronRight className="w-4 h-4 text-gray-400 flex-shrink-0" />
                  }
                  <span className="font-medium text-gray-800">{course.title}</span>
                  <Badge color="zinc">{course.level}</Badge>
                  {(course as unknown as { status?: string }).status === 'Draft' && (
                    <span className="text-xs px-2 py-0.5 rounded-full bg-amber-950/60 text-amber-400 border border-amber-900/40">
                      Rascunho
                    </span>
                  )}
                  {(course as unknown as { status?: string }).status === 'Published' && (
                    <span className="text-xs px-2 py-0.5 rounded-full bg-green-950/60 text-green-400 border border-green-900/40">
                      Publicado
                    </span>
                  )}
                </button>
                <button
                  onClick={() => publishCourse.mutate(course.id)}
                  className="text-xs text-brand-600 hover:text-brand-700 font-medium transition-colors px-3 py-1 border border-brand-200 rounded-lg"
                >
                  Publicar
                </button>
              </div>

              {/* Expanded: modules & lessons */}
              {expandedCourse === course.id && courseDetail && (
                <div className="border-t border-gray-200 bg-white/40 p-4 space-y-3">
                  {/* Modules */}
                  {(courseDetail.modules ?? []).map((mod: ModuleItem) => (
                    <div key={mod.id} className="border border-gray-200 rounded-lg overflow-hidden">
                      <div className="flex items-center gap-2 p-3 bg-white/60">
                        <button
                          onClick={() => setExpandedModule(expandedModule === mod.id ? null : mod.id)}
                          className="flex items-center gap-2 flex-1 text-left"
                        >
                          {expandedModule === mod.id
                            ? <ChevronDown className="w-3.5 h-3.5 text-gray-400" />
                            : <ChevronRight className="w-3.5 h-3.5 text-gray-400" />
                          }
                          <Layers className="w-3.5 h-3.5 text-gray-400" />
                          <span className="text-sm font-medium text-gray-700">{mod.title}</span>
                        </button>
                        <button
                          onClick={() => setShowLessonForm(showLessonForm === mod.id ? null : mod.id)}
                          className="text-xs text-gray-500 hover:text-gray-700 flex items-center gap-1"
                        >
                          <Plus className="w-3 h-3" /> Aula
                        </button>
                      </div>

                      {/* Add lesson form */}
                      {showLessonForm === mod.id && (
                        <div className="p-3 border-t border-gray-200 bg-white/30">
                          <AddLessonForm
                            onSubmit={(data) => addLesson.mutate({ courseId: course.id, moduleId: mod.id, ...data })}
                            onCancel={() => setShowLessonForm(null)}
                            loading={addLesson.isPending}
                          />
                        </div>
                      )}

                      {/* Lessons */}
                      {expandedModule === mod.id && (
                        <div className="divide-y divide-gray-200/50">
                          {(courseDetail.modules?.find((m: ModuleItem) => m.id === mod.id)?.lessons ?? []).map((lesson: LessonItem) => (
                            <div key={lesson.id} className="flex items-center gap-3 px-4 py-2.5">
                              <Video className="w-3.5 h-3.5 text-gray-400 flex-shrink-0" />
                              <span className="text-sm text-gray-600 flex-1">{lesson.title}</span>
                              {lesson.isPreview && <Badge color="green">Preview</Badge>}
                              {lesson.hasVideo ? (
                                <div className="flex items-center gap-2">
                                  <Badge color="brand">Vídeo ✓</Badge>
                                  <Link
                                    href={`/cursos/${course.slug}/${lesson.id}`}
                                    className="text-xs text-gray-500 hover:text-brand-700 flex items-center gap-1 transition-colors"
                                    title="Assistir aula"
                                  >
                                    <Play className="w-3 h-3" /> Assistir
                                  </Link>
                                </div>
                              ) : (
                                <button
                                  onClick={() => setUploadTarget({ courseId: course.id, moduleId: mod.id, lessonId: lesson.id })}
                                  className="text-xs text-brand-600 hover:text-brand-700 flex items-center gap-1 font-medium"
                                >
                                  <Upload className="w-3 h-3" /> Upload
                                </button>
                              )}
                            </div>
                          ))}
                        </div>
                      )}
                    </div>
                  ))}

                  {/* Add module */}
                  {showModuleForm === course.id ? (
                    <AddModuleForm
                      onSubmit={(data) => addModule.mutate({ courseId: course.id, ...data })}
                      onCancel={() => setShowModuleForm(null)}
                      loading={addModule.isPending}
                    />
                  ) : (
                    <button
                      onClick={() => setShowModuleForm(course.id)}
                      className="flex items-center gap-1.5 text-sm text-gray-500 hover:text-gray-700 transition-colors"
                    >
                      <Plus className="w-4 h-4" /> Adicionar módulo
                    </button>
                  )}
                </div>
              )}
            </div>
          ))}
        </div>
      )}

      {/* ─── Upload Modal ────────────────────────────────────────────────────── */}
      {uploadTarget && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm">
          <div className="w-full max-w-md bg-white border border-gray-200 rounded-2xl shadow-2xl p-6">
            <div className="flex items-center justify-between mb-4">
              <h2 className="text-lg font-bold text-gray-800">Upload de vídeo</h2>
              <button onClick={() => setUploadTarget(null)} className="text-gray-400 hover:text-gray-600">
                <X className="w-5 h-5" />
              </button>
            </div>
            <VideoUploader
              {...uploadTarget}
              onDone={() => {
                setUploadTarget(null);
                qc.invalidateQueries({ queryKey: ['admin-course-detail', uploadTarget.courseId] });
              }}
            />
          </div>
        </div>
      )}
    </div>
  );
}

// ─── Sub-forms ────────────────────────────────────────────────────────────────

function CreateCourseForm({ onSubmit, onCancel, loading }: {
  onSubmit: (d: CreateCourseData) => void; onCancel: () => void; loading: boolean;
}) {
  const [form, setForm] = useState<CreateCourseData>({ title: '', description: '', level: 'Beginner', price: 0 });
  return (
    <div className="card border-brand-200 bg-brand-50 mb-6">
      <h3 className="font-semibold text-gray-800 mb-4">Novo curso</h3>
      <div className="space-y-3">
        <input className="input-field" placeholder="Título do curso *"
          value={form.title} onChange={e => setForm(f => ({ ...f, title: e.target.value }))} />
        <textarea className="input-field resize-none" rows={2} placeholder="Descrição *"
          value={form.description} onChange={e => setForm(f => ({ ...f, description: e.target.value }))} />
        <div className="grid grid-cols-2 gap-3">
          <select className="input-field" value={form.level} onChange={e => setForm(f => ({ ...f, level: e.target.value }))}>
            <option value="Beginner">Iniciante</option>
            <option value="Intermediate">Intermediário</option>
            <option value="Advanced">Avançado</option>
          </select>
          <input className="input-field" type="number" placeholder="Preço (0 = grátis)"
            value={form.price} onChange={e => setForm(f => ({ ...f, price: Number(e.target.value) }))} />
        </div>
        <div className="flex gap-2 pt-1">
          <button onClick={() => onSubmit(form)} disabled={!form.title || !form.description || loading}
            className="btn-primary flex-1 py-2">
            {loading ? 'Criando...' : 'Criar curso'}
          </button>
          <button onClick={onCancel} className="px-4 py-2 text-sm text-gray-500 hover:text-gray-700 border border-gray-300 rounded-lg">
            Cancelar
          </button>
        </div>
      </div>
    </div>
  );
}

function AddModuleForm({ onSubmit, onCancel, loading }: {
  onSubmit: (d: { title: string; description?: string }) => void; onCancel: () => void; loading: boolean;
}) {
  const [title, setTitle] = useState('');
  return (
    <div className="flex gap-2 items-end mt-1">
      <input className="input-field flex-1" placeholder="Nome do módulo *" value={title} onChange={e => setTitle(e.target.value)} />
      <button onClick={() => onSubmit({ title })} disabled={!title || loading} className="btn-primary px-3 py-2 text-sm">
        {loading ? '...' : 'Adicionar'}
      </button>
      <button onClick={onCancel} className="px-3 py-2 text-sm text-gray-400 hover:text-gray-600">Cancelar</button>
    </div>
  );
}

function AddLessonForm({ onSubmit, onCancel, loading }: {
  onSubmit: (d: { title: string; description?: string; isPreview?: boolean }) => void; onCancel: () => void; loading: boolean;
}) {
  const [title, setTitle] = useState('');
  const [isPreview, setIsPreview] = useState(false);
  return (
    <div className="space-y-2">
      <input className="input-field" placeholder="Título da aula *" value={title} onChange={e => setTitle(e.target.value)} />
      <label className="flex items-center gap-2 cursor-pointer select-none">
        <input type="checkbox" checked={isPreview} onChange={e => setIsPreview(e.target.checked)}
          className="accent-brand-500" />
        <span className="text-xs text-gray-500">Preview gratuito (visível sem assinatura)</span>
      </label>
      <div className="flex gap-2">
        <button onClick={() => onSubmit({ title, isPreview })} disabled={!title || loading} className="btn-primary text-sm px-3 py-1.5">
          {loading ? '...' : 'Criar aula'}
        </button>
        <button onClick={onCancel} className="text-sm text-gray-400 hover:text-gray-600">Cancelar</button>
      </div>
    </div>
  );
}
