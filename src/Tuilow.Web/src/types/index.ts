// Achado C1 da avaliação www/app: o refresh token não é mais devolvido no corpo JSON — ele
// vive só no cookie HttpOnly "refresh_token" setado pelo backend (ver AuthController.SetRefreshTokenCookie).
// Devolvê-lo aqui de novo reabriria a exposição a XSS que essa migração fecha.
export interface AuthTokens {
  accessToken: string;
  accessTokenExpires: string;
}

// Corpo de POST /auth/register — reflete o formulário em app/(auth)/registro/page.tsx
// (que envia todos os campos do schema exceto confirmPassword).
export interface RegisterRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
}

// Corpo de POST /auth/login — reflete o formulário em app/(auth)/login/page.tsx.
export interface LoginRequest {
  email: string;
  password: string;
}

export type RoleName = 'Student' | 'Creator' | 'Admin' | 'ChannelMember';

export interface UserProfile {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  // Multi-role: um usuário pode ter vários roles simultâneos (ex.: Student + Creator).
  roles: RoleName[];
  isEmailConfirmed: boolean;
  avatarUrl?: string;
}

export interface Course {
  id: string;
  title: string;
  slug: string;
  shortDescription: string;
  thumbnailUrl?: string;
  level: 'Beginner' | 'Intermediate' | 'Advanced';
  price: number;
  isFree: boolean;
  // Estado real de comercialização ("Free"/"Paid"/"Subscription"/"Hidden") — fonte da verdade
  // para exibir "Grátis"/preço; isFree sozinho não é confiável (ver CourseCommercializationResolver
  // no backend: um curso no modo "Assinatura" grava price=0 mas não é grátis).
  commercializationState?: string;
  totalLessons: number;
  totalDuration: number;
  rating?: number;
  instructorName?: string;
  isEnrolled?: boolean;
}

export interface EnrollmentProgress {
  enrollmentId: string;
  progressPercent: number;
  status: string;
  completedLessons: number;
  totalLessons: number;
}

// GET /enrollments/me — cursos em que o aluno está matriculado (filtro "Matriculados" em /cursos).
export interface MyEnrollment {
  enrollmentId: string;
  courseId: string;
  title: string;
  slug: string;
  thumbnailUrl?: string;
  price: number;
  isFree: boolean;
  level: string;
  status: string;
  progressPercentage: number;
  enrolledAt: string;
  completedAt?: string;
  completedLessonsCount: number;
}

// GET /enrollments/continue-watching — última aula assistida entre todos os cursos matriculados.
export interface ContinueWatching {
  courseId: string;
  courseTitle: string;
  courseSlug: string;
  thumbnailUrl?: string;
  lessonId: string;
  lessonTitle: string;
  courseProgressPercentage: number;
  lastWatchedAt: string;
}

// GET /enrollments/history — todas as aulas com progresso, entre todos os cursos matriculados.
export interface LessonHistoryItem {
  courseId: string;
  courseTitle: string;
  courseSlug: string;
  thumbnailUrl?: string;
  lessonId: string;
  lessonTitle: string;
  isCompleted: boolean;
  completedAt?: string;
  lastWatchedAt: string;
}

export interface Plan {
  id: string;
  name: string;
  slug: string;
  description?: string;
  price: number;
  billingCycle: 'Monthly' | 'Quarterly' | 'Semiannual' | 'Annual';
  trialDays: number;
  features: PlanFeature[];
}

export interface PlanFeature {
  featureKey: string;
  featureValue?: string;
  displayName?: string;
}

// Plano de assinatura vinculado a UM produto específico (GET /subscriptions/plans/by-course/:id).
// Formato reduzido em relação a Plan (sem slug/features) — é o que o endpoint por-curso retorna.
export interface CoursePlan {
  id: string;
  name: string;
  description?: string;
  price: number;
  billingCycle: 'Monthly' | 'Quarterly' | 'Semiannual' | 'Annual';
  trialDays: number;
}

// LearnerProfile removido junto com "Meus Perfis" — ver comentário em lib/api.ts.

export interface Subscription {
  id: string;
  planName: string;
  status: string;
  price: number;
  billingCycle: string;
  currentPeriodEnd: string;
  isActive: boolean;
}

// ─── Jornada Guiada de Criação de Produtos ─────────────────────────

export type ProductStatus = 'Draft' | 'InReview' | 'Published' | 'Archived';
export type ProductType =
  | 'Course' | 'Ebook' | 'Bundle' | 'Subscription' | 'Mentoring' | 'Event' | 'Service';

export interface ProductListItem {
  id: string;
  name: string;
  slug: string;
  category?: string;
  productType: ProductType;
  status: ProductStatus;
  createdAt: string;
  totalSales: number;
  revenueGenerated: number;
}

export interface VideoRef {
  id: string;
  hasVideo: boolean;
}

export interface LessonDetail {
  id: string;
  title: string;
  description?: string;
  order: number;
  durationSeconds?: number;
  isPreview: boolean;
  hasVideo: boolean;
}

export interface ModuleDetail {
  id: string;
  title: string;
  description?: string;
  order: number;
  lessons: LessonDetail[];
}

export interface FaqItem {
  id?: string;
  question: string;
  answer: string;
  order?: number;
}

export interface TestimonialItem {
  authorName: string;
  authorRole?: string;
  quote: string;
  avatarUrl?: string;
}

export interface ProductDetail {
  id: string;
  title: string;
  slug: string;
  description: string;
  shortDescription?: string;
  thumbnailUrl?: string;
  price: number;
  isFree: boolean;
  // Ver Course.commercializationState acima — mesma regra, mesma fonte de verdade.
  commercializationState?: string;
  level: string;
  totalDurationMinutes: number;
  publishedAt?: string;
  modules: ModuleDetail[];
  status: ProductStatus;
  category?: string;
  subcategory?: string;
  productType: ProductType;
  viewCount: number;
  salesPageHeadline?: string;
  salesPageSubheadline?: string;
  salesPageCtaText?: string;
  salesPageBenefits: string[];
  faqItems: FaqItem[];
  instructorId: string;
  instructorName?: string;
  instructorAvatarUrl?: string;
  instructorBio?: string;
  salesPageVideoUrl?: string;
  testimonials: TestimonialItem[];
  guaranteeDays?: number;
  guaranteeText?: string;
}

export interface PublicationChecklist {
  basicInfoFilled: boolean;
  contentUploaded: boolean;
  priceDefined: boolean;
  salesPageCreated: boolean;
  isComplete: boolean;
}

export interface ProductDashboard {
  courseId: string;
  productName: string;
  slug: string;
  views: number;
  leads: number;
  students: number;
  sales: number;
  revenue: number;
  platformFee: number;
  netRevenue: number;
  platformFeePercentage: number;
}

export interface ProductCopySuggestion {
  shortDescription: string;
  fullDescription: string;
  benefits: string[];
  targetAudience: string;
  callToAction: string;
}

export interface SalesPageFaqSuggestion {
  question: string;
  answer: string;
}

export interface SalesPageSuggestion {
  headline: string;
  subheadline: string;
  benefits: string[];
  faq: SalesPageFaqSuggestion[];
  callToAction: string;
}

// Central de Divulgação — botão "Gerar com IA" por canal.
export type MarketingChannel =
  | 'InstagramPost' | 'InstagramStory' | 'WhatsApp' | 'Email' | 'MetaAds' | 'Headline';

export interface MarketingCopySuggestion {
  content: string;
  cta?: string;
}

// Cross-sell: outros cursos publicados do mesmo criador (GET /courses/by-instructor/:id).
export interface InstructorCourseSummary {
  id: string;
  title: string;
  slug: string;
  thumbnailUrl?: string;
  price: number;
  isFree: boolean;
  // Ver Course.commercializationState acima — mesma regra, mesma fonte de verdade.
  commercializationState?: string;
  level: string;
}

// ─── Canal do Criador ─────────────────────────

export interface SocialLinkItem {
  platform: string;
  url: string;
}

// GET /channel/me — tela "Meu Canal" (null se o criador ainda não criou um).
export interface MyChannel {
  id: string;
  handle: string;
  socialLinks: SocialLinkItem[];
  bannerUrl?: string;
  introVideoUrl?: string;
}

// GET /channel/:handle — vitrine pública em /canal/[handle].
export interface PublicChannelCourseItem {
  id: string;
  title: string;
  slug: string;
  thumbnailUrl?: string;
  price: number;
  isFree: boolean;
  isUnlocked: boolean;
  // Ver Course.commercializationState acima — mesma regra, mesma fonte de verdade.
  commercializationState: string;
}

export interface PublicChannel {
  channelId: string;
  handle: string;
  displayName: string;
  avatarUrl?: string;
  bio?: string;
  socialLinks: SocialLinkItem[];
  courses: PublicChannelCourseItem[];
  bannerUrl?: string;
  introVideoUrl?: string;
}

// ─── Estúdio do Criador ─────────────────────────

export type AudienceLevel = 'Beginner' | 'Intermediate' | 'Advanced';

// GET /creator-studio/studio/niche — perfil de nicho do criador (null se ainda não preencheu).
export interface CreatorStyleProfile {
  id: string;
  niche: string;
  targetAudience: string;
  objective: string;
  level: AudienceLevel;
  recordedScriptsCount: number;
  scriptsRequiredForClone: number;
  isCloneReady: boolean;
}

export interface CourseOutlineLesson {
  title: string;
  format: string;
}

export interface CourseOutlineModule {
  title: string;
  lessons: CourseOutlineLesson[];
}

// POST /creator-studio/studio/course-outline
export interface CourseOutlineSuggestion {
  courseName: string;
  courseDescription: string;
  modules: CourseOutlineModule[];
}

// POST /creator-studio/studio/lesson-script
export interface LessonScriptSuggestion {
  introduction: string;
  developmentTopics: string[];
  demonstrationSuggestions: string[];
  closingCta: string;
}

// GET /creator-studio/studio/lesson-scripts
export interface LessonScriptItem {
  id: string;
  courseId?: string;
  lessonId?: string;
  lessonTitle: string;
  introduction: string;
  developmentTopics: string[];
  demonstrationSuggestions: string[];
  closingCta: string;
  wasRecorded: boolean;
  recordedAt?: string;
  createdAt: string;
}

// GET /creator-studio/studio/recording-templates
export interface RecordingTemplateItem {
  id: string;
  name: string;
  sections: string[];
  isDefault: boolean;
}

// GET /creator-studio/studio/video-editing-capabilities
export interface VideoEditingCapabilities {
  isAvailable: boolean;
  statusMessage: string;
}
