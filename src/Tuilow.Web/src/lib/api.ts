import axios, { AxiosError, InternalAxiosRequestConfig } from 'axios';

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5000';

export const api = axios.create({
  baseURL: `${API_URL}/api/v1`,
  headers: { 'Content-Type': 'application/json' },
  timeout: 15000,
});

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
  confirmEmail: (token: string) => api.post('/auth/confirm-email', { token }),
  me: () => api.get('/auth/me'),
  // Auto-promoção a Creator (plataforma aberta — sem aprovação de Admin).
  becomeCreator: () => api.post('/auth/become-creator'),
  refreshToken: (refreshToken: string) => api.post('/auth/refresh-token', { refreshToken }),
};

export const coursesApi = {
  list: (params?: ListCoursesParams) => api.get('/courses', { params }),
  getBySlug: (slug: string) => api.get(`/courses/${slug}`),
  create: (data: unknown) => api.post('/courses', data),
  getLessonPlayUrl: (courseId: string, lessonId: string) =>
    api.get(`/courses/${courseId}/lessons/${lessonId}/play`),
  recordView: (slug: string) => api.post(`/courses/${slug}/view`),

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
  importExternal: (courseId: string, url: string) => api.post('/videos/import', { courseId, url }),
  listByCourse: (courseId: string) => api.get(`/videos/by-course/${courseId}`),
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
  publicationChecklist: (courseId: string) =>
    api.get(`/creator-studio/products/${courseId}/publication-checklist`),
  publish: (courseId: string) => api.post(`/creator-studio/products/${courseId}/publish`),
  dashboard: (courseId: string) => api.get(`/creator-studio/products/${courseId}/dashboard`),
  captureLead: (data: { courseId: string; name: string; email: string; phone?: string; source?: string }) =>
    api.post('/creator-studio/leads', data),
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

// "Meus Perfis" (learnerProfilesApi) removido temporariamente da experiência do usuário — era
// um conceito de perfil de aprendizado sem relação com o modelo de negócio atual. O módulo
// backend (Journey) foi preservado intacto, só desligado do pipeline (ver Host/Program.cs).

// ─── Types ───────────────────────────────────────────────────────
interface RegisterRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
}
interface LoginRequest { email: string; password: string; }
interface ListCoursesParams {
  level?: string;
  search?: string;
  page?: number;
  pageSize?: number;
}
