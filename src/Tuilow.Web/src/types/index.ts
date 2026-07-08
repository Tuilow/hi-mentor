export interface AuthTokens {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
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

export interface ProductDetail {
  id: string;
  title: string;
  slug: string;
  description: string;
  shortDescription?: string;
  thumbnailUrl?: string;
  price: number;
  isFree: boolean;
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
