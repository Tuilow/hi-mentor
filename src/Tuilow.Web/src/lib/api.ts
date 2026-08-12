import axios, { AxiosError, InternalAxiosRequestConfig } from 'axios';
import type { RegisterRequest, LoginRequest } from '@/types';

// Exportado (achado B10 da avaliação) — a página da aula usa isto para montar a URL do fetch
// "keepalive" disparado em visibilitychange/pagehide, fora do axios (ver
// (dashboard)/cursos/[slug]/[lessonId]/page.tsx), sem duplicar o fallback de localhost aqui.
export const API_URL = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5000';

export const api = axios.create({
  baseURL: `${API_URL}/api/v1`,
  headers: { 'Content-Type': 'application/json' },
  timeout: 15000,
  // Migração para cookie HttpOnly (achado C1 da avaliação www/app): o refresh token não é mais
  // lido/gravado em localStorage — ele viaja como cookie "refresh_token" e o navegador só o
  // envia se withCredentials estiver ligado (equivale a credentials: 'include' do fetch). Sem
  // isso o cookie HttpOnly setado pelo backend nunca voltaria nas próximas chamadas.
  withCredentials: true,
});

// ─── Indicador global de carregamento ─────────────────────────────
// Conta requisições em andamento e avisa quem estiver inscrito (GlobalLoadingBar).
// Existe para que QUALQUER clique que dispare uma chamada à API mostre feedback visual,
// mesmo em telas onde o autor esqueceu de criar um estado de loading local — antes disso,
// um clique podia "não fazer nada" na tela por até 15s (timeout) sem nenhum sinal, e o
// usuário não tinha como saber se estava travado ou só demorando.
let pendingRequests = 0;
const loadingSubscribers = new Set<(count: number) => void>();

const notifyLoadingSubscribers = () => {
  loadingSubscribers.forEach((cb) => cb(pendingRequests));
};

export const subscribeToApiLoading = (cb: (count: number) => void) => {
  loadingSubscribers.add(cb);
  cb(pendingRequests);
  return () => {
    loadingSubscribers.delete(cb);
  };
};

const beginRequest = () => {
  pendingRequests += 1;
  notifyLoadingSubscribers();
};

const endRequest = () => {
  pendingRequests = Math.max(0, pendingRequests - 1);
  notifyLoadingSubscribers();
};

api.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  beginRequest();
  return config;
});

// Garante que o contador sempre desce, mesmo em erro/timeout/cancelamento — se isso não
// rodasse em ambos os ramos, uma requisição com erro deixaria a barra de loading presa
// "para sempre" na tela, o que seria pior do que não ter barra nenhuma.
api.interceptors.response.use(
  (response) => {
    endRequest();
    return response;
  },
  (error: AxiosError) => {
    endRequest();
    return Promise.reject(error);
  }
);

// ─── Sessão local (access token + sinalizador para o middleware) ─────────────────
// Achado de teste manual (deploy real: front no Vercel, API no Railway — domínios registráveis
// DE FATO diferentes, ver comentário em AuthController.BuildCookieOptions no backend): o
// middleware.ts roda no domínio do FRONT (Edge Function do Vercel) e só enxerga cookies que o
// navegador anexa às requisições feitas PRO FRONT. O cookie HttpOnly "refresh_token" tem Domain
// = host da API (Railway) — o navegador NUNCA o envia em requisições para o Vercel, então o
// middleware sempre via "sem sessão" e redirecionava de volta pro /login mesmo com o login tendo
// funcionado de verdade (access_token gravado certinho, refresh_token certinho no cookie da API
// — só que em domínio nenhum que o middleware consiga ler). Resultado observado: toast "Bem-vindo
// de volta" aparece, mas a navegação pro /dashboard é imediatamente revertida pro /login pelo
// middleware, num loop.
//
// A correção é este cookie aqui: gravado pelo PRÓPRIO navegador via document.cookie, na mesma
// origem do front — por isso ele SEMPRE acompanha as requisições ao front, inclusive as que o
// middleware intercepta, não importa em qual domínio a API esteja. Não substitui o refresh_token
// nem é usado para autenticação de verdade (não é HttpOnly, não valida nada sozinho) — é só um
// sinalizador de UX pro middleware decidir "deixa passar ou manda pro login". A autorização real
// continua 100% no backend, via JWT no header Authorization + [Authorize] em cada endpoint.
const SESSION_HINT_COOKIE = 'has_session';
// 30 dias = mesma validade do refresh token (ver RefreshTokenCommandHandler/AuthTokens no
// backend) — reemitido a cada login/refresh bem-sucedido, então na prática dura enquanto a
// sessão durar de verdade.
const SESSION_HINT_MAX_AGE_SECONDS = 60 * 60 * 24 * 30;

export const setAccessToken = (token: string) => {
  localStorage.setItem('access_token', token);
  if (typeof document !== 'undefined') {
    // Secure só quando servido por HTTPS (produção) — em dev local (http://localhost) o
    // navegador descartaria o cookie inteiro se ele viesse marcado Secure.
    const secure = window.location.protocol === 'https:' ? '; Secure' : '';
    document.cookie = `${SESSION_HINT_COOKIE}=1; path=/; max-age=${SESSION_HINT_MAX_AGE_SECONDS}; SameSite=Lax${secure}`;
  }
};

export const clearAccessToken = () => {
  localStorage.removeItem('access_token');
  if (typeof document !== 'undefined') {
    document.cookie = `${SESSION_HINT_COOKIE}=; path=/; max-age=0`;
  }
};

// ─── Token injection ──────────────────────────────────────────────
api.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  if (typeof window !== 'undefined') {
    const token = localStorage.getItem('access_token');
    if (token) config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// ─── Refresh token 401 handler ───────────────────────────────────
let isRefreshing = false;
let failedQueue: Array<{ resolve: (v: unknown) => void; reject: (e: unknown) => void }> = [];

const processQueue = (error: AxiosError | null, token: string | null = null) => {
  failedQueue.forEach(({ resolve, reject }) => {
    if (error) reject(error);
    else resolve(token);
  });
  failedQueue = [];
};

api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const original = error.config as InternalAxiosRequestConfig & { _retry?: boolean };

    // Endpoints públicos de autenticação: um 401 aqui significa credenciais inválidas (ou refresh
    // token expirado), não "access token expirado" — tentar renovar token e, em caso de falha,
    // redirecionar via window.location.href pra essas mesmas telas estava recarregando a página
    // de login/registro ANTES do catch do formulário conseguir exibir a mensagem de erro real
    // vinda do back-end (ex.: "Credenciais inválidas."), fazendo o erro nunca aparecer na tela.
    const isPublicAuthEndpoint = ['/auth/login', '/auth/google', '/auth/refresh-token'].some(
      (path) => original.url?.includes(path)
    );

    if (error.response?.status === 401 && !original._retry && !isPublicAuthEndpoint) {
      if (isRefreshing) {
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject });
        }).then((token) => {
          original.headers.Authorization = `Bearer ${token}`;
          return api(original);
        });
      }

      original._retry = true;
      isRefreshing = true;

      try {
        // O refresh token não existe mais no localStorage — ele vive só no cookie HttpOnly
        // "refresh_token" (setado pelo backend em /auth/login etc.), enviado automaticamente
        // pelo navegador porque withCredentials está ligado. Nada para ler/montar aqui além do
        // credentials: se o cookie não existir/estiver expirado, o backend responde 401 e cai
        // no catch abaixo normalmente.
        const { data } = await axios.post(
          `${API_URL}/api/v1/auth/refresh-token`,
          {},
          { withCredentials: true }
        );

        setAccessToken(data.accessToken);
        processQueue(null, data.accessToken);
        original.headers.Authorization = `Bearer ${data.accessToken}`;
        return api(original);
      } catch (refreshError) {
        processQueue(refreshError as AxiosError, null);
        clearAccessToken();
        if (typeof window !== 'undefined') window.location.href = '/login';
        return Promise.reject(refreshError);
      } finally {
        isRefreshing = false;
      }
    }

    return Promise.reject(error);
  }
);

// ─── Auth helpers ─────────────────────────────────────────────────
export const authApi = {
  register: (data: RegisterRequest) => api.post('/auth/register', data),
  login: (data: LoginRequest) => api.post('/auth/login', data),
  googleLogin: (idToken: string) => api.post('/auth/google', { idToken }),
  forgotPassword: (email: string) => api.post('/auth/forgot-password', { email }),
  // Consome o token do link de "/redefinir-senha?token=..." enviado por e-mail (ver
  // EmailService.SendPasswordResetAsync). Só token + nova senha — sem userId, diferente de
  // confirmEmail, porque não há sessão nem userId disponível nesse fluxo.
  resetPassword: (token: string, newPassword: string) =>
    api.post('/auth/reset-password', { token, newPassword }),
  // Backend exige userId + token juntos (ConfirmEmailCommand) — ver app/confirmar-email/page.tsx.
  // Antes era (userId, token) — trocado para (email, code) porque o e-mail de cadastro agora
  // manda um código curto de 6 dígitos (ver User.Register no backend), digitável manualmente
  // na tela de confirmação, em vez de um GUID que só funcionava embutido num link.
  confirmEmail: (email: string, code: string) => api.post('/auth/confirm-email', { email, code }),
  me: () => api.get('/auth/me'),
  // Edita nome/telefone/bio/avatar — usado, entre outras telas, pelo editor do Canal do Criador.
  updateProfile: (data: {
    firstName: string; lastName: string; phone?: string;
    birthDate?: string; bio?: string; avatarUrl?: string;
  }) => api.put('/auth/me', data),
  // Auto-promoção a Creator (plataforma aberta — sem aprovação de Admin).
  becomeCreator: () => api.post('/auth/become-creator'),
  // Magic Link — troca o token recebido por e-mail/WhatsApp por um login completo, sem senha.
  consumeMagicLink: (token: string) => api.post('/auth/magic-link/consume', { token }),
  // Reenvio self-service pra quem perdeu a janela de 48h do Magic Link original (ver
  // app/acesso/page.tsx, tela de link expirado) -- mesmo padrão de privacidade de
  // forgotPassword, a resposta não revela se o e-mail existe.
  resendAccessLink: (email: string) => api.post('/auth/resend-access-link', { email }),
  // Cookie-driven (achado C1) — o refresh token não é mais passado por parâmetro, ele viaja no
  // cookie HttpOnly "refresh_token" (ver withCredentials acima). Mantido aqui por simetria com
  // o restante do módulo; o interceptor 401 acima chama o endpoint diretamente via axios.
  refreshToken: () => api.post('/auth/refresh-token', {}),
  // Revoga o refresh token no servidor e limpa o cookie HttpOnly — ver (dashboard)/layout.tsx.
  logout: () => api.post('/auth/logout'),
};

export interface ListCoursesParams {
  search?: string;
  level?: string;
  page?: number;
  pageSize?: number;
}

export const coursesApi = {
  list: (params?: ListCoursesParams) => api.get('/courses', { params }),
  getBySlug: (slug: string) => api.get(`/courses/${slug}`),
  create: (data: unknown) => api.post('/courses', data),
  getLessonPlayUrl: (courseId: string, lessonId: string) =>
    api.get(`/courses/${courseId}/lessons/${lessonId}/play`),
  recordView: (slug: string) => api.post(`/courses/${slug}/view`),
  // Cross-sell: outros cursos publicados do mesmo criador. Público — não exige login.
  getByInstructor: (instructorId: string, excludeCourseId?: string) =>
    api.get(`/courses/by-instructor/${instructorId}`, { params: { excludeCourseId } }),

  // Admin/criador — reaproveitados pelo assistente de criação de produtos.
  getByIdAdmin: (id: string) => api.get(`/admin/courses/${id}`),
  listAdmin: () => api.get('/admin/courses'),
  updateBasicInfo: (id: string, data: unknown) => api.patch(`/courses/${id}`, data),
  setPrice: (id: string, price: number) => api.put(`/courses/${id}/price`, { price }),
  setSalesPage: (id: string, data: unknown) => api.put(`/courses/${id}/sales-page`, data),
  archive: (id: string) => api.post(`/courses/${id}/archive`),
  duplicate: (id: string) => api.post(`/courses/${id}/duplicate`),
  deleteCourse: (id: string) => api.delete(`/courses/${id}`),
  addModule: (courseId: string, data: { title: string; description?: string }) =>
    api.post(`/courses/${courseId}/modules`, data),
  addLesson: (courseId: string, moduleId: string, data: { title: string; description?: string; isPreview?: boolean }) =>
    api.post(`/courses/${courseId}/modules/${moduleId}/lessons`, data),
  // Editar uma aula já criada (ex.: preencher a descrição depois que a aula já existe/já tem
  // vídeo vinculado — addLesson só aceita a descrição no momento da criação).
  updateLesson: (courseId: string, moduleId: string, lessonId: string, data: { title: string; description?: string }) =>
    api.put(`/courses/${courseId}/modules/${moduleId}/lessons/${lessonId}`, data),
  addAttachment: (courseId: string, moduleId: string, lessonId: string, data: unknown) =>
    api.post(`/courses/${courseId}/modules/${moduleId}/lessons/${lessonId}/attachments`, data),
  // Achado B6 da avaliação: reordenar módulos/aulas por arrastar-e-soltar. Wrappers finos —
  // exigem a lista COMPLETA de IDs (do módulo, ou de aulas dentro de um módulo) na nova ordem
  // desejada. Ainda não há UI de drag-and-drop consumindo isto no assistente; o backend já
  // suporta para quando essa UI entrar no roadmap.
  reorderModules: (courseId: string, orderedModuleIds: string[]) =>
    api.put(`/courses/${courseId}/modules/reorder`, { orderedModuleIds }),
  reorderLessons: (courseId: string, moduleId: string, orderedLessonIds: string[]) =>
    api.put(`/courses/${courseId}/modules/${moduleId}/lessons/reorder`, { orderedLessonIds }),
};

export interface CategoryOption {
  name: string;
  subcategories: string[];
}

// Alimenta o autocomplete de Categoria/Subcategoria do passo 1 do assistente (ver
// CategoryAutocomplete e admin/produtos/novo/page.tsx). Lista curada + o que os criadores já
// digitaram em cursos existentes (ver GetCategoriesQueryHandler no backend).
export const categoriesApi = {
  list: () => api.get<CategoryOption[]>('/categories'),
};

export const videosApi = {
  // title: nome do arquivo enviado pelo criador — sem ele, o vídeo nasce sem título no banco e a
  // lista "Vídeos disponíveis para vincular" cai no fallback "(sem título)" assim que a página
  // recarrega os vídeos do servidor (achado 12/08/2026).
  getUploadUrl: (courseId: string, title?: string) => api.post('/videos/upload-url', { courseId, title }),
  linkVideo: (videoId: string, data: { courseId: string; moduleId: string; lessonId: string; isPreview?: boolean }) =>
    api.post(`/videos/${videoId}/link-lesson`, data),
  importExternal: (courseId: string, url: string, download?: boolean) =>
    api.post('/videos/import', { courseId, url, download }),
  listByCourse: (courseId: string) => api.get(`/videos/by-course/${courseId}`),
  delete: (videoId: string) => api.delete(`/videos/${videoId}`),
};

export const materialsApi = {
  upload: (file: File) => {
    const formData = new FormData();
    formData.append('file', file);
    return api.post('/materials/upload', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
  },
  // GET /materials/{storedName} exige login (ver comentário do achado M8 no
  // MaterialsUploadController) — um <a href> comum não manda o Bearer token, então o download
  // precisa passar por este client (que já injeta Authorization no interceptor) e devolver o
  // arquivo como blob para o chamador montar um link temporário. `fileUrl` já vem pronto (URL
  // absoluta) em LessonAttachmentResponse -- o axios usa uma URL absoluta como está, ignorando
  // baseURL, então funciona sem reconstruir o caminho aqui.
  download: (fileUrl: string) => api.get(fileUrl, { responseType: 'blob' }),
};

// ─── Jornada Guiada de Criação de Produtos ─────────────────────────
export const creatorStudioApi = {
  myProducts: () => api.get('/creator-studio/my-products'),
  generateProductCopy: (data: { productName: string; category?: string; subcategory?: string }) =>
    api.post('/creator-studio/generate-product-copy', data),
  generateSalesPageCopy: (courseId: string) =>
    api.post(`/creator-studio/products/${courseId}/generate-sales-page-copy`),
  // Central de Divulgação — texto pronto por canal (Instagram/Stories/WhatsApp/E-mail/Ads/Headline).
  generateMarketingCopy: (courseId: string, channel: string) =>
    api.post(`/creator-studio/products/${courseId}/generate-marketing-copy`, { channel }),
  publicationChecklist: (courseId: string) =>
    api.get(`/creator-studio/products/${courseId}/publication-checklist`),
  publish: (courseId: string) => api.post(`/creator-studio/products/${courseId}/publish`),
  dashboard: (courseId: string) => api.get(`/creator-studio/products/${courseId}/dashboard`),
  captureLead: (data: { courseId: string; name: string; email: string; phone?: string; source?: string }) =>
    api.post('/creator-studio/leads', data),
};

// Estúdio do Criador — nicho, geração de estrutura/roteiro por IA, templates de gravação e
// progresso do Clone do Professor.
export const studioApi = {
  getNiche: () => api.get('/creator-studio/studio/niche'),
  setNiche: (data: { niche: string; targetAudience: string; objective: string; level: string }) =>
    api.put('/creator-studio/studio/niche', data),
  generateCourseOutline: (data: { niche: string; targetAudience: string; objective: string; level: string }) =>
    api.post('/creator-studio/studio/course-outline', data),
  generateLessonScript: (data: { lessonTitle: string; niche: string; targetAudience: string; level: string }) =>
    api.post('/creator-studio/studio/lesson-script', data),
  getMyScripts: () => api.get('/creator-studio/studio/lesson-scripts'),
  saveScript: (data: {
    lessonTitle: string;
    introduction: string;
    developmentTopics: string[];
    demonstrationSuggestions: string[];
    closingCta: string;
    courseId?: string;
    lessonId?: string;
  }) => api.post('/creator-studio/studio/lesson-scripts', data),
  markScriptAsRecorded: (scriptId: string) =>
    api.post(`/creator-studio/studio/lesson-scripts/${scriptId}/mark-recorded`),
  getMyTemplates: () => api.get('/creator-studio/studio/recording-templates'),
  saveTemplate: (data: { name: string; sections: string[]; isDefault: boolean; templateId?: string }) =>
    api.put('/creator-studio/studio/recording-templates', data),
  deleteTemplate: (templateId: string) =>
    api.delete(`/creator-studio/studio/recording-templates/${templateId}`),
  getVideoEditingCapabilities: () => api.get('/creator-studio/studio/video-editing-capabilities'),
};

export const courseSubscriptionPlansApi = {
  getByCourse: (courseId: string) => api.get(`/subscriptions/plans/by-course/${courseId}`),
  createForCourse: (courseId: string, data: { price: number; billingCycle: string; trialDays?: number }) =>
    api.post(`/subscriptions/plans/by-course/${courseId}`, data),
};

export const enrollmentsApi = {
  enroll: (courseId: string) => api.post('/enrollments', { courseId }),
  // Matrícula em curso GRÁTIS sem exigir login prévio (achado B2 da avaliação de UX) — espelha
  // coursePurchasesApi.purchase/subscriptionsApi.subscribeToCourse (checkout anônimo): só
  // nome/e-mail, sem senha. Endpoint [AllowAnonymous] (POST /enrollments/free — ver
  // EnrollmentsController.EnrollFree no backend); se o visitante já estiver logado, o backend usa
  // o UserId da sessão em vez do e-mail do formulário (CustomerName/CustomerEmail são ignorados).
  enrollFreeAnonymous: (courseId: string, data: { customerName: string; customerEmail: string }) =>
    api.post('/enrollments/free', { courseId, ...data }),
  trackProgress: (enrollmentId: string, data: unknown) =>
    api.post(`/enrollments/${enrollmentId}/progress`, data),
  getProgress: (courseId: string) => api.get(`/enrollments/courses/${courseId}`),
  // Cursos em que o aluno está matriculado (filtro "Matriculados" em /cursos).
  getMyEnrollments: () => api.get('/enrollments/me'),
  // "Continuar de onde parei" — última aula assistida entre todos os cursos matriculados.
  getContinueWatching: () => api.get('/enrollments/continue-watching'),
  // Histórico de aulas assistidas — todas as aulas com progresso, entre todos os cursos.
  getLessonHistory: () => api.get('/enrollments/history'),
  // Ativa o acesso de um programa a partir de um código ("Tenho um código de acesso", dashboard
  // do aluno sem nenhum programa) — ver EnrollmentsController.RedeemCode no backend.
  redeemAccessCode: (code: string) => api.post('/enrollments/redeem-code', { code }),
};

// Painel Admin ("Plataforma" → "Códigos de acesso") — gera e lista códigos de acesso para
// qualquer programa publicado. Ver AdminAccessCodesController (Authorize Roles=Admin) no backend.
export const adminAccessCodesApi = {
  list: () => api.get('/admin/access-codes'),
  generate: (data: { courseId: string; maxUses?: number; expiresAt?: string }) =>
    api.post('/admin/access-codes', data),
};

export const subscriptionsApi = {
  getPlans: () => api.get('/subscriptions/plans'),
  getMySubscription: () => api.get('/subscriptions/me'),
  subscribe: (data: unknown) => api.post('/subscriptions', data),
  cancel: (reason?: string) => api.delete('/subscriptions/me', { data: { reason } }),
  // Assinatura de UM produto específico, comprada da Página de Vendas pública (/c/[slug]) — não
  // exige login (checkout anônimo, ver SubscribeToCourseCommandHandler). Diferente de `subscribe`
  // acima (assinatura da plataforma/plano legado, exige login e PlanId direto).
  subscribeToCourse: (courseId: string, data: { customerName: string; customerEmail: string; cpfCnpj?: string; phone?: string }) =>
    api.post(`/subscriptions/course/${courseId}`, data),
  // Assinatura do usuário logado para UM produto específico (o que ele contratou/paga por
  // ESSE curso — modelo Kiwify) — mostrado na própria tela do curso, nunca numa central de
  // assinatura da plataforma. 404 quando não há assinatura ativa para o curso.
  getMyCourseSubscription: (courseId: string) => api.get(`/subscriptions/course/${courseId}/me`),
  // SANDBOX/DEV apenas — 404 fora de Development no backend. Substitui o webhook do Asaas (que
  // não alcança localhost) para permitir testar o fluxo de assinatura de produto ponta a ponta.
  simulateSubscriptionPayment: (subscriptionId: string) => api.post(`/subscriptions/${subscriptionId}/simulate-payment`),
};

// Compra avulsa de curso (pagamento único) — modelo principal de monetização do Tuilow.
// Espelha o wrapper de subscriptionsApi acima, só que para o endpoint de compra avulsa
// (Tuilow.Sales.Api.Controllers.CoursePurchasesController), que já existia no backend mas
// nunca tinha sido consumido pelo frontend do aluno.
export const coursePurchasesApi = {
  purchase: (data: { courseId: string; customerName: string; customerEmail: string; cpfCnpj?: string; phone?: string }) =>
    api.post('/course-purchases', data),
  getMyPurchases: () => api.get('/course-purchases/me'),
  // SANDBOX/DEV apenas — 404 fora de Development no backend. Substitui o webhook do Asaas
  // (que não alcança localhost) para permitir testar o fluxo de compra ponta a ponta.
  simulatePayment: (purchaseId: string) => api.post(`/course-purchases/${purchaseId}/simulate-payment`),
};

// Canal do Criador — vitrine pública em /canal/[handle] (@handle + redes sociais + catálogo).
export const channelApi = {
  getMy: () => api.get('/channel/me'),
  upsert: (data: {
    handle: string;
    socialLinks: { platform: string; url: string }[];
    bannerUrl?: string;
    introVideoUrl?: string;
  }) => api.put('/channel/me', data),
  // Público — não exige login. viewerUserId é inferido no backend via token opcional.
  getByHandle: (handle: string) => api.get(`/channel/${handle}`),
};

// Achado A4 da avaliação: verificação pública de autenticidade de certificado — página
// /certificado/[code] chama isto para confirmar que um código realmente foi emitido, sem exigir
// login (é para ser conferível por qualquer pessoa, ex.: um recrutador).
//
// Feature 12/08/2026 ("Baixar certificado" + aba "Certificados"): getMine/getForCourse/download
// completam este client — certificado é emitido automaticamente quando um curso é 100%
// concluído (ver CourseCompletedEventHandler no backend). `download` segue o mesmo padrão de
// materialsApi.download acima (GET autenticado com responseType 'blob', porque um <a href>
// comum não manda o Bearer token) — o PDF é gerado na hora a cada chamada, não fica armazenado
// em lugar nenhum.
export const certificatesApi = {
  verify: (code: string) => api.get(`/certificates/verify/${code}`),
  getMine: () => api.get('/certificates/me'),
  getForCourse: (courseId: string) => api.get(`/certificates/course/${courseId}`),
  download: (certificateId: string) =>
    api.get(`/certificates/${certificateId}/download`, { responseType: 'blob' }),
};

// "Meus Perfis" (learnerProfilesApi) removido temporariamente da experiência do usuário — era
// um conceito de perfil de aprendizado sem relação com o modelo de negócio atual. O módulo
// backend (Journey) foi preserva

// Painel do dono da plataforma — listagem/gestão de usuários (ativar, suspender, excluir) e
// visão geral de estatísticas. Ver AdminUsersController (api/v1/admin/users).
export interface ListUsersParams {
  search?: string;
  role?: string;
  status?: string;
  page?: number;
  pageSize?: number;
}

// Marketplace de split (creator como emissor da cobrança) -- conectar/consultar a própria
// conta Asaas do criador. Ver Finance.Api.Controllers.CreatorAsaasAccountController.
export interface CreatorAsaasAccountStatus {
  isConnected: boolean;
  status: 'NotConnected' | 'PendingValidation' | 'Active' | 'Restricted' | 'Rejected' | 'Disabled';
  canSell: boolean;
  walletId: string | null;
  commissionOverridePercentage: number | null;
  lastValidatedAt: string | null;
  lastValidationError: string | null;
}

export const financeAsaasAccountApi = {
  getStatus: () => api.get<CreatorAsaasAccountStatus>('/finance/asaas-account'),
  connect: (data: { apiKey: string; cpfCnpj?: string; legalName?: string }) =>
    api.post('/finance/asaas-account/connect', data),
};

// Painel do dono da plataforma -- contas Asaas conectadas pelos criadores (marketplace de
// split). Ver Finance.Api.Controllers.AdminFinanceController.
export interface AdminCreatorAsaasAccountItem {
  id: string;
  creatorId: string;
  creatorName: string;
  creatorEmail: string;
  status: 'NotConnected' | 'PendingValidation' | 'Active' | 'Restricted' | 'Rejected' | 'Disabled';
  isEnabledForSelling: boolean;
  cpfCnpjMasked: string | null;
  walletIdMasked: string | null;
  commissionOverridePercentage: number | null;
  lastValidatedAt: string | null;
  lastWebhookReceivedAt: string | null;
  lastValidationError: string | null;
}

// ─── Onboarding financeiro do criador (subconta Asaas/BaaS criada pela própria Tuilow) ──────
// Substitui, para criadores novos, o fluxo acima de "cole sua API Key" (financeAsaasAccountApi
// -- mantido só por compatibilidade com quem já conectou antes). Nenhum campo aqui carrega API
// Key/Wallet ID -- o backend nunca devolve isso (ver
// Finance.Api.Controllers.CreatorFinancialOnboardingController).
export interface OnboardingStep {
  key: string;
  title: string;
  state: 'done' | 'current' | 'pending' | 'blocked';
}

export interface OnboardingDocument {
  id: string;
  title: string;
  description: string | null;
  status: 'Pending' | 'AwaitingApproval' | 'Approved' | 'Rejected';
  onboardingUrl: string | null;
}

// Dados já digitados pelo criador -- devolvido só enquanto o passo 1 ainda pode ser reenviado
// (ex.: depois de uma rejeição), pra não obrigar a redigitar tudo. Nunca inclui API Key/Wallet ID.
export interface PreviousOnboardingData {
  legalName: string;
  cpfCnpj: string;
  birthDate: string | null;
  companyType: string | null;
  email: string;
  mobilePhone: string;
  phone: string | null;
  incomeValue: number;
  address: string;
  addressNumber: string;
  addressComplement: string | null;
  province: string;
  postalCode: string;
}

export interface CreatorFinancialOnboardingStatus {
  status: string;
  canSell: boolean;
  friendlyMessage: string | null;
  steps: OnboardingStep[];
  documents: OnboardingDocument[];
  previousData: PreviousOnboardingData | null;
}

export interface StartFinancialOnboardingData {
  legalName: string;
  cpfCnpj: string;
  birthDate?: string; // "YYYY-MM-DD" -- obrigatório quando cpfCnpj tem 11 dígitos (pessoa física)
  companyType?: string; // MEI | LIMITED | INDIVIDUAL | ASSOCIATION -- obrigatório quando cpfCnpj tem 14 dígitos
  email: string;
  mobilePhone: string;
  phone?: string;
  incomeValue: number;
  address: string;
  addressNumber: string;
  addressComplement?: string;
  province: string;
  postalCode: string;
}

export interface StartFinancialOnboardingResult {
  success: boolean;
  status: string;
  errorMessage: string | null;
}

export const financialOnboardingApi = {
  getStatus: () => api.get<CreatorFinancialOnboardingStatus>('/finance/onboarding'),
  start: (data: StartFinancialOnboardingData) =>
    api.post<StartFinancialOnboardingResult>('/finance/onboarding/start', data),
  // Sincroniza com a Asaas e devolve a lista de documentos já atualizada.
  getDocuments: () => api.get<OnboardingDocument[]>('/finance/onboarding/documents'),
  // Só funciona para documentos SEM onboardingUrl (ver OnboardingDocument acima) -- os que têm
  // link precisam ser enviados na própria página hospedada pela Asaas (aberta em nova aba).
  uploadDocument: (asaasDocumentId: string, file: File) => {
    const formData = new FormData();
    formData.append('file', file);
    return api.post(`/finance/onboarding/documents/${asaasDocumentId}/upload`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
  },
};

// Painel financeiro do próprio criador (dashboard + extrato de vendas) -- feature 12/08/2026:
// "quando ele clique em financeiro, tenha o controle dele de vendas, estornos, porcentagem de
// ganho... quem já comprou e pagou aparecer para ele". Backend já existia (GetCreatorFinancialDashboardQuery
// /GetCreatorSalesHistoryQuery) mas nunca tinha sido consumido pelo frontend -- a tela só mostrava
// o card estático "conta pronta" (ver ReadyToSellCard em admin/financeiro/page.tsx).
export interface CreatorFinancialDashboard {
  availableBalance: number;
  pendingBalance: number;
  totalGrossSales: number;
  totalPlatformFeePaid: number;
  totalNetEarned: number;
  totalWithdrawn: number;
  totalSalesCount: number;
  totalRefundedAmount: number;
  totalRefundedCount: number;
  currentCycleStart: string;
  currentCycleEnd: string;
  nextReleaseDate: string;
  // MarketplaceSplit -- informativo, não é saldo sacável (a Asaas já credita direto na conta do
  // próprio criador, o dinheiro nunca passa pela Hi Mentor nesse modelo).
  marketplaceGrossSales: number;
  marketplaceCommissionPaid: number;
  marketplaceNetEarned: number;
  marketplaceSalesCount: number;
  marketplaceRefundedAmount: number;
  marketplaceRefundedCount: number;
}

export interface CreatorSaleItem {
  coursePurchaseId: string;
  studentId: string;
  studentName: string;
  studentEmail: string;
  courseId: string;
  courseTitle: string;
  paymentModel: 'Legacy' | 'MarketplaceSplit';
  status: 'Pending' | 'Confirmed' | 'Failed' | 'Refunded';
  grossAmount: number;
  platformFeeAmount: number | null;
  commissionPercentage: number | null;
  creatorNetAmount: number | null;
  payoutStatus: 'Pending' | 'Available' | null;
  confirmedAt: string | null;
  refundedAt: string | null;
  createdAt: string;
}

export interface PlatformFeeInfo {
  percentage: number;
  effectiveFrom: string;
}

export const financeApi = {
  getDashboard: () => api.get<CreatorFinancialDashboard>('/finance/dashboard'),
  getSales: (params?: { from?: string; to?: string }) =>
    api.get<CreatorSaleItem[]>('/finance/sales', { params }),
  getCurrentFee: () => api.get<PlatformFeeInfo>('/finance/fee'),
};

// Painel do dono da plataforma -- pipeline de onboarding financeiro via subconta (novo modelo).
export interface AdminCreatorOnboardingItem {
  id: string;
  creatorId: string;
  creatorName: string;
  creatorEmail: string;
  status: string;
  cpfCnpjMasked: string | null;
  pendingDocumentsCount: number;
  approvedAt: string | null;
  rejectionReason: string | null;
}

export const adminFinanceApi = {
  listAsaasAccounts: (params?: { skip?: number; take?: number }) =>
    api.get<AdminCreatorAsaasAccountItem[]>('/admin/finance/asaas-accounts', { params }),
  setAsaasAccountEnabled: (creatorAsaasAccountId: string, enabled: boolean) =>
    api.put(`/admin/finance/asaas-accounts/${creatorAsaasAccountId}/enabled`, { enabled }),
  setCommissionOverride: (creatorAsaasAccountId: string, percentage: number | null) =>
    api.put(`/admin/finance/asaas-accounts/${creatorAsaasAccountId}/commission-override`, { percentage }),
  // Novo modelo de onboarding (subconta Asaas/BaaS).
  listOnboardings: (params?: { skip?: number; take?: number }) =>
    api.get<AdminCreatorOnboardingItem[]>('/admin/finance/onboardings', { params }),
  setOnboardingBlocked: (creatorAsaasSubaccountId: string, blocked: boolean, reason?: string) =>
    api.put(`/admin/finance/onboardings/${creatorAsaasSubaccountId}/blocked`, { blocked, reason }),
};

export const adminUsersApi = {
  list: (params?: ListUsersParams) => api.get('/admin/users', { params }),
  getStats: () => api.get('/admin/users/stats'),
  suspend: (userId: string) => api.put(`/admin/users/${userId}/suspend`),
  reactivate: (userId: string) => api.put(`/admin/users/${userId}/reactivate`),
  delete: (userId: string) => api.delete(`/admin/users/${userId}`),
  // "Cursos e acessos" no detalhe do usuário -- ver AdminUsersController.GetUserCourses.
  // Nunca traz um link pronto (nenhum token fica exposto até o admin pedir a reemissão).
  getCourses: (userId: string) => api.get(`/admin/users/${userId}/courses`),
  // Reemite o link de acesso (Magic Link) a um curso específico -- ver ReissueCourseAccessLink.
  reissueCourseAccessLink: (userId: string, courseId: string) =>
    api.post(`/admin/users/${userId}/courses/${courseId}/access-link`),
};
