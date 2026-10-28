import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/services/api.service';
import { DashboardStats } from '../../core/models/models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="topbar">
      <div class="topbar-title">
        <h1>Dashboard</h1>
        <p>Resumen general del gimnasio</p>
      </div>
      <div class="topbar-actions">
        <span class="text-sm text-muted">{{ today | date:'EEEE, d MMMM yyyy':'':'es' }}</span>
      </div>
    </div>

    <div class="page-container">
      @if (loading()) {
        <div class="loading-spinner"><div class="spinner"></div></div>
      } @else if (stats()) {
        <!-- KPI Cards -->
        <div class="grid-stats mb-6">
          <div class="stat-card">
            <div class="stat-icon primary">👥</div>
            <div class="stat-info">
              <div class="stat-label">Miembros Activos</div>
              <div class="stat-value">{{ stats()!.activeMembers }}</div>
              <div class="stat-change positive">+{{ stats()!.newMembersThisMonth }} este mes</div>
            </div>
          </div>
          <div class="stat-card">
            <div class="stat-icon accent">💰</div>
            <div class="stat-info">
              <div class="stat-label">Ingresos del Mes</div>
              <div class="stat-value">\${{ stats()!.revenueThisMonth | number:'1.0-0' }}</div>
              <div [class]="'stat-change ' + (stats()!.revenueThisMonth >= stats()!.revenueLastMonth ? 'positive' : 'negative')">
                vs \${{ stats()!.revenueLastMonth | number:'1.0-0' }} mes anterior
              </div>
            </div>
          </div>
          <div class="stat-card">
            <div class="stat-icon secondary">🧘</div>
            <div class="stat-info">
              <div class="stat-label">Clases Hoy</div>
              <div class="stat-value">{{ stats()!.classesToday }}</div>
              <div class="stat-change">de {{ stats()!.totalClasses }} clases activas</div>
            </div>
          </div>
          <div class="stat-card">
            <div class="stat-icon warning">⚠️</div>
            <div class="stat-info">
              <div class="stat-label">Membresías por Vencer</div>
              <div class="stat-value">{{ stats()!.membershipsExpiringIn7Days }}</div>
              <div class="stat-change negative">{{ stats()!.membershipsExpiringIn3Days }} en 3 días</div>
            </div>
          </div>
          <div class="stat-card">
            <div class="stat-icon primary">💳</div>
            <div class="stat-info">
              <div class="stat-label">Membresías Activas</div>
              <div class="stat-value">{{ stats()!.activeMembershipsCount }}</div>
              <div class="stat-change">de {{ stats()!.totalMembers }} miembros totales</div>
            </div>
          </div>
        </div>

        <!-- Charts Row -->
        <div class="grid-2 mb-6">
          <!-- Monthly Revenue Chart -->
          <div class="card">
            <div class="card-header">
              <span class="card-title">Ingresos Mensuales</span>
            </div>
            <div class="card-body">
              <div class="simple-chart">
                @for (item of stats()!.monthlyRevenue; track item.month) {
                  <div class="chart-bar-wrapper">
                    <div class="chart-bar-label">\${{ item.revenue | number:'1.0-0' }}</div>
                    <div class="chart-bar-track">
                      <div class="chart-bar-fill"
                        [style.height.%]="getBarHeight(item.revenue, stats()!.monthlyRevenue)">
                      </div>
                    </div>
                    <div class="chart-bar-month">{{ item.month.substring(0,3) }}</div>
                  </div>
                }
              </div>
            </div>
          </div>

          <!-- Memberships by Plan -->
          <div class="card">
            <div class="card-header">
              <span class="card-title">Membresías por Plan</span>
            </div>
            <div class="card-body">
              @for (plan of stats()!.membershipsByPlan; track plan.planName) {
                <div class="plan-stat-row">
                  <div class="flex items-center gap-3">
                    <div class="plan-color-dot" [style.background]="plan.color"></div>
                    <span class="text-sm">{{ plan.planName }}</span>
                  </div>
                  <div class="flex items-center gap-3">
                    <div class="progress-bar" style="width:120px">
                      <div class="progress-fill"
                        [style.width.%]="(plan.count / stats()!.activeMembershipsCount) * 100"
                        [style.background]="plan.color">
                      </div>
                    </div>
                    <span class="text-sm font-semibold">{{ plan.count }}</span>
                  </div>
                </div>
              }
            </div>
          </div>
        </div>

        <!-- Bottom Row -->
        <div class="grid-2">
          <!-- Expiring Memberships Alert -->
          <div class="card">
            <div class="card-header">
              <span class="card-title">⚠️ Membresías por Vencer (7 días)</span>
              <a routerLink="/memberships" class="btn btn-sm btn-secondary">Ver todas</a>
            </div>
            <div class="table-container">
              @if (stats()!.expiringMemberships.length === 0) {
                <div class="empty-state">
                  <div class="empty-state-icon">✅</div>
                  <h3>Sin vencimientos próximos</h3>
                </div>
              } @else {
                <table class="data-table">
                  <thead>
                    <tr>
                      <th>Miembro</th>
                      <th>Plan</th>
                      <th>Vence</th>
                      <th>Días</th>
                      <th>WhatsApp</th>
                    </tr>
                  </thead>
                  <tbody>
                    @for (m of stats()!.expiringMemberships; track m.id) {
                      <tr>
                        <td>
                          <div class="member-info">
                            <div class="member-avatar">{{ m.memberName.charAt(0) }}</div>
                            <div>
                              <div class="member-name">{{ m.memberName }}</div>
                              <div class="member-email">{{ m.memberPhone }}</div>
                            </div>
                          </div>
                        </td>
                        <td>{{ m.planName }}</td>
                        <td>{{ m.endDate | date:'dd/MM/yy' }}</td>
                        <td>
                          <span [class]="'badge ' + (m.daysRemaining <= 3 ? 'badge-expired' : 'badge-pending')">
                            {{ m.daysRemaining }}d
                          </span>
                        </td>
                        <td>
                          @if (m.notificationSent) {
                            <span class="badge badge-active">✅ Enviado</span>
                          } @else {
                            <span class="badge badge-pending">⏳ Pendiente</span>
                          }
                        </td>
                      </tr>
                    }
                  </tbody>
                </table>
              }
            </div>
          </div>

          <!-- Recent Members -->
          <div class="card">
            <div class="card-header">
              <span class="card-title">👥 Miembros Recientes</span>
              <a routerLink="/members" class="btn btn-sm btn-secondary">Ver todos</a>
            </div>
            <div class="table-container">
              <table class="data-table">
                <thead>
                  <tr>
                    <th>Miembro</th>
                    <th>Estado</th>
                    <th>Membresía</th>
                  </tr>
                </thead>
                <tbody>
                  @for (m of stats()!.recentMembers; track m.id) {
                    <tr>
                      <td>
                        <div class="member-info">
                          <div class="member-avatar">{{ m.firstName.charAt(0) }}</div>
                          <div>
                            <div class="member-name">{{ m.firstName }} {{ m.lastName }}</div>
                            <div class="member-email">{{ m.email }}</div>
                          </div>
                        </div>
                      </td>
                      <td><span [class]="'badge badge-' + m.status.toLowerCase()">{{ m.status }}</span></td>
                      <td>{{ m.activeMembership?.planName ?? '—' }}</td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .simple-chart {
      display: flex;
      align-items: flex-end;
      gap: 0.75rem;
      height: 180px;
      padding-bottom: 1rem;
    }
    .chart-bar-wrapper {
      flex: 1;
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 0.25rem;
      height: 100%;
    }
    .chart-bar-label {
      font-size: 0.6rem;
      color: var(--text-muted);
      text-align: center;
      writing-mode: vertical-rl;
    }
    .chart-bar-track {
      flex: 1;
      width: 100%;
      background: rgba(255,255,255,0.05);
      border-radius: 4px 4px 0 0;
      display: flex;
      align-items: flex-end;
    }
    .chart-bar-fill {
      width: 100%;
      background: var(--gradient-primary);
      border-radius: 4px 4px 0 0;
      transition: height 0.8s ease;
      min-height: 4px;
    }
    .chart-bar-month {
      font-size: 0.65rem;
      color: var(--text-muted);
      font-weight: 600;
    }
    .plan-stat-row {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 0.625rem 0;
      border-bottom: 1px solid var(--bg-border);
    }
    .plan-stat-row:last-child { border-bottom: none; }
    .plan-color-dot { width: 10px; height: 10px; border-radius: 50%; flex-shrink: 0; }
  `]
})
export class DashboardComponent implements OnInit {
  stats = signal<DashboardStats | null>(null);
  loading = signal(true);
  today = new Date();

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.api.getDashboardStats().subscribe({
      next: (data) => { this.stats.set(data); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  getBarHeight(value: number, data: { revenue: number }[]): number {
    const max = Math.max(...data.map(d => d.revenue));
    return max === 0 ? 0 : (value / max) * 100;
  }
}
