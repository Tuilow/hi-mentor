import axios, { AxiosError, InternalAxiosRequestConfig } from 'axios';
import type { RegisterRequest, LoginRequest } from '@/types';

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5000';

export const api = axios.create({
  baseURL: `${API_URL}/api/v1`,
  headers: { 'Content-Type': 'application/json' },
  timeout: 15000,
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

    if (error.response?.status === 401 && !original._retry) {
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
        const refreshToken = localStorage.getItem('refresh_token');
        if (!refreshToken) throw new Error('No refresh token');

        const { data } = await axios.post(`${API_URL}/api/v1/auth/refresh-token`, {
          refreshToken,
        });

        localStorage.setItem('access_token', data.accessToken);
        localStorage.setItem('refresh_token', data.refreshToken);
        processQueue(null, data.accessToken);
        original.headers.Authorization = `Bearer ${data.accessToken}`;
        return api(original);
      } catch (refreshError) {
        processQueue(refreshError as AxiosError, null);
        localStorage.removeItem('access_token');
        localStorage.removeItem('refresh_token');
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
  confirmEmail: (userId: string, token: string) => api.post('/auth/confirm-email', { userId, token }),
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
  refreshToken: (refreshToken: string) => api.post('/auth/refresh-token', { refreshToken }),
};

export interface ListCoursesParams {
  search?: string;
  level?: string;
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
  addAttachment: (courseId: string, moduleId: string, lessonId: string, data: unknown) =>
    api.post(`/courses/${courseId}/modules/${moduleId}/lessons/${lessonId}/attachments`, data),
};

export const videosApi = {
  getUploadUrl: (courseId: string) => api.post('/videos/upload-url', { courseId }),
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
  trackProgress: (enrollmentId: string, data: unknown) =>
    api.post(`/enrollments/${enrollmentId}/progress`, data),
  getProgress: (courseId: string) => api.get(`/enrollments/courses/${courseId}`),
  // Cursos em que o aluno está matriculado (filtro "Matriculados" em /cursos).
  getMyEnrollments: () => api.get('/enrollments/me'),
  // "Continuar de onde parei" — última aula assistida entre todos os cursos matriculados.
  getContinueWatching: () => api.get('/enrollments/continue-watching'),
  // Histórico de aulas assistidas — todas as aulas com progresso, entre todos os cursos.
  getLessonHistory: () => api.get('/enrollments/history'),
};

export const subscriptionsApi = {
  getPlans: () => api.get('/subscriptions/plans'),
  getMySubscription: () => api.get('/subscriptions/me'),
  subscribe: (data: unknown) => api.post('/subscriptions', data),
  cancel: (reason?: string) => api.delete('/subscriptions/me', { data: { reason } }),
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

export const adminUsersApi = {
  list: (params?: ListUsersParams) => api.get('/admin/users', { params }),
  getStats: () => api.get('/admin/users/stats'),
  suspend: (userId: string) => api.put(`/admin/users/${userId}/suspend`),
  reactivate: (userId: string) => api.put(`/admin/users/${userId}/reactivate`),
  delete: (userId: string) => api.delete(`/admin/users/${userId}`),
};
