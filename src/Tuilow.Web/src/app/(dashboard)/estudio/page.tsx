'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { Sparkles, Plus, Trash2, Mic, MonitorPlay, Rocket, Loader2 } from 'lucide-react';
import { studioApi, coursesApi } from '@/lib/api';
import type {
  CreatorStyleProfile, CourseOutlineSuggestion, LessonScriptSuggestion, LessonScriptItem,
  RecordingTemplateItem, AudienceLevel,
} from '@/types';

const LEVEL_LABEL: Record<AudienceLevel, string> = {
  Beginner: 'Iniciante', Intermediate: 'Intermediário', Advanced: 'Avançado',
};

type Tab = 'nicho' | 'roteiros' | 'templates';

/**
 * Estúdio do Criador — assistente de IA que ajuda o criador a planejar (nicho → estrutura do
 * curso), criar roteiros de gravação por aula, e organizar templates reutilizáveis. Gravação
 * (teleprompter/gravador de vídeo) fica em telas dedicadas (/estudio/teleprompter,
 * /estudio/gravador) para não sobrecarregar esta página.
 */
export default function EstudioPage() {
  const router = useRouter();
  const queryClient = useQueryClient();
  const [tab, setTab] = useState<Tab>('nicho');

  const { data: profile } = useQuery<CreatorStyleProfile | null>({
    queryKey: ['studio-niche'],
    queryFn: () => studioApi.getNiche().then(r => r.data),
  });

  return (
    <div className="max-w-4xl mx-auto">
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-800 flex items-center gap-2">
          🎙️ Estúdio do Criador
        </h1>
        <p className="text-gray-500 mt-1">
          Planeje, roteirize e grave suas aulas com a ajuda da IA — do nicho à publicação.
        </p>
      </div>

      {profile && <CloneProgressBanner profile={profile} />}

      <div className="flex items-center gap-1 mb-6 border-b border-gray-200">
        {([
          { id: 'nicho' as const, label: 'Nicho & Estrutura' },
          { id: 'roteiros' as const, label: 'Meus Roteiros' },
          { id: 'templates' as const, label: 'Templates de Gravação' },
        ]).map(t => (
          <button
            key={t.id}
            onClick={() => setTab(t.id)}
            className={`px-4 py-2.5 text-sm font-medium border-b-2 -mb-px transition-colors
              ${tab === t.id ? 'border-brand-600 text-brand-700' : 'border-transparent text-gray-400 hover:text-gray-600'}`}
          >
            {t.label}
          </button>
        ))}
      </div>

      {tab === 'nicho' && <NichoTab profile={profile ?? null} onNicheSaved={() => queryClient.invalidateQueries({ queryKey: ['studio-niche'] })} router={router} />}
      {tab === 'roteiros' && <RoteirosTab />}
      {tab === 'templates' && <TemplatesTab />}
    </div>
  );
}

function CloneProgressBanner({ profile }: { profile: CreatorStyleProfile }) {
  const pct = Math.min(100, Math.round((profile.recordedScriptsCount / profile.scriptsRequiredForClone) * 100));

  return (
    <div className="card border-violet-100 bg-violet-50/60 mb-6">
      <div className="flex items-center justify-between mb-2">
        <p className="font-semibold text-gray-800 flex items-center gap-1.5">
          ✨ Clone do Professor <span className="badge-purple">Premium</span>
        </p>
        <span className="text-xs text-gray-500">
          {profile.recordedScriptsCount}/{profile.scriptsRequiredForClone} roteiros gravados
        </span>
      </div>
      <div className="w-full h-2 rounded-full bg-white overflow-hidden mb-2">
        <div className="h-full bg-gradient-to-r from-violet-500 to-accent-500 rounded-full transition-all"
          style={{ width: `${pct}%` }} />
      </div>
      <p className="text-xs text-gray-500">
        {profile.isCloneReady
          ? 'Você já tem roteiros suficientes! Essa funcionalidade chega em breve — a IA vai aprender seu estilo e sugerir roteiros, títulos e CTAs automaticamente.'
          : `Grave usando ${profile.scriptsRequiredForClone - profile.recordedScriptsCount} roteiro(s) a mais para desbloquear: a IA aprende seu estilo e passa a gerar roteiros, títulos e CTAs no seu padrão.`}
      </p>
    </div>
  );
}

function NichoTab({
  profile, onNicheSaved, router,
}: {
  profile: CreatorStyleProfile | null;
  onNicheSaved: () => void;
  router: ReturnType<typeof useRouter>;
}) {
  const [niche, setNiche] = useState('');
  const [targetAudience, setTargetAudience] = useState('');
  const [objective, setObjective] = useState('');
  const [level, setLevel] = useState<AudienceLevel>('Beginner');
  const [hydrated, setHydrated] = useState(false);
  const [saving, setSaving] = useState(false);
  const [generatingOutline, setGeneratingOutline] = useState(false);
  const [outline, setOutline] = useState<CourseOutlineSuggestion | null>(null);
  const [creatingCourse, setCreatingCourse] = useState(false);
  const [activeLesson, setActiveLesson] = useState<string | null>(null);
  const [scriptDraft, setScriptDraft] = useState<LessonScriptSuggestion | null>(null);
  const [generatingScript, setGeneratingScript] = useState(false);
  const [savingScript, setSavingScript] = useState(false);

  useEffect(() => {
    if (hydrated || !profile) return;
    setNiche(profile.niche);
    setTargetAudience(profile.targetAudience);
    setObjective(profile.objective);
    setLevel(profile.level);
    setHydrated(true);
  }, [profile, hydrated]);

  const canGenerate = niche.trim() && targetAudience.trim() && objective.trim();

  const handleSaveNiche = async () => {
    if (!canGenerate) { toast.error('Preencha nicho, público-alvo e objetivo.'); return; }
    setSaving(true);
    try {
      await studioApi.setNiche({ niche, targetAudience, objective, level });
      toast.success('Perfil de nicho salvo.');
      onNicheSaved();
    } catch {
      toast.error('Não foi possível salvar o nicho.');
    } finally {
      setSaving(false);
    }
  };

  const handleGenerateOutline = async () => {
    if (!canGenerate) { toast.error('Preencha nicho, público-alvo e objetivo primeiro.'); return; }
    setGeneratingOutline(true);
    setOutline(null);
    try {
      const { data } = await studioApi.generateCourseOutline({ niche, targetAudience, objective, level });
      setOutline(data);
      toast.success('Estrutura gerada — revise e ajuste como preferir.');
    } catch {
      toast.error('Não foi possível gerar a estrutura agora.');
    } finally {
      setGeneratingOutline(false);
    }
  };

  const handleCreateCourse = async () => {
    if (!outline) return;
    setCreatingCourse(true);
    try {
      const { data: created } = await coursesApi.create({
        title: outline.courseName,
        description: outline.courseDescription,
        shortDescription: outline.courseDescription.slice(0, 150),
        level,
        price: 0,
        productType: 'Course',
      });
      const courseId = created.id;

      for (const mod of outline.modules) {
        const { data: createdModule } = await coursesApi.addModule(courseId, { title: mod.title });
        for (const lesson of mod.lessons) {
          await coursesApi.addLesson(courseId, createdModule.id, { title: lesson.title });
        }
      }

      toast.success('Curso criado a partir da estrutura! Continue no assistente de produto.');
      router.push(`/admin/produtos/novo?courseId=${courseId}`);
    } catch {
      toast.error('Não foi possível criar o curso agora.');
    } finally {
      setCreatingCourse(false);
    }
  };

  const handleGenerateScript = async (lessonTitle: string) => {
    if (!canGenerate) { toast.error('Preencha o nicho primeiro.'); return; }
    setActiveLesson(lessonTitle);
    setScriptDraft(null);
    setGeneratingScript(true);
    try {
      const { data } = await studioApi.generateLessonScript({ lessonTitle, niche, targetAudience, level });
      setScriptDraft(data);
    } catch {
      toast.error('Não foi possível gerar o roteiro agora.');
    } finally {
      setGeneratingScript(false);
    }
  };

  const handleSaveScript = async () => {
    if (!scriptDraft || !activeLesson) return;
    setSavingScript(true);
    try {
      await studioApi.saveScript({
        lessonTitle: activeLesson,
        introduction: scriptDraft.introduction,
        developmentTopics: scriptDraft.developmentTopics,
        demonstrationSuggestions: scriptDraft.demonstrationSuggestions,
        closingCta: scriptDraft.closingCta,
      });
      toast.success('Roteiro salvo — veja em "Meus Roteiros".');
      setActiveLesson(null);
      setScriptDraft(null);
    } catch {
      toast.error('Não foi possível salvar o roteiro.');
    } finally {
      setSavingScript(false);
    }
  };

  return (
    <div className="space-y-6">
      <div className="card border-gray-200 space-y-4">
        <p className="font-semibold text-gray-800">Passo 1 — Identificação do Nicho</p>
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1.5">Nicho principal</label>
          <input
            value={niche}
            onChange={e => setNiche(e.target.value)}
            placeholder="Ex.: Personal Trainer, Nutricionista, Advogado, Professor de Inglês..."
            className="w-full px-3 py-2 border border-gray-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-300"
          />
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1.5">Público-alvo</label>
          <input
            value={targetAudience}
            onChange={e => setTargetAudience(e.target.value)}
            placeholder="Ex.: Pessoas que querem emagrecer sem tempo para academia"
            className="w-full px-3 py-2 border border-gray-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-300"
          />
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1.5">Objetivo do curso</label>
          <input
            value={objective}
            onChange={e => setObjective(e.target.value)}
            placeholder="Ex.: Ensinar os fundamentos do emagrecimento saudável"
            className="w-full px-3 py-2 border border-gray-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-300"
          />
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1.5">Nível dos alunos</label>
          <select
            value={level}
            onChange={e => setLevel(e.target.value as AudienceLevel)}
            className="px-3 py-2 border border-gray-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-300"
          >
            {(Object.keys(LEVEL_LABEL) as AudienceLevel[]).map(l => (
              <option key={l} value={l}>{LEVEL_LABEL[l]}</option>
            ))}
          </select>
        </div>
        <div className="flex items-center gap-3 pt-2">
          <button onClick={handleSaveNiche} disabled={saving} className="btn-ghost disabled:opacity-50">
            {saving ? 'Salvando...' : 'Salvar nicho'}
          </button>
          <button onClick={handleGenerateOutline} disabled={generatingOutline}
            className="btn-primary flex items-center gap-2 disabled:opacity-50">
            {generatingOutline ? <Loader2 className="w-4 h-4 animate-spin" /> : <Sparkles className="w-4 h-4" />}
            {generatingOutline ? 'Gerando estrutura...' : 'Gerar estrutura do curso'}
          </button>
        </div>
      </div>

      {outline && (
        <div className="card border-gray-200 space-y-4">
          <div className="flex items-center justify-between gap-3">
            <div>
              <p className="font-semibold text-gray-800">{outline.courseName}</p>
              <p className="text-sm text-gray-500 mt-1">{outline.courseDescription}</p>
            </div>
            <button onClick={handleCreateCourse} disabled={creatingCourse}
              className="btn-primary flex items-center gap-2 whitespace-nowrap disabled:opacity-50">
              {creatingCourse ? <Loader2 className="w-4 h-4 animate-spin" /> : <Rocket className="w-4 h-4" />}
              {creatingCourse ? 'Criando...' : 'Criar curso com esta estrutura'}
            </button>
          </div>

          <div className="space-y-4">
            {outline.modules.map((mod, mi) => (
              <div key={mi} className="border border-gray-200 rounded-lg p-4">
                <p className="font-medium text-gray-800 mb-2">{mod.title}</p>
                <ul className="space-y-2">
                  {mod.lessons.map((lesson, li) => (
                    <li key={li}>
                      <div className="flex items-center justify-between gap-3 text-sm">
                        <span className="flex items-center gap-2">
                          <span className="badge-purple">{lesson.format}</span>
                          {lesson.title}
                        </span>
                        <button
                          onClick={() => handleGenerateScript(lesson.title)}
                          className="text-xs text-brand-600 hover:underline whitespace-nowrap flex items-center gap-1"
                        >
                          <Sparkles className="w-3.5 h-3.5" /> Gerar roteiro
                        </button>
                      </div>

                      {activeLesson === lesson.title && (
                        <div className="mt-3 rounded-lg border border-brand-200 bg-brand-50/40 p-4 space-y-3">
                          {generatingScript ? (
                            <p className="text-sm text-gray-500 flex items-center gap-2">
                              <Loader2 className="w-4 h-4 animate-spin" /> Gerando roteiro...
                            </p>
                          ) : scriptDraft ? (
                            <>
                              <div>
                                <p className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-1">Introdução</p>
                                <p className="text-sm text-gray-700">{scriptDraft.introduction}</p>
                              </div>
                              <div>
                                <p className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-1">Desenvolvimento</p>
                                <ul className="list-disc list-inside text-sm text-gray-700 space-y-0.5">
                                  {scriptDraft.developmentTopics.map((t, i) => <li key={i}>{t}</li>)}
                                </ul>
                              </div>
                              <div>
                                <p className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-1">Demonstrações</p>
                                <ul className="list-disc list-inside text-sm text-gray-700 space-y-0.5">
                                  {scriptDraft.demonstrationSuggestions.map((t, i) => <li key={i}>{t}</li>)}
                                </ul>
                              </div>
                              <div>
                                <p className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-1">Encerramento</p>
                                <p className="text-sm text-gray-700">{scriptDraft.closingCta}</p>
                              </div>
                              <div className="flex items-center gap-3 pt-1">
                                <button onClick={handleSaveScript} disabled={savingScript}
                                  className="btn-primary text-xs py-1.5 px-4 disabled:opacity-50">
                                  {savingScript ? 'Salvando...' : 'Salvar roteiro'}
                                </button>
                                <button onClick={() => { setActiveLesson(null); setScriptDraft(null); }}
                                  className="text-xs text-gray-400 hover:text-gray-600">
                                  Fechar
                                </button>
                              </div>
                            </>
                          ) : null}
                        </div>
                      )}
                    </li>
                  ))}
                </ul>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

function RoteirosTab() {
  const queryClient = useQueryClient();
  const { data: scripts = [], isLoading } = useQuery<LessonScriptItem[]>({
    queryKey: ['studio-scripts'],
    queryFn: () => studioApi.getMyScripts().then(r => r.data),
  });

  const handleMarkRecorded = async (id: string) => {
    try {
      await studioApi.markScriptAsRecorded(id);
      toast.success('Roteiro marcado como gravado! 🎉');
      queryClient.invalidateQueries({ queryKey: ['studio-scripts'] });
      queryClient.invalidateQueries({ queryKey: ['studio-niche'] });
    } catch {
      toast.error('Não foi possível marcar como gravado.');
    }
  };

  if (isLoading) return <p className="text-gray-400 text-sm">Carregando...</p>;

  if (scripts.length === 0) {
    return (
      <div className="text-center py-16 text-gray-400">
        <p className="text-4xl mb-4">📝</p>
        <p className="text-lg font-medium">Você ainda não salvou nenhum roteiro</p>
        <p className="text-sm mt-1">Gere um roteiro na aba &quot;Nicho &amp; Estrutura&quot; e salve para vê-lo aqui.</p>
      </div>
    );
  }

  return (
    <div className="space-y-3">
      {scripts.map(script => (
        <div key={script.id} className="card border-gray-200">
          <div className="flex items-center justify-between gap-3 mb-2">
            <p className="font-semibold text-gray-800">{script.lessonTitle}</p>
            {script.wasRecorded
              ? <span className="badge-green flex-shrink-0">Gravado</span>
              : <span className="badge-purple flex-shrink-0">Não gravado</span>}
          </div>
          <p className="text-sm text-gray-600 line-clamp-2 mb-3">{script.introduction}</p>
          <div className="flex items-center gap-4 flex-wrap">
            <a href={`/estudio/teleprompter?scriptId=${script.id}`} target="_blank" rel="noopener noreferrer"
              className="text-xs text-brand-600 hover:underline flex items-center gap-1">
              <MonitorPlay className="w-3.5 h-3.5" /> Abrir no teleprompter
            </a>
            <a href={`/estudio/gravador?scriptId=${script.id}`} target="_blank" rel="noopener noreferrer"
              className="text-xs text-brand-600 hover:underline flex items-center gap-1">
              <Mic className="w-3.5 h-3.5" /> Ir gravar
            </a>
            {!script.wasRecorded && (
              <button onClick={() => handleMarkRecorded(script.id)}
                className="text-xs text-gray-500 hover:text-gray-700">
                Marcar como gravado
              </button>
            )}
          </div>
        </div>
      ))}
    </div>
  );
}

function TemplatesTab() {
  const queryClient = useQueryClient();
  const { data: templates = [], isLoading } = useQuery<RecordingTemplateItem[]>({
    queryKey: ['studio-templates'],
    queryFn: () => studioApi.getMyTemplates().then(r => r.data),
  });

  const [name, setName] = useState('');
  const [sections, setSections] = useState<string[]>(['Vinheta inicial', 'Apresentação', 'Conteúdo', 'Resumo', 'Chamada para ação']);
  const [saving, setSaving] = useState(false);

  const addSection = () => setSections([...sections, '']);
  const updateSection = (i: number, value: string) => setSections(sections.map((s, idx) => (idx === i ? value : s)));
  const removeSection = (i: number) => setSections(sections.filter((_, idx) => idx !== i));

  const handleSave = async () => {
    if (!name.trim()) { toast.error('Dê um nome ao template.'); return; }
    setSaving(true);
    try {
      await studioApi.saveTemplate({
        name, sections: sections.filter(s => s.trim()), isDefault: templates.length === 0,
      });
      toast.success('Template salvo.');
      setName('');
      setSections(['Vinheta inicial', 'Apresentação', 'Conteúdo', 'Resumo', 'Chamada para ação']);
      queryClient.invalidateQueries({ queryKey: ['studio-templates'] });
    } catch {
      toast.error('Não foi possível salvar o template.');
    } finally {
      setSaving(false);
    }
  };

  const handleSetDefault = async (t: RecordingTemplateItem) => {
    try {
      await studioApi.saveTemplate({ name: t.name, sections: t.sections, isDefault: true, templateId: t.id });
      queryClient.invalidateQueries({ queryKey: ['studio-templates'] });
    } catch {
      toast.error('Não foi possível definir como padrão.');
    }
  };

  const handleDelete = async (id: string) => {
    try {
      await studioApi.deleteTemplate(id);
      toast.success('Template removido.');
      queryClient.invalidateQueries({ queryKey: ['studio-templates'] });
    } catch {
      toast.error('Não foi possível remover o template.');
    }
  };

  return (
    <div className="space-y-6">
      <div className="card border-gray-200 space-y-4">
        <p className="font-semibold text-gray-800">Novo template</p>
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1.5">Nome</label>
          <input
            value={name}
            onChange={e => setName(e.target.value)}
            placeholder="Ex.: Padrão de aula teórica"
            className="w-full px-3 py-2 border border-gray-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-300"
          />
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1.5">Seções (nesta ordem)</label>
          <div className="space-y-2">
            {sections.map((section, i) => (
              <div key={i} className="flex items-center gap-2">
                <span className="text-xs text-gray-400 w-5 flex-shrink-0">{i + 1}.</span>
                <input
                  value={section}
                  onChange={e => updateSection(i, e.target.value)}
                  className="flex-1 px-3 py-2 border border-gray-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-300"
                />
                <button onClick={() => removeSection(i)} className="p-2 text-gray-400 hover:text-red-500 transition-colors flex-shrink-0">
                  <Trash2 className="w-4 h-4" />
                </button>
              </div>
            ))}
          </div>
          <button onClick={addSection} className="flex items-center gap-1.5 text-sm text-brand-600 hover:underline mt-2">
            <Plus className="w-4 h-4" /> Adicionar seção
          </button>
        </div>
        <button onClick={handleSave} disabled={saving} className="btn-primary disabled:opacity-50">
          {saving ? 'Salvando...' : 'Salvar template'}
        </button>
      </div>

      {isLoading ? (
        <p className="text-gray-400 text-sm">Carregando...</p>
      ) : templates.length === 0 ? (
        <p className="text-gray-400 text-sm text-center py-8">Nenhum template criado ainda.</p>
      ) : (
        <div className="space-y-3">
          {templates.map(t => (
            <div key={t.id} className="card border-gray-200">
              <div className="flex items-center justify-between gap-3 mb-2">
                <p className="font-semibold text-gray-800 flex items-center gap-2">
                  {t.name}
                  {t.isDefault && <span className="badge-green">Padrão</span>}
                </p>
                <div className="flex items-center gap-3 flex-shrink-0">
                  {!t.isDefault && (
                    <button onClick={() => handleSetDefault(t)} className="text-xs text-brand-600 hover:underline">
                      Definir como padrão
                    </button>
                  )}
                  <button onClick={() => handleDelete(t.id)} className="text-gray-400 hover:text-red-500 transition-colors">
                    <Trash2 className="w-4 h-4" />
                  </button>
                </div>
              </div>
              <p className="text-sm text-gray-500">{t.sections.join(' → ')}</p>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
