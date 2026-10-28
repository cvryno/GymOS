export interface Member {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  photoUrl?: string;
  status: string;
  dateOfBirth: string;
  address: string;
  emergencyContactName: string;
  emergencyContactPhone: string;
  createdAt: string;
  activeMembership?: ActiveMembership;
  get fullName(): string;
}

export interface ActiveMembership {
  id: number;
  planName: string;
  startDate: string;
  endDate: string;
  status: string;
  daysRemaining: number;
}

export interface CreateMemberDto {
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  dateOfBirth: string;
  address: string;
  emergencyContactName: string;
  emergencyContactPhone: string;
  notes?: string;
}

export interface MembershipPlan {
  id: number;
  name: string;
  description: string;
  price: number;
  durationDays: number;
  isActive: boolean;
  includesClasses: boolean;
  maxClassesPerMonth?: number;
}

export interface Membership {
  id: number;
  memberId: number;
  memberName: string;
  memberEmail: string;
  memberPhone: string;
  planId: number;
  planName: string;
  startDate: string;
  endDate: string;
  status: string;
  amountPaid: number;
  daysRemaining: number;
  notificationSent: boolean;
}

export interface CreateMembershipDto {
  memberId: number;
  planId: number;
  startDate: string;
  amountPaid: number;
  notes?: string;
}

export interface Instructor {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  photoUrl?: string;
  specialties: string;
  bio?: string;
  isActive: boolean;
  hireDate: string;
  classCount: number;
}

export interface GymClass {
  id: number;
  name: string;
  description: string;
  category: string;
  instructorId: number;
  instructorName: string;
  durationMinutes: number;
  maxCapacity: number;
  color?: string;
  isActive: boolean;
  enrolledCount: number;
  schedules: Schedule[];
}

export interface Schedule {
  id: number;
  dayOfWeek: string;
  startTime: string;
  endTime: string;
  room?: string;
}

export interface Equipment {
  id: number;
  name: string;
  brand: string;
  model: string;
  serialNumber: string;
  status: string;
  category: string;
  purchaseDate: string;
  purchasePrice: number;
  lastMaintenanceDate?: string;
  nextMaintenanceDate?: string;
  location?: string;
  notes?: string;
}

export interface AccessLog {
  id: number;
  memberId: number;
  memberName: string;
  memberPhoto: string;
  checkIn: string;
  checkOut?: string;
  duration?: string;
}

export interface Invoice {
  id: number;
  invoiceNumber: string;
  memberId: number;
  memberName: string;
  subtotal: number;
  discount: number;
  tax: number;
  total: number;
  status: string;
  dueDate: string;
  paidAt?: string;
  paymentMethod?: string;
}

export interface DashboardStats {
  totalMembers: number;
  activeMembers: number;
  newMembersThisMonth: number;
  revenueThisMonth: number;
  revenueLastMonth: number;
  totalClasses: number;
  classesToday: number;
  membershipsExpiringIn3Days: number;
  membershipsExpiringIn7Days: number;
  activeMembershipsCount: number;
  monthlyRevenue: MonthlyRevenue[];
  membershipsByPlan: MembershipsByPlan[];
  topClasses: TopClass[];
  recentMembers: Member[];
  expiringMemberships: Membership[];
}

export interface MonthlyRevenue { month: string; revenue: number; }
export interface MembershipsByPlan { planName: string; count: number; color: string; }
export interface TopClass { className: string; enrollmentCount: number; }

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  email: string;
  fullName: string;
  role: string;
  photoUrl?: string;
}

export interface LoginDto { email: string; password: string; }
export interface GoogleLoginDto { idToken: string; }
