export interface AuthTokens {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
}

export interface UserProfile {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: 'Student' | 'Instructor' | 'Admin';
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
  billingCycle: 'Monthly' | 'Quarterly' | 'Annual';
  trialDays: number;
  features: PlanFeature[];
}

export interface PlanFeature {
  featureKey: string;
  featureValue?: string;
  displayName?: string;
}

export interface LearnerProfile {
  id: string;
  name: string;
  category?: string;
  ageMonths?: number;
  photoUrl?: string;
  level?: string;
}

export interface Subscription {
  id: string;
  planName: string;
  status: string;
  price: number;
  billingCycle: string;
  currentPeriodEnd: string;
  isActive: boolean;
}
