'use client';

import { Suspense, useEffect, useRef, useState } from 'react';
import { useSearchParams } from 'next/navigation';
import { useQuery } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { Circle, Pause, Play, Square, RotateCcw, Download, Upload, Video as VideoIcon, MonitorUp, Users } from 'lucide-react';
import { API_URL, studioApi, coursesApi, videosApi } from '@/lib/api';
import type { LessonScriptItem, ProductDetail } from '@/types';

type Mode = 'webcam' | 'screen' | 'both';
type RecState = 'idle' | 'recording' | 'paused' | 'stopped';

interface CourseAdminItem { id: string; title: string; }

function GravadorInner() {
  const params = useSearchParams();
  const scriptId = params.get('scriptId');

  const { data: scripts = [] } = useQuery<LessonScriptItem[]>({
    queryKey: ['studio-scripts'],
    queryFn: () => studioApi.getMyScripts().then(r => r.data),
  });
  const script = scripts.find(s => s.id === scriptId);

  const [mode, setMode] = useState<Mode>('webcam');
  const [recState, setRecState] = useState<RecState>('idle');
  const [recordedUrl, setRecordedUrl] = useState<string | null>(null);

  const previewRef = useRef<HTMLVideoElement>(null);
  const playbackRef = useRef<HTMLVideoElement>(null);
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const rafRef = useRef<number | null>(null);

  const streamsRef = useRef<MediaStream[]>([]);
  const recorderRef = useRef<MediaRecorder | null>(null);
  const chunksRef = useRef<Blob[]>([]);
  const recordedBlobRef = useRef<Blob | null>(null);

  const stopAllTracks = () => {
    streamsRef.current.forEach(s => s.getTracks().forEach(t => t.stop()));
    streamsRef.current = [];
    if (rafRef.current) { cancelAnimationFrame(rafRef.current); rafRef.current = null; }
  };

  useEffect(() => () => stopAllTracks(), []);

  const startCapture = async () => {
    try {
      let outputStream: MediaStream;

      if (mode === 'webcam') {
        const webcam = await navigator.mediaDevices.getUserMedia({ video: true, audio: true });
        streamsRef.current = [webcam];
        outputStream = webcam;
      } else if (mode === 'screen') {
        const screen = await navigator.mediaDevices.getDisplayMedia({ video: true, audio: true });
        streamsRef.current = [screen];
        outputStream = screen;
      } else {
        // Webcam + tela: compõe num canvas (tela em tamanho grande, webcam num círculo no canto).
        const webcam = await navigator.mediaDevices.getUserMedia({ video: true, audio: true });
        const screen = await navigator.mediaDevices.getDisplayMedia({ video: true, audio: false });
        streamsRef.current = [webcam, screen];

        const screenVideo = document.createElement('video');
        screenVideo.srcObject = screen;
        screenVideo.muted = true;
        await screenVideo.play();

        const webcamVideo = document.createElement('video');
        webcamVideo.srcObject = webcam;
        webcamVideo.muted = true;
        await webcamVideo.play();

        const canvas = canvasRef.current!;
        canvas.width = 1280;
        canvas.height = 720;
        const ctx = canvas.getContext('2d')!;

        const draw = () => {
          ctx.fillStyle = '#000';
          ctx.fillRect(0, 0, canvas.width, canvas.height);
          ctx.drawImage(screenVideo, 0, 0, canvas.width, canvas.height);

          const pipSize = 220;
          const pipX = canvas.width - pipSize - 24;
          const pipY = canvas.height - pipSize - 24;
          ctx.save();
          ctx.beginPath();
          ctx.arc(pipX + pipSize / 2, pipY + pipSize / 2, pipSize / 2, 0, Math.PI * 2);
          ctx.closePath();
          ctx.clip();
          ctx.drawImage(webcamVideo, pipX, pipY, pipSize, pipSize);
          ctx.restore();

          rafRef.current = requestAnimationFrame(draw);
        };
        draw();

        const canvasStream = canvas.captureStream(30);
        const audioTrack = webcam.getAudioTracks()[0];
        outputStream = new MediaStream([
          ...canvasStream.getVideoTracks(),
          ...(audioTrack ? [audioTrack] : []),
        ]);
      }

      if (previewRef.current) {
        previewRef.current.srcObject = outputStream;
        await previewRef.current.play();
      }

      chunksRef.current = [];
      const recorder = new MediaRecorder(outputStream, { mimeType: 'video/webm' });
      recorder.ondataavailable = e => { if (e.data.size > 0) chunksRef.current.push(e.data); };
      recorder.onstop = () => {
        const blob = new Blob(chunksRef.current, { type: 'video/webm' });
        recordedBlobRef.current = blob;
        setRecordedUrl(URL.createObjectURL(blob));
        stopAllTracks();
      };
      recorder.start(1000);
      recorderRef.current = recorder;
      setRecState('recording');
    } catch {
      toast.error('Não foi possível acessar câmera/tela. Verifique as permissões do navegador.');
    }
  };

  const pause = () => { recorderRef.current?.pause(); setRecState('paused'); };
  const resume = () => { recorderRef.current?.resume(); setRecState('recording'); };
  const stop = () => { recorderRef.current?.stop(); setRecState('stopped'); };

  const reRecord = () => {
    if (recordedUrl) URL.revokeObjectURL(recordedUrl);
    setRecordedUrl(null);
    recordedBlobRef.current = null;
    setRecState('idle');
  };

  return (
    <div className="max-w-4xl mx-auto">
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-800">🎥 Gravador</h1>
        {script && <p className="text-gray-500 mt-1">Gravando para: <strong>{script.lessonTitle}</strong></p>}
      </div>

      {recState === 'idle' && (
        <div className="card border-gray-200 mb-6">
          <p className="font-semibold text-gray-800 mb-3">O que gravar?</p>
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
            {([
              { id: 'webcam' as const, label: 'Webcam', icon: VideoIcon },
              { id: 'screen' as const, label: 'Tela', icon: MonitorUp },
              { id: 'both' as const, label: 'Webcam + Tela', icon: Users },
            ]).map(m => (
              <button
                key={m.id}
                onClick={() => setMode(m.id)}
                className={`flex flex-col items-center gap-2 p-4 rounded-xl border-2 transition-colors
                  ${mode === m.id ? 'border-brand-500 bg-brand-50' : 'border-gray-200 hover:border-gray-300'}`}
              >
                <m.icon className="w-6 h-6 text-brand-600" />
                <span className="text-sm font-medium text-gray-700">{m.label}</span>
              </button>
            ))}
          </div>
        </div>
      )}

      <div className="card border-gray-200 mb-6">
        <div className="relative w-full aspect-video bg-black rounded-lg overflow-hidden flex items-center justify-center">
          {recordedUrl ? (
            // eslint-disable-next-line jsx-a11y/media-has-caption
            <video ref={playbackRef} src={recordedUrl} controls className="w-full h-full object-contain" />
          ) : (
            // eslint-disable-next-line jsx-a11y/media-has-caption
            <video ref={previewRef} muted className="w-full h-full object-contain" />
          )}
          <canvas ref={canvasRef} className="hidden" />
          {recState === 'recording' && (
            <span className="absolute top-3 left-3 flex items-center gap-1.5 bg-red-600 text-white text-xs px-2.5 py-1 rounded-full">
              <Circle className="w-2.5 h-2.5 fill-white" /> Gravando
            </span>
          )}
          {recState === 'paused' && (
            <span className="absolute top-3 left-3 bg-amber-500 text-white text-xs px-2.5 py-1 rounded-full">Pausado</span>
          )}
        </div>

        <div className="flex items-center justify-center gap-3 mt-4">
          {recState === 'idle' && (
            <button onClick={startCapture} className="btn-primary flex items-center gap-2">
              <Circle className="w-4 h-4" /> Iniciar gravação
            </button>
          )}
          {recState === 'recording' && (
            <>
              <button onClick={pause} className="btn-ghost flex items-center gap-2"><Pause className="w-4 h-4" /> Pausar</button>
              <button onClick={stop} className="btn-primary bg-red-600 hover:bg-red-700 flex items-center gap-2"><Square className="w-4 h-4" /> Parar</button>
            </>
          )}
          {recState === 'paused' && (
            <>
              <button onClick={resume} className="btn-primary flex items-center gap-2"><Play className="w-4 h-4" /> Continuar</button>
              <button onClick={stop} className="btn-ghost flex items-center gap-2"><Square className="w-4 h-4" /> Parar</button>
            </>
          )}
          {recState === 'stopped' && (
            <button onClick={reRecord} className="btn-ghost flex items-center gap-2"><RotateCcw className="w-4 h-4" /> Regravar</button>
          )}
        </div>
      </div>

      {recState === 'stopped' && recordedBlobRef.current && (
        <PostRecordingActions blob={recordedBlobRef.current} recordedUrl={recordedUrl!} scriptId={scriptId} />
      )}
    </div>
  );
}

function PostRecordingActions({ blob, recordedUrl, scriptId }: { blob: Blob; recordedUrl: string; scriptId: string | null }) {
  const [showUpload, setShowUpload] = useState(false);
  const [courses, setCourses] = useState<CourseAdminItem[]>([]);
  const [loadingCourses, setLoadingCourses] = useState(false);
  const [courseId, setCourseId] = useState('');
  const [courseDetail, setCourseDetail] = useState<ProductDetail | null>(null);
  const [moduleId, setModuleId] = useState('');
  const [lessonId, setLessonId] = useState('');
  const [uploading, setUploading] = useState(false);

  const handleDownload = () => {
    const a = document.createElement('a');
    a.href = recordedUrl;
    a.download = `gravacao-${Date.now()}.webm`;
    a.click();
  };

  const openUploadPanel = async () => {
    setShowUpload(true);
    if (courses.length > 0) return;
    setLoadingCourses(true);
    try {
      const { data } = await coursesApi.listAdmin();
      setCourses(data);
    } catch {
      toast.error('Não foi possível carregar seus cursos.');
    } finally {
      setLoadingCourses(false);
    }
  };

  const handleSelectCourse = async (id: string) => {
    setCourseId(id);
    setModuleId('');
    setLessonId('');
    setCourseDetail(null);
    if (!id) return;
    try {
      const { data } = await coursesApi.getByIdAdmin(id);
      setCourseDetail(data);
    } catch {
      toast.error('Não foi possível carregar os módulos deste curso.');
    }
  };

  const handleUpload = async () => {
    if (!courseId || !moduleId || !lessonId) { toast.error('Selecione curso, módulo e aula.'); return; }
    setUploading(true);
    try {
      const file = new File([blob], `gravacao-${Date.now()}.webm`, { type: 'video/webm' });
      const { data } = await videosApi.getUploadUrl(courseId);
      const tus = await import('tus-js-client');
      await new Promise<void>((resolve, reject) => {
        const upload = new tus.Upload(file, {
          uploadUrl: data.uploadUrl,
          uploadLengthDeferred: true,
          chunkSize: 50 * 1024 * 1024,
          retryDelays: [0, 3000, 5000, 10000],
          metadata: { filename: file.name, filetype: file.type },
          // Achado M10 da avaliação: o mock de upload de vídeo (MockTusController) passou a
          // exigir [Authorize(Roles="Creator,Admin")] nos métodos de escrita (Options/Head/
          // Patch) — sem enviar o token aqui, o upload passaria a falhar com 401.
          // Achado de teste manual (Cloudflare Stream real -- o achado M10 original era so
          // pro Mock): a URL de upload de verdade (upload.cloudflarestream.com) rejeita a
          // requisicao inteira no preflight de CORS se vier um header Authorization -- o
          // endpoint deles nunca esperou nem permite esse header (a autorizacao ja esta
          // embutida no proprio uploadUrl de uso unico). So manda o token quando o upload
          // e para o NOSSO backend (fluxo Mock, sem Cloudflare configurado).
          headers: data.uploadUrl.startsWith(API_URL)
            ? { Authorization: `Bearer ${localStorage.getItem('access_token') ?? ''}` }
            : undefined,
          onError: reject,
          onSuccess: () => resolve(),
        });
        upload.start();
      });
      await videosApi.linkVideo(data.videoId, { courseId, moduleId, lessonId });

      if (scriptId) {
        try { await studioApi.markScriptAsRecorded(scriptId); } catch { /* não bloqueia o sucesso do upload */ }
      }

      toast.success('Vídeo enviado e vinculado à aula!');
    } catch {
      toast.error('Não foi possível enviar o vídeo agora.');
    } finally {
      setUploading(false);
    }
  };

  return (
    <div className="card border-gray-200 space-y-4">
      <div className="flex items-center gap-3 flex-wrap">
        <button onClick={handleDownload} className="btn-ghost flex items-center gap-2">
          <Download className="w-4 h-4" /> Baixar vídeo
        </button>
        {!showUpload && (
          <button onClick={openUploadPanel} className="btn-primary flex items-center gap-2">
            <Upload className="w-4 h-4" /> Enviar para uma aula
          </button>
        )}
      </div>

      {showUpload && (
        <div className="space-y-3 pt-2 border-t border-gray-100">
          {loadingCourses ? (
            <p className="text-sm text-gray-400">Carregando seus cursos...</p>
          ) : (
            <>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1.5">Curso</label>
                <select value={courseId} onChange={e => handleSelectCourse(e.target.value)}
                  className="w-full px-3 py-2 border border-gray-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-300">
                  <option value="">Selecione...</option>
                  {courses.map(c => <option key={c.id} value={c.id}>{c.title}</option>)}
                </select>
              </div>

              {courseDetail && (
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1.5">Módulo</label>
                  <select value={moduleId} onChange={e => { setModuleId(e.target.value); setLessonId(''); }}
                    className="w-full px-3 py-2 border border-gray-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-300">
                    <option value="">Selecione...</option>
                    {courseDetail.modules.map(m => <option key={m.id} value={m.id}>{m.title}</option>)}
                  </select>
                </div>
              )}

              {moduleId && courseDetail && (
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1.5">Aula</label>
                  <select value={lessonId} onChange={e => setLessonId(e.target.value)}
                    className="w-full px-3 py-2 border border-gray-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-300">
                    <option value="">Selecione...</option>
                    {courseDetail.modules.find(m => m.id === moduleId)?.lessons.map(l => (
                      <option key={l.id} value={l.id}>{l.title}</option>
                    ))}
                  </select>
                </div>
              )}

              <button onClick={handleUpload} disabled={uploading || !lessonId}
                className="btn-primary flex items-center gap-2 disabled:opacity-50">
                <Upload className="w-4 h-4" /> {uploading ? 'Enviando...' : 'Confirmar envio'}
              </button>
            </>
          )}
        </div>
      )}
    </div>
  );
}

export default function GravadorPage() {
  return (
    <Suspense fallback={<p className="text-gray-400 text-sm">Carregando...</p>}>
      <GravadorInner />
    </Suspense>
  );
}
