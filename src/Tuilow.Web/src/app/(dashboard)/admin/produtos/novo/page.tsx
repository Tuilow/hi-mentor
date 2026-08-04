'use client';

import { useEffect, useState, Suspense } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { useQuery } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import {
  Sparkles, Upload, Link2, Plus, Video, Check, Loader2,
  ChevronRight, ChevronLeft, Paperclip, GraduationCap, RefreshCw, Users, CalendarDays,
  BookOpen, Briefcase,
} from 'lucide-react';
import {
  API_URL, coursesApi, videosApi, materialsApi, creatorStudioApi, courseSubscriptionPlansApi, categoriesApi,
} from '@/lib/api';
import { CategoryAutocomplete } from '@/components/ui/CategoryAutocomplete';
import type {
  ProductDetail, PublicationChecklist, ModuleDetail, LessonDetail, ProductType,
} from '@/types';

const STEPS = [
  'Info Básica', 'Conteúdo', 'Organização', 'Materiais', 'Preço', 'Página de Vendas', 'Publicação',
];

// ─── Passo 0: seleção do tipo de produto ─────────────────────────────────
// Puramente classificatório — todos os tipos usam o mesmo mecanismo de entrega (módulos/
// aulas/vídeo) por trás. Serve para o criador rotular a oferta e para o front dar contexto
// (título/copy) ao restante do assistente. Ver Tuilow.Catalog.Domain.Enums.ProductType.
const PRODUCT_TYPES: Array<{
  id: ProductType; label: string; description: string; icon: typeof GraduationCap;
}> = [
  { id: 'Course', label: 'Curso Online', description: 'Aulas em vídeo organizadas em módulos, no seu ritmo.', icon: GraduationCap },
  { id: 'Subscription', label: 'Assinatura', description: 'Acesso recorrente, com cobrança mensal, trimestral ou anual.', icon: RefreshCw },
  { id: 'Mentoring', label: 'Mentoria', description: 'Acompanhamento próximo, individual ou em grupo.', icon: Users },
  { id: 'Event', label: 'Evento', description: 'Encontro ao vivo, único ou em série de datas.', icon: CalendarDays },
  { id: 'Ebook', label: 'E-book', description: 'Conteúdo em PDF para leitura e download.', icon: BookOpen },
  { id: 'Service', label: 'Serviço', description: 'Entrega personalizada, sob demanda.', icon: Briefcase },
];

function ProductTypeStep({ onSelect }: { onSelect: (type: ProductType) => void }) {
  return (
    <div className="max-w-4xl mx-auto animate-fade-in">
      <h1 className="text-2xl font-bold text-gray-800 mb-1">Que tipo de produto você quer criar?</h1>
      <p className="text-gray-500 text-sm mb-6">
        Escolha o formato — você poderá revisar tudo antes de publicar.
      </p>
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
        {PRODUCT_TYPES.map(({ id, label, description, icon: Icon }) => (
          <button
            key={id}
            onClick={() => onSelect(id)}
            className="group text-left rounded-2xl border-2 border-gray-200 bg-white p-5
              transition-all hover:border-brand-400 hover:shadow-lg hover:-translate-y-0.5
              focus:outline-none focus:ring-2 focus:ring-brand-500"
          >
            <div className="w-11 h-11 rounded-xl bg-brand-50 text-brand-600 flex items-center justify-center
              mb-3 transition-colors group-hover:bg-brand-600 group-hover:text-white">
              <Icon className="w-5 h-5" />
            </div>
            <p className="font-semibold text-gray-800">{label}</p>
            <p className="text-sm text-gray-500 mt-1">{description}</p>
          </button>
        ))}
      </div>
    </div>
  );
}

interface LocalVideo {
  videoId: string;
  title: string;
  source: string;
  durationSeconds?: number | null;
  status?: string;
}

interface TestimonialDraft {
  authorName: string;
  authorRole: string;
  quote: string;
  avatarUrl: string;
}

type PricingMode = 'free' | 'onetime' | 'subscription';
type BillingCycle = 'Monthly' | 'Quarterly' | 'Semiannual' | 'Annual';

function ProductWizard() {
  const router = useRouter();
  const params = useSearchParams();
  const existingCourseId = params.get('courseId');

  // Produtos novos começam no passo 0 (escolha do tipo); ao editar um rascunho existente,
  // o tipo já foi escolhido — pula direto para o passo 1.
  const [step, setStep] = useState(existingCourseId ? 1 : 0);
  const [courseId, setCourseId] = useState<string | null>(existingCourseId);
  const [saving, setSaving] = useState(false);

  // Passo 0
  const [productType, setProductType] = useState<ProductType | null>(null);

  // Passo 1
  const [name, setName] = useState('');
  const [category, setCategory] = useState('');
  const [subcategory, setSubcategory] = useState('');
  const [shortDescription, setShortDescription] = useState('');
  const [fullDescription, setFullDescription] = useState('');
  const [generatingCopy, setGeneratingCopy] = useState(false);

  // Sugestões de Categoria/Subcategoria (autocomplete) — lista curada + o que já existe em
  // outros cursos (ver GetCategoriesQueryHandler). Falha silenciosa: sem a lista, os campos
  // continuam funcionando como texto livre, só sem sugestões.
  const { data: categoryOptions = [] } = useQuery({
    queryKey: ['categories'],
    queryFn: () => categoriesApi.list().then(r => r.data),
    staleTime: 5 * 60 * 1000,
  });
  const matchedCategory = categoryOptions.find(
    c => c.name.toLowerCase() === category.trim().toLowerCase()
  );

  // Passo 2
  const [videos, setVideos] = useState<LocalVideo[]>([]);
  const [importUrl, setImportUrl] = useState('');
  const [importing, setImporting] = useState(false);

  // Passo 3
  const [modules, setModules] = useState<ModuleDetail[]>([]);

  // Passo 5
  const [pricingMode, setPricingMode] = useState<PricingMode>('free');
  const [price, setPrice] = useState(97);
  const [billingCycle, setBillingCycle] = useState<BillingCycle>('Monthly');

  // Passo 6
  const [headline, setHeadline] = useState('');
  const [subheadline, setSubheadline] = useState('');
  const [ctaText, setCtaText] = useState('');
  const [benefits, setBenefits] = useState<string[]>([]);
  const [faq, setFaq] = useState<{ question: string; answer: string }[]>([]);
  const [generatingSalesPage, setGeneratingSalesPage] = useState(false);
  const [videoUrl, setVideoUrl] = useState('');
  const [testimonials, setTestimonials] = useState<TestimonialDraft[]>([]);
  const [guaranteeDays, setGuaranteeDays] = useState<number | ''>('');
  const [guaranteeText, setGuaranteeText] = useState('');

  // Passo 7
  const [checklist, setChecklist] = useState<PublicationChecklist | null>(null);
  const [publishing, setPublishing] = useState(false);

  // Recarrega os vídeos já enviados/importados para este produto sempre que o courseId estiver
  // disponível — sem isso, sair e voltar ao assistente antes de vincular o vídeo a uma aula
  // (passo 3) fazia a lista "sumir" (ela só existia na memória da página).
  useEffect(() => {
    if (!courseId) return;
    videosApi.listByCourse(courseId).then(({ data }) => {
      setVideos(data.map((v: { videoId: string; title: string | null; source: string; durationSeconds: number | null; status?: string }) => ({
        videoId: v.videoId, title: v.title ?? '(sem título)', source: v.source, durationSeconds: v.durationSeconds, status: v.status,
      })));
    }).catch(() => { /* melhor deixar a lista vazia do que travar o assistente */ });
  }, [courseId]);

  // Enquanto algum vídeo estiver "Enviando..."/"Processando" (upload direto de arquivo em
  // andamento), consulta a lista de novo a cada poucos segundos pra atualizar o status na tela —
  // sem isso, o criador não teria nenhum feedback de quando o processamento termina.
  useEffect(() => {
    if (!courseId) return;
    const hasPending = videos.some(v => v.status === 'Uploading' || v.status === 'Processing');
    if (!hasPending) return;

    const interval = setInterval(() => {
      videosApi.listByCourse(courseId).then(({ data }) => {
        setVideos(data.map((v: { videoId: string; title: string | null; source: string; durationSeconds: number | null; status?: string }) => ({
          videoId: v.videoId, title: v.title ?? '(sem título)', source: v.source, durationSeconds: v.durationSeconds, status: v.status,
        })));
      }).catch(() => { /* próxima tentativa resolve */ });
    }, 4000);

    return () => clearInterval(interval);
  }, [courseId, videos]);

  // Hidrata o assistente ao editar um rascunho existente. Busca o curso e o plano de assinatura
  // em paralelo: quando o preço é "Assinatura", o preço do CURSO fica 0 (ver handleSavePricing
  // mais abaixo, que sempre chama coursesApi.setPrice(courseId, 0) nesse modo) — então "isFree"
  // sozinho não distingue Grátis de Assinatura, e a tela sempre reabria marcada como "Grátis"
  // mesmo com uma assinatura ativa configurada. O plano é a fonte de verdade quando existe.
  useEffect(() => {
    if (!existingCourseId) return;
    Promise.all([
      coursesApi.getByIdAdmin(existingCourseId),
      courseSubscriptionPlansApi.getByCourse(existingCourseId),
    ]).then(([courseRes, plansRes]) => {
      const data = courseRes.data as ProductDetail;
      const activePlan = plansRes.data[0];

      setName(data.title);
      setProductType(data.productType);
      setCategory(data.category ?? '');
      setSubcategory(data.subcategory ?? '');
      setShortDescription(data.shortDescription ?? '');
      setFullDescription(data.description);
      setModules(data.modules);
      if (activePlan) {
        setPricingMode('subscription');
        setPrice(activePlan.price);
        setBillingCycle(activePlan.billingCycle);
      } else {
        setPrice(data.price || 97);
        setPricingMode(data.isFree ? 'free' : 'onetime');
      }
      setHeadline(data.salesPageHeadline ?? '');
      setSubheadline(data.salesPageSubheadline ?? '');
      setCtaText(data.salesPageCtaText ?? '');
      setBenefits(data.salesPageBenefits ?? []);
      setFaq((data.faqItems ?? []).map(f => ({ question: f.question, answer: f.answer })));
      setVideoUrl(data.salesPageVideoUrl ?? '');
      setTestimonials((data.testimonials ?? []).map(t => ({
        authorName: t.authorName, authorRole: t.authorRole ?? '', quote: t.quote, avatarUrl: t.avatarUrl ?? '',
      })));
      setGuaranteeDays(data.guaranteeDays ?? '');
      setGuaranteeText(data.guaranteeText ?? '');
    }).catch(() => toast.error('Não foi possível carregar este produto.'));
  }, [existingCourseId]);

  useEffect(() => {
    if (step === 7 && courseId) {
      creatorStudioApi.publicationChecklist(courseId)
        .then(({ data }) => setChecklist(data))
        .catch(() => toast.error('Não foi possível carregar o checklist.'));
    }
  }, [step, courseId]);

  const refreshCourse = async (id: string) => {
    const { data } = await coursesApi.getByIdAdmin(id);
    setModules(data.modules);
    return data as ProductDetail;
  };

  // ─── Passo 0 ────────────────────────────────────────────────────────────
  const handleSelectType = (type: ProductType) => {
    setProductType(type);
    setStep(1);
  };

  // ─── Passo 1 ────────────────────────────────────────────────────────────
  const handleGenerateCopy = async () => {
    if (!name.trim()) { toast.error('Informe o nome do produto primeiro.'); return; }
    setGeneratingCopy(true);
    try {
      const { data } = await creatorStudioApi.generateProductCopy({
        productName: name, category: category || undefined, subcategory: subcategory || undefined,
      });
      setShortDescription(data.shortDescription);
      setFullDescription(data.fullDescription);
      setBenefits(data.benefits);
      setCtaText(data.callToAction);
      toast.success('Sugestão gerada — revise e ajuste como preferir antes de continuar.');
    } catch {
      toast.error('Não foi possível gerar a sugestão agora.');
    } finally {
      setGeneratingCopy(false);
    }
  };

  const handleSaveStep1 = async () => {
    if (!name.trim() || !fullDescription.trim()) {
      toast.error('Preencha ao menos o nome e a descrição completa.');
      return;
    }
    setSaving(true);
    try {
      let id = courseId;
      if (!id) {
        const { data } = await coursesApi.create({
          title: name, description: fullDescription, shortDescription, level: 'Beginner', price: 0,
          productType: productType ?? 'Course',
        });
        id = data.id;
        setCourseId(id);
      }
      await coursesApi.updateBasicInfo(id!, {
        title: name, category: category || null, subcategory: subcategory || null,
        shortDescription: shortDescription || null, description: fullDescription,
      });
      toast.success('Informações básicas salvas.');
      setStep(2);
    } catch {
      toast.error('Não foi possível salvar as informações básicas.');
    } finally {
      setSaving(false);
    }
  };

  // ─── Passo 2 ────────────────────────────────────────────────────────────
  const handleImportVideo = async () => {
    if (!importUrl.trim() || !courseId) return;
    setImporting(true);
    try {
      // Achado de teste manual: baixar o vídeo do YouTube pelo servidor (checkbox "baixar e
      // hospedar" que existia aqui) esbarra num bloqueio do próprio YouTube contra downloads
      // automatizados ("Sign in to confirm you're not a bot") — não tem correção estável do
      // nosso lado (nem cookies nem proxy resolvem de forma confiável pra vários criadores
      // diferentes), então a importação agora sempre guarda só a referência externa: o aluno
      // assiste embutido, mas o player é o do YouTube mesmo (download=false no backend).
      const { data } = await videosApi.importExternal(courseId, importUrl.trim());
      setVideos(v => [...v, {
        videoId: data.videoId, title: data.title ?? importUrl, source: data.source,
        durationSeconds: data.durationSeconds, status: data.status,
      }]);
      setImportUrl('');
      toast.success('Vídeo importado com sucesso.');
    } catch {
      toast.error('Não foi possível importar esse vídeo. Verifique a URL.');
    } finally {
      setImporting(false);
    }
  };

  const handleUploadVideo = async (file: File) => {
    if (!courseId) return;
    setImporting(true);
    try {
      const { data } = await videosApi.getUploadUrl(courseId);
      const isMockUpload = data.uploadUrl.startsWith(API_URL);

      if (isMockUpload) {
        // MockTusController (sem Cloudflare configurado) exige [Authorize] nos métodos TUS —
        // ver achado M10 da avaliação.
        const tus = await import('tus-js-client');
        await new Promise<void>((resolve, reject) => {
          const upload = new tus.Upload(file, {
            uploadUrl: data.uploadUrl,
            uploadLengthDeferred: true,
            chunkSize: 50 * 1024 * 1024,
            retryDelays: [0, 3000, 5000, 10000],
            metadata: { filename: file.name, filetype: file.type },
            headers: { Authorization: `Bearer ${localStorage.getItem('access_token') ?? ''}` },
            onError: reject,
            onSuccess: () => resolve(),
          });
          upload.start();
        });
      } else {
        // Achado de teste manual: nessa conta da Cloudflare Stream, o upload resumable via
        // TUS (HEAD/PATCH) retorna 400 pra qualquer uploadUrl gerada pelo
        // /stream/direct_upload -- confirmado com curl puro, fora do navegador e sem CORS
        // envolvido, então não é bug nosso (chamado aberto com o suporte da Cloudflare). A
        // própria documentação deles diz que a mesma uploadUrl também aceita um POST comum
        // multipart/form-data pra vídeos até 200MB, sem TUS -- é esse fallback usado aqui
        // enquanto o TUS não for corrigido do lado deles.
        if (file.size > 200 * 1024 * 1024) {
          throw new Error('Vídeo maior que 200MB não é suportado no momento — o upload resumable está temporariamente indisponível. Tente um arquivo menor ou comprima o vídeo.');
        }

        const formData = new FormData();
        formData.append('file', file);
        const response = await fetch(data.uploadUrl, { method: 'POST', body: formData });
        if (!response.ok) {
          throw new Error(`Upload para o Cloudflare Stream falhou (status ${response.status}).`);
        }
      }
      setVideos(v => [...v, { videoId: data.videoId, title: file.name, source: 'Upload' }]);
      toast.success('Vídeo enviado com sucesso.');
    } catch (err) {
      const message = err instanceof Error
        && (err.message.startsWith('Vídeo maior') || err.message.startsWith('Upload para o Cloudflare'))
        ? err.message
        : 'Erro no upload do vídeo.';
      toast.error(message);
      console.error(err);
    } finally {
      setImporting(false);
    }
  };

  const handleDeleteVideo = async (videoId: string) => {
    try {
      await videosApi.delete(videoId);
      setVideos(v => v.filter(item => item.videoId !== videoId));
      toast.success('Vídeo removido.');
    } catch (err: unknown) {
      const anyErr = err as { response?: { data?: { title?: string } } };
      toast.error(anyErr.response?.data?.title ?? 'Não foi possível remover esse vídeo — ele já pode estar vinculado a uma aula.');
    }
  };

  // ─── Passo 3 ────────────────────────────────────────────────────────────
  const handleAddModule = async (title: string) => {
    if (!courseId || !title.trim()) return;
    try {
      await coursesApi.addModule(courseId, { title });
      await refreshCourse(courseId);
    } catch {
      toast.error('Não foi possível adicionar o módulo.');
    }
  };

  const handleAddLesson = async (moduleId: string, title: string) => {
    if (!courseId || !title.trim()) return;
    try {
      await coursesApi.addLesson(courseId, moduleId, { title });
      await refreshCourse(courseId);
    } catch {
      toast.error('Não foi possível adicionar a aula.');
    }
  };

  const handleLinkVideo = async (moduleId: string, lessonId: string, videoId: string) => {
    if (!courseId) return;
    try {
      await videosApi.linkVideo(videoId, { courseId, moduleId, lessonId });
      await refreshCourse(courseId);
      toast.success('Vídeo vinculado à aula.');
    } catch {
      toast.error('Não foi possível vincular o vídeo.');
    }
  };

  // ─── Passo 4 ────────────────────────────────────────────────────────────
  const handleUploadMaterial = async (moduleId: string, lessonId: string, file: File) => {
    if (!courseId) return;
    try {
      const { data } = await materialsApi.upload(file);
      await coursesApi.addAttachment(courseId, moduleId, lessonId, {
        title: data.fileName, fileUrl: data.url, fileType: data.fileType, fileSizeBytes: data.fileSizeBytes,
      });
      toast.success('Material anexado.');
    } catch {
      toast.error('Não foi possível enviar o material.');
    }
  };

  // ─── Passo 5 ────────────────────────────────────────────────────────────
  const handleSavePricing = async () => {
    if (!courseId) return;
    setSaving(true);
    try {
      if (pricingMode === 'free') {
        await coursesApi.setPrice(courseId, 0);
      } else if (pricingMode === 'onetime') {
        await coursesApi.setPrice(courseId, price);
      } else {
        await coursesApi.setPrice(courseId, 0);
        await courseSubscriptionPlansApi.createForCourse(courseId, { price, billingCycle });
      }
      toast.success('Preço definido.');
      setStep(6);
    } catch {
      toast.error('Não foi possível salvar o preço.');
    } finally {
      setSaving(false);
    }
  };

  // ─── Passo 6 ────────────────────────────────────────────────────────────
  const handleGenerateSalesPage = async () => {
    if (!courseId) return;
    setGeneratingSalesPage(true);
    try {
      const { data } = await creatorStudioApi.generateSalesPageCopy(courseId);
      setHeadline(data.headline);
      setSubheadline(data.subheadline);
      setBenefits(data.benefits);
      setFaq(data.faq);
      setCtaText(data.callToAction);
      toast.success('Sugestão de página de vendas gerada — edite à vontade antes de salvar.');
    } catch {
      toast.error('Não foi possível gerar a sugestão agora.');
    } finally {
      setGeneratingSalesPage(false);
    }
  };

  const addTestimonial = () =>
    setTestimonials(t => [...t, { authorName: '', authorRole: '', quote: '', avatarUrl: '' }]);
  const removeTestimonial = (i: number) =>
    setTestimonials(t => t.filter((_, idx) => idx !== i));
  const updateTestimonial = (i: number, field: keyof TestimonialDraft, value: string) =>
    setTestimonials(t => t.map((item, idx) => (idx === i ? { ...item, [field]: value } : item)));

  const handleSaveSalesPage = async () => {
    if (!courseId) return;
    setSaving(true);
    try {
      await coursesApi.setSalesPage(courseId, {
        headline, subheadline, ctaText, benefits, faqItems: faq,
        videoUrl: videoUrl || null,
        testimonials: testimonials
          .filter(t => t.authorName.trim() && t.quote.trim())
          .map(t => ({
            authorName: t.authorName, authorRole: t.authorRole || null,
            quote: t.quote, avatarUrl: t.avatarUrl || null,
          })),
        guaranteeDays: guaranteeDays === '' ? null : guaranteeDays,
        guaranteeText: guaranteeText || null,
      });
      toast.success('Página de vendas salva.');
      setStep(7);
    } catch {
      toast.error('Não foi possível salvar a página de vendas.');
    } finally {
      setSaving(false);
    }
  };

  // ─── Passo 7 ────────────────────────────────────────────────────────────
  const handlePublish = async () => {
    if (!courseId) return;
    setPublishing(true);
    try {
      await creatorStudioApi.publish(courseId);
      toast.success('Produto publicado com sucesso! 🎉');
      router.push('/admin/produtos');
    } catch (err: unknown) {
      const anyErr = err as { response?: { data?: { message?: string } } };
      toast.error(anyErr.response?.data?.message ?? 'Não foi possível publicar o produto.');
    } finally {
      setPublishing(false);
    }
  };

  const canJumpTo = (target: number) => target === 1 || !!courseId;

  if (step === 0) {
    return <ProductTypeStep onSelect={handleSelectType} />;
  }

  const selectedType = PRODUCT_TYPES.find(t => t.id === productType);

  return (
    <div className="max-w-4xl mx-auto">
      <h1 className="text-2xl font-bold text-gray-800 mb-1">
        Criar {selectedType?.label ?? 'Produto'}
      </h1>
      <p className="text-gray-500 text-sm mb-6">
        Publique seu produto em poucos passos — a Tuilow não cobra mensalidade, só uma comissão sobre as vendas.
      </p>

      {/* Indicador de passos */}
      <div className="flex items-center gap-1 mb-8 overflow-x-auto pb-2">
        {STEPS.map((label, i) => {
          const n = i + 1;
          const active = n === step;
          const done = courseId && n < step;
          return (
            <button
              key={label}
              disabled={!canJumpTo(n)}
              onClick={() => canJumpTo(n) && setStep(n)}
              className={`flex items-center gap-2 px-3 py-1.5 rounded-full text-xs font-medium whitespace-nowrap
                transition-colors disabled:cursor-not-allowed disabled:opacity-40
                ${active ? 'bg-brand-600 text-white' : done ? 'bg-brand-50 text-brand-700' : 'bg-gray-100 text-gray-500'}`}
            >
              {done ? <Check className="w-3 h-3" /> : n}
              {label}
            </button>
          );
        })}
      </div>

      <div className="card">
        {step === 1 && (
          <div className="space-y-4">
            <div className="flex items-center justify-between">
              <h2 className="font-bold text-gray-800 text-lg">1. Informações Básicas</h2>
              {selectedType && (
                <span className="badge flex items-center gap-1.5 bg-brand-50 text-brand-700">
                  <selectedType.icon className="w-3.5 h-3.5" /> {selectedType.label}
                </span>
              )}
            </div>
            {!courseId && (
              <button onClick={() => setStep(0)} className="btn-ghost text-xs flex items-center gap-1 -mt-2">
                <ChevronLeft className="w-3.5 h-3.5" /> Trocar tipo de produto
              </button>
            )}
            <div>
              <label className="text-sm font-medium text-gray-600">Nome do produto</label>
              <input className="input-field mt-1" value={name} onChange={e => setName(e.target.value)}
                placeholder="Ex.: Curso de Excel para Iniciantes" />
            </div>
            <div className="grid grid-cols-2 gap-4">
              <CategoryAutocomplete
                label="Categoria"
                value={category}
                onChange={setCategory}
                options={categoryOptions.map(c => c.name)}
                placeholder="Ex.: Produtividade"
              />
              <CategoryAutocomplete
                label="Subcategoria"
                value={subcategory}
                onChange={setSubcategory}
                options={matchedCategory?.subcategories ?? []}
                placeholder="Ex.: Excel"
              />
            </div>
            <button onClick={handleGenerateCopy} disabled={generatingCopy}
              className="btn-secondary flex items-center gap-2 text-sm disabled:opacity-60">
              {generatingCopy ? <Loader2 className="w-4 h-4 animate-spin" /> : <Sparkles className="w-4 h-4" />}
              Gerar com IA
            </button>
            <div>
              <label className="text-sm font-medium text-gray-600">Descrição curta</label>
              <input className="input-field mt-1" value={shortDescription} onChange={e => setShortDescription(e.target.value)}
                placeholder="Uma frase que resume o produto" />
            </div>
            <div>
              <label className="text-sm font-medium text-gray-600">Descrição completa</label>
              <textarea className="input-field mt-1" rows={5} value={fullDescription}
                onChange={e => setFullDescription(e.target.value)}
                placeholder="Descreva o que o aluno vai aprender e conquistar com este produto" />
            </div>
            <div className="flex justify-end pt-2">
              <button onClick={handleSaveStep1} disabled={saving} className="btn-primary flex items-center gap-2">
                {saving && <Loader2 className="w-4 h-4 animate-spin" />} Avançar <ChevronRight className="w-4 h-4" />
              </button>
            </div>
          </div>
        )}

        {step === 2 && (
          <div className="space-y-5">
            <h2 className="font-bold text-gray-800 text-lg">2. Conteúdo</h2>
            <p className="text-sm text-gray-500">
              Prefira importar de uma plataforma externa (YouTube, Vimeo, Cloudflare Stream, Google Drive,
              Dropbox ou OneDrive) — reduz custo de armazenamento. Envio direto também é suportado.
            </p>

            <div className="flex gap-2">
              <input className="input-field flex-1" value={importUrl} onChange={e => setImportUrl(e.target.value)}
                placeholder="Cole a URL do vídeo (YouTube, Vimeo, Drive, Dropbox, OneDrive...)" />
              <button onClick={handleImportVideo} disabled={importing} className="btn-secondary flex items-center gap-2">
                {importing ? <Loader2 className="w-4 h-4 animate-spin" /> : <Link2 className="w-4 h-4" />} Importar
              </button>
            </div>

            {/youtube\.com|youtu\.be/.test(importUrl) && (
              <p className="text-sm text-gray-500 -mt-2">
                O vídeo continua hospedado no YouTube — o aluno assiste embutido aqui, com o player do YouTube.
              </p>
            )}

            <div className="flex items-center gap-2">
              <div className="divider flex-1" /> <span className="text-xs text-gray-400">ou</span> <div className="divider flex-1" />
            </div>

            <label className="btn-secondary inline-flex items-center gap-2 cursor-pointer">
              <Upload className="w-4 h-4" /> Enviar arquivo de vídeo
              <input type="file" accept="video/mp4,video/quicktime,video/x-msvideo,video/x-matroska" hidden
                onChange={e => { const f = e.target.files?.[0]; if (f) handleUploadVideo(f); }} />
            </label>

            {videos.length > 0 && (
              <ul className="space-y-2">
                {videos.map(v => (
                  <li key={v.videoId} className="flex items-center gap-2 text-sm bg-gray-50 rounded-lg px-3 py-2">
                    <Video className="w-4 h-4 text-brand-600" /> {v.title}
                    {(v.status === 'Uploading' || v.status === 'Processing') && (
                      <span className="badge ml-auto flex items-center gap-1 bg-amber-50 text-amber-700">
                        <Loader2 className="w-3 h-3 animate-spin" />
                        {v.status === 'Uploading' ? 'Enviando...' : 'Processando...'}
                      </span>
                    )}
                    {v.status === 'Error' && (
                      <span className="badge ml-auto bg-red-50 text-red-700">Erro ao baixar</span>
                    )}
                    {(!v.status || v.status === 'Ready') && (
                      <span className={v.status === 'Error' ? 'badge' : 'badge ml-auto'}>{v.source}</span>
                    )}
                    <button type="button" onClick={() => handleDeleteVideo(v.videoId)}
                      title="Remover vídeo"
                      className={`text-gray-400 hover:text-red-500 transition-colors px-1 ${v.status === 'Error' ? '' : 'ml-0'}`}>
                      ✕
                    </button>
                  </li>
                ))}
              </ul>
            )}

            <div className="flex justify-between pt-2">
              <button onClick={() => setStep(1)} className="btn-ghost flex items-center gap-2">
                <ChevronLeft className="w-4 h-4" /> Voltar
              </button>
              <button onClick={() => setStep(3)} className="btn-primary flex items-center gap-2">
                Avançar <ChevronRight className="w-4 h-4" />
              </button>
            </div>
          </div>
        )}

        {step === 3 && courseId && (
          <OrganizationStep
            courseId={courseId}
            modules={modules}
            videos={videos}
            onAddModule={handleAddModule}
            onAddLesson={handleAddLesson}
            onLinkVideo={handleLinkVideo}
            onBack={() => setStep(2)}
            onNext={() => setStep(4)}
          />
        )}

        {step === 4 && courseId && (
          <MaterialsStep
            modules={modules}
            onUpload={handleUploadMaterial}
            onBack={() => setStep(3)}
            onNext={() => setStep(5)}
          />
        )}

        {step === 5 && (
          <div className="space-y-5">
            <h2 className="font-bold text-gray-800 text-lg">5. Preço</h2>
            <div className="grid grid-cols-3 gap-3">
              {(['free', 'onetime', 'subscription'] as PricingMode[]).map(mode => (
                <button key={mode} onClick={() => setPricingMode(mode)}
                  className={`rounded-xl border-2 p-4 text-left transition-colors
                    ${pricingMode === mode ? 'border-brand-500 bg-brand-50' : 'border-gray-200'}`}>
                  <p className="font-semibold text-gray-800">
                    {mode === 'free' ? 'Grátis' : mode === 'onetime' ? 'Pagamento único' : 'Assinatura'}
                  </p>
                </button>
              ))}
            </div>

            {pricingMode === 'onetime' && (
              <div>
                <div className="flex gap-2 mb-2">
                  {[97, 197, 497].map(v => (
                    <button key={v} onClick={() => setPrice(v)}
                      className={`btn-secondary text-sm ${price === v ? 'ring-2 ring-brand-500' : ''}`}>
                      R$ {v}
                    </button>
                  ))}
                </div>
                <input type="number" className="input-field w-40" value={price}
                  onChange={e => setPrice(Number(e.target.value))} />
              </div>
            )}

            {pricingMode === 'subscription' && (
              <div className="flex gap-4 items-end">
                <div>
                  <label className="text-sm font-medium text-gray-600">Valor</label>
                  <input type="number" className="input-field mt-1 w-32" value={price}
                    onChange={e => setPrice(Number(e.target.value))} />
                </div>
                <div>
                  <label className="text-sm font-medium text-gray-600">Recorrência</label>
                  <select className="input-field mt-1" value={billingCycle}
                    onChange={e => setBillingCycle(e.target.value as BillingCycle)}>
                    <option value="Monthly">Mensal</option>
                    <option value="Quarterly">Trimestral</option>
                    <option value="Semiannual">Semestral</option>
                    <option value="Annual">Anual</option>
                  </select>
                </div>
              </div>
            )}

            <div className="flex justify-between pt-2">
              <button onClick={() => setStep(4)} className="btn-ghost flex items-center gap-2">
                <ChevronLeft className="w-4 h-4" /> Voltar
              </button>
              <button onClick={handleSavePricing} disabled={saving} className="btn-primary flex items-center gap-2">
                {saving && <Loader2 className="w-4 h-4 animate-spin" />} Avançar <ChevronRight className="w-4 h-4" />
              </button>
            </div>
          </div>
        )}

        {step === 6 && (
          <div className="space-y-4">
            <h2 className="font-bold text-gray-800 text-lg">6. Página de Vendas</h2>
            <button onClick={handleGenerateSalesPage} disabled={generatingSalesPage}
              className="btn-secondary flex items-center gap-2 text-sm disabled:opacity-60">
              {generatingSalesPage ? <Loader2 className="w-4 h-4 animate-spin" /> : <Sparkles className="w-4 h-4" />}
              Gerar com IA
            </button>
            <div>
              <label className="text-sm font-medium text-gray-600">Título</label>
              <input className="input-field mt-1" value={headline} onChange={e => setHeadline(e.target.value)} />
            </div>
            <div>
              <label className="text-sm font-medium text-gray-600">Subtítulo</label>
              <input className="input-field mt-1" value={subheadline} onChange={e => setSubheadline(e.target.value)} />
            </div>
            <div>
              <label className="text-sm font-medium text-gray-600">Benefícios (um por linha)</label>
              <textarea className="input-field mt-1" rows={4} value={benefits.join('\n')}
                onChange={e => setBenefits(e.target.value.split('\n'))} />
            </div>
            <div>
              <label className="text-sm font-medium text-gray-600">Chamada para ação (CTA)</label>
              <input className="input-field mt-1" value={ctaText} onChange={e => setCtaText(e.target.value)} />
            </div>
            <div>
              <label className="text-sm font-medium text-gray-600">Vídeo de apresentação (URL)</label>
              <input className="input-field mt-1" value={videoUrl} onChange={e => setVideoUrl(e.target.value)}
                placeholder="https://youtube.com/watch?v=... ou link do Vimeo" />
            </div>
            <div>
              <label className="text-sm font-medium text-gray-600">Garantia</label>
              <div className="flex gap-2 mt-1">
                <input type="number" min={0} className="input-field w-28" value={guaranteeDays}
                  onChange={e => setGuaranteeDays(e.target.value === '' ? '' : Number(e.target.value))}
                  placeholder="Dias" />
                <input className="input-field flex-1" value={guaranteeText}
                  onChange={e => setGuaranteeText(e.target.value)}
                  placeholder="Ex.: Satisfação garantida ou seu dinheiro de volta" />
              </div>
            </div>
            <div>
              <div className="flex items-center justify-between">
                <label className="text-sm font-medium text-gray-600">Depoimentos de alunos</label>
                <button type="button" onClick={addTestimonial} className="btn-ghost text-xs flex items-center gap-1">
                  <Plus className="w-3.5 h-3.5" /> Adicionar
                </button>
              </div>
              {testimonials.length === 0 ? (
                <p className="text-xs text-gray-400 mt-1">
                  Nenhum depoimento ainda — adicione avaliações reais de alunos para reforçar a prova social.
                </p>
              ) : (
                <div className="space-y-3 mt-2">
                  {testimonials.map((t, i) => (
                    <div key={i} className="border border-gray-200 rounded-lg p-3 space-y-2">
                      <div className="flex gap-2">
                        <input className="input-field text-sm flex-1" placeholder="Nome do aluno"
                          value={t.authorName} onChange={e => updateTestimonial(i, 'authorName', e.target.value)} />
                        <input className="input-field text-sm flex-1" placeholder="Cargo/curso (opcional)"
                          value={t.authorRole} onChange={e => updateTestimonial(i, 'authorRole', e.target.value)} />
                        <button type="button" onClick={() => removeTestimonial(i)}
                          className="text-gray-400 hover:text-red-500 transition-colors px-2">✕</button>
                      </div>
                      <textarea className="input-field text-sm" rows={2} placeholder="O que o aluno disse"
                        value={t.quote} onChange={e => updateTestimonial(i, 'quote', e.target.value)} />
                    </div>
                  ))}
                </div>
              )}
            </div>
            {faq.length > 0 && (
              <div>
                <p className="text-sm font-medium text-gray-600 mb-2">Perguntas frequentes</p>
                <div className="space-y-2">
                  {faq.map((f, i) => (
                    <div key={i} className="bg-gray-50 rounded-lg p-3 text-sm">
                      <p className="font-medium text-gray-700">{f.question}</p>
                      <p className="text-gray-500">{f.answer}</p>
                    </div>
                  ))}
                </div>
              </div>
            )}
            <div className="flex justify-between pt-2">
              <button onClick={() => setStep(5)} className="btn-ghost flex items-center gap-2">
                <ChevronLeft className="w-4 h-4" /> Voltar
              </button>
              <button onClick={handleSaveSalesPage} disabled={saving} className="btn-primary flex items-center gap-2">
                {saving && <Loader2 className="w-4 h-4 animate-spin" />} Avançar <ChevronRight className="w-4 h-4" />
              </button>
            </div>
          </div>
        )}

        {step === 7 && (
          <div className="space-y-5">
            <h2 className="font-bold text-gray-800 text-lg">7. Publicação</h2>
            {checklist ? (
              <ul className="space-y-2 text-sm">
                {[
                  ['Dados preenchidos', checklist.basicInfoFilled],
                  ['Conteúdo enviado', checklist.contentUploaded],
                  ['Preço definido', checklist.priceDefined],
                  ['Página de vendas criada', checklist.salesPageCreated],
                ].map(([label, ok]) => (
                  <li key={label as string} className="flex items-center gap-2">
                    <span className={`w-5 h-5 rounded-full flex items-center justify-center text-white
                      ${ok ? 'bg-green-500' : 'bg-gray-300'}`}>
                      <Check className="w-3 h-3" />
                    </span>
                    {label}
                  </li>
                ))}
              </ul>
            ) : (
              <p className="text-gray-400 text-sm">Carregando checklist...</p>
            )}
            <div className="flex justify-between pt-2">
              <button onClick={() => setStep(6)} className="btn-ghost flex items-center gap-2">
                <ChevronLeft className="w-4 h-4" /> Voltar
              </button>
              <button onClick={handlePublish} disabled={publishing || !checklist?.isComplete}
                className="btn-primary flex items-center gap-2 disabled:opacity-50">
                {publishing && <Loader2 className="w-4 h-4 animate-spin" />} Publicar Produto
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}

// ─── Sub-componente: Organização (passo 3) ───────────────────────────────
function OrganizationStep({
  modules, videos, onAddModule, onAddLesson, onLinkVideo, onBack, onNext,
}: {
  courseId: string;
  modules: ModuleDetail[];
  videos: LocalVideo[];
  onAddModule: (title: string) => void;
  onAddLesson: (moduleId: string, title: string) => void;
  onLinkVideo: (moduleId: string, lessonId: string, videoId: string) => void;
  onBack: () => void;
  onNext: () => void;
}) {
  const [newModuleTitle, setNewModuleTitle] = useState('');
  const [lessonDrafts, setLessonDrafts] = useState<Record<string, string>>({});

  // Sem isso, dava pra avançar pro passo de Materiais (e dali até Publicação) com um produto
  // sem nenhum módulo/aula — só ia falhar (silenciosamente pro usuário) lá na frente, no
  // checklist de publicação. Exige pelo menos 1 módulo com pelo menos 1 aula dentro.
  const canAdvance = modules.some(m => m.lessons.length > 0);

  return (
    <div className="space-y-5">
      <h2 className="font-bold text-gray-800 text-lg">3. Organização</h2>
      <p className="text-sm text-gray-500">
        Crie os módulos e aulas do produto. Depois de criar uma aula, escolha um dos vídeos
        enviados/importados no passo anterior no menu <strong>&quot;Vincular vídeo&quot;</strong> ao lado dela.
      </p>

      <div className="bg-brand-50 border border-brand-100 rounded-xl p-3">
        <p className="text-xs font-semibold text-brand-700 mb-2">
          Vídeos disponíveis para vincular ({videos.length})
        </p>
        {videos.length === 0 ? (
          <p className="text-xs text-gray-500">
            Nenhum vídeo disponível ainda — volte ao passo &quot;Conteúdo&quot; para enviar ou importar um.
          </p>
        ) : (
          <ul className="flex flex-wrap gap-2">
            {videos.map(v => (
              <li key={v.videoId} className="flex items-center gap-1.5 text-xs bg-white border border-gray-200 rounded-full px-3 py-1">
                <Video className="w-3 h-3 text-brand-600" /> {v.title}
              </li>
            ))}
          </ul>
        )}
      </div>

      <div className="space-y-4">
        {modules.length === 0 && (
          <p className="text-sm text-gray-400">Nenhum módulo criado ainda — crie um abaixo para começar.</p>
        )}
        {modules.map(m => (
          <div key={m.id} className="border border-gray-200 rounded-xl p-4">
            <p className="font-semibold text-gray-800 mb-2">{m.title}</p>
            {m.lessons.length === 0 && (
              <p className="text-xs text-gray-400 mb-2">Nenhuma aula ainda — crie uma abaixo.</p>
            )}
            <ul className="space-y-2 mb-3">
              {m.lessons.map((l: LessonDetail) => (
                <li key={l.id} className="flex items-center gap-2 text-sm bg-gray-50 rounded-lg px-3 py-2">
                  <span className="flex-1">{l.title}</span>
                  {l.hasVideo ? (
                    <span className="badge-green badge">Vídeo ✓</span>
                  ) : videos.length === 0 ? (
                    <span className="text-xs text-gray-400">Sem vídeos disponíveis</span>
                  ) : (
                    <select className="text-xs border border-gray-200 rounded-lg px-2 py-1"
                      onChange={e => e.target.value && onLinkVideo(m.id, l.id, e.target.value)}
                      defaultValue="">
                      <option value="" disabled>Vincular vídeo</option>
                      {videos.map(v => <option key={v.videoId} value={v.videoId}>{v.title}</option>)}
                    </select>
                  )}
                </li>
              ))}
            </ul>
            <div className="flex gap-2">
              <input className="input-field flex-1 text-sm" placeholder="Título da aula"
                value={lessonDrafts[m.id] ?? ''}
                onChange={e => setLessonDrafts(d => ({ ...d, [m.id]: e.target.value }))} />
              <button className="btn-secondary text-sm" onClick={() => {
                onAddLesson(m.id, lessonDrafts[m.id] ?? '');
                setLessonDrafts(d => ({ ...d, [m.id]: '' }));
              }}>
                <Plus className="w-4 h-4" /> Adicionar aula
              </button>
            </div>
          </div>
        ))}
      </div>

      <div className="flex gap-2">
        <input className="input-field flex-1" placeholder="Título do módulo"
          value={newModuleTitle} onChange={e => setNewModuleTitle(e.target.value)} />
        <button className="btn-secondary" onClick={() => { onAddModule(newModuleTitle); setNewModuleTitle(''); }}>
          <Plus className="w-4 h-4" /> Módulo
        </button>
      </div>

      <div className="flex flex-col items-end gap-1.5 pt-2">
        <div className="flex justify-between w-full">
          <button onClick={onBack} className="btn-ghost flex items-center gap-2">
            <ChevronLeft className="w-4 h-4" /> Voltar
          </button>
          <button onClick={onNext} disabled={!canAdvance}
            className="btn-primary flex items-center gap-2 disabled:opacity-50 disabled:cursor-not-allowed">
            Avançar <ChevronRight className="w-4 h-4" />
          </button>
        </div>
        {!canAdvance && (
          <p className="text-xs text-gray-400">Crie pelo menos um módulo com uma aula para continuar.</p>
        )}
      </div>
    </div>
  );
}

// ─── Sub-componente: Materiais (passo 4) ─────────────────────────────────
function MaterialsStep({
  modules, onUpload, onBack, onNext,
}: {
  modules: ModuleDetail[];
  onUpload: (moduleId: string, lessonId: string, file: File) => void;
  onBack: () => void;
  onNext: () => void;
}) {
  return (
    <div className="space-y-5">
      <h2 className="font-bold text-gray-800 text-lg">4. Materiais</h2>
      <p className="text-sm text-gray-500">
        Anexe PDFs, apresentações, planilhas ou outros arquivos de apoio a cada aula (opcional).
      </p>

      {modules.length === 0 && (
        <p className="text-sm text-gray-400">Nenhuma aula criada ainda — volte ao passo anterior para criar módulos e aulas.</p>
      )}

      <div className="space-y-4">
        {modules.map(m => (
          <div key={m.id}>
            <p className="font-medium text-gray-700 text-sm mb-2">{m.title}</p>
            <ul className="space-y-2">
              {m.lessons.map((l: LessonDetail) => (
                <li key={l.id} className="flex items-center gap-2 bg-gray-50 rounded-lg px-3 py-2 text-sm">
                  <span className="flex-1">{l.title}</span>
                  <label className="btn-ghost text-xs flex items-center gap-1 cursor-pointer">
                    <Paperclip className="w-3.5 h-3.5" /> Anexar
                    <input type="file" hidden accept=".pdf,.doc,.docx,.ppt,.pptx,.zip,.jpg,.jpeg,.png,.xls,.xlsx,.csv"
                      onChange={e => { const f = e.target.files?.[0]; if (f) onUpload(m.id, l.id, f); }} />
                  </label>
                </li>
              ))}
            </ul>
          </div>
        ))}
      </div>

      <div className="flex justify-between pt-2">
        <button onClick={onBack} className="btn-ghost flex items-center gap-2">
          <ChevronLeft className="w-4 h-4" /> Voltar
        </button>
        <button onClick={onNext} className="btn-primary flex items-center gap-2">
          Avançar <ChevronRight className="w-4 h-4" />
        </button>
      </div>
    </div>
  );
}

export default function NovoProdutoPage() {
  return (
    <Suspense fallback={<p className="text-gray-400 text-sm">Carregando...</p>}>
      <ProductWizard />
    </Suspense>
  );
}
