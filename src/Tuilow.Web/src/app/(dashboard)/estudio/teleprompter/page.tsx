'use client';

import { Suspense, useEffect, useRef, useState } from 'react';
import { useSearchParams } from 'next/navigation';
import { useQuery } from '@tanstack/react-query';
import { Play, Pause, Maximize, Minimize, Type, Gauge } from 'lucide-react';
import { studioApi } from '@/lib/api';
import type { LessonScriptItem } from '@/types';

function TeleprompterInner() {
  const params = useSearchParams();
  const scriptId = params.get('scriptId');

  const { data: scripts = [], isLoading } = useQuery<LessonScriptItem[]>({
    queryKey: ['studio-scripts'],
    queryFn: () => studioApi.getMyScripts().then(r => r.data),
  });

  const script = scripts.find(s => s.id === scriptId);

  const [playing, setPlaying] = useState(false);
  const [speed, setSpeed] = useState(30); // px/segundo
  const [fontSize, setFontSize] = useState(32);
  const [fullscreen, setFullscreen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);
  const rafRef = useRef<number | null>(null);
  const lastTsRef = useRef<number | null>(null);

  useEffect(() => {
    if (!playing) {
      if (rafRef.current) cancelAnimationFrame(rafRef.current);
      lastTsRef.current = null;
      return;
    }

    const step = (ts: number) => {
      if (lastTsRef.current === null) lastTsRef.current = ts;
      const dt = (ts - lastTsRef.current) / 1000;
      lastTsRef.current = ts;

      const el = containerRef.current;
      if (el) {
        el.scrollTop += speed * dt;
        if (el.scrollTop + el.clientHeight >= el.scrollHeight) setPlaying(false);
      }
      rafRef.current = requestAnimationFrame(step);
    };

    rafRef.current = requestAnimationFrame(step);
    return () => { if (rafRef.current) cancelAnimationFrame(rafRef.current); };
  }, [playing, speed]);

  const toggleFullscreen = () => {
    if (!document.fullscreenElement) {
      document.documentElement.requestFullscreen();
      setFullscreen(true);
    } else {
      document.exitFullscreen();
      setFullscreen(false);
    }
  };

  useEffect(() => {
    const onChange = () => setFullscreen(!!document.fullscreenElement);
    document.addEventListener('fullscreenchange', onChange);
    return () => document.removeEventListener('fullscreenchange', onChange);
  }, []);

  if (isLoading) {
    return <div className="min-h-screen bg-black flex items-center justify-center text-white/60 text-sm">Carregando...</div>;
  }

  if (!script) {
    return (
      <div className="min-h-screen bg-black flex items-center justify-center text-white/60 text-sm">
        Roteiro não encontrado.
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-black flex flex-col">
      {/* Controles */}
      <div className="flex items-center gap-4 px-4 py-3 bg-gray-900 border-b border-gray-800 flex-wrap">
        <button
          onClick={() => setPlaying(p => !p)}
          className="flex items-center gap-2 bg-brand-600 hover:bg-brand-700 text-white text-sm px-4 py-1.5 rounded-lg transition-colors"
        >
          {playing ? <Pause className="w-4 h-4" /> : <Play className="w-4 h-4" />}
          {playing ? 'Pausar' : 'Rolar'}
        </button>

        <div className="flex items-center gap-2 text-white/70 text-xs">
          <Gauge className="w-4 h-4" />
          <input type="range" min={10} max={120} value={speed} onChange={e => setSpeed(Number(e.target.value))} className="w-24" />
          <span className="w-10">{speed}px/s</span>
        </div>

        <div className="flex items-center gap-2 text-white/70 text-xs">
          <Type className="w-4 h-4" />
          <input type="range" min={18} max={64} value={fontSize} onChange={e => setFontSize(Number(e.target.value))} className="w-24" />
          <span className="w-10">{fontSize}px</span>
        </div>

        <button
          onClick={toggleFullscreen}
          className="ml-auto flex items-center gap-2 text-white/70 hover:text-white text-xs"
        >
          {fullscreen ? <Minimize className="w-4 h-4" /> : <Maximize className="w-4 h-4" />}
          {fullscreen ? 'Sair da tela cheia' : 'Tela cheia'}
        </button>
      </div>

      {/* Roteiro */}
      <div ref={containerRef} className="flex-1 overflow-y-auto px-8 py-16 md:px-24">
        <div className="max-w-3xl mx-auto text-white leading-relaxed" style={{ fontSize: `${fontSize}px` }}>
          <p className="text-brand-400 uppercase text-sm tracking-wider mb-2">{script.lessonTitle}</p>

          <p className="mb-8">{script.introduction}</p>

          {script.developmentTopics.length > 0 && (
            <div className="mb-8">
              <p className="text-white/40 uppercase text-sm tracking-wider mb-2">Desenvolvimento</p>
              {script.developmentTopics.map((t, i) => <p key={i} className="mb-4">{t}</p>)}
            </div>
          )}

          {script.demonstrationSuggestions.length > 0 && (
            <div className="mb-8">
              <p className="text-white/40 uppercase text-sm tracking-wider mb-2">Demonstrações</p>
              {script.demonstrationSuggestions.map((t, i) => <p key={i} className="mb-4">{t}</p>)}
            </div>
          )}

          <p className="text-white/40 uppercase text-sm tracking-wider mb-2">Encerramento</p>
          <p className="mb-24">{script.closingCta}</p>
        </div>
      </div>
    </div>
  );
}

export default function TeleprompterPage() {
  return (
    <Suspense fallback={<div className="min-h-screen bg-black flex items-center justify-center text-white/60 text-sm">Carregando...</div>}>
      <TeleprompterInner />
    </Suspense>
  );
}
