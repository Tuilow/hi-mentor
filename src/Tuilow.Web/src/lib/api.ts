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
};

export const coursesApi = {
  list: (params?: ListCoursesParams) => api.get('/courses', { params }),
  getBySlug: (slug: string) => api.get(`/courses/${slug}`),
  create: (data: unknown) => api.post('/courses', data),
  getLessonPlayUrl: (courseId: string, lessonId: string) =>
    api.get(`/courses/${courseId}/lessons/${lessonId}/play`),
};

export const enrollmentsApi = {
  enroll: (courseId: string) => api.post('/enrollments', { courseId }),
  trackProgress: (enrollmentId: string, data: unknown) =>
    api.post(`/enrollments/${enrollmentId}/progress`, data),
  getProgress: (courseId: string) => api.get(`/enrollments/courses/${courseId}`),
};

export const subscriptionsApi = {
  getPlans: () => api.get('/subscriptions/plans'),
  getMySubscription: () => api.get('/subscriptions/me'),
  subscribe: (data: unknown) => api.post('/subscriptions', data),
  cancel: (reason?: string) => api.delete('/subscriptions/me', { data: { reason } }),
};

export const learnerProfilesApi = {
  getMyProfiles: () => api.get('/learner-profiles'),
  register: (data: unknown) => api.post('/learner-profiles', data),
};

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
