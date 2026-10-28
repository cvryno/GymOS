import { Component, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

interface NavItem {
  icon: string;
  label: string;
  route: string;
  adminOnly?: boolean;
}

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [CommonModule, RouterModule, RouterLink, RouterLinkActive],
  template: `
    <div class="app-layout">
      <!-- Sidebar -->
      <aside class="sidebar">
        <div class="sidebar-logo">
          <div class="sidebar-logo-icon">🏋️</div>
          <div class="sidebar-logo-text">
            <h2>GymOS</h2>
            <span>Sistema de Gimnasio</span>
          </div>
        </div>

        <nav class="sidebar-nav">
          <span class="nav-section-label">Principal</span>
          @for (item of mainNav; track item.route) {
            @if (!item.adminOnly || auth.isAdmin()) {
              <a [routerLink]="item.route" routerLinkActive="active"
                class="nav-item">
                <span class="nav-item-icon">{{ item.icon }}</span>
                <span>{{ item.label }}</span>
              </a>
            }
          }

          <span class="nav-section-label" style="margin-top:1rem">Gestión</span>
          @for (item of mgmtNav; track item.route) {
            <a [routerLink]="item.route" routerLinkActive="active" class="nav-item">
              <span class="nav-item-icon">{{ item.icon }}</span>
              <span>{{ item.label }}</span>
            </a>
          }
        </nav>

        <div class="sidebar-footer">
          <div class="user-info" (click)="logout()">
            <div class="user-avatar">
              {{ userInitials() }}
            </div>
            <div class="user-details">
              <div class="user-name">{{ auth.user()?.fullName }}</div>
              <div class="user-role">{{ auth.user()?.role }} · Cerrar sesión</div>
            </div>
          </div>
        </div>
      </aside>

      <!-- Main Content -->
      <main class="main-content">
        <router-outlet />
      </main>
    </div>
  `
})
export class LayoutComponent {
  mainNav: NavItem[] = [
    { icon: '📊', label: 'Dashboard', route: '/dashboard' },
    { icon: '👥', label: 'Miembros', route: '/members' },
    { icon: '💳', label: 'Membresías', route: '/memberships' },
    { icon: '✅', label: 'Control de Acceso', route: '/access' },
  ];

  mgmtNav: NavItem[] = [
    { icon: '🧘', label: 'Clases', route: '/classes' },
    { icon: '🏋️', label: 'Instructores', route: '/instructors' },
    { icon: '🔧', label: 'Equipos', route: '/equipment' },
    { icon: '💰', label: 'Facturación', route: '/invoices' },
  ];

  userInitials = computed(() => {
    const name = this.auth.user()?.fullName ?? 'U';
    return name.split(' ').map(n => n[0]).join('').substring(0, 2).toUpperCase();
  });

  constructor(public auth: AuthService) {}

  logout(): void {
    if (confirm('¿Estás seguro que deseas cerrar sesión?')) {
      this.auth.logout();
    }
  }
}
