'use client';

import { useState } from 'react';
import { createPortal } from 'react-dom';
import { useQuery, useQueryClient, useMutation } from '@tanstack/react-query';
import { authApi, enrollmentsApi, setAccessToken } from '@/lib/api';
import type { MyEnrollment, ContinueWatching, RedeemAccessCodeResult } from '@/types';
import Link from 'next/link';
import toast from 'react-hot-toast';
import { Play, BookOpen, CheckCircle2, TrendingUp, Rocket, Ticket, X } from 'lucide-react';
import { Card, StatCard, Progress, Button, Input } from '@/components/ui/design-system';

// Redesign "Hi Mentor" (ago/2026): anatomia (cabeçalho + "continuar assistindo" +
// stat cards) seguindo hi-mentor/src/pages/MentorDashboard.tsx e MentoradoHome.tsx —
// mas só as seções com dado real hoje. O protótipo também tem um feed de "atividade
// recente", agenda de "próximos encontros" e lista de "mentorados em risco"; nenhum
// desses dados existe no backend do Tuilow ainda, então essas seções ficam de fora
// (ver RELATORIO_REDESIGN_HI_MENTOR.md) em vez de mostrar números inventados.
export default function DashboardPage() {
  const [becomingCreator, setBecomingCreator] = useState(false);
  const [codeModalOpen, setCodeModalOpen] = useState(false);
  const queryClient = useQueryClient();

  // Mesma query key usada no sidebar (DashboardLayout) — um único cache compartilhado evita
  // duas chamadas redundantes a /auth/me a cada carregamento de página (eram duas requisições
  // concorrentes independentes, o que deixava o carregamento mais lento e multiplicava as
  // chances de uma delas cair num 401 por token perto de expirar) e permite atualizar sidebar
  // + conteúdo juntos com um simples invalidateQueries, sem reload de página.
  const { data: user } = useQuery({
    queryKey: ['user-profile'],
    queryFn: () => authApi.me().then(r => r.data),
  });

  // Mesma query key usada em /cursos (filtro "Matriculados") — um único cache alimenta os dois
  // lugares. Antes os 4 cards abaixo eram só '—' fixo, nunca conectados a dado nenhum.
  const { data: myEnrollments = [] } = useQuery<MyEnrollment[]>({
    queryKey: ['my-enrollments'],
    queryFn: () => enrollmentsApi.getMyEnrollments().then(r => r.data),
  });

  // "Continuar assistindo" — última aula tocada, entre todos os programas matriculados.
  const { data: continueWatching } = useQuery<ContinueWatching | null>({
    queryKey: ['continue-watching'],
    queryFn: () => enrollmentsApi.getContinueWatching().then(r => r.data).catch(() => null),
  });

  const totalCompletedLessons = myEnrollments.reduce((acc, e) => acc + e.completedLessonsCount, 0);
  const avgProgress = myEnrollments.length > 0
    ? Math.round(myEnrollments.reduce((acc, e) => acc + e.progressPercentage, 0) / myEnrollments.length)
    : 0;
  // Achado M7 da avaliação: chamava-se "certificados conquistados", mas nenhum certificado é
  // emitido de fato (ver achado A4) — é só a contagem de matrículas com status Completed. Manter
  // o rótulo antigo prometia um documento que o aluno não consegue baixar. Renomeado para
  // "programas concluídos" (a métrica em si está correta, só o nome estava errado) até a emissão
  // real de certificados existir.
  const completedCoursesCount = myEnrollments.filter(e => e.status === 'Completed').length;

  const isCreator = user?.roles?.includes('Creator') || user?.roles?.includes('Admin');
  const hasNoPrograms = myEnrollments.length === 0;

  const handleBecomeCreator = async () => {
    setBecomingCreator(true);
    try {
      // O backend já devolve um access token novo (com o claim de role "Creator") e regrava o
      // cookie HttpOnly de refresh token — evita uma chamada extra a /auth/refresh-token logo em
      // seguida, que competia pelo refresh token de uso único e causava um erro intermitente aqui.
      // Nota: refresh token não é mais gravado aqui — ele só existe no cookie HttpOnly (achado
      // C1), nunca no corpo da resposta nem em localStorage.
      const { data } = await authApi.becomeCreator();
      setAccessToken(data.accessToken);

      // Invalida o cache em vez de recarregar a página inteira: sidebar e conteúdo atualizam
      // na hora com os novos recursos de mentor (sem repetir toda a dança de autenticação de
      // um reload completo, que era o que deixava lento e, ocasionalmente, deslogava o usuário
      // se algum token estivesse perto de expirar durante o reload).
      await queryClient.invalidateQueries({ queryKey: ['user-profile'] });
      toast.success('Você agora é um mentor na Hi Mentor! Veja "Meus Programas" no menu.');
    } catch {
      toast.error('Não foi possível ativar o modo mentor. Tente novamente.');
    } finally {
      setBecomingCreator(false);
    }
  };

  const stats = [
    { label: 'Programas', value: String(myEnrollments.length), icon: <BookOpen size={20} className="text-violet-600" />, iconBg: 'bg-violet-50' },
    { label: 'Aulas concluídas', value: String(totalCompletedLessons), icon: <CheckCircle2 size={20} className="text-emerald-600" />, iconBg: 'bg-emerald-50' },
    { label: 'Progresso médio', value: `${avgProgress}%`, icon: <TrendingUp size={20} className="text-indigo-600" />, iconBg: 'bg-indigo-50' },
    { label: 'Programas concluídos', value: String(completedCoursesCount), icon: <Rocket size={20} className="text-amber-600" />, iconBg: 'bg-amber-50' },
  ];

  return (
    <div className="max-w-4xl mx-auto">
      {/* Header */}
      <div className="mb-8">
        <h1 className="text-2xl font-bold text-ink mb-1" style={{ letterSpacing: '-0.025em' }}>
          Olá, {user?.firstName ?? '...'}! 👋
        </h1>
        <p className="text-ink-3">Continue de onde parou ou explore novos programas.</p>
      </div>

      {/* Continuar assistindo — última aula tocada, entre todos os programas matriculados */}
      {continueWatching && (
        <Link href={`/cursos/${continueWatching.courseSlug}/${continueWatching.lessonId}`}>
          <Card hover className="mb-6 flex items-center gap-4">
            <div className="w-20 h-14 rounded-xl bg-subtle shrink-0 overflow-hidden flex items-center justify-center">
              {continueWatching.thumbnailUrl ? (
                // eslint-disable-next-line @next/next/no-img-element
                <img src={continueWatching.thumbnailUrl} alt="" className="w-full h-full object-cover" />
              ) : (
                <Play size={20} className="text-ink-3" fill="currentColor" />
              )}
            </div>
            <div className="flex-1 min-w-0">
              <p className="text-xs text-ink-3 uppercase tracking-wider font-semibold">Continuar assistindo</p>
              <p className="font-semibold text-ink truncate">{continueWatching.lessonTitle}</p>
              <p className="text-xs text-ink-3 truncate">{continueWatching.courseTitle}</p>
            </div>
            <div className="shrink-0 flex items-center gap-3">
              <div className="w-24 hidden sm:block">
                <Progress value={continueWatching.courseProgressPercentage} size="xs" />
              </div>
              <Button size="sm" icon={<Play size={14} fill="currentColor" />}>Continuar</Button>
            </div>
          </Card>
        </Link>
      )}

      {/* Estado "aluno sem nenhum programa" — mantém o layout/identidade visual existentes
          (mesmo componente Card do card "Tornar-se mentor" logo abaixo), só adiciona a ação
          concreta que faltava para quem acabou de entrar: ativar um código de acesso. Não é
          vitrine nem recomendação de curso — só aparece enquanto myEnrollments está vazio, e some
          sozinho assim que a ativação (ou qualquer outro acesso) invalidar essa query. */}
      {hasNoPrograms && (
        <Card className="mb-6">
          <p className="font-semibold text-ink">Você ainda não possui nenhum programa</p>
          <p className="text-sm text-ink-3 mt-0.5">
            Quando você receber acesso a um curso, mentoria ou programa, ele aparecerá aqui.
          </p>

          <div className="mt-4 bg-violet-50/60 border border-violet-100 rounded-xl p-4 flex items-center justify-between gap-4 flex-wrap">
            <div className="flex items-start gap-3">
              <div className="w-9 h-9 rounded-lg bg-violet-100 flex items-center justify-center shrink-0 text-violet-600">
                <Ticket size={18} />
              </div>
              <div>
                <p className="font-semibold text-ink text-sm">Recebeu um código de acesso?</p>
                <p className="text-sm text-ink-3 mt-0.5">Ative seu código para começar sua jornada.</p>
              </div>
            </div>
            <Button onClick={() => setCodeModalOpen(true)} className="shrink-0">
              Ativar código de acesso
            </Button>
          </div>
        </Card>
      )}

      {/* Torne-se mentor — plataforma aberta: qualquer pessoa pode publicar programas */}
      {!isCreator && (
        <Card className="mb-6 bg-violet-50/60 border-violet-100 flex items-center justify-between gap-4 flex-wrap">
          <div>
            <p className="font-semibold text-ink">Quer publicar seus próprios programas de mentoria?</p>
            <p className="text-sm text-ink-3 mt-0.5">
              Torne-se um mentor Hi Mentor: sem mensalidade, sem aprovação. Você só paga uma comissão quando vende.
            </p>
          </div>
          <Button onClick={handleBecomeCreator} loading={becomingCreator} className="shrink-0">
            {becomingCreator ? 'Ativando...' : 'Tornar-se mentor'}
          </Button>
        </Card>
      )}

      {/* Stats */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        {stats.map(s => (
          <StatCard key={s.label} {...s} />
        ))}
      </div>

      {codeModalOpen && (
        <ActivateAccessCodeModal
          onClose={() => setCodeModalOpen(false)}
          onSuccess={(result) => {
            setCodeModalOpen(false);
            // Invalida em vez de recarregar a página inteira (item 5 do pedido): o bloco de
            // estado vazio, os 4 stat cards e o sidebar ("Minha Jornada") atualizam sozinhos
            // porque todos leem da mesma query key ['my-enrollments'].
            queryClient.invalidateQueries({ queryKey: ['my-enrollments'] });
            queryClient.invalidateQueries({ queryKey: ['continue-watching'] });
            toast.success(`Acesso ativado! "${result.courseTitle}" já está em Meus programas.`);
          }}
        />
      )}
    </div>
  );
}

// Mesmo padrão de portal + fixed inset-0 bg-black/40 já usado no modal de detalhe de
// (dashboard)/plataforma/usuarios/page.tsx.
function ActivateAccessCodeModal({
  onClose, onSuccess,
}: {
  onClose: () => void;
  onSuccess: (result: RedeemAccessCodeResult) => void;
}) {
  const [code, setCode] = useState('');

  const redeemMutation = useMutation({
    mutationFn: () => enrollmentsApi.redeemAccessCode(code.trim()),
    onSuccess: (res) => onSuccess(res.data as RedeemAccessCodeResult),
    onError: (err: unknown) => {
      // .title/.detail/.message cobre tanto o ExceptionHandlingMiddleware (Host) quanto o
      // ProblemDetails padrão do ASP.NET para erro de model binding — mesmo fallback usado em
      // (dashboard)/cursos/[slug]/page.tsx e app/c/[slug]/page.tsx.
      const anyErr = err as { response?: { data?: { title?: string; detail?: string; message?: string } } };
      toast.error(
        anyErr.response?.data?.title
        ?? anyErr.response?.data?.detail
        ?? anyErr.response?.data?.message
        ?? 'Não foi possível ativar o código. Tente novamente.'
      );
    },
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!code.trim()) {
      toast.error('Informe o código de acesso.');
      return;
    }
    redeemMutation.mutate();
  };

  return createPortal(
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4" onClick={onClose}>
      <div
        className="bg-surface rounded-2xl shadow-xl w-full max-w-sm border border-line"
        onClick={e => e.stopPropagation()}
      >
        <div className="flex items-start justify-between p-6 pb-4">
          <div>
            <h2 className="text-lg font-bold text-ink">Ativar acesso</h2>
            <p className="text-sm text-ink-3 mt-1">Digite o código que você recebeu para acessar seu programa.</p>
          </div>
          <button onClick={onClose} className="p-1.5 rounded-lg hover:bg-subtle text-ink-3 shrink-0">
            <X size={18} />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="px-6 pb-6 flex flex-col gap-4">
          <Input
            label="Código de acesso"
            value={code}
            onChange={e => setCode(e.target.value.toUpperCase())}
            placeholder="Ex.: A1B2C3D4"
            autoFocus
            maxLength={16}
          />
          <Button type="submit" loading={redeemMutation.isPending} className="w-full">
            {redeemMutation.isPending ? 'Ativando...' : 'Ativar acesso'}
          </Button>
        </form>
      </div>
    </div>,
    document.body
  );
}
