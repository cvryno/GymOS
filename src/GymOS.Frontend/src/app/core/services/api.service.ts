import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  Member, CreateMemberDto, PagedResult, Membership, MembershipPlan,
  CreateMembershipDto, Instructor, GymClass, Equipment, AccessLog,
  Invoice, DashboardStats, AuthResponse, LoginDto
} from '../models/models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private base = environment.apiUrl;

  constructor(private http: HttpClient) {}

  // ── AUTH ──
  login(dto: LoginDto): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.base}/auth/login`, dto);
  }
  googleLogin(idToken: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.base}/auth/google`, { idToken });
  }
  refresh(refreshToken: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.base}/auth/refresh`, `"${refreshToken}"`, {
      headers: { 'Content-Type': 'application/json' }
    });
  }

  // ── DASHBOARD ──
  getDashboardStats(): Observable<DashboardStats> {
    return this.http.get<DashboardStats>(`${this.base}/dashboard`);
  }

  // ── MEMBERS ──
  getMembers(page = 1, pageSize = 10, search = '', status = ''): Observable<PagedResult<Member>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search) params = params.set('search', search);
    if (status) params = params.set('status', status);
    return this.http.get<PagedResult<Member>>(`${this.base}/members`, { params });
  }
  getMember(id: number): Observable<Member> {
    return this.http.get<Member>(`${this.base}/members/${id}`);
  }
  createMember(dto: CreateMemberDto): Observable<Member> {
    return this.http.post<Member>(`${this.base}/members`, dto);
  }
  updateMember(id: number, dto: any): Observable<Member> {
    return this.http.put<Member>(`${this.base}/members/${id}`, dto);
  }
  deleteMember(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/members/${id}`);
  }
  getMemberMemberships(id: number): Observable<Membership[]> {
    return this.http.get<Membership[]>(`${this.base}/members/${id}/memberships`);
  }

  // ── MEMBERSHIPS ──
  getMemberships(page = 1, pageSize = 10, status = '', search = ''): Observable<PagedResult<Membership>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (status) params = params.set('status', status);
    if (search) params = params.set('search', search);
    return this.http.get<PagedResult<Membership>>(`${this.base}/memberships`, { params });
  }
  getExpiringMemberships(days = 7): Observable<Membership[]> {
    return this.http.get<Membership[]>(`${this.base}/memberships/expiring?days=${days}`);
  }
  createMembership(dto: CreateMembershipDto): Observable<Membership> {
    return this.http.post<Membership>(`${this.base}/memberships`, dto);
  }
  cancelMembership(id: number): Observable<void> {
    return this.http.put<void>(`${this.base}/memberships/${id}/cancel`, {});
  }
  sendWhatsAppNotification(id: number): Observable<any> {
    return this.http.post(`${this.base}/memberships/${id}/notify-whatsapp`, {});
  }
  getExpiring(days = 7): Observable<Membership[]> { return this.getExpiringMemberships(days); }

  // ── MEMBERSHIP PLANS ──
  getPlans(): Observable<MembershipPlan[]> {
    return this.http.get<MembershipPlan[]>(`${this.base}/memberships/plans`);
  }
  createPlan(dto: any): Observable<MembershipPlan> {
    return this.http.post<MembershipPlan>(`${this.base}/memberships/plans`, dto);
  }
  updatePlan(id: number, dto: any): Observable<void> {
    return this.http.put<void>(`${this.base}/memberships/plans/${id}`, dto);
  }

  // ── INSTRUCTORS ──
  getInstructors(page = 1, pageSize = 10, search = ''): Observable<PagedResult<Instructor>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search) params = params.set('search', search);
    return this.http.get<PagedResult<Instructor>>(`${this.base}/instructors`, { params });
  }
  createInstructor(dto: any): Observable<Instructor> {
    return this.http.post<Instructor>(`${this.base}/instructors`, dto);
  }
  updateInstructor(id: number, dto: any): Observable<void> {
    return this.http.put<void>(`${this.base}/instructors/${id}`, dto);
  }
  deleteInstructor(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/instructors/${id}`);
  }

  // ── CLASSES ──
  getClasses(page = 1, pageSize = 10, category = '', search = ''): Observable<PagedResult<GymClass>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (category) params = params.set('category', category);
    if (search) params = params.set('search', search);
    return this.http.get<PagedResult<GymClass>>(`${this.base}/classes`, { params });
  }
  getClassCategories(): Observable<string[]> {
    return this.http.get<string[]>(`${this.base}/classes/categories`);
  }
  createClass(dto: any): Observable<GymClass> {
    return this.http.post<GymClass>(`${this.base}/classes`, dto);
  }
  getClassEnrollments(id: number): Observable<any[]> { return this.http.get<any[]>(`${this.base}/classes/${id}/enrollments`); }
  enrollMember(classId: number, memberId: number): Observable<any> { return this.http.post(`${this.base}/classes/${classId}/enroll`, { memberId }); }
  cancelEnrollment(id: number): Observable<any> { return this.http.post(`${this.base}/classes/enrollments/${id}/cancel`, {}); }
  // ── EQUIPMENT ──
  getEquipment(page = 1, pageSize = 10, status = '', category = '', search = ''): Observable<PagedResult<Equipment>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (status) params = params.set('status', status);
    if (category) params = params.set('category', category);
    if (search) params = params.set('search', search);
    return this.http.get<PagedResult<Equipment>>(`${this.base}/equipment`, { params });
  }
  createEquipment(dto: any): Observable<Equipment> {
    return this.http.post<Equipment>(`${this.base}/equipment`, dto);
  }
  updateEquipment(id: number, dto: any): Observable<void> {
    return this.http.put<void>(`${this.base}/equipment/${id}`, dto);
  }
  deleteEquipment(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/equipment/${id}`);
  }
  getMaintenanceDue(): Observable<Equipment[]> {
    return this.http.get<Equipment[]>(`${this.base}/equipment/maintenance-due`);
  }
  recordMaintenance(id: number, dto: { maintenanceDate: string; nextMaintenanceDate?: string; notes?: string }): Observable<void> {
    return this.http.post<void>(`${this.base}/equipment/${id}/maintenance`, dto);
  }

  // ── ACCESS ──
  getAccessLogs(page = 1, pageSize = 20): Observable<PagedResult<AccessLog>> {
    return this.http.get<PagedResult<AccessLog>>(`${this.base}/access?page=${page}&pageSize=${pageSize}`);
  }
  getTodayAccess(): Observable<AccessLog[]> {
    return this.http.get<AccessLog[]>(`${this.base}/access/today`);
  }
  getCurrentlyIn(): Observable<AccessLog[]> {
    return this.http.get<AccessLog[]>(`${this.base}/access/currently-in`);
  }
  checkIn(memberId: number): Observable<AccessLog> {
    return this.http.post<AccessLog>(`${this.base}/access/checkin`, { memberId });
  }
  checkOut(logId: number): Observable<AccessLog> {
    return this.http.post<AccessLog>(`${this.base}/access/${logId}/checkout`, {});
  }

  // ── INVOICES ──
  getInvoices(page = 1, pageSize = 10, status = ''): Observable<PagedResult<Invoice>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (status) params = params.set('status', status);
    return this.http.get<PagedResult<Invoice>>(`${this.base}/invoices`, { params });
  }
  markInvoicePaid(id: number, paymentMethod = 'Efectivo'): Observable<void> {
    return this.http.put<void>(`${this.base}/invoices/${id}/mark-paid`, {}, { params: { paymentMethod } });
  }
  createInvoice(dto: any): Observable<Invoice> { return this.http.post<Invoice>(`${this.base}/invoices`, dto); }
  refundInvoice(id: number): Observable<void> { return this.http.post<void>(`${this.base}/invoices/${id}/refund`, {}); }
  getInvoiceSummary(): Observable<any> {
    return this.http.get(`${this.base}/invoices/summary`);
  }
  attendanceReport(from: string, to: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.base}/dashboard/reports/attendance`, { params: { from, to } });
  }
}
